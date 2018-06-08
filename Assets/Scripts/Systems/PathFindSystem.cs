﻿using Unity.Entities;
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
        public int Length;
    }

    public struct RequestBatch
    {
        public NavMeshQuery query;
        public NativeArray<int> entities;
        public NativeArray<PathRequestData> requests;
        public NativeArray<int> pathStart;
        public NativeArray<PathPoint> pathBuffer;

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
            entities = new NativeArray<int>(MAX_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            requests = new NativeArray<PathRequestData>(MAX_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            pathBuffer = new NativeArray<PathPoint>(MAX_COUNT * MAX_PATHSIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            pathStart = new NativeArray<int>(MAX_COUNT, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            state = new NativeArray<State>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

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
        }

        public void CopyResult(ref FixedArrayArray<PathPoint> paths, ref ComponentDataArray<PathRequestData> pathRequests)
        {
            var s = state[0];
            for(int i = 0;i < s.entitySize; ++i)
            {
                var entity = entities[i];
                var request = requests[i];
                var originRequest = pathRequests[entity];
                if (originRequest.status > PathRequestStatus.InProgress) continue; //放弃寻路, 丢弃结果
                if (math.distance(originRequest.start, request.start) > 0.001f || math.distance(originRequest.end, request.end) > 0.001f) continue; //新的请求, 丢弃结果
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

        public void Update(ref NativeArray<PolygonId> resultBuffer, ref NativeArray<StraightPathFlags> pathFlagBuffer, ref NativeArray<float> vertexSideBuffer, ref NativeArray<NavMeshLocation> straitPathBuffer, int maxIter = 100)
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
                        var maxPathSize = math.min(pathSize, MAX_PATHSIZE);
                        PathUtils.FindStraightPath(query, request.start, request.end, resultBuffer, pathSize
                            , ref straitPathBuffer, ref pathFlagBuffer, ref vertexSideBuffer, ref pathSize, maxPathSize);
                        for (var i = 0; i < pathSize; i++)
                        {
                            pathBuffer[offset + i] = new PathPoint
                            {
                                location = straitPathBuffer[i],
                                vertexSide = vertexSideBuffer[i],
                                flag = pathFlagBuffer[i]
                            };
                        }
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

    [Inject]
    Requesters requesters;

    RequestBatch[] batches;

    NativeQueue<int> waitingEntities;
    
    NativeCounter counter;

    public const int MAX_QUERIES = 10;

    [ComputeJobOptimization]
    private struct FetchRequest : IJobParallelFor
    {
        public NativeQueue<int>.Concurrent waitingEntities;
        [ReadOnly]
        public ComponentDataArray<PathRequestData> requests;
        public NativeCounter.Concurrent counter;

        public void Execute(int index)
        {
            var request = requests[index];
            if(request.status == PathRequestStatus.NewRequest)
            {
                waitingEntities.Enqueue(index);
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
        public NativeQueue<int> waitingEntities;
        public ComponentDataArray<PathRequestData> requests;

        public void Execute()
        {
            var count = counter.Count;
            var averageCount = math.min((count / (MAX_QUERIES - ID)) + 1, RequestBatch.MAX_COUNT);
            var s = batch.state[0];
            var perfectSplit = averageCount - s.entitySize;
            
            for (var i = 0; i < perfectSplit; ++i)
            {
                if (s.entitySize >= RequestBatch.MAX_COUNT - 1) break;
                int entity;
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

        [DeallocateOnJobCompletion]
        public NativeArray<PolygonId> resultBuffer;
        [DeallocateOnJobCompletion]
        public NativeArray<NavMeshLocation> pathBuffer;
        [DeallocateOnJobCompletion]
        public NativeArray<StraightPathFlags> pathFlagBuffer;
        [DeallocateOnJobCompletion]
        public NativeArray<float> vertexSideBuffer;

        public void Execute()
        {
            batch.Update(ref resultBuffer,ref pathFlagBuffer,ref vertexSideBuffer,ref pathBuffer);
        }
    }

    [ComputeJobOptimization]
    private struct GetResult : IJob
    {
        public RequestBatch batch;
        public ComponentDataArray<PathRequestData> requests;
        public FixedArrayArray<PathPoint> paths;

        public void Execute()
        {
            batch.CopyResult(ref paths, ref requests);
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

    protected override void OnStartRunning()
    {
        batches = new RequestBatch[MAX_QUERIES];
        for (var i = 0; i < MAX_QUERIES; ++i)
            batches[i] = new RequestBatch(2000);
        waitingEntities = new NativeQueue<int>(Allocator.Persistent);
        counter = new NativeCounter(Allocator.Persistent);
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
        var count = 0;
        for (var j = 0; j < MAX_QUERIES; ++j)
        {
            var batch = batches[j];
            count += batch.state[0].entitySize;
        }

        counter.Count = count;

        var fetchJob = new FetchRequest
        {
            waitingEntities = waitingEntities,
            requests = requesters.requestDatas,
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
                requests = requesters.requestDatas,
                counter = counter,
                ID = i
            };
            fillFence = fillJob.Schedule(fillFence);
        }
        var fences = new NativeArray<JobHandle>(MAX_QUERIES, Allocator.Temp);
        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var processJob = new ProcessRequest
            {
                batch = batches[i],
                resultBuffer = new NativeArray<PolygonId>(SimulationState.MaxPathSize * 5, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                pathBuffer = new NativeArray<NavMeshLocation>(SimulationState.MaxPathSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                pathFlagBuffer = new NativeArray<StraightPathFlags>(SimulationState.MaxPathSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                vertexSideBuffer = new NativeArray<float>(SimulationState.MaxPathSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
            };
            fences[i] = processJob.Schedule(fillFence);
        }
        var processFence = JobHandle.CombineDependencies(fences);
        var getFence = processFence;

        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var getJob = new GetResult
            {
                batch = batches[i],
                paths = requesters.paths,
                requests = requesters.requestDatas
            };
            getFence = getJob.Schedule(getFence);
        }

        for (var i = 0; i < MAX_QUERIES; ++i)
        {
            var clearJob = new ClearResult
            {
                batch = batches[i]
            };
            fences[i] = clearJob.Schedule(getFence);
        }
        var clearFence = JobHandle.CombineDependencies(fences);

        fences.Dispose();

        return clearFence;
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