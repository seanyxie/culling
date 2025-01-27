﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct Spawner : IComponentData
{
    public Vector3 Origin;
    public Entity EntityPrefab;
    public int EntityCount;
    public Entity SphereOccluderPrefab;
    public int SphereOccluderCount;
    public Entity QuadOccluderPrefab;
    public int QuadOccluderCount;
    public float MinGenerationSpan;
    public float MaxGenerationSpan;
    public float MinScale;
    public float MaxScale;
    public float MinSelfRotationSpeed;
    public float MaxSelfRotationSpeed;
    public float MinWorldRotationSpeed;
    public float MaxWorldRotationSpeed;
    public int StaticEntityPercentage;

    public float SphereOccluderMinGenerationSpan;
    public float SphereOccluderMaxGenerationSpan;
    public float SphereOccluderMinScale;
    public float SphereOccluderMaxScale;

    public float QuadOccluderMinGenerationSpan;
    public float QuadOccluderMaxGenerationSpan;
    public float QuadOccluderMinScale;
    public float QuadOccluderMaxScale;
}

public struct WorldOccluderExtents : IComponentData
{
    public float3 LocalRight;
    public float LocalRightLength;
    public float3 LocalUp;
    public float LocalUpLength;
}

public struct WorldOccluderRadius : IComponentData
{
    public float Value;
}