using System.Collections.Generic;
using UnityEngine;
namespace ConvexHullDemo3D {
    public sealed class HullTeachingOverlay3D : MonoBehaviour {
        public HullSurface3D accepted,removing,adding,active,projectionSurface;
        public Color projectionPlaneColor=new Color(.62f,.76f,.85f,.24f);
        public LineRenderer linePrefab;
        public Material depthLineMaterial,xrayLineMaterial;
        [Min(.001f)] public float edgeWidth=.025f,markerRadius=.23f;
        public bool xrayAssistance=true;
        public Color boundaryColor=new Color(.12f,.42f,.9f),removeColor=new Color(.95f,.18f,.12f),addColor=new Color(.15f,.8f,.32f),candidateColor=new Color(1,.55f,.05f),horizonColor=new Color(1,.78f,.06f);
        readonly Dictionary<Vector3,int> pointIndices=new Dictionary<Vector3,int>();
        bool MergeColors=>session!=null&&session.Algorithm==TeachingAlgorithm3D.MergeHull;
        readonly List<LineRenderer> previewLines=new List<LineRenderer>();
        bool OwnershipColors=>session!=null&&session.Algorithm==TeachingAlgorithm3D.QuickHull;
        public static Color FaceOwnershipColor(int id)=>Color.HSVToRGB(Mathf.Repeat(.06f+id*.61803398875f,1),.65f,.86f);
        public Color PointOwnershipColor(int index){if(MergeColors){int group=session.PointGroups==null?-1:session.PointGroups[index];return group==-2?new Color(.18f,.20f,.23f):group<0?new Color(.48f,.52f,.57f):DimmedGroupColor(group); }int owner=session.PointOwners[index];return owner>=0?FaceOwnershipColor(owner):owner==-1?new Color(.18f,.20f,.23f):owner==-3?boundaryColor:new Color(.48f,.52f,.57f);}
        public void ApplyPointColors(ConvexHull3DController demo){if(!OwnershipColors&&!MergeColors)return;foreach(var point in demo.Points)if(pointIndices.TryGetValue(point.Position,out int index))point.View.SetColor(PointOwnershipColor(index));}
        readonly Dictionary<int,TeachingFace3D> faces=new Dictionary<int,TeachingFace3D>();
        readonly Dictionary<int,TeachingFace3D> pending=new Dictionary<int,TeachingFace3D>();
        readonly HashSet<int> removed=new HashSet<int>();
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        readonly List<(LineRenderer line,Vector3 from,Vector3 to)> growing=new List<(LineRenderer,Vector3,Vector3)>();
        readonly List<(LineRenderer line,int point,Color color)> rings=new List<(LineRenderer,int,Color)>();
        Vector3[] positions;
        HullTeachingSession3D session;Camera camera;TeachingEvent3D step;int used;
        public int VisibleFaceCount=>faces.Count+pending.Count;
        public int ActiveLineCount=>used;
        public void Begin(HullTeachingSession3D value,Camera world){Clear();session=value;camera=world;positions=value.Geometry.Positions.ToArray();pointIndices.Clear();for(int i=0;i<positions.Length;i++)pointIndices[positions[i]]=i;gameObject.SetActive(true);}
        public void Clear(){faces.Clear();pending.Clear();removed.Clear();session=null;step=null;positions=null;rings.Clear();used=0;foreach(var line in lines)if(line!=null)line.gameObject.SetActive(false);foreach(var surface in new[]{accepted,removing,adding,active,projectionSurface})if(surface!=null)surface.gameObject.SetActive(false);}
        public void Apply(TeachingEvent3D value){step=value;
            if(value.Removed!=null){removed.Clear();foreach(int id in value.Removed)removed.Add(id);}
            if(value.Kind==TeachingEventKind3D.Commit||value.Kind==TeachingEventKind3D.Leaf||value.Kind==TeachingEventKind3D.MergeDone){if(value.Removed!=null)foreach(int id in value.Removed)faces.Remove(id);foreach(var pair in pending)faces[pair.Key]=pair.Value;pending.Clear();removed.Clear();if(value.Added!=null)foreach(var f in value.Added)faces[f.Id]=f;}
            else if(value.Added!=null)foreach(var f in value.Added)pending[f.Id]=f;
            if(value.Kind==TeachingEventKind3D.Complete){foreach(var pair in pending)faces[pair.Key]=pair.Value;pending.Clear();removed.Clear();}
            Render();Animate(0);
        }
        void Render(){
            var normal=new List<TeachingFace3D>();var red=new List<TeachingFace3D>();var green=new List<TeachingFace3D>(pending.Values);var orange=new List<TeachingFace3D>();
            foreach(var f in faces.Values){if(removed.Contains(f.Id))red.Add(f);else if(f.Id==step.Face)orange.Add(f);else normal.Add(f);}if(step.Preview!=null)orange.Add(step.Preview);
            Display(accepted,normal,boundaryColor,OwnershipColors||MergeColors?.24f:.12f);Display(removing,red,removeColor,.32f);Display(adding,green,addColor,.28f);Display(active,orange,candidateColor,.24f);
            used=0;rings.Clear();growing.Clear();previewLines.Clear();var edges=new HashSet<TeachingEdge3D>();var boundaryPoints=new HashSet<int>();
            foreach(var f in faces.Values){Color color=removed.Contains(f.Id)?removeColor:f.Id==step.Face?candidateColor:OwnershipColors?FaceOwnershipColor(f.Id):MergeColors?FaceColor(f):boundaryColor;for(int i=0;i<f.Vertices.Length;i++){int a=f.Vertices[i],b=f.Vertices[(i+1)%f.Vertices.Length];boundaryPoints.Add(a);if(edges.Add(new TeachingEdge3D(a,b)))Line(positions[a],positions[b],color,edgeWidth,false);}}
            foreach(var f in pending.Values)for(int i=0;i<f.Vertices.Length;i++){var from=positions[f.Vertices[i]];var to=positions[f.Vertices[(i+1)%f.Vertices.Length]];Line(from,to,addColor,edgeWidth,false);growing.Add((lines[used-1],from,to));}
            if(step.Edges!=null)for(int i=0;i+1<step.Edges.Length;i+=2)Line(positions[step.Edges[i]],positions[step.Edges[i+1]],horizonColor,edgeWidth*2,true);
            if(step.Preview!=null)for(int i=0;i<step.Preview.Vertices.Length;i++){Line(positions[step.Preview.Vertices[i]],positions[step.Preview.Vertices[(i+1)%step.Preview.Vertices.Length]],candidateColor,edgeWidth*1.5f,true);previewLines.Add(lines[used-1]);}
            // 垂线明确表示“点到平面距离”，不是点到三角形中心的距离。
            TeachingFace3D face=MergeColors?step.Preview:null;if(face==null&&step.Face>=0)faces.TryGetValue(step.Face,out face);
            if(step.Point>=0&&face!=null&&step.Kind!=TeachingEventKind3D.Rotate){
                var p=positions[step.Point];double distance=session.Geometry.Distance(face,step.Point)*session.Geometry.Scale;var foot=p-face.Normal.Float*(float)distance;Line(p,foot,candidateColor,edgeWidth*1.5f,true);
                // 垂足可能在无限平面上，却落在有限面域之外。补到最近边界的线，
                // 让“点 → 垂足 → 面”连续；这段线不参与点到平面距离的计算。
                if(TryConnectFoot(foot,face.Vertices,positions,face.Normal.Float,out var boundary))
                    DashedLine(foot,boundary,candidateColor,edgeWidth*1.5f);
            }
            RenderGroups();
            if(step.Candidate>=0)Ring(step.Candidate,addColor,markerRadius*1.35f);
            if(step.Point>=0)Ring(step.Point,candidateColor,markerRadius);
            // 当前凸包顶点的外圈是教学状态；原始点的几何数据与实时统计完全不变。
            foreach(int p in boundaryPoints)if(p!=step.Point&&p!=step.Candidate)Ring(p,boundaryColor,markerRadius*.8f);
            for(int i=used;i<lines.Count;i++)lines[i].gameObject.SetActive(false);UpdateRings();
        }
        // 支持三角形，也支持礼品包装法合并后的凸多边形面域。
        // 凸多边形内部点在所有有向边的同一侧；外部点连接到最近的边段。
        public static bool TryConnectFoot(Vector3 foot,int[] vertices,Vector3[] points,Vector3 normal,out Vector3 boundary){
            boundary=foot;if(vertices==null||vertices.Length<3)return false;
            bool positive=false,negative=false;float nearest=float.PositiveInfinity;
            for(int i=0;i<vertices.Length;i++){
                Vector3 a=points[vertices[i]],b=points[vertices[(i+1)%vertices.Length]],edge=b-a;
                float lengthSquared=edge.sqrMagnitude;if(lengthSquared<=0)continue;
                float side=Vector3.Dot(Vector3.Cross(edge,foot-a),normal);
                float tolerance=1e-6f*Mathf.Sqrt(lengthSquared);
                positive|=side>tolerance;negative|=side<-tolerance;
                float t=Mathf.Clamp01(Vector3.Dot(foot-a,edge)/lengthSquared);
                Vector3 closest=a+edge*t;float distance=(foot-closest).sqrMagnitude;
                if(distance<nearest){nearest=distance;boundary=closest;}
            }
            return positive&&negative&&nearest>1e-12f;
        }
        public Color GroupColor(int group){var node=session.MergeGroups[group];return node.Parent<0&&node.Completed?boundaryColor:FaceOwnershipColor(group+4);}
        Color DimmedGroupColor(int group){var color=GroupColor(group);if(step!=null&&!Focused(group)){color.r*=.6f;color.g*=.6f;color.b*=.6f;}return color;}
        Color FaceColor(TeachingFace3D face)=>face.Bridge?addColor:face.Group>=0?GroupColor(face.Group):boundaryColor;
        bool Focused(int group)=>group<0||step.Group<0||group==step.Group||group==step.LeftGroup||group==step.RightGroup;
        void RenderGroups(){
            if(projectionSurface!=null)projectionSurface.gameObject.SetActive(false);
            if(!MergeColors||step.Group<0)return;
            var group=session.MergeGroups[step.Group];
            foreach(var node in session.MergeGroups)if(node.Visible&&node.Dimension==HullDimension.Segment){Color color=GroupColor(node.Id);if(!Focused(node.Id))color.a=.3f;Line(positions[node.Vertices[0]],positions[node.Vertices[1]],color,edgeWidth,false);}
            // 先呈现两个投影轮廓，再在下一步骤用黄色跨组边连接它们。
            if(step.Kind==TeachingEventKind3D.Projection||step.Kind==TeachingEventKind3D.Horizon){
                if(projectionSurface!=null){var min=group.Bounds.min-Vector3.one*.55f;var max=group.Bounds.max+Vector3.one*.55f;Vector3[] corners;
                    if(group.ProjectionXZ){float y=group.Bounds.min.y-.35f;corners=new[]{new Vector3(min.x,y,min.z),new Vector3(max.x,y,min.z),new Vector3(max.x,y,max.z),new Vector3(min.x,y,max.z)};}
                    else{float z=group.Bounds.min.z-.35f;corners=new[]{new Vector3(min.x,min.y,z),new Vector3(max.x,min.y,z),new Vector3(max.x,max.y,z),new Vector3(min.x,max.y,z)};}
                    DrawGuidePlane(projectionSurface,corners,projectionPlaneColor);
                }
                foreach(var pair in new[]{(group.ProjectionLeft,group.Left),(group.ProjectionRight,group.Right)})if(pair.Item1!=null){var ids=pair.Item1;for(int i=0;i<ids.Length;i++){Vector3 p=positions[ids[i]],q=positions[ids[(i+1)%ids.Length]];var a=ProjectMergePoint(p,group);var b=ProjectMergePoint(q,group);Line(a,b,GroupColor(pair.Item2),edgeWidth*1.5f,true);DashedLine(p,a,GroupColor(pair.Item2),edgeWidth*.6f);}}
                if(step.Edges!=null&&step.Edges.Length==2)Line(ProjectMergePoint(positions[step.Edges[0]],group),ProjectMergePoint(positions[step.Edges[1]],group),horizonColor,edgeWidth*2,true);
            }
        }
        // 面、边框和网格使用同一组世界坐标；投影轮廓严格落在承载面上。
        void DrawGuidePlane(HullSurface3D surface,Vector3[] corners,Color color){
            surface.gameObject.SetActive(true);surface.Display(new HullResult3D{Dimension=HullDimension.Planar,Vertices=corners,Triangles=new[]{0,1,2,0,2,3}},color,Color.clear,0);
            var border=color;border.a=.8f;for(int i=0;i<4;i++)Line(corners[i],corners[(i+1)%4],border,edgeWidth*1.3f,false);
            var grid=color;grid.a=.28f;for(int i=1;i<6;i++){float t=i/6f;Line(Vector3.Lerp(corners[0],corners[1],t),Vector3.Lerp(corners[3],corners[2],t),grid,edgeWidth*.45f,false);Line(Vector3.Lerp(corners[0],corners[3],t),Vector3.Lerp(corners[1],corners[2],t),grid,edgeWidth*.45f,false);}
        }
        Vector3 ProjectMergePoint(Vector3 point,MergeGroup3D group){if(group.ProjectionXZ)point.y=group.Bounds.min.y-.35f;else point.z=group.Bounds.min.z-.35f;return point;}
        void AnimatePreview(float progress){
            if(step.Kind!=TeachingEventKind3D.Rotate||step.PreviousPreview==null||step.Preview==null)return;
            var old=step.PreviousPreview.Vertices;var next=step.Preview.Vertices;Vector3 a=positions[old[0]],b=positions[old[1]],axis=(b-a).normalized;
            Vector3 from=positions[old[2]]-a,to=positions[next[2]]-a;float alongFrom=Vector3.Dot(from,axis),alongTo=Vector3.Dot(to,axis);Vector3 radialFrom=from-axis*alongFrom,radialTo=to-axis*alongTo;
            float t=Mathf.SmoothStep(0,1,progress),angle=Vector3.SignedAngle(radialFrom,radialTo,axis);Vector3 c=a+axis*Mathf.Lerp(alongFrom,alongTo,t)+Quaternion.AngleAxis(angle*t,axis)*radialFrom.normalized*Mathf.Lerp(radialFrom.magnitude,radialTo.magnitude,t);
            var vertices=new[]{a,b,c};var fill=candidateColor;fill.a=.24f;active.gameObject.SetActive(true);active.Display(new HullResult3D{Dimension=HullDimension.Planar,Vertices=vertices,Triangles=new[]{0,1,2}},fill,candidateColor,edgeWidth*.12f);
            for(int i=0;i<previewLines.Count&&i<3;i++){previewLines[i].SetPosition(0,vertices[i]);previewLines[i].SetPosition(1,vertices[(i+1)%3]);}
        }
        void Display(HullSurface3D surface,List<TeachingFace3D> group,Color color,float alpha){surface.gameObject.SetActive(group.Count>0);if(group.Count==0)return;var triangles=new List<int>();foreach(var f in group)for(int i=1;i<f.Vertices.Length-1;i++){triangles.Add(f.Vertices[0]);triangles.Add(f.Vertices[i]);triangles.Add(f.Vertices[i+1]);}List<Color> vertexColors=null;
            if(OwnershipColors||MergeColors){vertexColors=new List<Color>();foreach(var f in group){var tint=OwnershipColors?FaceOwnershipColor(f.Id):surface==active?candidateColor:surface==removing?removeColor:surface==adding?addColor:FaceColor(f);tint.a=MergeColors&&!Focused(f.Group)?.22f:1f;for(int i=1;i<f.Vertices.Length-1;i++)for(int j=0;j<3;j++)vertexColors.Add(tint);}}
            var fill=color;fill.a=alpha;var fine=color;fine.a=.15f;surface.Display(new HullResult3D{Dimension=HullDimension.Volume,Vertices=positions,Triangles=triangles.ToArray()},fill,fine,edgeWidth*.12f,vertexColors);}
        LineRenderer Rent(Color color,float width,bool assist){if(used==lines.Count)lines.Add(Instantiate(linePrefab,transform));var line=lines[used++];line.gameObject.SetActive(true);line.loop=false;line.widthMultiplier=width;line.startColor=line.endColor=color;line.sharedMaterial=assist&&xrayAssistance?xrayLineMaterial:depthLineMaterial;return line;}
        void Line(Vector3 a,Vector3 b,Color color,float width,bool assist){var line=Rent(color,width,assist);line.positionCount=2;line.SetPosition(0,a);line.SetPosition(1,b);}
        // 补线使用世界单位长度的短线段，避免屏幕分辨率改变虚线比例。
        // 从两端开始/结束于实段，极长补线限制段数，复用现有线对象池。
        void DashedLine(Vector3 a,Vector3 b,Color color,float width){
            float length=Vector3.Distance(a,b);if(length<=1e-6f)return;
            int count=Mathf.Clamp(Mathf.CeilToInt(length/.2f),2,128);
            float unit=1f/(count+(count-1)*.6f);
            for(int i=0;i<count;i++){
                float start=i*1.6f*unit;
                Line(Vector3.Lerp(a,b,start),Vector3.Lerp(a,b,start+unit),color,width,true);
            }
        }
        void Ring(int point,Color color,float radius){var line=Rent(color,edgeWidth*1.2f,true);line.loop=true;line.positionCount=32;line.textureScale=new Vector2(radius,1);rings.Add((line,point,color));}
        void LateUpdate(){UpdateRings();}
        void UpdateRings(){if(camera==null||positions==null)return;foreach(var ring in rings){float radius=ring.line.textureScale.x;Vector3 center=positions[ring.point];for(int j=0;j<32;j++){float angle=j*Mathf.PI/16;ring.line.SetPosition(j,center+(camera.transform.right*Mathf.Cos(angle)+camera.transform.up*Mathf.Sin(angle))*radius);}}}
        public void Animate(float progress){if(step==null)return;AnimatePreview(progress);foreach(var edge in growing)edge.line.SetPosition(1,Vector3.Lerp(edge.from,edge.to,Mathf.SmoothStep(0,1,progress)));if(step.Kind==TeachingEventKind3D.Add){var fill=addColor;fill.a=.28f*Mathf.SmoothStep(0,1,progress);var edge=addColor;edge.a=.15f;adding.SetStyle(fill,edge,edgeWidth*.12f);}else if(step.Kind==TeachingEventKind3D.Remove){var fill=removeColor;fill.a=Mathf.Lerp(.32f,.1f,progress);removing.SetStyle(fill,removeColor,edgeWidth*.12f);}}
    }
}
