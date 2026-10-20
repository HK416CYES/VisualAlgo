using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo3D {
    public enum HullDimension { Empty=-1, Point=0, Segment=1, Planar=2, Volume=3 }
    public sealed class HullResult3D {
        public HullDimension Dimension { get; internal set; }
        public Vector3[] Vertices { get; internal set; } = Array.Empty<Vector3>();
        public int[] SourceIndices { get; internal set; } = Array.Empty<int>();
        public int[] Triangles { get; internal set; } = Array.Empty<int>();
    }
    /// <summary>独立三维 QuickHull。输入不会被修改；重复坐标保留最早输入的代表。</summary>
    public static class ConvexHull3D {
        public static HullResult3D Compute(IReadOnlyList<Vector3> input,double tolerance=1e-7) => new Solver().Compute(input,tolerance);
        internal readonly struct D {
            public readonly double X,Y,Z;
            public D(double x,double y,double z){X=x;Y=y;Z=z;}
            public static D operator +(D a,D b)=>new D(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
            public static D operator -(D a,D b)=>new D(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
            public static D operator *(D a,double b)=>new D(a.X*b,a.Y*b,a.Z*b);
            public double Length2=>Dot(this,this);
            public D Unit=>this*(1/Math.Sqrt(Length2));
            public static double Dot(D a,D b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
            public static D Cross(D a,D b)=>new D(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        }
        readonly struct Edge : IEquatable<Edge> {
            public readonly int A,B;
            public Edge(int a,int b){A=Math.Min(a,b);B=Math.Max(a,b);}
            public bool Equals(Edge b)=>A==b.A&&B==b.B;
            public override bool Equals(object o)=>o is Edge e&&Equals(e);
            public override int GetHashCode()=>unchecked(A*397^B);
        }
        sealed class Face {
            public int A,B,C,Farthest=-1;public double Offset,Maximum;public D Normal;public bool Visible;
            public readonly List<int> Outside=new List<int>();
        }
        struct Projected {public int Index;public double X,Y;}
        /// <summary>控制器复用 Solver，面及其外点容器进入对象池，减少连续拖动时的分配。</summary>
        public sealed class Solver {
            readonly List<Vector3> original=new List<Vector3>();
            readonly List<int> source=new List<int>(),orphans=new List<int>(),triangles=new List<int>();
            readonly List<D> points=new List<D>();
            readonly Dictionary<Vector3,int> unique=new Dictionary<Vector3,int>();
            readonly List<Face> faces=new List<Face>(),newFaces=new List<Face>();
            readonly Stack<Face> facePool=new Stack<Face>();
            readonly Dictionary<Edge,(int a,int b)> horizon=new Dictionary<Edge,(int,int)>();
            readonly Dictionary<Edge,int> edgeOwners=new Dictionary<Edge,int>();
            readonly List<Projected> projected=new List<Projected>();
            readonly List<int> chain=new List<int>();
            readonly HashSet<int> facetVertices=new HashSet<int>();
            double epsilon;D interior;
            static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
            double Distance(Face f,int p)=>D.Dot(f.Normal,points[p])-f.Offset;
            void Release(Face f){f.Outside.Clear();facePool.Push(f);}
            Face Create(int a,int b,int c) {
                var normal=D.Cross(points[b]-points[a],points[c]-points[a]);
                if(normal.Length2<1e-28)throw new InvalidOperationException("Degenerate QuickHull horizon face.");
                normal=normal.Unit;
                if(D.Dot(normal,interior-points[a])>0){int swap=b;b=c;c=swap;normal=normal*(-1);}
                var f=facePool.Count>0?facePool.Pop():new Face();
                f.A=a;f.B=b;f.C=c;f.Normal=normal;f.Offset=D.Dot(normal,points[a]);f.Farthest=-1;f.Maximum=epsilon;f.Visible=false;faces.Add(f);return f;
            }
            void Assign(int p,List<Face> candidates) {
                Face best=null;double distance=epsilon;
                foreach(var f in candidates){double d=Distance(f,p);if(d>distance){distance=d;best=f;}}
                if(best==null)return;best.Outside.Add(p);
                if(distance>best.Maximum){best.Maximum=distance;best.Farthest=p;}
            }
            void Boundary(int a,int b){var key=new Edge(a,b);if(!horizon.Remove(key))horizon.Add(key,(a,b));}
            public HullResult3D Compute(IReadOnlyList<Vector3> input,double tolerance=1e-7) {
                if(input==null)throw new ArgumentNullException(nameof(input));
                if(double.IsNaN(tolerance)||double.IsInfinity(tolerance)||tolerance<=0||tolerance>=.01)throw new ArgumentOutOfRangeException(nameof(tolerance));
                epsilon=tolerance;foreach(var f in faces)Release(f);faces.Clear();newFaces.Clear();original.Clear();source.Clear();points.Clear();unique.Clear();triangles.Clear();
                for(int i=0;i<input.Count;i++) {
                    var p=input[i];if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw new ArgumentException("Point coordinates must be finite.");
                    if(unique.ContainsKey(p))continue;unique.Add(p,original.Count);original.Add(p);source.Add(i);
                }
                if(original.Count==0)return new HullResult3D{Dimension=HullDimension.Empty};
                if(original.Count==1)return Build(HullDimension.Point,new[]{0});
                // 用 double 先居中再归一化；判断阈值由点集尺度决定，而非世界原点或屏幕像素。
                D min=new D(original[0].x,original[0].y,original[0].z),max=min;
                foreach(var p in original){min=new D(Math.Min(min.X,p.x),Math.Min(min.Y,p.y),Math.Min(min.Z,p.z));max=new D(Math.Max(max.X,p.x),Math.Max(max.Y,p.y),Math.Max(max.Z,p.z));}
                D center=(min+max)*.5;double scale=Math.Sqrt((max-min).Length2);
                foreach(var p in original)points.Add((new D(p.x,p.y,p.z)-center)*(1/scale));
                int a=0,b=0,c=0,dIndex=0;
                // 选择最宽坐标轴的极值对，避免初始线段过短。
                D span=max-min;int axis=span.X>=span.Y&&span.X>=span.Z?0:span.Y>=span.Z?1:2;
                for(int i=1;i<points.Count;i++){if(Coordinate(points[i],axis)<Coordinate(points[a],axis))a=i;if(Coordinate(points[i],axis)>Coordinate(points[b],axis))b=i;}
                D ab=points[b]-points[a];double best=-1;
                for(int i=0;i<points.Count;i++){double distance=D.Cross(ab,points[i]-points[a]).Length2/ab.Length2;if(distance>best){best=distance;c=i;}}
                if(best<=epsilon*epsilon)return Build(HullDimension.Segment,new[]{a,b});
                D normal=D.Cross(ab,points[c]-points[a]).Unit;best=-1;
                for(int i=0;i<points.Count;i++){double distance=Math.Abs(D.Dot(normal,points[i]-points[a]));if(distance>best){best=distance;dIndex=i;}}
                if(best<=epsilon){var all=new List<int>();for(int i=0;i<points.Count;i++)all.Add(i);var polygon=PlanarHull(all,normal);Fan(polygon);return Build(HullDimension.Planar,polygon);}
                interior=(points[a]+points[b]+points[c]+points[dIndex])*.25;
                Create(a,b,c);Create(a,dIndex,b);Create(b,dIndex,c);Create(c,dIndex,a);
                for(int i=0;i<points.Count;i++)Assign(i,faces);
                int iterations=0;
                while(true) {
                    Face chosen=null;double farthest=epsilon;
                    foreach(var face in faces)if(face.Farthest>=0&&face.Maximum>farthest){chosen=face;farthest=face.Maximum;}
                    if(chosen==null)break;
                    if(++iterations>points.Count)throw new InvalidOperationException("QuickHull did not converge.");
                    int eye=chosen.Farthest;horizon.Clear();orphans.Clear();newFaces.Clear();
                    // 只移除从新极点能看见的面。可见面中只出现一次的边构成轮廓。
                    foreach(var face in faces) {
                        face.Visible=Distance(face,eye)>epsilon;
                        if(!face.Visible)continue;
                        Boundary(face.A,face.B);Boundary(face.B,face.C);Boundary(face.C,face.A);
                        foreach(int p in face.Outside)if(p!=eye)orphans.Add(p);
                    }
                    for(int i=faces.Count-1;i>=0;i--)if(faces[i].Visible){Release(faces[i]);faces.RemoveAt(i);}
                    foreach(var edge in horizon.Values)newFaces.Add(Create(edge.a,edge.b,eye));
                    foreach(int p in orphans)Assign(p,newFaces);
                }
                ConsolidateFacets();return Build(HullDimension.Volume,triangles);
            }
            static double Coordinate(D p,int axis)=>axis==0?p.X:axis==1?p.Y:p.Z;
            List<int> PlanarHull(IEnumerable<int> ids,D normal) {
                // 在当前面自己的二维坐标系中做单调链，保留极点并去掉共线边中间点。
                D helper=Math.Abs(normal.X)<.8?new D(1,0,0):new D(0,1,0);
                D u=D.Cross(helper,normal).Unit,v=D.Cross(normal,u);
                projected.Clear();foreach(int i in ids)projected.Add(new Projected{Index=i,X=D.Dot(points[i],u),Y=D.Dot(points[i],v)});
                projected.Sort((x,y)=>{int c=x.X.CompareTo(y.X);if(c==0)c=x.Y.CompareTo(y.Y);return c!=0?c:x.Index.CompareTo(y.Index);});
                chain.Clear();
                for(int i=0;i<projected.Count;i++){while(chain.Count>=2&&Turn(projected[chain[chain.Count-2]],projected[chain[chain.Count-1]],projected[i])<=epsilon*epsilon)chain.RemoveAt(chain.Count-1);chain.Add(i);}
                int lower=chain.Count;
                for(int i=projected.Count-2;i>=0;i--){while(chain.Count>lower&&Turn(projected[chain[chain.Count-2]],projected[chain[chain.Count-1]],projected[i])<=epsilon*epsilon)chain.RemoveAt(chain.Count-1);chain.Add(i);}
                if(chain.Count>1)chain.RemoveAt(chain.Count-1);
                var result=new List<int>(chain.Count);foreach(int i in chain)result.Add(projected[i].Index);return result;
            }
            static double Turn(Projected a,Projected b,Projected c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
            void Fan(List<int> polygon){for(int i=1;i+1<polygon.Count;i++){triangles.Add(polygon[0]);triangles.Add(polygon[i]);triangles.Add(polygon[i+1]);}}
            int Root(int[] parent,int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
            void ConsolidateFacets() {
                // 合并相邻共面三角形，再三角化面域；初始四面体中的非极点也因此被清除。
                int[] parent=new int[faces.Count];for(int i=0;i<parent.Length;i++)parent[i]=i;edgeOwners.Clear();
                for(int i=0;i<faces.Count;i++){var f=faces[i];Join(f.A,f.B,i,parent);Join(f.B,f.C,i,parent);Join(f.C,f.A,i,parent);}
                var patches=new Dictionary<int,List<int>>();
                for(int i=0;i<faces.Count;i++){int root=Root(parent,i);if(!patches.TryGetValue(root,out var list)){list=new List<int>();patches.Add(root,list);}list.Add(i);}
                foreach(var pair in patches){var list=pair.Value;var first=faces[list[0]];
                    if(list.Count==1){triangles.Add(first.A);triangles.Add(first.B);triangles.Add(first.C);continue;}
                    facetVertices.Clear();foreach(int i in list){var f=faces[i];facetVertices.Add(f.A);facetVertices.Add(f.B);facetVertices.Add(f.C);}
                    Fan(PlanarHull(facetVertices,first.Normal));
                }
            }
            void Join(int a,int b,int i,int[] parent){var key=new Edge(a,b);if(!edgeOwners.TryGetValue(key,out int j)){edgeOwners.Add(key,i);return;}
                var f=faces[i];var g=faces[j];if(D.Dot(f.Normal,g.Normal)>1-1e-12&&Math.Abs(f.Offset-g.Offset)<=epsilon*4)parent[Root(parent,i)]=Root(parent,j);
            }
            HullResult3D Build(HullDimension dimension,IEnumerable<int> used) {
                var uniqueIds=new HashSet<int>(used);var ids=new List<int>(uniqueIds);ids.Sort();
                var result=new HullResult3D{Dimension=dimension,Vertices=new Vector3[ids.Count],SourceIndices=new int[ids.Count],Triangles=new int[triangles.Count]};
                var mapping=new Dictionary<int,int>();for(int i=0;i<ids.Count;i++){mapping.Add(ids[i],i);result.Vertices[i]=original[ids[i]];result.SourceIndices[i]=source[ids[i]];}
                for(int i=0;i<triangles.Count;i++)result.Triangles[i]=mapping[triangles[i]];return result;
            }
        }
    }
}
