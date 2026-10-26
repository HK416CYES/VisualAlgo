using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;
namespace ConvexHullDemo3D {
    public sealed class HullContextMenu3D : PanelController {
        public Button deleteButton;
        ConvexHull3DController demo;
        Vector2 anchor;
        public void Bind(ConvexHull3DController controller){demo=controller;}
        protected override void AddListeners(){if(deleteButton)deleteButton.onClick.AddListener(Delete);}
        protected override void RemoveListeners(){if(deleteButton)deleteButton.onClick.RemoveListener(Delete);}
        void Delete(){demo.DeleteContextPoint();}
        protected override void HierarchyFixOnShow(){transform.SetAsLastSibling();}
        public void PositionAt(Vector2 screen){anchor=screen;var rect=(RectTransform)transform;var parent=(RectTransform)rect.parent;var canvas=GetComponentInParent<Canvas>().rootCanvas;var viewport=demo.worldCamera.pixelRect;float scale=canvas.scaleFactor;
            float x=Mathf.Clamp(screen.x,viewport.xMin+8,Mathf.Max(viewport.xMin+8,viewport.xMax-rect.rect.width*scale-8));
            float y=Mathf.Clamp(screen.y,Mathf.Min(viewport.yMax-8,viewport.yMin+rect.rect.height*scale+8),viewport.yMax-8);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2(x,y),canvas.worldCamera,out var local);rect.anchoredPosition=local-parent.rect.min;
        }
        void LateUpdate(){if(demo)PositionAt(anchor);}
        public bool Contains(Vector2 screen){var canvas=GetComponentInParent<Canvas>().rootCanvas;return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,screen,canvas.worldCamera);}
    }
}
