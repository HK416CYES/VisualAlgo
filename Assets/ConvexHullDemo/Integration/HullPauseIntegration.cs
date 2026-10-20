using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UIFramework;
using ConvexHullDemo;
using ConvexHullDemo3D;
using VisualAlgo.Managers.UI;

namespace VisualAlgo.ConvexHulls {
    /// <summary>将宿主暂停菜单接入凸包；只冻结输入和动画计时，不销毁算法会话。</summary>
    public sealed class HullPauseIntegration : MonoBehaviour {
        public PauseMenuController menu;
        public ConvexHullDemoController planar;
        public ConvexHull3DController spatial;
        public Slider sensitivity;
        readonly List<(CanvasGroup group,bool interactable,bool blocks)> groups = new();
        void OnEnable() {
            if(planar!=null)planar.UsesPauseMenu=true;
            if(spatial!=null)spatial.UsesPauseMenu=true;
            menu.PauseChanged+=Apply;
            menu.ZoomSensitivityChanged+=ApplySensitivity;
            Apply(menu.IsPaused);
        }
        void Start(){ApplySensitivity(menu.ZoomSensitivity);if(sensitivity!=null)sensitivity.interactable=planar!=null;}
        void ApplySensitivity(float value){if(planar!=null)planar.zoomSpeed=Mathf.Clamp(value,.001f,1f);}
        void Apply(bool paused) {
            if(planar!=null){planar.IsScenePaused=paused;planar.CancelGesture();planar.CloseMenu();}
            if(spatial!=null){spatial.IsScenePaused=paused;spatial.CancelGestures();}
            if(paused){
                if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
                if(groups.Count==0)foreach(var frame in FindObjectsByType<UIFrame>(FindObjectsSortMode.None)){
                    var group=frame.GetComponent<CanvasGroup>();if(group==null)continue;
                    groups.Add((group,group.interactable,group.blocksRaycasts));group.interactable=false;group.blocksRaycasts=false;
                }
            }else{foreach(var state in groups)if(state.group!=null){state.group.interactable=state.interactable;state.group.blocksRaycasts=state.blocks;}groups.Clear();}
        }
        void OnDisable(){if(menu!=null){menu.PauseChanged-=Apply;menu.ZoomSensitivityChanged-=ApplySensitivity;}Apply(false);if(planar!=null)planar.UsesPauseMenu=false;if(spatial!=null)spatial.UsesPauseMenu=false;}
    }
}
