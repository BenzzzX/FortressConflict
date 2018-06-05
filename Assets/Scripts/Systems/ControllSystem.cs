using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.AI;

using Unity.Rendering;

public class ControllSystem : ComponentSystem {

    public struct Fortresses
    {
        [ReadOnly]
        public ComponentDataArray<FortressData> fortresses;
        [ReadOnly]
        public ComponentDataArray<OwnerData> captured;
        [ReadOnly]
        public ComponentDataArray<Position> positions;
        [ReadOnly]
        public ComponentDataArray<PathRequestData> pathRequests;

        public EntityArray entities;

        public int Length;
    }

    [Inject]
    [ReadOnly]
    Fortresses fortresses;

    [Inject]
    [ReadOnly]
    ComponentDataFromEntity<FortressData> fortressData;
    
    [Inject]
    ComponentDataFromEntity<PathRequestData> pathRequests;

    [Inject]
    [ReadOnly]
    FixedArrayFromEntity<PathData> paths;

    [Inject]
    [ReadOnly]
    ComponentDataFromEntity<Position> positions;

    NativeList<Entity> selected;
    Entity target;

    public const float maxOffset = 1.1f;

    protected override void OnCreateManager(int capacity)
    {
        selected = new NativeList<Entity>(Allocator.Persistent);
        target = new Entity();
    }

    protected override void OnDestroyManager()
    {
        selected.Dispose();
    }

    protected override void OnUpdate()
    {
        //去掉无效的引用
        for (var i = 0; i < selected.Length; ++i)
        {
            var entity = selected[i];
            var owner = EntityManager.GetComponentData<OwnerData>(entity);
            while (i < selected.Length && owner.alliance != 0)
                selected.RemoveAtSwapBack(i);
        }

        //按下左键, 选择中
        if (Input.GetMouseButton(0) && fortresses.Length > 0)
        {
            var mousePos = Input.mousePosition;
            var cam = Camera.main;
            if (cam == null) return;

            //从鼠标射线
            var ray = cam.ScreenPointToRay(mousePos);
            var hitInfo = new RaycastHit();
            Physics.Raycast(ray, out hitInfo, Mathf.Infinity);

            //未命中就跳过
            if (hitInfo.distance == 0) return;
            var hitPoint = hitInfo.point;

            var closestIndex = 0;

            var closestDistance = math.distance(fortresses.positions[closestIndex].Value, hitPoint);

            for(var i=0;i<fortresses.Length;++i)
            {
                var distance = math.distance(fortresses.positions[i].Value, hitPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }


            if (closestDistance < maxOffset)
            {
                var entity = fortresses.entities[closestIndex];
                var owner = EntityManager.GetComponentData<OwnerData>(entity);
                if (selected.Length == 0 && owner.alliance == 0) //第一个直接选中
                {
                    selected.Add(entity);
                    FortressSettings setting = FortressSettings.Instance;
                    EntityManager.SetSharedComponentData(entity, setting.selectedRenderer);
                }
                else if (selected.Length == 1 && entity == selected[0]) { }
                else if (target != entity) //新的目标
                {
                    if (selected.Length > 1) //两个或以上可以取消选择作为目标
                    {
                        var index = selected.IndexOf(entity);
                        if (index >= 0) //如果已经选中了,需要取消
                        {
                            selected.RemoveAtSwapBack(index);
                            FortressSettings setting = FortressSettings.Instance;
                            PostUpdateCommands.SetSharedComponent(entity, setting.baseRenderer);
                        }
                    }

                    target = entity;

                    for (var i = 0; i < selected.Length; ++i)
                    {
                        var request = pathRequests[selected[i]];
                        request.status = PathRequestStatus.NewRequest;
                        request.start = positions[selected[i]].Value;
                        request.end = positions[target].Value;
                        request.mask = NavMesh.AllAreas;
                        pathRequests[selected[i]] = request;
                    }
                }
            }
            else
            {
                if (target != new Entity()) //如果没有确认为目标,则选中
                {
                    for (var i = 0; i < selected.Length; ++i)
                    {
                        var request = pathRequests[selected[i]];
                        request.status = PathRequestStatus.Idle;
                        pathRequests[selected[i]] = request;
                    }

                    var owner = EntityManager.GetComponentData<OwnerData>(target);
                    if (owner.alliance == 0)
                        if (!selected.Contains(target))
                        {
                            selected.Add(target);
                            FortressSettings setting = FortressSettings.Instance;
                            PostUpdateCommands.SetSharedComponent(target, setting.selectedRenderer);
                        }
                    target = new Entity();
                }
            }
        }
        else //松开左键,执行动作
        {
            if (selected.Length > 0 && target != new Entity())
            {
                for (var i = 0; i < selected.Length; ++i)
                {
                    PostUpdateCommands.CreateEntity();
                    var data = new FortressMarchData();
                    data.entity = selected[i];
                    data.targetEntity = target;
                    data.withTroops = fortressData[selected[i]].troops / 2;
                    PostUpdateCommands.AddComponent(data);
                }
            }

            //取消所有选中
            for (var i = 0; i < selected.Length; ++i) 
            {
                
                var request = pathRequests[selected[i]];
                request.status = PathRequestStatus.Idle;
                pathRequests[selected[i]] = request;

                FortressSettings setting = FortressSettings.Instance;
                PostUpdateCommands.SetSharedComponent(selected[i], setting.baseRenderer);
            }

            selected.Clear();
            target = new Entity();
        }
        
        for (var i = 0; i < selected.Length; ++i)
        {
            var pathType = ComponentType.FixedArray(typeof(PathData), SimulationState.MaxPathSize);
            if (!EntityManager.HasComponent(selected[i], pathType))
                EntityManager.AddComponent(selected[i], pathType);
        }
    }
}
