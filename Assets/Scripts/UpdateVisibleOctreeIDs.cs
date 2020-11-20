﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateVisibleOctreeIDs : SystemBase
{
    protected override void OnUpdate()
    {
        var frustrumAABB = Main.FrustrumAABB;
        var frustrumPlanes = Main.FrustrumPlanes;

        this.Entities.ForEach((DynamicBuffer<VisibleClusterID> visibleClusterIDs, 
            DynamicBuffer<VisibleOctreeID> visibleOctreeIDs,
            DynamicBuffer<VisibleLeafInClusterCount> visibleLeafInClusterCounts) =>
        {
            UpdateVisibilityBuffers(frustrumPlanes, frustrumAABB, visibleClusterIDs, visibleOctreeIDs, visibleLeafInClusterCounts);
        })
        .ScheduleParallel();
    }

    static void UpdateVisibilityBuffers(WorldFrustrumPlanes planes, AABB frustrumAABB, 
        DynamicBuffer<VisibleClusterID> visibleClusters, 
        DynamicBuffer<VisibleOctreeID> visibleOctreeLeafs, 
        DynamicBuffer<VisibleLeafInClusterCount> visibleLeafInClusterCounts)
    {
        visibleClusters.Clear();
        visibleOctreeLeafs.Clear();
        visibleLeafInClusterCounts.Clear();

        int3 minID0;
        int3 maxID0;
        Octree.GetMinMaxClusterIDs(frustrumAABB, out minID0, out maxID0);

        for (int x0 = minID0.x; x0 <= maxID0.x; ++x0)
        {
            for (int y0 = minID0.y; y0 <= maxID0.y; ++y0)
            {
                for (int z0 = minID0.z; z0 <= maxID0.z; ++z0)
                {
                    var id0 = new int3(x0, y0, z0);

                    if (Math.IsCubeInFrustrum(Octree.ClusterIDToPoint(id0), Octree.ClusterExtent, planes))
                    {
                        var clusterID = new ClusterID 
                        { 
                            Value = Octree.PackID(id0) 
                        };
                        visibleClusters.Add(new VisibleClusterID { Value = clusterID });

                        int3 minID1;
                        int3 maxID1;
                        Octree.GetMinMaxNodeChildrenID(id0, out minID1, out maxID1);

                        int visibleLeafCount = 0;
                        for (int x1 = minID1.x; x1 < maxID1.x; ++x1)
                        {
                            for (int y1 = minID1.y; y1 < maxID1.y; ++y1)
                            {
                                for (int z1 = minID1.z; z1 < maxID1.z; ++z1)
                                {
                                    var id1 = new int3(x1, y1, z1);

                                    if (Math.IsCubeInFrustrum(Octree.LeafIDToPoint(id1), Octree.LeafExtent, planes))
                                    {
                                        var id = new OctreeID
                                        {
                                            Value = Octree.PackID(id1),
                                        };

                                        visibleOctreeLeafs.Add(new VisibleOctreeID { Value = id });
                                        ++visibleLeafCount;
                                    }
                                }
                            }
                        }

                        visibleLeafInClusterCounts.Add(new VisibleLeafInClusterCount { Value = visibleLeafCount });
                    }
                }
            }
        }

#if ENABLE_ASSERTS
        AssertNoDupplicate(visibleClusters);
#endif
    }

    static void AssertNoDupplicate(DynamicBuffer<VisibleClusterID> ids)
    {
        for (int i = 0; i < ids.Length; ++i)
        {
            var a = ids[i].Value.Value;

            for (int j = 0; j < ids.Length; ++j)
            {
                if (i == j) continue;

                var b = ids[j].Value.Value;

                Debug.Assert(a != b);
            }
        }
    }
}
