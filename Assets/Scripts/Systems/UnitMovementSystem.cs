using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using UnityEngine.Experimental.AI;
using UnityEngine;


public class UnitPathFollowSystem : JobComponentSystem {
    public struct Units
    {
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Heading> headings;
        public ComponentDataArray<UnitAgentData> agents;
        public ComponentDataArray<InFormationData> inFormations;
        public SharedComponentDataArray<UnitTypeData> types;

        public int Length;
    }

    [Inject]
    Units units;

    private struct FillCell : IJobParallelFor
    {
        [ReadOnly]
        public ComponentDataArray<Position> positions;
        public NativeMultiHashMap<int, int>.Concurrent hashMap;

        public void Execute(int index)
        {
            var position = positions[index].Value;
            hashMap.Add(GridHash.Hash(position, 1f), index);
        }
    }

    private struct MergeCell : IJobNativeMultiHashMapMergedSharedKeyIndices
    {
        public NativeArray<int> cellIndices;
        
        public NativeArray<Position> cellSeparation;
        public NativeArray<int> cellCount;


        public void ExecuteFirst(int index)
        {
            cellIndices[index] = index;
        }

        public void ExecuteNext(int cellIndex, int index)
        {
            cellCount[cellIndex] += 1;
            cellSeparation[cellIndex] = new Position { Value = cellSeparation[cellIndex].Value + cellSeparation[index].Value };
            cellIndices[index] = cellIndex;
        }
    }


    private struct Steer : IJobParallelFor
    {
        [ReadOnly] public NavMeshQuery query;
        [ReadOnly] public ComponentDataFromEntity<FormationData> formationDatas;
        [ReadOnly] public ComponentDataFromEntity<Position> formationPositions;
        [ReadOnly] public ComponentDataFromEntity<Heading> formationHeadings;
        [ReadOnly] public ComponentDataArray<InFormationData> inFormations;
        [ReadOnly] public NativeArray<int> cellIndices;
        [ReadOnly] public NativeArray<Position> cellSeparation;
        [ReadOnly] public NativeArray<int> cellCount;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<UnitAgentData> agents;
        [ReadOnly] public SharedComponentDataArray<UnitTypeData> types;

        public ComponentDataArray<Heading> headings;
        public NativeArray<Vector3> steerTargets;
        public NativeArray<NavMeshLocation> prevLocations;

        public float dt;

        public void Execute(int index)
        {
            var inFormation = inFormations[index];
            var formation = inFormation.formationEntity;
            var formationData = formationDatas[formation];
            var agent = agents[index];
            var type = types[index];
            var align = formationHeadings[formation];
            var targetPos = formationData.GetUnitAlignTarget(inFormation.index, formationPositions[formation], align, type.width);
            var position = positions[index];
            var heading = headings[index];
            var distance = math.distance(targetPos, position.Value);
            var targetDir = math_experimental.normalizeSafe(
                math.select(align.Value
                , targetPos - position.Value
                , distance > math_experimental.epsilon));
            
            var dir = heading.Value;
            var cos = math.dot(targetDir, dir);
            var angle = math.degrees(math.acos(math.min(math.abs(cos), 1f)) * 2f);

            if (cos < 1f - math_experimental.epsilon)
            {
                heading.Value = Vector3.RotateTowards(dir, targetDir, Mathf.PI * dt, 10f);
            }

            var shiftTarget = targetDir * type.speed * dt * math.abs(cos) + position.Value;
            var nextTarget = math.select(shiftTarget, targetPos, distance < type.speed * dt);
            nextTarget.y -= type.zOffset;
            headings[index] = heading;
            prevLocations[index] = agent.location;
            steerTargets[index] = nextTarget;
        }
    }

    private struct BatchedMove : IJobParallelForBatch
    {
        [ReadOnly]
        public NavMeshQuery query;

        public NativeArray<Vector3> steerTargets;
        public NativeArray<NavMeshLocation> prevLocations;

        public void Execute(int startIndex, int count)
        {
            var locationSlice = new NativeSlice<NavMeshLocation>(prevLocations, startIndex, count);
            var targetSlice = new NativeSlice<Vector3>(steerTargets, startIndex, count);
            query.MoveLocationsInSameAreas(locationSlice, targetSlice);
        }
    }

    private struct SyncTransform : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Vector3> steerTargets;
        [DeallocateOnJobCompletion]
        public NativeArray<NavMeshLocation> newLocations;

        public ComponentDataArray<Position> positions;
        public ComponentDataArray<UnitAgentData> agents;
        [ReadOnly] public SharedComponentDataArray<UnitTypeData> types;

        public void Execute(int index)
        {
            var position = positions[index];
            var agent = agents[index];
            agent.location = newLocations[index];
            position.Value = agent.location.position;
            position.Value.y -= types[index].zOffset;
            positions[index] = position;
            agents[index] = agent;
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return base.OnUpdate(inputDeps);
    }
}
