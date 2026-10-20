using System;
using System.Collections.Generic;
using UnityEngine;
namespace ConvexHullDemo3D {
    public enum TeachingAlgorithm3D { Incremental, QuickHull, GiftWrapping, MergeHull }
    public enum MergeHullDetail { [InspectorName("关键步骤")] KeySteps, [InspectorName("详细步骤")] Detailed }
    public enum TeachingEventKind3D { Prepare, Inspect, Candidate, OutsideSet, Reject, Horizon, Remove, Add, Commit, Complete, Split, Leaf, MergeBegin, Compare, Rotate, Close, MergeDone, Projection }
    public sealed class TeachingEvent3D {
        public TeachingEventKind3D Kind;
        public string Message;
        public int Point=-1, Candidate=-1, Face=-1;
        public double Value;
        public TeachingFace3D Preview, PreviousPreview;
        public int Group=-1,LeftGroup=-1,RightGroup=-1,Depth;
        public Vector3 LabelPosition;
        public TeachingFace3D[] Added;
        public int[] Removed, Edges;
        public TeachingEvent3D(TeachingEventKind3D kind,string message){Kind=kind;Message=message;}
    }
    public readonly struct TeachingVector3D {
        public readonly double X,Y,Z;
        public TeachingVector3D(double x,double y,double z){X=x;Y=y;Z=z;}
        public static TeachingVector3D operator +(TeachingVector3D a,TeachingVector3D b)=>new TeachingVector3D(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static TeachingVector3D operator -(TeachingVector3D a,TeachingVector3D b)=>new TeachingVector3D(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static TeachingVector3D operator *(TeachingVector3D a,double t)=>new TeachingVector3D(a.X*t,a.Y*t,a.Z*t);
        public double Length2=>Dot(this,this);
        public TeachingVector3D Unit=>this*(1/Math.Sqrt(Length2));
        public static double Dot(TeachingVector3D a,TeachingVector3D b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        public static TeachingVector3D Cross(TeachingVector3D a,TeachingVector3D b)=>new TeachingVector3D(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        public Vector3 Float=>new Vector3((float)X,(float)Y,(float)Z);
    }
    public sealed class TeachingFace3D {
        public readonly int Id;
        public int Group=-1;
        public bool Bridge;
        public readonly int[] Vertices;
        public readonly TeachingVector3D Normal;
        public readonly double Offset;
        public readonly List<int> Outside=new List<int>();
        public TeachingFace3D(int id,int[] vertices,TeachingVector3D normal,double offset){Id=id;Vertices=vertices;Normal=normal;Offset=offset;}
    }
    public readonly struct TeachingEdge3D : IEquatable<TeachingEdge3D> {
        public readonly int A,B;
        public TeachingEdge3D(int a,int b){A=Math.Min(a,b);B=Math.Max(a,b);}
        public bool Equals(TeachingEdge3D other)=>A==other.A&&B==other.B;
        public override bool Equals(object o)=>o is TeachingEdge3D edge&&Equals(edge);
        public override int GetHashCode()=>unchecked(A*397^B);
    }
    /// <summary>教学算法共用的几何谓词，不调用实时 ConvexHull3D.Compute，也不读实时答案。</summary>
    public sealed class TeachingGeometry3D {
        public readonly List<Vector3> Positions=new List<Vector3>();
        public readonly List<int> SourceIndices=new List<int>();
        public readonly List<TeachingVector3D> Points=new List<TeachingVector3D>();
        public readonly double Epsilon;
        public double Scale{get;private set;}
        public HullDimension Dimension{get;private set;}
        public int A,B,C,D;
        public TeachingVector3D Interior;
        public int[] DegenerateBoundary;
        int nextFace;
        public TeachingGeometry3D(IReadOnlyList<Vector3> input,double epsilon=1e-7,bool sorted=false){
            Epsilon=epsilon;var seen=new HashSet<Vector3>();
            for(int i=0;i<input.Count;i++){var p=input[i];if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))throw new ArgumentException("坐标必须为有限值");if(seen.Add(p)){Positions.Add(p);SourceIndices.Add(i);}}
            if(sorted){var order=new List<int>();for(int i=0;i<Positions.Count;i++)order.Add(i);order.Sort((a,b)=>Positions[a].x!=Positions[b].x?Positions[a].x.CompareTo(Positions[b].x):Positions[a].y!=Positions[b].y?Positions[a].y.CompareTo(Positions[b].y):Positions[a].z.CompareTo(Positions[b].z));var old=Positions.ToArray();var sources=SourceIndices.ToArray();for(int i=0;i<order.Count;i++){Positions[i]=old[order[i]];SourceIndices[i]=sources[order[i]];}}
            if(Positions.Count==0){Dimension=HullDimension.Empty;DegenerateBoundary=Array.Empty<int>();return;}
            var min=new TeachingVector3D(Positions[0].x,Positions[0].y,Positions[0].z);var max=min;
            foreach(var p in Positions){min=new TeachingVector3D(Math.Min(min.X,p.x),Math.Min(min.Y,p.y),Math.Min(min.Z,p.z));max=new TeachingVector3D(Math.Max(max.X,p.x),Math.Max(max.Y,p.y),Math.Max(max.Z,p.z));}
            var center=(min+max)*.5;Scale=Math.Sqrt((max-min).Length2);if(Scale==0)Scale=1;
            foreach(var p in Positions)Points.Add((new TeachingVector3D(p.x,p.y,p.z)-center)*(1/Scale));
            if(Points.Count==1){Dimension=HullDimension.Point;DegenerateBoundary=new[]{0};return;}
            // 最宽轴上的极值对 -> 离直线最远点 -> 离平面最远点，形成稳定初始单纯形。
            var span=max-min;int axis=span.X>=span.Y&&span.X>=span.Z?0:span.Y>=span.Z?1:2;
            for(int i=1;i<Points.Count;i++){if(Coordinate(Points[i],axis)<Coordinate(Points[A],axis))A=i;if(Coordinate(Points[i],axis)>Coordinate(Points[B],axis))B=i;}
            double best=-1;var ab=Points[B]-Points[A];for(int i=0;i<Points.Count;i++){double q=TeachingVector3D.Cross(ab,Points[i]-Points[A]).Length2/ab.Length2;if(q>best){best=q;C=i;}}
            if(best<=epsilon*epsilon){Dimension=HullDimension.Segment;DegenerateBoundary=new[]{A,B};return;}
            var normal=TeachingVector3D.Cross(ab,Points[C]-Points[A]).Unit;best=-1;
            for(int i=0;i<Points.Count;i++){double q=Math.Abs(TeachingVector3D.Dot(normal,Points[i]-Points[A]));if(q>best){best=q;D=i;}}
            if(best<=epsilon){Dimension=HullDimension.Planar;var ids=new List<int>();for(int i=0;i<Points.Count;i++)ids.Add(i);DegenerateBoundary=PlanarBoundary(ids,normal);return;}
            Dimension=HullDimension.Volume;Interior=(Points[A]+Points[B]+Points[C]+Points[D])*.25;
        }
        static double Coordinate(TeachingVector3D v,int axis)=>axis==0?v.X:axis==1?v.Y:v.Z;
        public double Distance(TeachingFace3D face,int point)=>TeachingVector3D.Dot(face.Normal,Points[point])-face.Offset;
        public TeachingFace3D Triangle(int a,int b,int c,bool orient=true){
            var normal=TeachingVector3D.Cross(Points[b]-Points[a],Points[c]-Points[a]);if(normal.Length2<1e-28)return null;normal=normal.Unit;
            if(orient&&TeachingVector3D.Dot(normal,Interior-Points[a])>0){int swap=b;b=c;c=swap;normal=normal*(-1);}
            return new TeachingFace3D(nextFace++,new[]{a,b,c},normal,TeachingVector3D.Dot(normal,Points[a]));
        }
        public TeachingFace3D Polygon(int[] vertices,TeachingVector3D normal)=>new TeachingFace3D(nextFace++,vertices,normal,TeachingVector3D.Dot(normal,Points[vertices[0]]));
        public List<TeachingFace3D> Tetrahedron()=>new List<TeachingFace3D>{Triangle(A,B,C),Triangle(A,D,B),Triangle(B,D,C),Triangle(C,D,A)};
        // 共面点投影到局部二维坐标，以单调链只保留面域的几何极点。
        public int[] PlanarBoundary(IEnumerable<int> indices,TeachingVector3D normal){
            var helper=Math.Abs(normal.X)<.8?new TeachingVector3D(1,0,0):new TeachingVector3D(0,1,0);
            var u=TeachingVector3D.Cross(helper,normal).Unit;var v=TeachingVector3D.Cross(normal,u);
            var projected=new List<(int id,double x,double y)>();foreach(var i in indices)projected.Add((i,TeachingVector3D.Dot(Points[i],u),TeachingVector3D.Dot(Points[i],v)));
            projected.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y!=b.y?a.y.CompareTo(b.y):a.id.CompareTo(b.id));
            if(projected.Count<=2){var small=new int[projected.Count];for(int i=0;i<small.Length;i++)small[i]=projected[i].id;return small;}
            double Cross(int a,int b,int c)=>(projected[b].x-projected[a].x)*(projected[c].y-projected[a].y)-(projected[b].y-projected[a].y)*(projected[c].x-projected[a].x);
            var chain=new List<int>();for(int i=0;i<projected.Count;i++){while(chain.Count>=2&&Cross(chain[chain.Count-2],chain[chain.Count-1],i)<=Epsilon*Epsilon)chain.RemoveAt(chain.Count-1);chain.Add(i);}int lower=chain.Count;
            for(int i=projected.Count-2;i>=0;i--){while(chain.Count>lower&&Cross(chain[chain.Count-2],chain[chain.Count-1],i)<=Epsilon*Epsilon)chain.RemoveAt(chain.Count-1);chain.Add(i);}chain.RemoveAt(chain.Count-1);
            var result=new int[chain.Count];for(int i=0;i<result.Length;i++)result[i]=projected[chain[i]].id;return result;
        }
        public List<TeachingFace3D> Consolidate(IReadOnlyList<TeachingFace3D> faces){
            var result=new List<TeachingFace3D>();var used=new bool[faces.Count];
            for(int i=0;i<faces.Count;i++)if(!used[i]){var f=faces[i];var ids=new HashSet<int>();for(int j=i;j<faces.Count;j++)if(!used[j]&&TeachingVector3D.Dot(f.Normal,faces[j].Normal)>1-1e-12&&Math.Abs(f.Offset-faces[j].Offset)<=Epsilon*4){used[j]=true;foreach(int p in faces[j].Vertices)ids.Add(p);}result.Add(Polygon(PlanarBoundary(ids,f.Normal),f.Normal));}return result;
        }
        public HullResult3D Result(IReadOnlyList<TeachingFace3D> faces){
            var ids=new HashSet<int>();foreach(var f in faces)foreach(int p in f.Vertices)ids.Add(p);if(faces.Count==0&&DegenerateBoundary!=null)foreach(int p in DegenerateBoundary)ids.Add(p);
            var order=new List<int>(ids);order.Sort();var vertices=new Vector3[order.Count];var sources=new int[order.Count];var map=new Dictionary<int,int>();
            for(int i=0;i<order.Count;i++){vertices[i]=Positions[order[i]];sources[i]=SourceIndices[order[i]];map.Add(order[i],i);}var triangles=new List<int>();foreach(var f in faces)for(int i=1;i<f.Vertices.Length-1;i++){triangles.Add(map[f.Vertices[0]]);triangles.Add(map[f.Vertices[i]]);triangles.Add(map[f.Vertices[i+1]]);}
            return new HullResult3D{Dimension=Dimension,Vertices=vertices,SourceIndices=sources,Triangles=triangles.ToArray()};
        }
    }
}
