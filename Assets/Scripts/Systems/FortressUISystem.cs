using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.PlayerLoop;

[UpdateAfter(typeof(PathFindSystem))]
public class FortressUISystem : JobComponentSystem
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

    [Inject]
    public Fortresses fortresses;

    public ComponentPool<LineRenderer> lineRenderers;

    [Inject]
    public Pathfinders pathfinders;

    private FortressUIDrawer drawer;

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        var obj = new GameObject("UIDrawer");
        drawer = obj.AddComponent<FortressUIDrawer>();
        drawer.system = this;
        lineRenderers = new ComponentPool<LineRenderer>();
        lineRenderers.prefab = FortressSettings.Instance.lineRenderer;
    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inDeps)
    {
        NativeArrayExtensions.ResizeNativeArray(ref drawer.fortressDatas, fortresses.Length);
        NativeArrayExtensions.ResizeNativeArray(ref drawer.positions, fortresses.Length);
        inDeps.Complete();

        var copyFortressDataJob = new CopyComponentData<FortressData>
        {
            Source = fortresses.fortressDatas,
            Results = drawer.fortressDatas
        };

        var copyFortressDataFence = copyFortressDataJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        var copyPositionJob = new CopyComponentData<Position>
        {
            Source = fortresses.positions,
            Results = drawer.positions
        };

        var copyPositionFence = copyPositionJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        drawer.length = fortresses.Length;

        var paths = pathfinders.paths;
        var pathRequests = pathfinders.pathRequests;
        var length = pathfinders.Length;
        for (var i = 0; i < length; ++i)
        {
            var request = pathRequests[i];
            if (request.status == PathRequestStatus.Done)
            {
                var path = paths[i];
                var renderer = lineRenderers.New();
                var points = new Vector3[request.pathSize];
                for (var j = 0; j < request.pathSize; ++j)
                    points[j] = path[j].position;
                renderer.positionCount = request.pathSize;
                renderer.SetPositions(points);
            }
        }

        lineRenderers.Present();

        return JobHandle.CombineDependencies(copyFortressDataFence, copyPositionFence);
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();

        drawer.fortressDatas.Dispose();
        drawer.positions.Dispose();
    }
}
