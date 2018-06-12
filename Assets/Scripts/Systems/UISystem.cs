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
    public struct Formations
    {
        [ReadOnly]
        public ComponentDataArray<FormationData> datas;
        [ReadOnly]
        public ComponentDataArray<Position> positions;
        [ReadOnly]
        public ComponentDataArray<Heading> headings;
        [ReadOnly]
        public SharedComponentDataArray<FormationTypeData> types;

        public int Length;
    }

    [Inject]
    public Formations formations;

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inDeps)
    {
        for (var i = 0; i < formations.Length; ++i)
        {
            var data = formations.datas[i];
            var position = formations.positions[i];
            var heading = formations.headings[i];
            var type = formations.types[i];
            for (int j = 0; j < data.troops;)
            {
                var n = math.min(j + type.unitType.formationWidth, data.troops);
                Debug.DrawLine(data.GetUnitAlignTarget(n - 1, position, heading, type.unitType.formationWidth)
                    , data.GetUnitAlignTarget(j, position, heading, type.unitType.formationWidth)
                    , Color.yellow, Time.deltaTime * 2);
                j = n;
            }
        }

        return inDeps;
    }

    protected override void OnDestroyManager()
    {
    }
}
