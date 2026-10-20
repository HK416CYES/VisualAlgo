using UnityEngine;
using UnityEngine.UI;
namespace ConvexHullDemo3D {
    /// <summary>将世界坐标轴投影到相机观察方向，在 UIFramework 的固定屏幕面板内绘制。</summary>
    public sealed class HullOrientationIndicator3D : MaskableGraphic {
        public Text[] labels=new Text[3];
        [Min(10)] public float axisLength=36;
        [Min(1)] public float shaftWidth=3;
        Camera world;HullGizmo3D gizmo;
        readonly Vector3[] directions=new Vector3[3];
        readonly int[] order={0,1,2};
        bool initialized;
        public void Bind(Camera camera,HullGizmo3D source){world=camera;gizmo=source;initialized=false;Refresh();}
        public Vector3 ViewDirection(int axis)=>world==null?HullGizmo3D.Axis(axis):Quaternion.Inverse(world.transform.rotation)*HullGizmo3D.Axis(axis);
        void LateUpdate(){Refresh();}
        void Refresh(){
            if(world==null)return;
            bool changed=!initialized;initialized=true;
            // 固定世界轴只随观察朝向变化，不依赖选中点、相机位置或操作轴的位置。
            for(int i=0;i<3;i++){
                Vector3 direction=ViewDirection(i);
                if((directions[i]-direction).sqrMagnitude>1e-10f)changed=true;
                directions[i]=direction;
                if(labels[i]!=null){var color=gizmo.colors[i];if(labels[i].color!=color)changed=true;labels[i].color=color;
                    labels[i].rectTransform.anchoredPosition=(Vector2)directions[i]*(axisLength+12);
                }
            }
            if(changed)SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh){
            vh.Clear();if(world==null||gizmo==null)return;
            // 远端先画，近端后画；不需要额外相机，也不影响主相机或 UI 相机的状态。
            for(int i=0;i<3;i++)order[i]=i;
            for(int i=0;i<2;i++)for(int j=i+1;j<3;j++)if(directions[order[i]].z<directions[order[j]].z){int t=order[i];order[i]=order[j];order[j]=t;}
            foreach(int i in order){var end=(Vector2)directions[i]*axisLength;var color=gizmo.colors[i];var faint=color;faint.a=.3f;Stroke(vh,Vector2.zero,-end*.65f,shaftWidth*.6f,faint);}
            foreach(int i in order){Vector2 end=(Vector2)directions[i]*axisLength;Color color=gizmo.colors[i];
                if(end.magnitude>7){var along=end.normalized;var side=new Vector2(-along.y,along.x);Stroke(vh,Vector2.zero,end-along*6,shaftWidth,color);Triangle(vh,end,end-along*8+side*4,end-along*8-side*4,color);}
                else Circle(vh,end,4,color);
            }
            Circle(vh,Vector2.zero,3,new Color(.4f,.45f,.52f));
        }
        static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color){int start=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);}
        static void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color){Vector2 dir=(b-a).normalized;Vector2 side=new Vector2(-dir.y,dir.x)*width*.5f;Triangle(vh,a-side,a+side,b+side,color);Triangle(vh,a-side,b+side,b-side,color);}
        static void Circle(VertexHelper vh,Vector2 center,float radius,Color color){for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Triangle(vh,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color);}}
    }
}
