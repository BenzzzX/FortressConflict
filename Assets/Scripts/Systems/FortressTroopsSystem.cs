using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;

public class FortressTroopsSystem : JobComponentSystem
{
    public struct Spawning
    {
        public ComponentDataArray<FortressData> fortresses;
        [ReadOnly]
        public ComponentDataArray<OwnerData> captured;
        public ComponentDataArray<SpawnData> spawners;

        public int Length;
    }

    public struct Overloading
    {
        public ComponentDataArray<FortressData> fortresses;
        public ComponentDataArray<OverloadData> overloads;

        public int Length;
    }

    [Inject]
    private Spawning spawnings;

    [Inject]
    private Overloading overloadings;

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var spawnJob = new SpawnJob
        {
            dt = Time.deltaTime,
            fortresses = spawnings.fortresses,
            spawners = spawnings.spawners
        };
        var overloadJob = new OverloadJob
        {
            dt = Time.deltaTime,
            fortresses = overloadings.fortresses,
            overloads = overloadings.overloads
        };
        var spawnFence = spawnJob.Schedule(spawnings.Length, SimulationState.TinyBatchSize, inputDeps);
        var overloadFence = overloadJob.Schedule(overloadings.Length, SimulationState.TinyBatchSize, spawnFence);
        return JobHandle.CombineDependencies(spawnFence, overloadFence);
	}
}


[ComputeJobOptimization]
public struct SpawnJob : IJobParallelFor
{
    public ComponentDataArray<FortressData> fortresses;
    public ComponentDataArray<SpawnData> spawners;

    [ReadOnly]
    public float dt;

    public void Execute(int index)
    {
        var fortress = fortresses[index];
        if (fortress.troops >= fortress.maxTroops) return;

        var spawner = spawners[index];
        float then = dt + spawner.remain;
        float time = math.floor(then / spawner.frequency);
        spawner.remain = then - time * spawner.frequency;
        fortress.troops += (int)time;
        fortress.troops = math.clamp(fortress.troops, 0, fortress.maxTroops);
        spawners[index] = spawner;
        fortresses[index] = fortress;
    }
}

[ComputeJobOptimization]
public struct OverloadJob : IJobParallelFor
{
    public ComponentDataArray<FortressData> fortresses;
    public ComponentDataArray<OverloadData> overloads;

    [ReadOnly]
    public float dt;

    public void Execute(int index)
    {
        var fortress = fortresses[index];
        if (fortress.troops <= fortress.maxTroops) return;

        var overloader = overloads[index];
        float then = dt + overloader.remain;
        float time = math.floor(then / overloader.frequency);
        overloader.remain = then - time * overloader.frequency;
        fortress.troops -= (int)time;
        fortress.troops = math.clamp(fortress.troops, fortress.maxTroops, fortress.maxTroops + overloader.overload);
        overloads[index] = overloader;
        fortresses[index] = fortress;
    }
}