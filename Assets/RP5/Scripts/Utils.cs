using System;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using UnityEngine;

namespace RP5
{
    using float3 = System.Numerics.Vector3;
    using float2 = System.Numerics.Vector2;
    using float4 = System.Numerics.Vector4;

    public class SceneConstants
    {
        public uint directional_lights_count { get; set; }
        public uint point_lights_count { get; set; }
        public uint spot_lights_count { get; set; }
    }
    struct FullScreenCsThreadGroup
    {
        public int x, y, z;
    };
    
    public static class Utils
    {
        public static int AlignUp(int lhs, int rhs) => (lhs + (rhs - 1)) / rhs;



        //public void DrawPlane(Vector3 position, Vector3 normal)
        //{
        //    Vector3 v3;
        //    if (normal.normalized != Vector3.forward)
        //        v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        //    else
        //        v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;
        //    var corner0 = position + v3;
        //    var corner2 = position - v3;
        //    var q = Quaternion.AngleAxis(90.0f, normal);
        //    v3 = q * v3;
        //    var corner1 = position + v3;
        //    var corner3 = position - v3;
        //    Debug.DrawLine(corner0, corner2, Color.green);
        //    Debug.DrawLine(corner1, corner3, Color.green);
        //    Debug.DrawLine(corner0, corner1, Color.green);
        //    Debug.DrawLine(corner1, corner2, Color.green);
        //    Debug.DrawLine(corner2, corner3, Color.green);
        //    Debug.DrawLine(corner3, corner0, Color.green);
        //    Debug.DrawRay(position, normal, Color.red);
        //}


        public static Mesh CreateFullscreenMesh()
        {
            Vector3[] positions =
            {
                new Vector3(-1.0f,  -1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
            };
            Vector2[] uvs = {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1),
            };
            int[] indices = { 0, 2, 1, 1, 2, 3 };
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;
            mesh.uv = uvs;
            return mesh;
        }
    }
}

