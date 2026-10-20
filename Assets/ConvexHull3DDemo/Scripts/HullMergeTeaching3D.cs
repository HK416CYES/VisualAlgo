using System;
using System.Collections.Generic;
using UnityEngine;
namespace ConvexHullDemo3D {
    /// <summary>递归树中的一个子问题。Vertices 仅包含子凸包极点，邻接表不会包含内部点。</summary>
    public sealed class MergeGroup3D {
        public int Id,Parent=-1,Depth,Left=-1,Right=-1;
        public int[] Points,Vertices;
        public HullDimension Dimension;
        public Bounds Bounds;
        public float SplitX;
        public int[] ProjectionLeft,ProjectionRight;
        public bool ProjectionXZ;
        public bool Visible,Completed;
        public readonly List<TeachingFace3D> Faces=new List<TeachingFace3D>();
        public readonly Dictionary<int,List<int>> Neighbors=new Dictionary<int,List<int>>();
        public void BuildNeighbors(){Neighbors.Clear();foreach(int v in Vertices)Neighbors[v]=new List<int>();
            if(Dimension==HullDimension.Segment){Link(Vertices[0],Vertices[1]);return;}
            foreach(var face in Faces)for(int i=0;i<face.Vertices.Length;i++)Link(face.Vertices[i],face.Vertices[(i+1)%face.Vertices.Length]);
            foreach(var list in Neighbors.Values)list.Sort();
        }
        void Link(int a,int b){if(a==b)return;if(!Neighbors[a].Contains(b))Neighbors[a].Add(b);if(!Neighbors[b].Contains(a))Neighbors[b].Add(a);}
    }
    public sealed partial class HullTeachingSession3D {
        readonly MergeHullDetail detail;
        public readonly List<MergeGroup3D> MergeGroups=new List<MergeGroup3D>();
        public int[] PointGroups{get;private set;}
        public long NeighborComparisons{get;private set;}
        public long ValidationComparisons{get;private set;}
        MergeGroup3D MakeGroup(int[] ids,int parent,int depth){var group=new MergeGroup3D{Id=MergeGroups.Count,Parent=parent,Depth=depth,Points=ids};group.Bounds=new Bounds(Geometry.Positions[ids[0]],Vector3.zero);foreach(int id in ids)group.Bounds.Encapsulate(Geometry.Positions[id]);MergeGroups.Add(group);return group;}
        TeachingEvent3D MergeEvent(TeachingEventKind3D kind,MergeGroup3D group,string message){return new TeachingEvent3D(kind,message){Group=group.Id,LeftGroup=group.Left,RightGroup=group.Right,Depth=group.Depth,LabelPosition=group.Bounds.center};}
        IEnumerable<TeachingEvent3D> MergeRun(){
            PointGroups=new int[Geometry.Positions.Count];for(int i=0;i<PointGroups.Length;i++)PointGroups[i]=-1;
            if(PointGroups.Length==0)yield break;
            int[] ids=new int[PointGroups.Length];for(int i=0;i<ids.Length;i++)ids[i]=i;
            var root=MakeGroup(ids,-1,0);foreach(var step in SolveGroup(root))yield return step;
            faces.Clear();faces.AddRange(root.Faces);
        }
        IEnumerable<TeachingEvent3D> SolveGroup(MergeGroup3D group){
            // 这里只检测维度，并直接构造最多四点的基本形状；不调用实时凸包算法。
            var coordinates=new List<Vector3>();foreach(int id in group.Points){coordinates.Add(Geometry.Positions[id]);PointGroups[id]=group.Id;yield return null;}
            double localScale=group.Bounds.size.magnitude;var local=new TeachingGeometry3D(coordinates,Geometry.Epsilon*Geometry.Scale/Math.Max(1e-20,localScale));
            group.Dimension=local.Dimension;
            if(group.Points.Length<=4||local.Dimension!=HullDimension.Volume){
                var localFaces=local.Dimension==HullDimension.Volume?local.Tetrahedron():new List<TeachingFace3D>();
                if(local.Dimension==HullDimension.Planar){var ids=local.DegenerateBoundary;localFaces.Add(local.Polygon(ids,TeachingVector3D.Cross(local.Points[ids[1]]-local.Points[ids[0]],local.Points[ids[2]]-local.Points[ids[0]]).Unit));}
                foreach(var f in localFaces){var mapped=new int[f.Vertices.Length];for(int i=0;i<mapped.Length;i++)mapped[i]=group.Points[local.SourceIndices[f.Vertices[i]]];var face=Geometry.Polygon(mapped,f.Normal);face.Group=group.Id;group.Faces.Add(face);}
                var vertices=new SortedSet<int>();foreach(var face in group.Faces)foreach(int id in face.Vertices)vertices.Add(id);
                if(group.Faces.Count==0)foreach(int id in local.DegenerateBoundary)vertices.Add(group.Points[local.SourceIndices[id]]);
                group.Vertices=new int[vertices.Count];vertices.CopyTo(group.Vertices);group.BuildNeighbors();group.Visible=group.Completed=true;
                foreach(int id in group.Points)if(!vertices.Contains(id))PointGroups[id]=-2;
                var leaf=MergeEvent(TeachingEventKind3D.Leaf,group,local.Dimension==HullDimension.Planar?"共面子组：直接显示面域":local.Dimension==HullDimension.Segment?"共线子组：保留端点":"基本子凸包完成");leaf.Added=group.Faces.ToArray();yield return leaf;yield break;
            }
            int split=-1,best=int.MaxValue;for(int i=1;i<group.Points.Length;i++){yield return null;if(Geometry.Positions[group.Points[i-1]].x==Geometry.Positions[group.Points[i]].x)continue;int imbalance=Math.Abs(group.Points.Length-2*i);if(imbalance<best){best=imbalance;split=i;}}
            if(split<0)throw new InvalidOperationException("非共面子组没有有效 X 分割位置");
            var leftIds=new int[split];var rightIds=new int[group.Points.Length-split];Array.Copy(group.Points,0,leftIds,0,split);Array.Copy(group.Points,split,rightIds,0,rightIds.Length);
            var left=MakeGroup(leftIds,group.Id,group.Depth+1);var right=MakeGroup(rightIds,group.Id,group.Depth+1);group.Left=left.Id;group.Right=right.Id;
            group.SplitX=(float)(((double)Geometry.Positions[leftIds[leftIds.Length-1]].x+Geometry.Positions[rightIds[0]].x)*.5);
            foreach(int id in leftIds)PointGroups[id]=left.Id;foreach(int id in rightIds)PointGroups[id]=right.Id;
            yield return MergeEvent(TeachingEventKind3D.Split,group,"按 X 划分左右子组");
            foreach(var step in SolveGroup(left))yield return step;foreach(var step in SolveGroup(right))yield return step;
            yield return MergeEvent(TeachingEventKind3D.MergeBegin,group,"合并左组与右组");
            foreach(var step in MergeChildren(group,left,right))yield return step;
        }
        IEnumerable<TeachingEvent3D> MergeChildren(MergeGroup3D group,MergeGroup3D left,MergeGroup3D right){
            var vertices=new List<int>(left.Vertices);vertices.AddRange(right.Vertices);vertices.Sort();var leftSet=new HashSet<int>(left.Vertices);
            bool CrossGroup(int a,int b)=>leftSet.Contains(a)!=leftSet.Contains(b);
            // 投影只用于寻找支撑边。随后所有面判断都在三维中进行。
            var projectionNormal=new TeachingVector3D(0,0,1);var outline=Geometry.PlanarBoundary(vertices,projectionNormal);
            if(outline.Length<3){projectionNormal=new TeachingVector3D(0,1,0);outline=Geometry.PlanarBoundary(vertices,projectionNormal);}
            group.ProjectionXZ=projectionNormal.Y!=0;group.ProjectionLeft=Geometry.PlanarBoundary(left.Vertices,projectionNormal);group.ProjectionRight=Geometry.PlanarBoundary(right.Vertices,projectionNormal);
            yield return MergeEvent(TeachingEventKind3D.Projection,group,group.ProjectionXZ?"观察 XZ 投影，寻找公共支撑边":"观察 XY 投影，寻找公共支撑边");
            int seedA=-1,seedB=-1;for(int i=0;i<outline.Length;i++){int a=outline[i],b=outline[(i+1)%outline.Length];yield return null;if(CrossGroup(a,b)){seedA=a;seedB=b;break;}}
            if(seedA<0)throw new InvalidOperationException("未找到跨组投影支撑边");
            var direction=Geometry.Points[seedB]-Geometry.Points[seedA];var supportNormal=TeachingVector3D.Cross(direction,projectionNormal).Unit;
            var support=Geometry.Polygon(new[]{seedA,seedB},supportNormal);
            var seed=MergeEvent(TeachingEventKind3D.Horizon,group,"投影公共支撑边 → 三维桥接边");seed.Edges=new[]{seedA,seedB};yield return seed;
            var queue=new Queue<(int a,int b,TeachingFace3D owner)>();queue.Enqueue((seedB,seedA,support));
            var counts=new Dictionary<TeachingEdge3D,int>();var facetKeys=new HashSet<string>();var bridge=new List<TeachingFace3D>();
            while(queue.Count>0){
                var work=queue.Dequeue();var key=new TeachingEdge3D(work.a,work.b);if(counts.TryGetValue(key,out int count)&&count>=2)continue;
                int a=work.b,b=work.a;var neighbors=new SortedSet<int>();
                foreach(int id in leftSet.Contains(a)?left.Neighbors[a]:right.Neighbors[a])neighbors.Add(id);
                foreach(int id in leftSet.Contains(b)?left.Neighbors[b]:right.Neighbors[b])neighbors.Add(id);
                // 点子凸包没有邻点；另一端的邻接候选仍足以找到相邻支撑面。
                int candidate=-1;foreach(int id in neighbors){NeighborComparisons++;yield return null;if(Geometry.Distance(work.owner,id)<-Geometry.Epsilon){candidate=id;break;}}
                if(candidate<0)throw new InvalidOperationException("桥接边缺少非共面邻接候选");
                var current=Geometry.Triangle(a,b,candidate,false);bool rejectedShown=false;
                foreach(int id in neighbors){if(id==a||id==b||id==candidate)continue;NeighborComparisons++;double distance=Geometry.Distance(current,id);bool replace=distance>Geometry.Epsilon;
                    bool show=replace||detail==MergeHullDetail.Detailed||!rejectedShown;
                    if(show){var compare=MergeEvent(TeachingEventKind3D.Compare,group,replace?"检查点在旧面外侧，需要更新":"检查点在面内侧，保留候选");compare.Point=id;compare.Candidate=candidate;compare.Preview=current;compare.Edges=new[]{a,b};compare.Value=distance*Geometry.Scale;yield return compare;}
                    else yield return null;
                    if(replace){var next=Geometry.Triangle(a,b,id,false);var rotate=MergeEvent(TeachingEventKind3D.Rotate,group,"绕固定桥接边更新候选面");rotate.Point=id;rotate.Candidate=id;rotate.PreviousPreview=current;rotate.Preview=next;rotate.Edges=new[]{a,b};yield return rotate;candidate=id;current=next;}
                    else rejectedShown=true;
                }
                var coplanar=new List<int>();foreach(int id in vertices){ValidationComparisons++;double distance=Geometry.Distance(current,id);yield return null;if(distance>Geometry.Epsilon*4)throw new InvalidOperationException("邻接搜索未得到全局支撑面");if(Math.Abs(distance)<=Geometry.Epsilon*4)coplanar.Add(id);}
                var polygon=Geometry.PlanarBoundary(coplanar,current.Normal);var sorted=(int[])polygon.Clone();Array.Sort(sorted);string facetKey=string.Join(",",sorted);
                if(!facetKeys.Add(facetKey))throw new InvalidOperationException("桥接面带重复访问未闭合面");
                var face=Geometry.Polygon(polygon,current.Normal);face.Group=group.Id;face.Bridge=true;bridge.Add(face);
                var add=MergeEvent(TeachingEventKind3D.Add,group,"确认一个桥接面");add.Added=new[]{face};add.Edges=new[]{a,b};yield return add;
                yield return MergeEvent(TeachingEventKind3D.Commit,group,"沿下一条跨组边继续包装");
                for(int i=0;i<polygon.Length;i++){int x=polygon[i],y=polygon[(i+1)%polygon.Length];if(!CrossGroup(x,y))continue;var edge=new TeachingEdge3D(x,y);counts.TryGetValue(edge,out int seen);counts[edge]=seen+1;if(seen==0)queue.Enqueue((x,y,face));}
            }
            foreach(var pair in counts)if(pair.Value!=2)throw new InvalidOperationException("桥接面带未闭合");
            var close=MergeEvent(TeachingEventKind3D.Close,group,"桥接面带闭合");var beltEdges=new List<int>();foreach(var pair in counts){beltEdges.Add(pair.Key.A);beltEdges.Add(pair.Key.B);}close.Edges=beltEdges.ToArray();yield return close;
            var retained=new List<TeachingFace3D>();var removed=new List<int>();var reversed=new List<TeachingFace3D>();
            foreach(var child in new[]{left,right})foreach(var f in child.Faces){bool positive=false,negative=false;foreach(int id in vertices){ValidationComparisons++;double distance=Geometry.Distance(f,id);positive|=distance>Geometry.Epsilon*4;negative|=distance<-Geometry.Epsilon*4;yield return null;}
                bool sameBridge=false;foreach(var added in bridge)if(Math.Abs(Math.Abs(TeachingVector3D.Dot(f.Normal,added.Normal))-1)<1e-12&&Math.Abs(Geometry.Distance(added,f.Vertices[0]))<=Geometry.Epsilon*4){sameBridge=true;break;}
                if(sameBridge||positive&&(negative||child.Dimension==HullDimension.Volume)){removed.Add(f.Id);continue;}
                if(positive){var ids=(int[])f.Vertices.Clone();Array.Reverse(ids);var flip=Geometry.Polygon(ids,f.Normal*(-1));flip.Group=group.Id;retained.Add(flip);reversed.Add(flip);removed.Add(f.Id);}else retained.Add(f);
            }
            var remove=MergeEvent(TeachingEventKind3D.Remove,group,"红色旧面进入内部，将一次性移除");remove.Removed=removed.ToArray();yield return remove;
            foreach(var f in retained){f.Group=group.Id;f.Bridge=false;}foreach(var f in bridge){f.Group=group.Id;f.Bridge=false;}
            group.Faces.AddRange(retained);group.Faces.AddRange(bridge);
            // 桥接面按完整共面域生成，因此保留面不必消失再重建。
            var extreme=new SortedSet<int>();foreach(var f in group.Faces)foreach(int id in f.Vertices)extreme.Add(id);group.Vertices=new int[extreme.Count];extreme.CopyTo(group.Vertices);group.Dimension=HullDimension.Volume;group.Visible=group.Completed=true;group.BuildNeighbors();left.Visible=right.Visible=false;
            foreach(int id in group.Points)PointGroups[id]=extreme.Contains(id)?group.Id:-2;
            var done=MergeEvent(TeachingEventKind3D.MergeDone,group,group.Parent<0?"最终凸包完成":"归并完成，返回上一层");done.Removed=removed.ToArray();done.Added=reversed.ToArray();yield return done;
        }
    }
}
