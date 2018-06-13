using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using UnityEngine.Experimental.AI;
using UnityEngine;


public class DeploySystem : ComponentSystem
{
    public struct Units
    {
        public ComponentDataArray<Position> positions;
        [ReadOnly]
        public ComponentDataArray<InFormationData> inFormations;
        public EntityArray entities;

        public int Length;
    }

    [Inject] Units units;

    [Inject] ComponentDataFromEntity<FormationData> formationDatas;

    protected override void OnUpdate()
    {
        for(var i=0;i<units.Length;++i)
        {
            var formationData = formationDatas[units.inFormations[i].formationEntity];
            if(formationData.state == FormationState.Deploying && math.distance(PathUtils.ProjectToSegment(formationData.goalLineL, formationData.goalLineR, units.positions[i].Value), units.positions[i].Value) < 1.5)
            {
                PostUpdateCommands.DestroyEntity(units.entities[i]);
                formationData.troops--;
                formationDatas[units.inFormations[i].formationEntity] = formationData;
            }
        }
    }
}
