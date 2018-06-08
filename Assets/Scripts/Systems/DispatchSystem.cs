using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

class DispatchSystem : ComponentSystem
{
    public struct Fortresses
    {
        public ComponentDataArray<FortressData> fortressDatas;
        [ReadOnly]
        public ComponentDataArray<OwnerData> captured;
        [ReadOnly]
        public ComponentDataArray<Position> positions;
        [ReadOnly]
        public ComponentDataArray<PathRequestData> pathRequests;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;

        public ComponentDataArray<DispatchData> marchDatas;

        public int Length;
    }

    [Inject]
    Fortresses fortresses;


    protected override void OnCreateManager(int capacity)
    {
    }

    protected override void OnDestroyManager()
    {
    }

    protected override void OnUpdate()
    {
        var pathBuffer = new NativeArray<PathPoint>(20, Allocator.Temp);
        var pathType = ComponentType.FixedArray(typeof(PathPoint), SimulationState.MaxPathSize);
        for (int i = 0;i<fortresses.Length;++i)
        {
            var marchData = fortresses.marchDatas[i];
            var request = fortresses.pathRequests[i];
            
            if (marchData.troops > 0 && request.status == PathRequestStatus.Done)
            {
                var fortressData = fortresses.fortressDatas[i];
                var path = fortresses.paths[i];
                pathBuffer.CopyFrom(path);
                var troops = Mathf.Min(marchData.troops, FortressSettings.Instance.formationTroops, fortressData.troops);
                fortressData.troops -= troops;
                marchData.troops -= troops;
                marchData.troops = Mathf.Min(fortressData.troops, marchData.troops);
                fortresses.marchDatas[i] = marchData;
                fortresses.fortressDatas[i] = fortressData;



                var formation = EntityManager.Instantiate(FortressSettings.Instance.formationPrefab);

                var formationData = EntityManager.GetComponentData<FormationData>(formation);
                formationData.troops = troops;
                EntityManager.SetComponentData(formation, formationData);

                var position = new Position { Value = pathBuffer[0].location.position };
                EntityManager.SetComponentData(formation, position);

                var agent = EntityManager.GetComponentData<CrowdAgentData>(formation);
                agent.location = pathBuffer[0].location;
                agent.pathId = 1;
                agent.steerTarget = pathBuffer[1];
                agent.fromPoint = pathBuffer[0];
                agent.state = CrowdState.Moving;
                EntityManager.SetComponentData(formation, agent);

                EntityManager.AddComponent(formation, pathType);
                var pathData = EntityManager.GetFixedArray<PathPoint>(formation);
                pathData.CopyFrom(pathBuffer);
            }
        }
        pathBuffer.Dispose();

    }
}
