using System;
using System.Collections.Generic;
using UnityEngine;
namespace ConvexHullDemo3D {
    /// <summary>流式教学会话。null 是计算让步；非 null 是可见步骤，不预存所有比较。</summary>
    public sealed partial class HullTeachingSession3D : IDisposable {
        public readonly TeachingGeometry3D Geometry;
        public readonly TeachingAlgorithm3D Algorithm;
        public TeachingEvent3D Current{get;private set;}
        public HullResult3D Result{get;private set;}
        public bool Finished{get;private set;}
        // -2 尚待分配；-1 已被包含；-3 当前凸包顶点；非负数为唯一所属面 ID。
        public int[] PointOwners {get;private set;}
        public int VisibleSteps{get;private set;}
        public long WorkUnits{get;private set;}
        readonly IEnumerator<TeachingEvent3D> iterator;
        readonly List<TeachingFace3D> faces=new List<TeachingFace3D>();
        public HullTeachingSession3D(IReadOnlyList<Vector3> snapshot,TeachingAlgorithm3D algorithm,double tolerance=1e-7,MergeHullDetail mergeDetail=MergeHullDetail.KeySteps){detail=mergeDetail;Geometry=new TeachingGeometry3D(snapshot,tolerance,algorithm==TeachingAlgorithm3D.MergeHull);Algorithm=algorithm;PointOwners=new int[Geometry.Positions.Count];for(int i=0;i<PointOwners.Length;i++)PointOwners[i]=-2;iterator=Run().GetEnumerator();}
        public bool Advance(int budget=4096){if(Finished)return false;for(int i=0;i<Math.Max(1,budget);i++){WorkUnits++;if(!iterator.MoveNext()){Finished=true;return false;}if(iterator.Current==null)continue;Current=iterator.Current;VisibleSteps++;return true;}return false;}
        public void Dispose(){iterator.Dispose();Finished=true;}
        TeachingEvent3D Event(TeachingEventKind3D kind,string text,int point=-1,int face=-1)=>new TeachingEvent3D(kind,text){Point=point,Face=face};
        static int[] FaceIds(IEnumerable<TeachingFace3D> values){var ids=new List<int>();foreach(var f in values)ids.Add(f.Id);return ids.ToArray();}
        IEnumerable<TeachingEvent3D> Run(){
            yield return Event(TeachingEventKind3D.Prepare,$"快照去重：{Geometry.Positions.Count} 个不同坐标；编号仅用于本次演示");
            if(Algorithm==TeachingAlgorithm3D.MergeHull){foreach(var step in MergeRun())yield return step;}
            else if(Geometry.Dimension!=HullDimension.Volume){
                if(Geometry.Dimension==HullDimension.Planar){var p=Geometry.DegenerateBoundary;var normal=TeachingVector3D.Cross(Geometry.Points[p[1]]-Geometry.Points[p[0]],Geometry.Points[p[2]]-Geometry.Points[p[0]]).Unit;faces.Add(Geometry.Polygon(p,normal));yield return new TeachingEvent3D(TeachingEventKind3D.Add,"全部点共面：在所在平面求二维边界并填充"){Added=faces.ToArray()};}
                else if(Geometry.Dimension==HullDimension.Segment)yield return new TeachingEvent3D(TeachingEventKind3D.Horizon,"全部点共线：凸包是两个端点之间的线段"){Edges=Geometry.DegenerateBoundary};
                else yield return Event(TeachingEventKind3D.Inspect,Geometry.Dimension==HullDimension.Point?"只有一个不同坐标：凸包退化为点":"没有点",Geometry.Dimension==HullDimension.Point?0:-1);
            }else{
                var sequence=Algorithm==TeachingAlgorithm3D.GiftWrapping?Wrap():Expand(Algorithm==TeachingAlgorithm3D.QuickHull);
                foreach(var step in sequence)yield return step;
            }
            Result=Geometry.Result(faces);var completed=Event(TeachingEventKind3D.Complete,$"完成：{Result.Vertices.Length} 个几何极点，{Result.Triangles.Length/3} 个渲染三角形",Geometry.Dimension==HullDimension.Point?0:-1);if(Geometry.Dimension==HullDimension.Segment)completed.Edges=Geometry.DegenerateBoundary;yield return completed;
        }
        IEnumerable<TeachingEvent3D> Expand(bool quick){
            faces.AddRange(Geometry.Tetrahedron());foreach(int id in new[]{Geometry.A,Geometry.B,Geometry.C,Geometry.D})PointOwners[id]=-3;yield return new TeachingEvent3D(TeachingEventKind3D.Add,"选取四个不共面的点，建立初始四面体"){Added=faces.ToArray()};yield return Event(TeachingEventKind3D.Commit,"初始四面体建立；蓝色表示当前边界，并非最终答案");
            var initial=new HashSet<int>{Geometry.A,Geometry.B,Geometry.C,Geometry.D};
            if(quick){
                for(int i=0;i<Geometry.Points.Count;i++)if(!initial.Contains(i)){
                    TeachingFace3D owner=null;double best=Geometry.Epsilon;
                    foreach(var f in faces){double distance=Geometry.Distance(f,i);yield return null;if(distance>best){best=distance;owner=f;}}
                    if(owner!=null)owner.Outside.Add(i);PointOwners[i]=owner?.Id??-1;
                    yield return Event(owner!=null?TeachingEventKind3D.OutsideSet:TeachingEventKind3D.Reject,owner!=null?$"点 {i+1} 分配到面 {owner.Id+1} 的面外点集合":$"点 {i+1} 已被初始四面体包含",i,owner?.Id??-1);
                }
            }
            int cursor=0;
            while(true){
                int point=-1;
                if(quick){
                    TeachingFace3D active=null;foreach(var f in faces)if(f.Outside.Count>0){active=f;break;}if(active==null)break;
                    double farthest=-1;
                    // 每个面外点都会比较；即使提前遇到了最终最远点，也继续遍历集合剩余点。
                    foreach(int candidate in active.Outside){double distance=Geometry.Distance(active,candidate);if(distance>farthest){farthest=distance;point=candidate;}
                        yield return new TeachingEvent3D(TeachingEventKind3D.Candidate,$"面 {active.Id+1}：检查点 {candidate+1}，垂距 {distance*Geometry.Scale:0.###}；暂时最远点 {point+1}"){Point=candidate,Candidate=point,Face=active.Id,Value=distance*Geometry.Scale};}
                    yield return Event(TeachingEventKind3D.Inspect,$"全部候选比较完毕，使用最远点 {point+1} 扩张",point,active.Id);
                }else{
                    while(cursor<Geometry.Points.Count&&initial.Contains(cursor))cursor++;
                    if(cursor==Geometry.Points.Count)break;point=cursor++;
                    yield return Event(TeachingEventKind3D.Inspect,$"插入点 {point+1}：检查它位于各面的哪一侧",point);
                }
                var visible=new List<TeachingFace3D>();
                foreach(var f in faces){double distance=Geometry.Distance(f,point);if(distance>Geometry.Epsilon)visible.Add(f);
                    yield return new TeachingEvent3D(TeachingEventKind3D.Inspect,$"点 {point+1} 对面 {f.Id+1} 的有向距离 {distance*Geometry.Scale:0.###}：{(distance>Geometry.Epsilon?"可见，待删除":"不可见，保留")}"){Point=point,Face=f.Id,Removed=FaceIds(visible),Value=distance*Geometry.Scale};}
                if(visible.Count==0){yield return Event(TeachingEventKind3D.Reject,$"点 {point+1} 在当前凸包内或边界上，表面无需改变",point);continue;}
                var horizon=new Dictionary<TeachingEdge3D,(int a,int b)>();var orphans=new HashSet<int>();
                foreach(var f in visible){for(int j=0;j<f.Vertices.Length;j++){int a=f.Vertices[j],b=f.Vertices[(j+1)%f.Vertices.Length];var edge=new TeachingEdge3D(a,b);if(!horizon.Remove(edge))horizon.Add(edge,(a,b));}foreach(int p in f.Outside)if(p!=point)orphans.Add(p);}
                var boundary=new List<int>();foreach(var edge in horizon.Values){boundary.Add(edge.a);boundary.Add(edge.b);}
                yield return new TeachingEvent3D(TeachingEventKind3D.Horizon,"黄色轮廓边：一侧连接可见面，另一侧连接保留面"){Point=point,Removed=FaceIds(visible),Edges=boundary.ToArray()};
                yield return new TeachingEvent3D(TeachingEventKind3D.Remove,"红色面将被移除；先保留轮廓，预览替换面"){Point=point,Removed=FaceIds(visible),Edges=boundary.ToArray()};
                var created=new List<TeachingFace3D>();foreach(var edge in horizon.Values){var f=Geometry.Triangle(edge.a,edge.b,point);if(f!=null)created.Add(f);}
                yield return new TeachingEvent3D(TeachingEventKind3D.Add,$"将点 {point+1} 连接到每条轮廓边，形成 {created.Count} 个新三角面"){Point=point,Added=created.ToArray(),Removed=FaceIds(visible),Edges=boundary.ToArray()};
                foreach(var f in visible)faces.Remove(f);faces.AddRange(created);
                if(quick){foreach(int id in orphans)PointOwners[id]=-2;PointOwners[point]=-3;
                    var currentVertices=new HashSet<int>();foreach(var f in faces)foreach(int id in f.Vertices)currentVertices.Add(id);
                    for(int id=0;id<PointOwners.Length;id++)if(PointOwners[id]==-3&&!currentVertices.Contains(id))PointOwners[id]=-1;
                }
                yield return new TeachingEvent3D(TeachingEventKind3D.Commit,"提交表面替换：删除红面，绿色新面成为当前边界"){Point=point,Removed=FaceIds(visible)};
                if(quick)foreach(int p in orphans){TeachingFace3D owner=null;double best=Geometry.Epsilon;foreach(var f in created){double distance=Geometry.Distance(f,p);yield return null;if(distance>best){best=distance;owner=f;}}if(owner!=null)owner.Outside.Add(p);PointOwners[p]=owner?.Id??-1;
                    yield return Event(owner!=null?TeachingEventKind3D.OutsideSet:TeachingEventKind3D.Reject,owner!=null?$"重新分配点 {p+1} 到新面 {owner.Id+1} 的外点集合":$"点 {p+1} 已被扩张后的凸包包含",p,owner?.Id??-1);}
            }
            var consolidated=Geometry.Consolidate(faces);var old=FaceIds(faces);faces.Clear();faces.AddRange(consolidated);
            if(quick){for(int i=0;i<PointOwners.Length;i++)PointOwners[i]=-1;foreach(var f in faces)foreach(int id in f.Vertices)PointOwners[id]=-3;}
            yield return new TeachingEvent3D(TeachingEventKind3D.Commit,"整理共面面域：移除面内部与共线中间点，保留真正棱边"){Removed=old,Added=faces.ToArray()};
        }
        IEnumerable<TeachingEvent3D> Wrap(){
            // 先在 XY 投影寻找支撑边。对应的竖直支撑平面使所有点位于同一侧。
            int a=0;for(int i=1;i<Geometry.Points.Count;i++){var p=Geometry.Points[i];var q=Geometry.Points[a];if(p.X<q.X||(p.X==q.X&&(p.Y<q.Y||(p.Y==q.Y&&p.Z<q.Z))))a=i;}
            int b=-1;var origin=Geometry.Points[a];
            for(int i=0;i<Geometry.Points.Count;i++)if(i!=a){var p=Geometry.Points[i]-origin;if(p.X*p.X+p.Y*p.Y<1e-28)continue;if(b<0)b=i;else{var q=Geometry.Points[b]-origin;double cross=q.X*p.Y-q.Y*p.X;if(cross<0||(cross==0&&p.X*p.X+p.Y*p.Y>q.X*q.X+q.Y*q.Y))b=i;}yield return new TeachingEvent3D(TeachingEventKind3D.Candidate,$"在 XY 投影寻找起始支撑边：比较点 {i+1}，当前边 {a+1}—{b+1}"){Point=i,Candidate=b,Edges=new[]{a,b}};}
            if(b<0)throw new InvalidOperationException("无法找到起始支撑边");
            var ab=Geometry.Points[b]-origin;var supportNormal=new TeachingVector3D(ab.Y,-ab.X,0).Unit;
            var support=new TeachingFace3D(-1,new[]{a,b},supportNormal,TeachingVector3D.Dot(supportNormal,origin));
            var queue=new Queue<(int a,int b,TeachingFace3D owner)>();
            // 搜索函数使用邻面边的反向；虚拟支撑平面的方向与此处保持一致。
            queue.Enqueue((b,a,support));var closed=new HashSet<TeachingEdge3D>();var owners=new Dictionary<TeachingEdge3D,TeachingFace3D>();var facetKeys=new HashSet<string>();
            while(queue.Count>0){var work=queue.Dequeue();var key=new TeachingEdge3D(work.a,work.b);if(closed.Contains(key))continue;
                int start=work.b,end=work.a,candidate=-1;
                for(int i=0;i<Geometry.Points.Count;i++){if(Geometry.Distance(work.owner,i)<-Geometry.Epsilon){candidate=i;break;}yield return null;}
                if(candidate<0)throw new InvalidOperationException("包装边缺少有效外侧候选");
                yield return new TeachingEvent3D(TeachingEventKind3D.Horizon,$"沿边 {start+1}—{end+1} 寻找相邻支撑面"){Edges=new[]{start,end}};
                for(int i=0;i<Geometry.Points.Count;i++)if(i!=start&&i!=end){
                    var trial=Geometry.Triangle(start,end,candidate,false);if(trial==null)throw new InvalidOperationException("包装候选与边共线");
                    double distance=Geometry.Distance(trial,i);
                    if(distance>Geometry.Epsilon)candidate=i;
                    // 共面点不随意替换候选；整个面域在确认支撑平面后统一求边界。
                    var preview=Geometry.Triangle(start,end,candidate,false);
                    yield return new TeachingEvent3D(TeachingEventKind3D.Candidate,$"绕边检查点 {i+1}：{(distance>Geometry.Epsilon?"更新候选平面":"保留候选平面")}；候选点 {candidate+1}"){Point=i,Candidate=candidate,Preview=preview,Edges=new[]{start,end},Value=distance*Geometry.Scale};
                }
                var plane=Geometry.Triangle(start,end,candidate,false);var coplanar=new List<int>();
                for(int i=0;i<Geometry.Points.Count;i++){double distance=Geometry.Distance(plane,i);if(distance>Geometry.Epsilon*4)throw new InvalidOperationException("候选平面不是支撑面");if(Math.Abs(distance)<=Geometry.Epsilon*4)coplanar.Add(i);yield return null;}
                int[] polygon=Geometry.PlanarBoundary(coplanar,plane.Normal);var sorted=(int[])polygon.Clone();Array.Sort(sorted);string facetKey=string.Join(",",sorted);
                if(!facetKeys.Add(facetKey))throw new InvalidOperationException("礼品包装重复访问了未闭合面");
                var face=Geometry.Polygon(polygon,plane.Normal);faces.Add(face);
                yield return new TeachingEvent3D(TeachingEventKind3D.Add,$"所有点均在平面内侧，确认支撑面（{polygon.Length} 个面域顶点）"){Added=new[]{face},Edges=new[]{start,end}};
                yield return Event(TeachingEventKind3D.Commit,"新面成为外壳的一部分，继续处理尚未闭合的边");
                for(int i=0;i<polygon.Length;i++){int x=polygon[i],y=polygon[(i+1)%polygon.Length];var edge=new TeachingEdge3D(x,y);if(owners.ContainsKey(edge))closed.Add(edge);else{owners.Add(edge,face);queue.Enqueue((x,y,face));}}
            }
            foreach(var edge in owners.Keys)if(!closed.Contains(edge))throw new InvalidOperationException("包装结果存在未闭合边");
        }
    }
}
