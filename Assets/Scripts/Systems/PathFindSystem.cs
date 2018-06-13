using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.AI;

[UpdateAfter(typeof(ControllSystem))]
public class PathFindSystem : JobComponentSystem
{
    public struct Requesters
    {
        public ComponentDataArray<PathRequestData> requestDatas;
        public FixedArrayArray<PathPoint> paths;
        public EntityArray entities;
        public int Length;
    }

    public struct RequestBatch
    {
        public NavMeshQuery query;
        public NativeArray<Entity> entities;
        public NativeArray<PathRequestData> requests;
        public NativeArray<int> pathStart;
        public NativeArray<PathPoint> pathBuffer;
        NativeArray<PolygonId> resultBuffer;

        public struct State
        {
            public int pathSize;
            public int entitySize;
            public int entityInUse;

            public const int NONE = -1;
        }
        public NativeArray<State> state;

        public const int MAX_COUNT = 10;
        public const int MAX_PATHSIZE = SimulationState.MaxPathSize;
        public RequestBatch(int nodePoolSize = 2000)
        {
            var world = NavMeshWorld.GetDefaultWorld();
            query = new NavMeshQuery(world, Allocator.Persistent, nodePoolSize);
            entities = new NativeArray<Entity>(MAX_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            requests = new NativeArray<PathRequestData>(MAX_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            pathBuffer = new NativeArray<PathPoint>(MAX_COUNT * MAX_PATHSIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            pathStart = new NativeArray<int>(MAX_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            state = new NativeArray<State>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            resultBuffer = new NativeArray<PolygonId>(MAX_COUNT * 5, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var s = new State();
            s.entityInUse = State.NONE;
            s.entitySize = 0;
            s.pathSize = 0;
            state[0] = s;
        }

        public void Dispose()
        {
            query.Dispose();
            entities.Dispose();
            requests.Dispose();
            pathBuffer.Dispose();
            pathStart.Dispose();
            state.Dispose();
            resultBuffer.Dispose();
        }

        public int CopyResult(ref FixedArrayFromEntity<PathPoint> paths, ref ComponentDataFromEntity<PathRequestData> pathRequests)
        {
            var s = state[0];
            var count = 0;
            for(int i = 0;i < s.entitySize; ++i)
            {
                var entity = entities[i];
                var request = requests[i];
                if(!pathRequests.Exists(entity)) //Entity 已被删除, 丢弃结果
                {
                    continue;
                }
                var originRequest = pathRequests[entity];
                if (request.status >= PathRequestStatus.Done)
                    count++;
                if (originRequest.status > PathRequestStatus.InProgress) //放弃寻路, 丢弃结果
                {
                    continue;
                }
                if (math.distance(originRequest.start, request.start) > 0.001f || math.distance(originRequest.end, request.end) > 0.001f)
                {
                    continue; //新的请求, 丢弃结果
                }

                pathRequests[entity] = request;
                if(request.status == PathRequestStatus.Done)
                {
                    var offset = pathStart[i];
                    var pathSize = request.pathSize;
                    var path = paths[entity];
                    for (var j = 0; j<pathSize; ++j)
                    {
                        path[j] = pathBuffer[offset + j];
                    }
                }
            }
            return count;
        }

        public void ClearResult()
        {
            var s = state[0];
            s.pathSize = 0;
            for (int i = 0; i < s.entitySize;)
            {
                var request = requests[i];
                if (request.status > PathRequestStatus.InProgress)
                {
                    if (s.entityInUse == s.entitySize - 1)
                        s.entityInUse = i;
                    entities[i] = entities[s.entitySize - 1];
                    requests[i] = requests[s.entitySize - 1];
                    s.entitySize -= 1;
                    continue;
                }
                ++i;
            }
            state[0] = s;
        }

        public void Update(int maxIter = 100)
        {
            var s = state[0];
            PathQueryStatus status = PathQueryStatus.Success;
            if (s.entitySize == 0) return;
            while (maxIter > 0)
            {
                if (s.entityInUse == State.NONE)
                {
                    PathRequestData request = requests[0];
                    for (int i = s.entitySize - 1; i >= 0; --i)
                    {
                        request = requests[i];
                        if (request.status < PathRequestStatus.Done)
                        {
                            s.entityInUse = i;
                            break;
                        }
                    }
                    if (s.entityInUse == State.NONE) break;
                    var startLoc = query.MapLocation(request.start, Vector3.one * 10f, request.agentType, request.mask);
                    var endLoc = query.MapLocation(request.end, Vector3.one * 10f, request.agentType, request.mask);
                    status = query.BeginFindPath(startLoc, endLoc, request.mask);
                    if (status.IsFailure()) break;
                    request.status = PathRequestStatus.InProgress;
                    requests[s.entityInUse] = request;
                }
                int nIter;
                status = query.UpdateFindPath(maxIter, out nIter);
                if (status.IsFailure()) break;
                maxIter -= nIter;
                if (status.IsSuccess())
                {
                    int pathSize;
                    var request = requests[s.entityInUse];
                    status = query.EndFindPath(out pathSize);
                    if (status.IsFailure()) break;
                    if (status.IsSuccess())
                    {
                        query.GetPathResult(resultBuffer);
                        var offset = s.pathSize;
                        var pathSlice = new NativeSlice<PathPoint>(pathBuffer, offset);
                        status = PathUtils.FindStraightPath(query, request.start, request.end, resultBuffer, pathSize
                            , pathSlice, ref pathSize, MAX_PATHSIZE);
                        if (status.IsFailure()) break;
                        pathStart[s.entityInUse] = offset;
                        request.pathSize = pathSize;
                        s.pathSize += pathSize;
                    }
                    request.status = PathRequestStatus.Done;
                    requests[s.entityInUse] = request;
                    s.entityInUse = State.NONE;
                }
                state[0] = s;
            }

            if (status.IsFailure())
            {
                var request = requests[s.entityInUse];
                request.status = PathRequestStatus.Failure;
                requests[s.entityInUse] = request;
                s.entityInUse = State.NONE;
                state[0] = s;
            }
        }
    }


    [ComputeJobOptimization]
    private struct FetchRequest : IJobParallelFor
    {
        public NativeQueue<Entity>.Concurrent waitingEntities;
        [ReadOnly]
        public ComponentDataArray<PathRequestData> requests;
        [ReadOnly]
        public EntityArray entities;
        public NativeCounter.Concurrent counter;

        public void Execute(int index)
        {
            var request = requests[index];
            if(request.status == PathRequestStatus.NewRequest)
            {
                waitingEntities.Enqueue(entities[index]);
                counter.Increment();
            }
        }
    }

    [ComputeJobOptimization]
    private struct FillQuery : IJob
    {
        public RequestBatch batch;
        public NativeCounter counter;
        public int ID;
        public NativeQueue<Entity> waitingEntities;
        public ComponentDataFromEntity<PathRequestData> requests;

        public void Execute()
        {
            var count = counter.Count;
            var averageCount = math.min((count / (MAX_QUERIES - ID)) + 1, RequestBatch.MAX_COUNT);
            var s = batch.state[0];
            var perfectSplit = averageCount - s.entitySize;
            
            for (var i = 0; i < perfectSplit; ++i)
            {
                if (s.entitySize >= RequestBatch.MAX_COUNT - 1) break;
                Entity entity;
                if(!waitingEntities.TryDequeue(out entity)) break;
                batch.entities[s.entitySize] = entity;
                var request = requests[entity];
                request.status = PathRequestStatus.InQueue;
                requests[entity] = request;
                batch.requests[s.entitySize] = request;
                count -= 1;
                s.entitySize += 1;
            }
            batch.state[0] = s;
            counter.Count = count;
        }
    }

    [ComputeJobOptimization]
    private struct ProcessRequest : IJob
    {
        public RequestBatch batch;

        public void Execute()
        {
            batch.Update();
        }
    }

    [ComputeJobOptimization]
    private struct GetResult : IJob
    {
        public RequestBatch batch;
        public ComponentDataFromEntity<PathRequestData> requests;
        public FixedArrayFromEntity<PathPoint> paths;
        public NativeCounter counter;

        public void Execute()
        {
            counter.Count -= batch.CopyResult(ref paths, ref requests);
        }
    }

    [ComputeJobOptimization]
    private struct ClearResult : IJob
    {
        public RequestBatch batch;

        public void Execute()
        {
            batch.ClearResult();
        }
    }


    [Inject]
    Requesters requesters;

    [Inject]
    ComponentDataFromEntity<PathRequestData> requestDatas;

    [Inject]
    FixedArrayFromEntity<PathPoint> paths;

    RequestBatch[] batches;

    NativeQueue<Entity> waitingEntities;

    NativeCounter counter;

    JobHandle endFence;

    public const int MAX_QUERIES = 10;

    protected override void OnStartRunning()
    {
        batches = new RequestBatch[MAX_QUERIES];
        for (var i = 0; i < MAX_QUERIES; ++i)
            batches[i] = new RequestBatch(2000);
        waitingEntities = new NativeQueue<Entity>(Allocator.Persistent);
        counter = new NativeCounter(Allocator.Persistent);

        endFence = new JobHandle();
    }

    protected override void OnStopRunning()
    {
        for (var i = 0; i < MAX_QUERIES; ++i)
            batches[i].Dispose();
        waitingEntities.Dispose();
        counter.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        waitingEntities.Clear();
        var fetchJob = new FetchRequest
        {
            waitingEntities = waitingEntities,
            requests = requesters.requestDatas,
            entities = requesters.entities,
            counter = counter
        };
        var fetchFence = fetchJob.Schedule(requesters.Length, SimulationState.TinyBatchSize, inputDeps);

        var fillFence = fetchFence;
        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var fillJob = new FillQuery
            {
                batch = batches[i],
                waitingEntities = waitingEntities,
                requests = requestDatas,
                counter = counter,
                ID = i
            };
            fillFence = fillJob.Schedule(fillFence);
        }
        var fences = new NativeArray<JobHandle>(MAX_QUERIES, Allocator.Temp);
        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var processJob = new ProcessRequest { batch = batches[i] };
            fences[i] = processJob.Schedule(fillFence);
        }
        var processFence = JobHandle.CombineDependencies(fences);
        var getFence = processFence;

        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var getJob = new GetResult
            {
                batch = batches[i],
                paths = paths,
                requests = requestDatas,
                counter = counter
            };
            getFence = getJob.Schedule(getFence);
        }

        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var clearResultJob = new ClearResult
            {
                batch = batches[i]
            };
            fences[i] = clearResultJob.Schedule(getFence);
        }
        endFence = JobHandle.CombineDependencies(fences);

        fences.Dispose();

        return endFence;
    }
}

public static class StatusUtils
{
    public static bool IsSuccess(this PathQueryStatus status)
    {
        return (status & PathQueryStatus.Success) != 0;
    }

    public static bool IsFailure(this PathQueryStatus status)
    {
        return (status & PathQueryStatus.Failure) != 0;
    }

    public static bool IsInProgress(this PathQueryStatus status)
    {
        return (status & PathQueryStatus.InProgress) != 0;
    }
}