﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class Main : MonoBehaviour
{
    public static float3 Viewer;
    public static float3 NearPlaneCenter;
    public static Quad NearPlane;
    public static float4x4 WorldToNDC;
    public static WorldFrustrumPlanes FrustrumPlanes;
    public static AABB FrustrumAABB;
    public static float4 EntityOutFrumstrumColor;
    public static float4 EntityInFrustrumColor;
    public static float4 EntityOccludedColor;

    public static World World;
    public static EntityManager EntityManager;
    public static EntityQuery EntityQuery;
    public static VisibleOctreeNode[] VisibleOctreeNodes;
    public static VisibleOctreeCluster[] VisibleOctreeClusters;

    public static bool IsLocked;
    public static bool DisplayStats;

    [SerializeField] ViewerCamera viewerCamera;
    [SerializeField] OrbitalCamera orbitalCamera;
    [SerializeField] Color entityOutFrumstrumColor;
    [SerializeField] Color entityInFrustrumColor;
    [SerializeField] Color entityOccludedColor;
    [SerializeField] Color boudingSphereColor;
    [SerializeField] Material[] octreeLayerMaterials;
    [SerializeField] Color frustrumAABBColor;
    [SerializeField] Mesh cubeMesh;
    [SerializeField] MeshFilter frustrumPlanesMesh;
    [SerializeField] Canvas statsPanel;
    [SerializeField] bool lockOnStart = false;

    bool displayBoundingSpheres = false;
    int displayOctreeDepth = -1; // -1 means do not display anything
    bool displayFrustrumAABB = false;

    private void Awake()
    {
        EntityOutFrumstrumColor = this.entityOutFrumstrumColor.ToFloat4();
        EntityInFrustrumColor = this.entityInFrustrumColor.ToFloat4();
        EntityOccludedColor = this.entityOccludedColor.ToFloat4();
    }

    private void Start()
    {
        this.frustrumPlanesMesh.GetComponent<MeshRenderer>().enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        this.viewerCamera.Use(true);
        this.statsPanel.enabled = false;
        SetLock(this.lockOnStart);
        SetStatsPanelVisible(false);
    }

    private void Update()
    {
        Inputs();

        this.frustrumPlanesMesh.mesh = this.viewerCamera.Camera.ComputeFrustumMesh();

        FrustrumPlanes = this.viewerCamera.Camera.ComputeFrustrumPlanes();
        FrustrumAABB = this.viewerCamera.Camera.ComputeFrustrumAABB();
        WorldToNDC = this.viewerCamera.Camera.projectionMatrix * this.viewerCamera.Camera.worldToCameraMatrix;
        Viewer = this.viewerCamera.transform.position;
        NearPlaneCenter = this.viewerCamera.transform.position + this.viewerCamera.transform.forward * this.viewerCamera.Camera.nearClipPlane;

        var nearPlane = new Quad();
        nearPlane.Center = NearPlaneCenter;
        nearPlane.LocalRight = this.viewerCamera.transform.right * this.viewerCamera.Camera.NearPlaneHalfWidth();
        nearPlane.LocalUp = this.viewerCamera.transform.up * this.viewerCamera.Camera.NearPlaneHalfHeight();
        nearPlane.Normal = FrustrumPlanes.Near.normal;
        NearPlane = nearPlane;

        if (World != null && !World.Equals(null))
        {
            if (this.displayOctreeDepth != -1)
            {
                DrawOctree();
            }
        }
    }

    private void OnDrawGizmos()
    {
        this.viewerCamera.Camera.DrawFrustrum(Color.yellow);

        if (World != null && !World.Equals(null))
        {
            if (this.displayBoundingSpheres)
            {
                DrawEntityBoundingSpheres();
            }

            if (this.displayFrustrumAABB)
            {
                DrawFrustrumAABB();
            }
        }
    }

    void Inputs()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.viewerCamera.ToggleUse();
            this.orbitalCamera.ToggleUse();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLock();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleStatsPanelVisible();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ++this.displayOctreeDepth;
            if (this.displayOctreeDepth > Octree.LeafLayer + 1) this.displayOctreeDepth = -1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            this.displayFrustrumAABB = !this.displayFrustrumAABB;
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            this.displayBoundingSpheres = !this.displayBoundingSpheres;
        }
    }

    void DrawEntityBoundingSpheres()
    {
        var translations = EntityQuery.ToComponentDataArray<Translation>(Allocator.Temp);
        var radiuses = EntityQuery.ToComponentDataArray<WorldBoundingRadius>(Allocator.Temp);

        for (int i = 0; i < translations.Length; ++i)
        {
            var center = translations[i].Value;
            var radius = radiuses[i].Value;

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = this.boudingSphereColor;
            Gizmos.DrawSphere(center, radius);
        }
    }

    void DrawOctree()
    {
        if (this.displayOctreeDepth == -1) return;

        if (this.displayOctreeDepth == 0)
        {
            DrawVisibleClusters();
        }
        else if (this.displayOctreeDepth == Octree.LeafLayer + 1)
        {
            DrawAllVisibleOctreeLayers();
        }
        else
        {
            DrawVisibleOctreeNodes(this.displayOctreeDepth);
        }
    }

    void DrawVisibleClusters()
    {
        var material = this.octreeLayerMaterials[0];

        var size = Octree.ClusterSize;
        var matrices = new List<Matrix4x4>(VisibleOctreeClusters.Length);

        foreach (var packedNode in VisibleOctreeClusters)
        {
            var node = Octree.UnpackID(packedNode.Value);

            var center = Octree.ClusterIDToPoint(node.xyz);

            var matrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * size);
            matrices.Add(matrix);

            Draw.CubeCubeEdges(this.cubeMesh, material.color.Opaque(), size * 0.5f, center);
        }
        
        Graphics.DrawMeshInstanced(this.cubeMesh, 0, material, matrices);
    }

    void DrawAllVisibleOctreeLayers()
    {
        for (int i = 1; i < Octree.LeafLayer; ++i)
        {
            DrawVisibleOctreeNodes(i);
        }
    }

    void DrawVisibleOctreeNodes(int layer)
    {
        var matID = math.min(layer, this.octreeLayerMaterials.Length - 1);
        var material = this.octreeLayerMaterials[matID];

        var matrices = new List<Matrix4x4>(VisibleOctreeNodes.Length);
        var size = Octree.NodeSize(layer);

        foreach (var packedNode in VisibleOctreeNodes)
        {
            var node = Octree.UnpackID(packedNode.Value);

            if (node.w != layer) continue;

            var center = Octree.NodeIDToPoint(node);
            
            var matrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * size);
            matrices.Add(matrix);

            Draw.CubeCubeEdges(this.cubeMesh, material.color.Opaque(), size * 0.5f, center);
        }

        Graphics.DrawMeshInstanced(this.cubeMesh, 0, material, matrices);
    }

    void DrawFrustrumAABB()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = this.frustrumAABBColor;
        Gizmos.DrawCube(FrustrumAABB.Center, FrustrumAABB.Size);
    }

    void SetStatsPanelVisible(bool visible)
    {
        DisplayStats = visible;
        this.statsPanel.enabled = visible;
    }

    void ToggleStatsPanelVisible()
    {
        SetStatsPanelVisible(!DisplayStats);
    }

    void SetLock(bool locked)
    {
        IsLocked = locked;
        this.viewerCamera.IsLocked = locked;
    }

    void ToggleLock()
    {
        SetLock(!IsLocked);
    }
}

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

public static class MiscExt
{
    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    public static Color Opaque(this Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }
}