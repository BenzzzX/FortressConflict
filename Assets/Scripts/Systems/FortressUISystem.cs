using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.PlayerLoop;

[UpdateAfter(typeof(PostLateUpdate))]
public class FortressUISystem : JobComponentSystem
{
    public struct Fortresses
    {
        [ReadOnly]
        public ComponentDataArray<FortressData> fortresses;
        [ReadOnly]
        public ComponentDataArray<Position> positions;

        public int Length;
    }

    public struct Pathfinders
    {
        [ReadOnly]
        public ComponentDataArray<PathRequestData> pathRequests;
        [ReadOnly]
        public FixedArrayArray<PathData> paths;

        public int Length;
    }

    [Inject]
    public Fortresses fortresses;


    [Inject]
    public Pathfinders pathfinders;

    private FortressUIDrawer drawer;

    public NativeArray<int> downs;

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        var obj = new GameObject("UIDrawer");
        drawer = obj.AddComponent<FortressUIDrawer>();
        drawer.system = this;
    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inDeps)
    {
        if (drawer == null) return inDeps;
        int length = fortresses.Length;

        NativeArrayExtensions.ResizeNativeArray(ref downs, length);

        return inDeps;
    }

    protected override void OnDestroyManager()
    {
        if (downs.IsCreated) downs.Dispose();
        base.OnDestroyManager();
    }
}
