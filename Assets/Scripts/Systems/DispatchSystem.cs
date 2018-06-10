using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics.Experimental;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;

class DispatchSystem : ComponentSystem
{
    public struct Fortresses
    {
        [ReadOnly]
        public ComponentDataArray<OwnerData> captured;
        [ReadOnly]
        public ComponentDataArray<PathRequestData> pathRequests;
        [ReadOnly]
        public FixedArrayArray<PathPoint> paths;
        [ReadOnly]
        public SharedComponentDataArray<FormationTypeData> types;
        public ComponentDataArray<DispatchData> dispatchs;
        public ComponentDataArray<FortressData> datas;
        public EntityArray entities;
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
        var pathType = ComponentType.FixedArray(typeof(PathPoint), SimulationState.MaxPathSize);
        var fortressSpawnFormation = new NativeList<Entity>(fortresses.Length, Allocator.Temp);
        var fortressSpawnUnit = new NativeList<Entity>(fortresses.Length, Allocator.Temp);

        //收集需要生成的军队
        for (int i = 0; i < fortresses.Length; ++i)
        {
            var marchData = fortresses.dispatchs[i];
            var request = fortresses.pathRequests[i];

            if (marchData.troops > 0 && request.status == PathRequestStatus.Done)
            {
                if (marchData.doneDispatch == 1)
                {
                    fortressSpawnFormation.Add(fortresses.entities[i]);
                }
                else
                {
                    fortressSpawnUnit.Add(fortresses.entities[i]);
                }
            }
        }
        
        for(int i=0;i<fortressSpawnFormation.Length;++i)
        {
            var fortress = fortressSpawnFormation[i];
            var formation = EntityManager.CreateEntity();

            var dispatch = EntityManager.GetComponentData<DispatchData>(fortress);

            var path = EntityManager.GetFixedArray<PathPoint>(fortress);
            var type = EntityManager.GetSharedComponentData<FormationTypeData>(fortress);
            var owner = EntityManager.GetComponentData<DispatchData>(fortress);
            

            var position = new Position { Value = path[0].location.position };
            var dir = new float3(dispatch.offset.x, 0, dispatch.offset.z);
            var heading = new Heading { Value = math_experimental.normalizeSafe(dir) };
            

            var formationData = new FormationData
            {
                troops = 0,
                sideOffset = 0,
                state = FormationState.Spawning
            };

            var agent = new FormationAgentData
            {
                location = path[0].location,
                pathId = 1,
                steerTarget = path[1],
                fromPoint = path[0],
                state = AgentState.Moving,
            };

            EntityManager.AddComponentData(formation, position);
            EntityManager.AddComponentData(formation, formationData);
            EntityManager.AddComponentData(formation, agent);
            EntityManager.AddComponentData(formation, owner);
            EntityManager.AddComponentData(formation, heading);
            EntityManager.AddComponent(formation, pathType);
            EntityManager.AddSharedComponentData(formation, type);

            var pathData = EntityManager.GetFixedArray<PathPoint>(formation);
            path = EntityManager.GetFixedArray<PathPoint>(fortress);
            pathData.CopyFrom(path);


            dispatch.remain = 0;
            dispatch.doneDispatch = 0;
            dispatch.dispatching = formation;
            EntityManager.SetComponentData(fortress, dispatch);
        }

        fortressSpawnFormation.Dispose();

        for (int i = 0; i < fortressSpawnUnit.Length; ++i)
        {
            var fortress = fortressSpawnUnit[i];
            var dispatch = EntityManager.GetComponentData<DispatchData>(fortress);
            dispatch.remain -= Time.deltaTime;
            if (dispatch.remain <= 0f)
            {
                var formation = dispatch.dispatching;
                var fortressData = EntityManager.GetComponentData<FortressData>(fortress);
                var formationData = EntityManager.GetComponentData<FormationData>(formation);
                var type = EntityManager.GetSharedComponentData<FormationTypeData>(fortress);
                var troops = Mathf.Min(dispatch.troops, type.unitType.width, fortressData.troops, type.maxTroops - formationData.troops);
                fortressData.troops -= troops;
                dispatch.troops -= troops;
                dispatch.troops = Mathf.Min(fortressData.troops, dispatch.troops);
                dispatch.remain = dispatch.frequency;
                if (formationData.troops == type.maxTroops || dispatch.troops == 0) //done
                {
                    dispatch.doneDispatch = 1;
                    formationData.state &= ~FormationState.Spawning;
                }
                //@TODO: Spawn Unit
                for(var j=0;j<troops;++j)
                {

                    formationData.troops++;
                }
                EntityManager.SetComponentData(formation, formationData);
                EntityManager.SetComponentData(fortress, fortressData);
            }
            EntityManager.SetComponentData(fortress, dispatch);
        }

        fortressSpawnUnit.Dispose();
    }
}
