using UnityEngine;
namespace ConvexHullDemo3D {
    public sealed class HullGizmo3D : MonoBehaviour {
        public LineRenderer[] shafts;
        public MeshRenderer[] heads;
        public Color[] colors={new Color(.88f,.12f,.12f),new Color(.12f,.65f,.22f),new Color(.12f,.35f,.95f)};
        public static Vector3 Axis(int axis)=>axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
        public readonly bool[] Available=new bool[3];
        MaterialPropertyBlock block;
        public void Display(Vector3 point,Camera camera,float length,float width,int draggingAxis) {
            gameObject.SetActive(true);transform.position=point;
            var ray=(point-camera.transform.position).normalized;
            for(int i=0;i<3;i++) {
                Available[i]=Vector3.Cross(Axis(i),ray).sqrMagnitude>.01f;
                Color color=Available[i]?colors[i]:new Color(.48f,.50f,.54f,.6f);if(i==draggingAxis)color=new Color(1,.65f,.08f);
                shafts[i].positionCount=2;shafts[i].SetPosition(0,point+Axis(i)*.2f);shafts[i].SetPosition(1,point+Axis(i)*(length-.18f));
                shafts[i].widthMultiplier=width;shafts[i].startColor=shafts[i].endColor=color;
                heads[i].transform.position=point+Axis(i)*(length-.18f);heads[i].transform.rotation=Quaternion.FromToRotation(Vector3.up,Axis(i));heads[i].transform.localScale=new Vector3(width*4,.18f,width*4);
                if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",color);heads[i].SetPropertyBlock(block);
            }
        }
        public int Hit(Ray ray,Vector3 origin,float length,float radius) {
            int best=-1;float nearest=float.PositiveInfinity;
            for(int i=0;i<3;i++)if(Available[i]) {
                Vector3 u=Axis(i),w=ray.origin-origin;float dot=Vector3.Dot(ray.direction,u),den=1-dot*dot;if(den<.0001f)continue;
                float along=(Vector3.Dot(w,u)-dot*Vector3.Dot(w,ray.direction))/den;
                along=Mathf.Clamp(along,.22f,length);Vector3 q=origin+u*along;float t=Vector3.Dot(q-ray.origin,ray.direction);
                if(t>0&&(ray.GetPoint(t)-q).sqrMagnitude<=radius*radius&&t<nearest){best=i;nearest=t;}
            }
            return best;
        }
    }
}
