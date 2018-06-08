using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.PlayerLoop;

[UpdateAfter(typeof(PathFindSystem))]
public class UISystem : JobComponentSystem
{
    public struct Fortresses
    {
        [ReadOnly]
        public ComponentDataArray<FortressData> fortressDatas;
        [ReadOnly]
        public ComponentDataArray<Position> positions;

        public int Length;
    }

    public struct Pathfinders
    {
        [ReadOnly]
        public ComponentDataArray<PathRequestData> pathRequests;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;

        public int Length;
    }

    public struct Formations
    {
        [ReadOnly]
        public ComponentDataArray<FormationData> formationDatas;
        [ReadOnly]
        public ComponentDataArray<Position> positions;
        [ReadOnly]
        public ComponentDataArray<Heading> headings;

        public int Length;
    }



    [Inject]
    public Fortresses fortresses;

    [Inject]
    public Formations formations;

    [Inject]
    public Pathfinders pathfinders;

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inDeps)
    {
        for (var i = 0; i < formations.Length; ++i)
        {
            var formationData = formations.formationDatas[i];
            var position = formations.positions[i];
            var heading = formations.headings[i];
            for (int j = 0; j < formationData.troops; j++)
            {
                Debug.DrawLine(position.Value, formationData.GetUnitSteerTarget(position, heading, j), Color.yellow, Time.deltaTime * 2);
            }
        }

        return inDeps;
    }

    protected override void OnDestroyManager()
    {
    }
}
