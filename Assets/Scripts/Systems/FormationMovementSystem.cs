using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Experimental.AI;

public class FormationMovementSystem : JobComponentSystem
{
    public struct Formations
    {
        public ComponentDataArray<FormationData> formationDatas;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<CrowdAgent> agents;
        public FixedArrayArray<PathPoint> paths;
    }

    [Inject]
    Formations formations;

    struct FollowPath : IJobParallelFor
    {
        public NavMeshQuery query;
        public ComponentDataArray<FormationData> formationDatas;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<CrowdAgent> agents;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;

        public void Execute(int index)
        {
            var formationData = formationDatas[index];
            var position = positions[index];
            var agent = agents[index];
            var path = paths[index];
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

    }
}