using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo3D {
    public sealed class HullSurface3D : MonoBehaviour {
        public MeshFilter frontFilter,backFilter;
        public MeshRenderer frontRenderer,backRenderer;
        public LineRenderer segment;
        Mesh mesh;MaterialPropertyBlock block;
        readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>(),barycentric=new List<Vector3>(),heights=new List<Vector3>();
        readonly List<int> indices=new List<int>();
        public void Display(HullResult3D hull,Color faceColor,Color edgeColor,float width,List<Color> faceVertexColors=null) {
            if(mesh==null){mesh=new Mesh{name="3D convex hull runtime surface"};mesh.MarkDynamic();frontFilter.sharedMesh=backFilter.sharedMesh=mesh;}
            WriteMesh(mesh,hull,vertices,normals,barycentric,heights,indices);
            if(faceVertexColors!=null)mesh.SetColors(faceVertexColors);
            SetStyle(faceColor,edgeColor,width);
            block.SetFloat("_UseVertexColors",faceVertexColors!=null?1:0);frontRenderer.SetPropertyBlock(block);backRenderer.SetPropertyBlock(block);
            segment.positionCount=hull.Dimension==HullDimension.Segment?2:0;
            if(segment.positionCount==2){segment.SetPosition(0,hull.Vertices[0]);segment.SetPosition(1,hull.Vertices[1]);}
        }
        public void SetStyle(Color faceColor,Color edgeColor,float width) {
            if(block==null)block=new MaterialPropertyBlock();block.SetColor("_FaceColor",faceColor);block.SetColor("_EdgeColor",edgeColor);block.SetFloat("_EdgeWidth",width);
            frontRenderer.SetPropertyBlock(block);backRenderer.SetPropertyBlock(block);segment.startColor=segment.endColor=edgeColor;segment.widthMultiplier=width*2;
        }
        public static Mesh CreatePreview(HullResult3D hull) {
            var mesh=new Mesh{name="Initial 3D convex hull"};WriteMesh(mesh,hull,new List<Vector3>(),new List<Vector3>(),new List<Vector3>(),new List<Vector3>(),new List<int>());return mesh;
        }
        static void WriteMesh(Mesh target,HullResult3D hull,List<Vector3> v,List<Vector3> n,List<Vector3> b,List<Vector3> h,List<int> t) {
            v.Clear();n.Clear();b.Clear();h.Clear();t.Clear();
            for(int i=0;i<hull.Triangles.Length;i+=3) {
                var a=hull.Vertices[hull.Triangles[i]];var p=hull.Vertices[hull.Triangles[i+1]];var c=hull.Vertices[hull.Triangles[i+2]];
                var cross=Vector3.Cross(p-a,c-a);float area=cross.magnitude;var normal=cross.normalized;
                var altitude=new Vector3(area/Mathf.Max(1e-20f,(p-c).magnitude),area/Mathf.Max(1e-20f,(a-c).magnitude),area/Mathf.Max(1e-20f,(a-p).magnitude));
                v.Add(a);v.Add(p);v.Add(c);for(int j=0;j<3;j++){n.Add(normal);h.Add(altitude);t.Add(t.Count);}b.Add(Vector3.right);b.Add(Vector3.up);b.Add(Vector3.forward);
            }
            target.Clear();target.indexFormat=v.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
            target.SetVertices(v);target.SetNormals(n);target.SetUVs(0,b);target.SetUVs(1,h);target.SetTriangles(t,0);target.RecalculateBounds();
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
