using System.Collections.Generic;
using ConvexHullDemo;
using ConvexHullDemo3D;
using UnityEngine;
using UnityEngine.UI;
namespace VisualAlgo.StartMenu {
    /// <summary>独立的按钮点集：点运动时重新求凸包，不修改演示场景。</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HullMenuPreview : MaskableGraphic {
        public bool spatial;
        public Color movingPointColor=new Color(.85f,.08f,.48f);
        public Color interiorColor=new Color(.16f,.17f,.18f);
        [SerializeField,Range(0,1)] float progress;
        public float Progress {get=>progress;set{float next=Mathf.Clamp01(value);if(next==progress)return;progress=next;Rebuild();SetVerticesDirty();}}
        public Vector2 MovingPoint2D=>Vector2.Lerp(new Vector2(.60f,.45f),new Vector2(.95f,.88f),progress);
        public Vector3 MovingPoint3D=>Vector3.Lerp(Vector3.zero,new Vector3(1.1f,.45f,0),progress);
        readonly Vector2[] planar=new Vector2[9];readonly Vector3[] space=new Vector3[9];
        readonly ConvexHull3D.Solver solver=new ConvexHull3D.Solver();
        List<Vector2> boundary;HullResult3D hull;
        public IReadOnlyList<Vector2> PlanarBoundary {get{Ensure();return boundary;}}
        public HullResult3D SpatialHull {get{Ensure();return hull;}}
        void Ensure(){if(spatial?hull==null:boundary==null)Rebuild();}
        void Rebuild(){
            if(spatial){int i=0;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)space[i++]=new Vector3(x,y,z)*.5f;space[8]=MovingPoint3D;hull=solver.Compute(space);}
            else {planar[0]=new Vector2(.12f,.2f);planar[1]=new Vector2(.2f,.75f);planar[2]=new Vector2(.56f,.9f);planar[3]=new Vector2(.88f,.65f);planar[4]=new Vector2(.82f,.18f);planar[5]=new Vector2(.44f,.08f);planar[6]=new Vector2(.4f,.5f);planar[7]=new Vector2(.65f,.35f);planar[8]=MovingPoint2D;boundary=ConvexHull2D.Compute(planar);}
        }
        protected override void OnEnable(){base.OnEnable();Rebuild();}
#if UNITY_EDITOR
        protected override void OnValidate(){base.OnValidate();Rebuild();SetVerticesDirty();}
#endif
        Vector2 P(Vector2 p){var r=rectTransform.rect;return r.min+Vector2.Scale(p,r.size);}
        Vector2 Project(Vector3 p)=>P(new Vector2(.5f+.4f*(p.x-.65f*p.z),.48f+.35f*(p.y+.35f*p.x+.35f*p.z)));
        static void Tri(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint){int n=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color tint){var d=b-a;var normal=new Vector2(-d.y,d.x).normalized;Tri(vh,a+normal,b+normal,b-normal,tint);Tri(vh,a+normal,b-normal,a-normal,tint);}
        static void Dot(VertexHelper vh,Vector2 p,float radius,Color tint){for(int i=0;i<20;i++){float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;Tri(vh,p,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint);}}
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();Ensure();var fill=color;fill.a*=.07f;
            if(!spatial){for(int i=1;i<boundary.Count-1;i++)Tri(vh,P(boundary[0]),P(boundary[i]),P(boundary[i+1]),fill);for(int i=0;i<boundary.Count;i++)Line(vh,P(boundary[i]),P(boundary[(i+1)%boundary.Count]),color);for(int i=0;i<planar.Length;i++)Dot(vh,P(planar[i]),i==8?5:4,i==8?movingPointColor:boundary.Contains(planar[i])?color:interiorColor);Dot(vh,P(planar[8]),2,Color.white);return;}
            // 共面三角形的内部对角线不画，只显示立体凸包的真实棱边。
            var normals=new Dictionary<(int,int),Vector3>();var visible=new HashSet<(int,int)>();
            for(int i=0;i<hull.Triangles.Length;i+=3){int a=hull.Triangles[i],b=hull.Triangles[i+1],c=hull.Triangles[i+2];var va=hull.Vertices[a];var vb=hull.Vertices[b];var vc=hull.Vertices[c];Tri(vh,Project(va),Project(vb),Project(vc),fill);var normal=Vector3.Cross(vb-va,vc-va).normalized;
                Edge(a,b,normal);Edge(b,c,normal);Edge(c,a,normal);}
            void Edge(int a,int b,Vector3 n){var key=a<b?(a,b):(b,a);if(normals.TryGetValue(key,out var old)){if(Vector3.Dot(old,n)<.9999f)visible.Add(key);}else normals.Add(key,n);}
            foreach(var edge in visible)Line(vh,Project(hull.Vertices[edge.Item1]),Project(hull.Vertices[edge.Item2]),color);
            for(int i=0;i<space.Length;i++){bool onHull=System.Array.IndexOf(hull.SourceIndices,i)>=0;Dot(vh,Project(space[i]),i==8?5:4,i==8?movingPointColor:onHull?color:interiorColor);}Dot(vh,Project(space[8]),2,Color.white);
        }
    }
}
