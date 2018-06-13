﻿//
// Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//

// The original source code has been modified by Unity Technologies.

using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.AI;

[Flags]
public enum StraightPathFlags
{
    Start = 0x01,              // The vertex is the start position.
    End = 0x02,                // The vertex is the end position.
    OffMeshConnection = 0x04   // The vertex is start of an off-mesh link.
}

public class PathUtils
{

    static public float3 ProjectToSegment(float3 v, float3 w, float3 p)
    {
        float l2 = math.lengthSquared(v - w);
        float t = math.max(0f, math.min(1, math.dot(p - v, w - v) / l2));
        float3 projection = v + t * (w - v);
        return projection;
    }

    public static float Perp2D(Vector3 u, Vector3 v)
    {
        return u.z * v.x - u.x * v.z;
    }

    public static void Swap(ref Vector3 a, ref Vector3 b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    // Calculate the closest point of approach for line-segment vs line-segment.
    public static bool SegmentSegmentCPA(out float3 c0, out float3 c1, float3 p0, float3 p1, float3 q0, float3 q1)
    {
        var u = p1 - p0;
        var v = q1 - q0;
        var w0 = p0 - q0;

        float a = math.dot(u, u);
        float b = math.dot(u, v);
        float c = math.dot(v, v);
        float d = math.dot(u, w0);
        float e = math.dot(v, w0);

        float den = (a * c - b * b);
        float sc, tc;

        if (den == 0)
        {
            sc = 0;
            tc = d / b;

            // todo: handle b = 0 (=> a and/or c is 0)
        }
        else
        {
            sc = (b * e - c * d) / (a * c - b * b);
            tc = (a * e - b * d) / (a * c - b * b);
        }

        c0 = math.lerp(p0, p1, sc);
        c1 = math.lerp(q0, q1, tc);

        return den != 0;
    }

    // Retrace portals between corners and register if type of polygon changes
    public static int RetracePortals(NavMeshQuery query, int startIndex, int endIndex
        , NativeSlice<PolygonId> path, int n, Vector3 termPos
        , NativeSlice<PathPoint> straightPath
        , int maxStraightPath)
    {
        PathPoint point;
        for (var k = startIndex; k < endIndex - 1; ++k)
        {
            var type1 = query.GetPolygonType(path[k]);
            var type2 = query.GetPolygonType(path[k + 1]);

            if (type1 != type2)
            {
                Vector3 l, r;
                var status = query.GetPortalPoints(path[k], path[k + 1], out l, out r);

                float3 cpa1, cpa2;
                SegmentSegmentCPA(out cpa1, out cpa2, l, r, straightPath[n - 1].location.position, termPos);

                point = straightPath[n];

                point.location = query.CreateLocation(cpa1, path[k + 1]);

                point.flag = (type2 == NavMeshPolyTypes.OffMeshConnection) ? StraightPathFlags.OffMeshConnection : 0;
                if (++n == maxStraightPath)
                {
                    return maxStraightPath;
                }
                straightPath[n] = point;
            }
        }
        point = straightPath[n];
        point.location = query.CreateLocation(termPos, path[endIndex]);
        point.flag = query.GetPolygonType(path[endIndex]) == NavMeshPolyTypes.OffMeshConnection ? StraightPathFlags.OffMeshConnection : 0;
        straightPath[n] = point;
        return ++n;
    }

    public static PathQueryStatus FindStraightPath(NavMeshQuery query, Vector3 startPos, Vector3 endPos
        , NativeSlice<PolygonId> path, int pathSize
        , NativeSlice<PathPoint> straightPath
        , ref int straightPathCount
        , int maxStraightPath)
    {
        if (!query.IsValid(path[0]))
        {
            straightPath[0] = new PathPoint(); // empty terminator
            return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
        }
        var point = straightPath[0];
        point.location = query.CreateLocation(startPos, path[0]);
        point.flag = StraightPathFlags.Start;
        point.vertexSide = 0;
        straightPath[0] = point;

        var apexIndex = 0;
        var n = 1;

        if (pathSize > 1)
        {
            var startPolyWorldToLocal = query.PolygonWorldToLocalMatrix(path[0]);

            var apex = startPolyWorldToLocal.MultiplyPoint(startPos);
            var left = new Vector3(0, 0, 0); // Vector3.zero accesses a static readonly which does not work in burst yet
            var right = new Vector3(0, 0, 0);
            var leftIndex = -1;
            var rightIndex = -1;

            for (var i = 1; i <= pathSize; ++i)
            {
                var polyWorldToLocal = query.PolygonWorldToLocalMatrix(path[apexIndex]);

                Vector3 vl, vr;
                if (i == pathSize)
                {
                    vl = vr = polyWorldToLocal.MultiplyPoint(endPos);
                }
                else
                {
                    var success = query.GetPortalPoints(path[i - 1], path[i], out vl, out vr);
                    if (!success)
                    {
                        return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
                    }
                    
                    vl = polyWorldToLocal.MultiplyPoint(vl);
                    vr = polyWorldToLocal.MultiplyPoint(vr);
                }

                vl = vl - apex;
                vr = vr - apex;

                // Ensure left/right ordering
                if (Perp2D(vl, vr) < 0)
                    Swap(ref vl, ref vr);

                // Terminate funnel by turning
                if (Perp2D(left, vr) < 0)
                {
                    var polyLocalToWorld = query.PolygonLocalToWorldMatrix(path[apexIndex]);
                    var termPos = polyLocalToWorld.MultiplyPoint(apex + left);

                    n = RetracePortals(query, apexIndex, leftIndex, path, n, termPos, straightPath, maxStraightPath);
                    point = straightPath[n-1];
                    point.vertexSide = -1;
                    straightPath[n-1] = point;

                    //Debug.Log("LEFT");

                    if (n == maxStraightPath)
                    {
                        straightPathCount = n;
                        return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                    }

                    apex = polyWorldToLocal.MultiplyPoint(termPos);
                    left.Set(0, 0, 0);
                    right.Set(0, 0, 0);
                    i = apexIndex = leftIndex;
                    continue;
                }
                if (Perp2D(right, vl) > 0)
                {
                    var polyLocalToWorld = query.PolygonLocalToWorldMatrix(path[apexIndex]);
                    var termPos = polyLocalToWorld.MultiplyPoint(apex + right);

                    n = RetracePortals(query, apexIndex, rightIndex, path, n, termPos, straightPath, maxStraightPath);
                    point = straightPath[n - 1];
                    point.vertexSide = 1;
                    straightPath[n-1] = point;

                    //Debug.Log("RIGHT");

                    if (n == maxStraightPath)
                    {
                        straightPathCount = n;
                        return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                    }

                    apex = polyWorldToLocal.MultiplyPoint(termPos);
                    left.Set(0, 0, 0);
                    right.Set(0, 0, 0);
                    i = apexIndex = rightIndex;
                    continue;
                }

                // Narrow funnel
                if (Perp2D(left, vl) >= 0)
                {
                    left = vl;
                    leftIndex = i;
                }
                if (Perp2D(right, vr) <= 0)
                {
                    right = vr;
                    rightIndex = i;
                }
            }
        }

        // Remove the the next to last if duplicate point - e.g. start and end positions are the same
        // (in which case we have get a single point)
        if (n > 0 && (straightPath[n - 1].location.position == endPos))
            n--;

        n = RetracePortals(query, apexIndex, pathSize - 1, path, n, endPos, straightPath, maxStraightPath);
        point = straightPath[n - 1];
        point.vertexSide = 0;
        point.flag = StraightPathFlags.End;
        straightPath[n - 1] = point;

        straightPathCount = n;

        // Fix flag for final path point
        return PathQueryStatus.Success;
    }
}
