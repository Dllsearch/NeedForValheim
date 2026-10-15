// Array-only mesh adapter: these tests execute deformation, not Unity rendering.
using System;
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public Vector3 normalized {get{float n=(float)Math.Sqrt(x*x+y*y+z*z);return n>0?new Vector3(x/n,y/n,z/n):new Vector3();}}
}
public static class Mathf {public static float Sqrt(float x)=>(float)Math.Sqrt(x);}
public class Mesh {
    public Vector3[] vertices,normals;
    public void RecalculateTangents(){}
    public void RecalculateBounds(){}
}
}
