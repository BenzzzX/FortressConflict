using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.AI;
using System.Collections.Generic;

using Unity.Rendering;

public class ControllSystem : ComponentSystem {

    public struct Fortresses
    {
        [ReadOnly]
        public ComponentDataArray<FortressData> fortressDatas;
        [ReadOnly]
        public ComponentDataArray<OwnerData> captured;
        [ReadOnly]
        public ComponentDataArray<Position> positions;

        public ComponentDataArray<DispatchData> marchDatas;

        public EntityArray entities;

        public int Length;
    }

    [Inject]
    [ReadOnly]
    Fortresses fortresses;

    [Inject]
    [ReadOnly]
    ComponentDataFromEntity<DispatchData> dispatches;
    
    [Inject]
    ComponentDataFromEntity<PathRequestData> pathRequests;

    [Inject]
    [ReadOnly]
    FixedArrayFromEntity<PathPoint> paths;

    [Inject]
    [ReadOnly]
    ComponentDataFromEntity<Position> positions;


    List<GameObject> selectedObject;
    NativeList<Entity> selected;
    Entity lastTarget;
    GameObject lastTargetObject;

    public const float maxOffset = 2f;

    protected override void OnCreateManager(int capacity)
    {
        selectedObject = new List<GameObject>();
        selected = new NativeList<Entity>(Allocator.Persistent);
        lastTarget = new Entity();
    }

    protected override void OnDestroyManager()
    {
        selected.Dispose();
    }
    

    void FinishSelection()
    {
        //取消所有选中
        for (var i = 0; i < selectedObject.Count; ++i)
        {
            var renderer = selectedObject[i].GetComponent<MeshRenderer>();
            FortressSettings setting = FortressSettings.Instance;
            renderer.material = setting.baseMaterial;
            var lineRenderer = selectedObject[i].GetComponent<LineRenderer>();
            lineRenderer.enabled = false;
        }

        selected.Clear();
        selectedObject.Clear();
        lastTarget = new Entity();
        lastTargetObject = null;
    }

    protected override void OnUpdate()
    {

        if(lastTarget != new Entity())
        {
            for (var i = 0; i < selectedObject.Count; ++i)
            {
                var entity = selected[i];
                var request = pathRequests[entity];
                if (request.status == PathRequestStatus.Done)
                {
                    var path = paths[entity];
                    var renderer = selectedObject[i].GetComponent<LineRenderer>();
                    renderer.enabled = true;
                    var points = new Vector3[request.pathSize];
                    for (var j = 0; j < request.pathSize; ++j)
                        points[j] = path[j].location.position;
                    renderer.positionCount = request.pathSize;
                    renderer.SetPositions(points);
                }
            }
        }

        //按下左键, 选择中
        if (Input.GetMouseButtonUp(0) && fortresses.Length > 0)
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
            var newTargetObject = hitInfo.transform.gameObject;


            Entity newTarget = new Entity();


            if (newTargetObject != null)
            {
                var entityWrapper = newTargetObject.GetComponent<GameObjectEntity>();

                if (entityWrapper != null)
                    newTarget = entityWrapper.Entity;
            }


            if (newTarget != new Entity())
            {
                var owner = EntityManager.GetComponentData<OwnerData>(newTarget);

                if (selected.Length > 0 && lastTarget == newTarget)
                {
                    for (var i = 0; i < selected.Length; ++i)
                    {
                        //@TODO: Start march

                        var self = selected[i];
                        DispatchData march = EntityManager.GetComponentData<DispatchData>(self);
                        FortressData fortressData = EntityManager.GetComponentData<FortressData>(self);
                        if(march.target == lastTarget)
                        {
                            march.troops += (fortressData.troops - march.troops) / 2;
                        }
                        else
                        {
                            march.target = lastTarget;
                            march.troops = fortressData.troops / 2;
                        }
                        EntityManager.SetComponentData(self, march);
                    }

                    FinishSelection();


                }
                else if(selected.Length == 1 && newTarget == selected[0])
                {
                    //@TODO: Stop march
                    DispatchData march = EntityManager.GetComponentData<DispatchData>(newTarget);
                    march.troops = 0;
                    EntityManager.SetComponentData(newTarget, march);

                    FinishSelection();
                }
                else
                {

                    if (lastTarget != new Entity()) //如果没有确认为目标,则选中
                    {
                        var targetOwner = EntityManager.GetComponentData<OwnerData>(lastTarget);
                        if (targetOwner.alliance == 0)
                        {
                            selected.Add(lastTarget);
                            selectedObject.Add(lastTargetObject);
                            var renderer = lastTargetObject.GetComponent<MeshRenderer>();
                            FortressSettings setting = FortressSettings.Instance;
                            renderer.material = setting.selectedMaterial;
                        }
                        lastTarget = new Entity();
                        lastTargetObject = null;
                    }

                    if (selected.Length == 0 && owner.alliance == 0) //第一个直接选中
                    {
                        selected.Add(newTarget);
                        selectedObject.Add(newTargetObject);
                        var renderer = newTargetObject.GetComponent<MeshRenderer>();
                        FortressSettings setting = FortressSettings.Instance;
                        renderer.material = setting.selectedMaterial;
                    }
                    else if (lastTarget != newTarget) //新的目标
                    {
                        if (selected.Length > 1) //两个或以上可以取消选择作为目标
                        {
                            var index = selected.IndexOf(newTarget);
                            if (index >= 0) //如果已经选中了,需要取消
                            {
                                var renderer = newTargetObject.GetComponent<MeshRenderer>();
                                FortressSettings setting = FortressSettings.Instance;
                                renderer.material = setting.baseMaterial;
                                var lineRenderer = newTargetObject.GetComponent<LineRenderer>();
                                lineRenderer.enabled = false;
                                selected.RemoveAtSwapBack(index);
                                selectedObject.RemoveAtSwapBack(index);
                            }
                        }

                        lastTarget = newTarget;
                        lastTargetObject = newTargetObject;

                        for (var i = 0; i < selected.Length; ++i)
                        {
                            var request = pathRequests[selected[i]];
                            if(request.status == PathRequestStatus.Done && math.distance(positions[lastTarget].Value, request.end) < 0.1f) continue;
                            request.status = PathRequestStatus.NewRequest;
                            var dispatch = dispatches[selected[i]];

                            request.start = positions[selected[i]].Value + dispatch.offset;
                            
                            dispatch = dispatches[lastTarget];
                            request.end = positions[lastTarget].Value + dispatch.offset;
                            request.mask = NavMesh.AllAreas;
                            pathRequests[selected[i]] = request;
                        }
                    }
                }
            }
            else
            {
                FinishSelection();
            }
        }

    }
}
