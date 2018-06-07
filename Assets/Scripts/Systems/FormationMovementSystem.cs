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
        public ComponentDataArray<CrowdAgentData> agents;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;

        public int Length;
    }

    [Inject]
    Formations formations;

    struct FollowPath : IJobParallelFor
    {
        [ReadOnly]
        public NavMeshQuery query;
        public ComponentDataArray<FormationData> formationDatas;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<CrowdAgentData> agents;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;
        float dt;

        public void Execute(int index)
        {
            var formationData = formationDatas[index];
            var agent = agents[index];
            if (!formationData.isAttacking && agent.state == CrowdState.moving)
            {
                var position = positions[index];
                var heading = headings[index];

                var targetPos = agent.steerTarget.position;
                var distance = math.distance(targetPos, position.Value);
                if (distance < math_experimental.epsilon)
                {
                    if(agent.steerTarget.flag == StraightPathFlags.End)
                    {
                        agent.state |= CrowdState.reached;
                        return;
                    }
                    var path = paths[index];
                    agent.pathId += 1;
                    agent.fromPoint = agent.steerTarget;
                    agent.steerTarget = path[agent.pathId];
                    targetPos = agent.steerTarget.position;
                }

                var targetDir = math_experimental.normalizeSafe(targetPos - position.Value);
                var dir = heading.Value;
                var cos = math.dot(targetDir, dir);
                var angle = math.degrees(math.acos(math.min(math.abs(cos), 1f)) * 2f);

                if (cos < 1f - math_experimental.epsilon)
                {
                    var targetRot = math.lookRotationToQuaternion(targetDir, math.up());
                    var rot = math.lookRotationToQuaternion(dir, math.up());
                    var alpha = math.min(agent.rotateSpeed * dt / angle, 1f);
                    var newRot = math.slerp(rot, targetRot, alpha);
                    heading.Value = math.forward(newRot);
                }

                var headingTarget = heading.Value * agent.speed * dt + position.Value;
                var shiftTarget = targetDir * agent.speed * dt + position.Value;

                var radius = ((agent.speed / (agent.rotateSpeed / 360f)) / 6.283f);

                var nextTarget = math.select(headingTarget, shiftTarget, distance < radius);
                nextTarget = math.select(nextTarget, targetPos, distance < agent.speed * dt);

                agent.location = query.MoveLocation(agent.location, nextTarget);
                position.Value = agent.location.position;

                formationData.sideOffset = math.lerp(agent.steerTarget.vertexSide, agent.fromPoint.vertexSide, 
                    distance / math.distance(agent.steerTarget.position, agent.fromPoint.position));

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
            agents = formations.agents,
            query = query
        };

        return moveJob.Schedule(formations.Length, SimulationState.SmallBatchSize, inputDeps);
    }
}