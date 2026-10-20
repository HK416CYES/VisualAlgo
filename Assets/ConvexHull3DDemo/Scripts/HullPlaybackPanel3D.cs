using System.Collections.Generic;
using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;
namespace ConvexHullDemo3D {
    public sealed class HullPlaybackPanel3D : PanelController {
        public RectTransform card,labelsRoot;
        public Text labelPrefab;
        public Button pauseButton,nextButton,stopButton;
        public Dropdown speedDropdown;
        readonly List<Text> labels=new List<Text>();
        HullPlayback3D playback;Camera world,ui;HullTeachingSession3D numbered;
        public void Bind(HullPlayback3D value,Camera camera){playback=value;world=camera;ui=GetComponentInParent<Canvas>().rootCanvas.worldCamera;playback.Changed+=Refresh;Refresh();}
        protected override void AddListeners(){pauseButton.onClick.AddListener(Pause);nextButton.onClick.AddListener(Next);stopButton.onClick.AddListener(Stop);speedDropdown.onValueChanged.AddListener(Speed);}
        protected override void RemoveListeners(){pauseButton.onClick.RemoveListener(Pause);nextButton.onClick.RemoveListener(Next);stopButton.onClick.RemoveListener(Stop);speedDropdown.onValueChanged.RemoveListener(Speed);if(playback!=null)playback.Changed-=Refresh;}
        void Pause()=>playback?.TogglePause();void Next()=>playback?.NextStep();void Stop()=>playback?.Stop();void Speed(int value)=>playback?.SetSpeed(value);
        public void Refresh(){if(playback==null)return;
            float[] speeds={.25f,.5f,1,2,4};for(int i=0;i<speeds.Length;i++)if(Mathf.Approximately(speeds[i],playback.Speed))speedDropdown.SetValueWithoutNotify(i);
            pauseButton.GetComponentInChildren<Text>().text=playback.IsPaused?"继续":"暂停";nextButton.interactable=playback.CanStep;
            if(!playback.IsActive){numbered=null;foreach(var label in labels)label.gameObject.SetActive(false);return;}
            if(numbered!=playback.Session){numbered=playback.Session;while(labels.Count<numbered.Geometry.Positions.Count)labels.Add(Instantiate(labelPrefab,labelsRoot));for(int i=0;i<labels.Count;i++){labels[i].text=(i+1).ToString();labels[i].gameObject.SetActive(i<numbered.Geometry.Positions.Count);}}
        }
        public static string Name(TeachingAlgorithm3D value)=>value==TeachingAlgorithm3D.Incremental?"增量法":value==TeachingAlgorithm3D.QuickHull?"QuickHull":value==TeachingAlgorithm3D.MergeHull?"分治归并法":"礼品包装法";
        void LateUpdate(){if(playback==null||!playback.IsActive||numbered==null)return;var step=numbered.Current;
            for(int i=0;i<numbered.Geometry.Positions.Count;i++){Vector3 screen=world.WorldToScreenPoint(numbered.Geometry.Positions[i]+world.transform.up*.25f+world.transform.right*.2f);bool relevant=numbered.Geometry.Positions.Count<=80||step!=null&&(step.Point==i||step.Candidate==i);
                bool visible=relevant&&screen.z>world.nearClipPlane&&world.pixelRect.Contains(screen)&&!RectTransformUtility.RectangleContainsScreenPoint(card,screen,ui)&&!RectTransformUtility.RectangleContainsScreenPoint(playback.demo.Toolbar.card,screen,ui);labels[i].gameObject.SetActive(visible);if(visible){RectTransformUtility.ScreenPointToLocalPointInRectangle(labelsRoot,screen,ui,out var local);labels[i].rectTransform.anchoredPosition=local;}}
        }
        protected override void HierarchyFixOnShow(){transform.SetAsLastSibling();}
    }
}
