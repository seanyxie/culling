﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using System;

[UpdateBefore(typeof(TransformSystemGroup))]
public class UpdateEntityOctreeLeaf : SystemBase
{
    protected override void OnUpdate()
    {
        this.Entities
        .WithChangeFilter<Translation>()
        .ForEach((ref OctreeLeaf octreeLeaf, in Translation translation, in Entity entity) =>
        {
            var newID = Octree.PackID(Octree.PointToILeafID(translation.Value));

            octreeLeaf = new OctreeLeaf
            {
                Value = newID
            };
        })
        .ScheduleParallel();
    }
}