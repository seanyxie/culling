﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(HybridRendererSystem))]
public class UpdateStats : SystemBase
{
    const int NbFrameSample = 10;

    double lastElapsedTime;
    int frame;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<VisibleSetsComponent>();
    }

    protected override void OnStartRunning()
    {
        this.lastElapsedTime = this.Time.ElapsedTime;
    }

    protected override void OnUpdate()
    {
        UpdateFPSDatas();

        if (Stats.Details == StatsDetails.None) return;

        UpdateVisibleSets.LastScheduledJob.Complete();

        var visibleSetsEntity = GetSingletonEntity<VisibleSetsComponent>();
        var visibleSets = this.EntityManager.GetComponentData<VisibleSetsComponent>(visibleSetsEntity).Value;

        Stats.VisibleOctreeClusters = visibleSets.ClusterLayer.Count();
        Stats.VisibleOctreeLeafs = visibleSets.LeafLayer.Count();

        if (Stats.Details == StatsDetails.Normal) return;

        var stats = new NativeArray<int>(8, Allocator.TempJob);

        foreach (var visibleCluster in visibleSets[0])
        {
            this.Entities
            .WithAll<EntityTag>()
            .WithSharedComponentFilter(new OctreeCluster { Value = visibleCluster })
            .WithNativeDisableParallelForRestriction(stats)
            .ForEach((in EntityCullingResult result, in OctreeNode octreeNode) =>
            {
                var id = (int)result.Value;
                ++stats[id];  
            })
            .Run();
        }

        this.Entities
        .WithAll<EntityTag>()
        .WithSharedComponentFilter(new OctreeCluster { Value = Octree.PackedRoot })
        .WithNativeDisableParallelForRestriction(stats)
        .ForEach((in EntityCullingResult result) =>
        {
            if (result.Value == CullingResult.CulledByFrustrumAABB)
            {
                ++stats[6];
            }

            ++stats[7];
        })
        .Run();

        var notCulled = stats[0];

        // stats[1] is a placeholder. 
        // Would contain the number of entities culled by the octree clusters if they were not excluded from the query
        Debug.Assert(stats[1] == 0);

        var culledByOctreeNodes = stats[2];
        var culledByFrustrumPlanes = stats[3];
        var culledBySphereOccluder = stats[4];
        var culledByQuadOccluder = stats[5];
        var culledByFrustrumAABB = stats[6];
        var atRootOctreeLayer = stats[7];

        var culledByOctreeClusters = Stats.TotalEntityNumber - culledByOctreeNodes 
            - culledByFrustrumPlanes - culledByQuadOccluder - culledBySphereOccluder - notCulled - culledByFrustrumAABB;

        Stats.CulledByOctreeNodes = culledByOctreeNodes;
        Stats.CulledByFrustrumAABB = culledByFrustrumAABB;
        Stats.CulledByFrustrumPlanes = culledByFrustrumPlanes;
        Stats.CulledByQuadOccluders = culledByQuadOccluder;
        Stats.CulledBySphereOccluders = culledBySphereOccluder;
        Stats.CulledByOctreeClusters = culledByOctreeClusters;
        Stats.AtRootOctreeLayer = atRootOctreeLayer;

        stats.Dispose();
    }

    void UpdateFPSDatas()
    {
        ++this.frame;

        if (this.frame >= NbFrameSample)
        {
            var t = this.Time.ElapsedTime - this.lastElapsedTime;

            var averageDT = t / this.frame;
            Stats.FPS = (int)math.round(1f / (averageDT));

            this.frame = 0;
            this.lastElapsedTime = this.Time.ElapsedTime;
        }
    }
}
