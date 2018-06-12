using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using UnityEngine.Experimental.AI;
using UnityEngine;


public class UnitPathFollowSystem : JobComponentSystem {


    [ComputeJobOptimization]
    private struct CopyAgents : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<Position> positions;
        public NativeArray<int> agentIndices;
        public NativeArray<float2> agents;

        public void Execute(int index)
        {
            var position = positions[index].Value;
            agents[index] = position.xz;
            agentIndices[index] = index;
        }
    }

    [ComputeJobOptimization]
    private struct FollowFormation : IJobParallelFor
    {
        [ReadOnly] public NavMeshQuery query;
        [ReadOnly] public ComponentDataFromEntity<FormationData> formationDatas;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public ComponentDataFromEntity<Position> formationPositions;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public ComponentDataFromEntity<Heading> formationHeadings;
        [ReadOnly] public ComponentDataArray<InFormationData> inFormations;

        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<UnitAgentData> navAgents;
        //@TODO: Remove this
        [ReadOnly] public ComponentDataArray<UnitAgentTypeData> types;

        public float dt;

        public NativeArray<float2> desireVelocitys;
        public NativeArray<NavMeshLocation> prevLocations;

        public void Execute(int index)
        {
            var inFormation = inFormations[index];
            var formation = inFormation.formationEntity;
            var formationData = formationDatas[formation];
            var agent = navAgents[index];
            var type = types[index];
            var align = formationHeadings[formation];
            var targetPos = formationData.GetUnitAlignTarget(inFormation.index, formationPositions[formation], align, type.formationWidth, type.radius);
            var position = positions[index];
            position.Value.y -= 1;

            var distance = math.distance(targetPos, position.Value);

            var vector = targetPos - position.Value;
            var targetDir = math_experimental.normalizeSafe(vector);

            var velocity = math.select(targetDir * type.maxSpeed, vector / dt, distance < type.maxSpeed * dt);
            desireVelocitys[index] = velocity.xz;
            prevLocations[index] = agent.location;
        }
    }

    [ComputeJobOptimization]
    private struct BuildKdTree : IJob
    {
        public NativeArray<KdTreeUtility.TreeNode> tree;
        public NativeArray<float2> agents;
        public NativeArray<int> agentIndices;
        public NativeArray<int> agentIndicesInverse;


        public void Execute()
        {
            KdTreeUtility.BuildTree(tree, agents, agentIndices);
            for (var i = 0; i < agentIndices.Length; ++i)
                agentIndicesInverse[agentIndices[i]] = i;
        }
    }



    [ComputeJobOptimization]
    private struct RVO : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<KdTreeUtility.TreeNode> tree;

        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<float2> agents;
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<int> agentIndices;
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<int> agentIndicesInverse;

        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> neighberBuffer;
        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> distanceBuffer;
        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<RVOUtility.Line> lineBuffer;
        
        [ReadOnly] public ComponentDataArray<UnitAgentTypeData> types;
        [ReadOnly] public ComponentDataArray<UnitAgentData> navAgents;
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<float2> desireVelocity;
        public NativeArray<float2> newVelocitys;

        public float dt;

        static float sqr(float f) { return f * f; }

        static float det(float2 vector1, float2 vector2)
        {
            return vector1.x * vector2.y - vector1.y * vector2.x;
        }

        public void Execute(int index)
        {
            var neighbers = new NativeSlice<int>(neighberBuffer, index * MAX_NEIGHBORS, MAX_NEIGHBORS);
            var distances = new NativeSlice<float>(distanceBuffer, index * MAX_NEIGHBORS, MAX_NEIGHBORS);
            var lines = new NativeSlice<RVOUtility.Line>(lineBuffer, index * 2 * MAX_NEIGHBORS, MAX_NEIGHBORS);
            var projLines = new NativeSlice<RVOUtility.Line>(lineBuffer, (index * 2 + 1) * MAX_NEIGHBORS, MAX_NEIGHBORS);
            int planSize = 0;
            int neighborSize = 0;
            var type = types[index];
            var velocity = navAgents[index].velocity;
            KdTreeUtility.QueryNeighbors(tree, agents, agentIndicesInverse[index], type.neighborDist * type.neighborDist, neighbers, distances, ref neighborSize);
            float invTimeHorizon = 1.0f / type.timeHorizon;

            /* Create agent ORCA planes. */
            for (var i = 0; i < neighborSize; ++i)
            {
                int other = agentIndices[neighbers[i]];

                float2 relativePosition = agents[neighbers[i]] - agents[agentIndicesInverse[index]];
                float2 relativeVelocity = velocity - navAgents[other].velocity;
                float distSq = math.lengthSquared(relativePosition);
                float combinedRadius = type.radius + types[other].radius;
                float combinedRadiusSq = sqr(combinedRadius);

                RVOUtility.Line line;// = new Plane();
                float2 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    float2 w = relativeVelocity - invTimeHorizon * relativePosition;
                    /* Vector from cutoff center to relative velocity. */
                    float wLengthSq = math.lengthSquared(w);

                    float dotProduct = math.dot(w, relativePosition);

                    if (dotProduct < 0.0f && sqr(dotProduct) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = math.sqrt(wLengthSq);
                        float2 unitW = w / wLength;

                        line.direction = new float2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        float leg = math.sqrt(distSq - combinedRadiusSq);

                        if (det(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.direction = new float2(relativePosition.x * leg - relativePosition.y * combinedRadius, 
                                relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.direction = -new float2(relativePosition.x * leg + relativePosition.y * combinedRadius,
                                -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        float dotProduct2 = math.dot(relativeVelocity, line.direction);

                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    float invTimeStep = 1f / dt;

                    /* Vector from cutoff center to relative velocity. */
                    float2 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = math.length(w);
                    float2 unitW = w / wLength;

                    line.direction = new float2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = velocity + 0.5f * u;
                lines[i] = line;
            }
            float2 newVelocity;
            float2 optVelocity = desireVelocity[index];
            int lineFail = RVOUtility.linearProgram2(lines, neighborSize, type.maxSpeed, ref optVelocity, false, out newVelocity);
            

            if (lineFail < neighborSize)
            {
                RVOUtility.linearProgram3(lines, neighborSize, projLines, lineFail, type.maxSpeed, ref newVelocity);
            }
            newVelocitys[index] = newVelocity;
        }
    }

    [ComputeJobOptimization]
    private struct ApplyVelocity : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<float2> newVelocitys;
        [ReadOnly] public ComponentDataArray<Position> positions;
        public NativeArray<Vector3> steerTargets;
        public float dt;

        public void Execute(int index)
        {
            var v = newVelocitys[index] * dt;
            steerTargets[index] = positions[index].Value + new float3(v.x, 0, v.y);
        }
    }

    [ComputeJobOptimization]
    private struct BatchedMove : IJobParallelForBatch
    {
        [ReadOnly]
        public NavMeshQuery query;

        public NativeArray<Vector3> steerTargets;
        public NativeArray<NavMeshLocation> prevLocations;

        public void Execute(int startIndex, int count)
        {
            var locationSlice = new NativeSlice<NavMeshLocation>(prevLocations, startIndex, count);
            var targetSlice = new NativeSlice<Vector3>(steerTargets, startIndex, count);
            query.MoveLocationsInSameAreas(locationSlice, targetSlice);
        }
    }

    [ComputeJobOptimization]
    private struct SyncTransform : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<Vector3> steerTargets;
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<NavMeshLocation> newLocations;

        public ComponentDataArray<Position> positions;
        public ComponentDataArray<UnitAgentData> navAgents;
        public ComponentDataArray<Heading> headings;
        [ReadOnly] public ComponentDataArray<UnitAgentTypeData> types;

        public float dt;

        public void Execute(int index)
        {
            var position = positions[index];
            var agent = navAgents[index];
            Heading heading = headings[index];
            Position newPosition;
            newPosition.Value = newLocations[index].position;
            newPosition.Value.y += 1f;
            var offset = newPosition.Value - position.Value;
            agent.location = newLocations[index];
            agent.velocity = offset.xz / dt;
            heading.Value = math_experimental.normalizeSafe(offset, heading.Value);

            positions[index] = newPosition;
            headings[index] = heading;
            navAgents[index] = agent;
        }
    }


    public struct Units
    {
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<UnitAgentData> agents;
        [ReadOnly]
        public ComponentDataArray<InFormationData> inFormations;
        [ReadOnly]
        public ComponentDataArray<UnitAgentTypeData> types;

        public int Length;
    }

    [Inject] Units units;

    [Inject] ComponentDataFromEntity<FormationData> formationDatas;
    [Inject] ComponentDataFromEntity<Position> formationPositions;
    [Inject] ComponentDataFromEntity<Heading> formationHeadings;
    NavMeshQuery query;

    const int MAX_NEIGHBORS = 8;

    protected override void OnCreateManager(int capacity)
    {
        var world = NavMeshWorld.GetDefaultWorld();
        query = new NavMeshQuery(world, Allocator.Persistent);
    }

    protected override void OnDestroyManager()
    {
        query.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var positionBuffer = new NativeArray<Vector3>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var locationBuffer = new NativeArray<NavMeshLocation>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var agentBuffer = new NativeArray<float2>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var tree = new NativeArray<KdTreeUtility.TreeNode>(units.Length * 2 - 1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var agentIndices = new NativeArray<int>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var agentIndicesInverse = new NativeArray<int>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var desireVelocitys = new NativeArray<float2>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var distanceBuffer = new NativeArray<float>(units.Length * MAX_NEIGHBORS, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var neighborBuffer = new NativeArray<int>(units.Length * MAX_NEIGHBORS, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var lineBuffer = new NativeArray<RVOUtility.Line>(units.Length * MAX_NEIGHBORS * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var newVelocitys = new NativeArray<float2>(units.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var initJob = new CopyAgents
        {
            agents = agentBuffer,
            positions = units.positions,
            agentIndices = agentIndices
        };

        var buildJob = new BuildKdTree
        {
            tree = tree,
            agents = agentBuffer,
            agentIndices = agentIndices,
            agentIndicesInverse = agentIndicesInverse
        };


        var followJob = new FollowFormation
        {
            navAgents = units.agents,
            formationDatas = formationDatas,
            formationPositions = formationPositions,
            formationHeadings = formationHeadings,
            inFormations = units.inFormations,
            query = query,
            positions = units.positions,
            prevLocations = locationBuffer,
            types = units.types,
            desireVelocitys = desireVelocitys,
            dt = Time.deltaTime
        };


        var rvoJob = new RVO
        {
            agentIndices = agentIndices,
            agentIndicesInverse = agentIndicesInverse,
            agents = agentBuffer,
            desireVelocity = desireVelocitys,
            distanceBuffer = distanceBuffer,
            neighberBuffer = neighborBuffer,
            lineBuffer = lineBuffer,
            dt = Time.deltaTime,
            newVelocitys = newVelocitys,
            tree = tree,
            types = units.types,
            navAgents = units.agents
        };

       
        var applyJob = new ApplyVelocity
        {
            newVelocitys = newVelocitys,
            positions = units.positions,
            steerTargets = positionBuffer,
            dt = Time.deltaTime
        };

        var moveJob = new BatchedMove
        {
            prevLocations = locationBuffer,
            steerTargets = positionBuffer,
            query = query,
        };

        var syncJob = new SyncTransform
        {
            newLocations = locationBuffer,
            steerTargets = positionBuffer,
            navAgents = units.agents,
            positions = units.positions,
            headings = units.headings,
            types = units.types,
            dt = Time.deltaTime
        };

        //初始化rvo需要的数据
        var fence = initJob.Schedule(units.Length, SimulationState.TinyBatchSize, inputDeps);
        //构建KdTree
        var buildFence = buildJob.Schedule(fence);
        //确定期望速度
        var followFence = followJob.Schedule(units.Length, SimulationState.TinyBatchSize, inputDeps);
        //RVO算规避,确定实际速度
        fence = rvoJob.Schedule(units.Length, SimulationState.TinyBatchSize, JobHandle.CombineDependencies(buildFence, followFence));
        //根据速度移动
        fence = applyJob.Schedule(units.Length, SimulationState.TinyBatchSize, fence);
        //将移动的位置限制在寻路网格上
        fence = moveJob.ScheduleBatch(units.Length, SimulationState.TinyBatchSize, fence);
        //同步在寻路网格上的位置
        fence = syncJob.Schedule(units.Length, SimulationState.TinyBatchSize, fence);

        return fence;
    }
}
