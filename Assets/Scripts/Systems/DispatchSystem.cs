using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

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
        base.OnCreateManager(capacity);
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
    }

    protected override void OnUpdate()
    {
        
    }
}
