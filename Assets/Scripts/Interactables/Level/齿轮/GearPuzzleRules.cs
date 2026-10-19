using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>节圆半径决定接触和转速；不使用图片外框作为啮合判定。</summary>
public static class GearPuzzleRules
{
    public const float ContactTolerance = 3f;
    public static readonly Vector2 BoardOffset = new Vector2(-160, -5);
    // 0 / 6 are fixed. 7-9 are decoys verified by brute force (exactly one solution):
    // 7 sits on the 3-4 link and jams with r60; 8 jams with 1-2; 9 is a dead-end branch
    // off gear 4 that blocks axle 5. Consecutive distances: 120, 100, 80, 140, 160, 120.
    public static readonly Vector2[] Axles =
    {
        new Vector2(-300,180), new Vector2(-228,84), new Vector2(-128,84),
        new Vector2(-64,36), new Vector2(48,-48), new Vector2(176,-144),
        new Vector2(296,-144), new Vector2(7,107), new Vector2(-156,180),
        new Vector2(208,-39)
    };
    public static readonly float[] Radii = {60,60,40,40,100,60,60};
    public static bool Touch(Vector2 a, float ra, Vector2 b, float rb)
        => Mathf.Abs(Vector2.Distance(a,b) - ra - rb) <= ContactTolerance;

    public static bool CanPlace(int moving, int axle, int[] slots, float[] radii)
    {
        if (axle <= 0 || axle == 6 || axle >= Axles.Length) return false;
        for (int i=0; i<slots.Length; i++)
        {
            if (i == moving || slots[i] < 0) continue;
            if (slots[i] == axle) return false;
            if (Vector2.Distance(Axles[axle], Axles[slots[i]]) <
                radii[moving] + radii[i] - ContactTolerance) return false;
        }
        return true;
    }

    /// <summary>BFS builds actual contact graph, including odd-cycle jams.</summary>
    public static float[] Speeds(int[] slots, float[] radii, out bool jammed)
    {
        var speeds = new float[slots.Length];
        var visited = new bool[slots.Length];
        var queue = new Queue<int>();
        jammed = false;
        if (slots.Length == 0 || slots[0] < 0) return speeds;
        speeds[0] = 30f; visited[0] = true; queue.Enqueue(0);
        while (queue.Count > 0)
        {
            int a = queue.Dequeue();
            for (int b=0; b<slots.Length; b++)
            {
                if (a == b || slots[b] < 0 || !Touch(Axles[slots[a]], radii[a], Axles[slots[b]], radii[b])) continue;
                float expected = -speeds[a] * radii[a] / radii[b];
                if (visited[b])
                {
                    if (Mathf.Abs(speeds[b] - expected) > .1f) jammed = true;
                }
                else { speeds[b] = expected; visited[b] = true; queue.Enqueue(b); }
            }
        }
        if (jammed) Array.Clear(speeds,0,speeds.Length);
        return speeds;
    }
}
