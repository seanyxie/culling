﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct OctreeID : IComponentData
{
    public int3 ID0;
    public int3 ID1;
}
