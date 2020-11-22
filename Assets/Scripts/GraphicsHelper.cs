﻿using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Draw
{
    public static void CubeCubeEdges(Mesh cubeMesh, Color color, float cubeExtent, Vector3 center, float thickness = 1f)
    {
        var size = cubeExtent * 2f;
        var t = thickness;

        var x = new Vector3(cubeExtent, 0, 0);
        var y = new Vector3(0, cubeExtent, 0);
        var z = new Vector3(0, 0, cubeExtent);

        var centers = new Vector3[12]
        {
            center + x + z,
            center + x - z,

            center - x + z,
            center - x - z,

            center + y + z,
            center + y - z,

            center - y + z,
            center - y - z,

            center + x + y,
            center + x - y,

            center - x + y,
            center - x - y,
        };

        var scales = new Vector3[12]
        {
            new Vector3(t, size, t),
            new Vector3(t, size, t),

            new Vector3(t, size, t),
            new Vector3(t, size, t),

            new Vector3(size, t, t),
            new Vector3(size, t, t),

            new Vector3(size, t, t),
            new Vector3(size, t, t),

            new Vector3(t, t, size),
            new Vector3(t, t, size),

            new Vector3(t, t, size),
            new Vector3(t, t, size),
        };

        var edgeMatrices = new List<Matrix4x4>(12);
        for (int j = 0; j < 12; ++j)
        {
            edgeMatrices.Add(Matrix4x4.TRS(centers[j], Quaternion.identity, scales[j]));
        }

        LineMaterial.color = color;
        Graphics.DrawMeshInstanced(cubeMesh, 0, LineMaterial, edgeMatrices);
    }

    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    public static Color Opaque(this Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }

    static Material LineMaterialInstance;
    static Material LineMaterial
    {
        get
        {
            if (LineMaterialInstance == null)
            {
                LineMaterialInstance = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                LineMaterialInstance.enableInstancing = true;
            }

            return LineMaterialInstance;
        }
    }
}