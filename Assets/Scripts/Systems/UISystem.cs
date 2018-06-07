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

    public ComponentPool<LineRenderer> lineRenderers;

    [Inject]
    public Pathfinders pathfinders;

    private FortressUIRenderer fortressUI;
    private FormationUIRenderer formationUI;

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        var obj = new GameObject("UIDrawer");
        fortressUI = obj.AddComponent<FortressUIRenderer>();
        formationUI = obj.AddComponent<FormationUIRenderer>();

        lineRenderers = new ComponentPool<LineRenderer>();
        lineRenderers.prefab = FortressSettings.Instance.lineRenderer;
    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inDeps)
    {
        NativeArrayExtensions.ResizeNativeArray(ref fortressUI.fortressDatas, fortresses.Length);
        NativeArrayExtensions.ResizeNativeArray(ref fortressUI.positions, fortresses.Length);
        NativeArrayExtensions.ResizeNativeArray(ref formationUI.formationDatas, formations.Length);
        NativeArrayExtensions.ResizeNativeArray(ref formationUI.positions, formations.Length);
        NativeArrayExtensions.ResizeNativeArray(ref formationUI.headings, formations.Length);
        inDeps.Complete();

        var fences = new NativeArray<JobHandle>(5, Allocator.Temp);

        var copyFortressDataJob = new CopyComponentData<FortressData>
        {
            Source = fortresses.fortressDatas,
            Results = fortressUI.fortressDatas
        };

        fences[0] = copyFortressDataJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        var copyFortressPositionJob = new CopyComponentData<Position>
        {
            Source = fortresses.positions,
            Results = fortressUI.positions
        };

        fences[1] = copyFortressPositionJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        var copyFormationDataJob = new CopyComponentData<FormationData>
        {
            Source = formations.formationDatas,
            Results = formationUI.formationDatas
        };

        fences[2] = copyFormationDataJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        var copyFormationPositionJob = new CopyComponentData<Position>
        {
            Source = formations.positions,
            Results = formationUI.positions
        };

        fences[3] = copyFormationPositionJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        var copyFormationHeadingJob = new CopyComponentData<Heading>
        {
            Source = formations.headings,
            Results = formationUI.headings
        };

        fences[4] = copyFormationHeadingJob.Schedule(fortresses.Length, SimulationState.TinyBatchSize);

        fortressUI.length = fortresses.Length;

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

        var fence = JobHandle.CombineDependencies(fences);
        fences.Dispose();
        return fence;
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();

        fortressUI.fortressDatas.Dispose();
        fortressUI.positions.Dispose();

        formationUI.formationDatas.Dispose();
        formationUI.positions.Dispose();
        formationUI.headings.Dispose();
    }
}
