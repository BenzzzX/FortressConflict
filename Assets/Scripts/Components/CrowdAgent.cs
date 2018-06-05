﻿using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;

public struct CrowdAgent : IComponentData
{
    public float3 destination;
    public int pathSize;
    public float speed;

    // 1    - right
    // 0    - strait
    // -1   - left
    public float nextCornerSide;
    public float3 steeringTarget;

    // bit 0    - newDestinationRequested
    // bit 1    - goToDestination
    // bit 2    - destinationInView
    // bit 3    - destinationReached
    private int bitfield;

    public bool waitingUnit
    {
        get { return IsBit(0); }
        set { SetBit(0, value); }
    }

    public bool goToDestination
    {
        get { return IsBit(1); }
        set { SetBit(1, value); }
    }

    public bool destinationReached
    {
        get { return IsBit(2); }
        set { SetBit(2, value); }
    }

    private bool IsBit(int bit)
    {
        return (bitfield & (1 << bit)) != 0;
    }

    private void SetBit(int bit, bool value)
    {
        bitfield = value ? (bitfield | (1 << bit)) : (bitfield & ~(1 << bit));
    }

}

