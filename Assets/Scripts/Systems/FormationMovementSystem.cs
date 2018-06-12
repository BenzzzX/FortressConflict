using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using UnityEngine.Experimental.AI;
using UnityEngine;

public class FormationMovementSystem : JobComponentSystem
{
    public struct Formations
    {
        public ComponentDataArray<FormationData> formationDatas;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<FormationAgentData> agents;
        [ReadOnly]
        public ComponentDataArray<FormationTypeData> formationTypes;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;

        public int Length;
    }

    [Inject]
    Formations formations;

    [ComputeJobOptimization]
    struct FollowPath : IJobParallelFor
    {
        [ReadOnly]
        public NavMeshQuery query;
        public ComponentDataArray<FormationData> formationDatas;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<FormationAgentData> agents;
        [ReadOnly]
        public ComponentDataArray<FormationTypeData> formationTypes;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;
        public float dt;

        public void Execute(int index)
        {
            var formationData = formationDatas[index];
            var agent = agents[index];
            if ((formationData.state & FormationState.Attacking) == 0 && agent.state == AgentState.Moving)
            {
                var position = positions[index];
                var heading = headings[index];
                var type = formationTypes[index];

                var targetPos = (float3)agent.steerTarget.location.position;
                var distance = math.distance(targetPos, position.Value);
                if (distance < math_experimental.epsilon)
                {
                    if(agent.steerTarget.flag == StraightPathFlags.End)
                    {
                        agent.state |= AgentState.Reached;
                        agents[index] = agent;
                        return;
                    }
                    var path = paths[index];
                    agent.pathId += 1;
                    agent.fromPoint = agent.steerTarget;
                    agent.steerTarget = path[agent.pathId];
                    targetPos = agent.steerTarget.location.position;
                    distance = math.distance(targetPos, position.Value);
                }

                var targetDir = math_experimental.normalizeSafe(targetPos - position.Value);
                var dir = math_experimental.normalizeSafe(heading.Value);
                var cos = math.dot(targetDir, dir);

                if (cos < 1f - math_experimental.epsilon)
                {
                    heading.Value = Vector3.RotateTowards(dir, targetDir, dt * math.radians(type.rotateSpeed), 10f);
                }

                var speedScale = math.select(1f, 0.5f, (formationData.state & FormationState.Spawning) != 0);


                var shiftTarget = targetDir * type.speed * speedScale * dt * cos * cos + position.Value;
                
                var nextTarget = math.select(shiftTarget, targetPos, distance < type.speed * speedScale * dt);

                agent.location = query.MoveLocation(agent.location, nextTarget);
                position.Value = agent.location.position;

                var alpha = distance / math.distance(agent.steerTarget.location.position, agent.fromPoint.location.position);
                formationData.sideOffset = math.lerp(agent.steerTarget.vertexSide, agent.fromPoint.vertexSide,
                     alpha * alpha);

                positions[index] = position;
                agents[index] = agent;
                headings[index] = heading;
                formationDatas[index] = formationData;
            }
        }
    }

    NavMeshQuery query;

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
        var moveJob = new FollowPath
        {
            formationDatas = formations.formationDatas,
            headings = formations.headings,
            paths = formations.paths,
            positions = formations.positions,
            formationTypes = formations.formationTypes,
            agents = formations.agents,
            query = query,
            dt = Time.deltaTime
           
        };

        return moveJob.Schedule(formations.Length, SimulationState.SmallBatchSize, inputDeps);
    }
}