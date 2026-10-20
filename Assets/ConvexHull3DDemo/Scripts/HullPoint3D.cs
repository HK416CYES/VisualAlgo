using UnityEngine;
namespace ConvexHullDemo3D {
    public sealed class HullPoint3D : MonoBehaviour {
        public MeshRenderer dot;
        MaterialPropertyBlock block;
        public void SetColor(Color color){if(block==null)block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);dot.SetPropertyBlock(block);}
    }
}
