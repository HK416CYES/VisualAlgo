using System;
using System.Collections.Generic;
using UnityEngine;
namespace ConvexHullDemo3D {
    public sealed class HullPlayback3D : MonoBehaviour {
        public ConvexHull3DController demo;
        [Tooltip("分治归并：关键步骤或逐次候选比较；播放开始时锁定。 ")] public MergeHullDetail mergeHullDetail=MergeHullDetail.KeySteps;
        public HullTeachingOverlay3D overlay;
        [Min(.05f)] public float stepInterval=.6f;
        [Min(.01f)] public float transitionDuration=.2f;
        [Range(32,8192)] public int comparisonBudget=4096;
        public TeachingAlgorithm3D SelectedAlgorithm{get;private set;}
        public HullTeachingSession3D Session{get;private set;}
        public bool IsActive=>Session!=null;
        public bool IsPaused{get;private set;}
        public float Speed{get;private set;}=1;
        public bool CanStep=>IsActive&&IsPaused&&!singleStep&&!complete;
        public string Message{get;private set;}="";
        public event Action Changed;
        float elapsed;bool ready,singleStep,complete;
        public void SelectAlgorithm(int index){if(IsActive)return;SelectedAlgorithm=(TeachingAlgorithm3D)Mathf.Clamp(index,0,3);Changed?.Invoke();}
        public bool Play(out string error){if(IsActive){error="演示正在进行";return false;}if(demo.Points.Count==0){error="请先添加点";return false;}
            demo.FlushChanges();
            var snapshot=new List<Vector3>();foreach(var p in demo.Points)snapshot.Add(p.Position);
            try{Session=new HullTeachingSession3D(snapshot,SelectedAlgorithm,demo.relativeTolerance,mergeHullDetail);}catch(Exception e){Session=null;error=e.Message;return false;}
            IsPaused=false;singleStep=false;ready=false;complete=false;elapsed=0;Message="准备演示";
            // 不修改 Camera、Time.timeScale 或飞行手势；动画暂停仅作用于这个播放器。
            demo.SetTeachingMode(true);overlay.Begin(Session,demo.worldCamera);Changed?.Invoke();error="";return true;
        }
        public void TogglePause(){if(!IsActive)return;IsPaused=!IsPaused;singleStep=false;Changed?.Invoke();}
        public void NextStep(){if(!CanStep)return;singleStep=true;ready=false;elapsed=0;Changed?.Invoke();}
        public void SetSpeed(int index){float[] values={.25f,.5f,1,2,4};Speed=values[Mathf.Clamp(index,0,4)];Changed?.Invoke();}
        void Update(){Tick(Time.unscaledDeltaTime);}
        public void Tick(float seconds){if (demo != null && demo.IsScenePaused) return;if(!IsActive||(IsPaused&&!singleStep))return;
            if(complete){if(singleStep){overlay.Animate(1);singleStep=false;Changed?.Invoke();return;}elapsed+=seconds*Speed;if(elapsed>=stepInterval)Stop();return;}
            if(!ready){try{if(!Session.Advance(comparisonBudget)){if(Session.Finished)Stop();return;}overlay.Apply(Session.Current);overlay.ApplyPointColors(demo);Message=Session.Current.Message;elapsed=0;ready=true;complete=Session.Current.Kind==TeachingEventKind3D.Complete;Changed?.Invoke();}catch(Exception e){Stop();Message="演示中止："+e.Message;Changed?.Invoke();}return;}
            elapsed+=Mathf.Max(0,seconds)*Speed;overlay.Animate(Mathf.Clamp01(elapsed/Mathf.Max(.01f,transitionDuration)));
            if(singleStep){if(elapsed>=transitionDuration){singleStep=false;Changed?.Invoke();}return;}
            if(elapsed>=Mathf.Max(stepInterval,transitionDuration)){ready=false;elapsed=0;}
        }
        public void Stop(){if(Session==null)return;Session.Dispose();Session=null;IsPaused=false;singleStep=false;complete=false;ready=false;elapsed=0;overlay.Clear();overlay.gameObject.SetActive(false);if(demo!=null)demo.SetTeachingMode(false);Changed?.Invoke();}
        void OnDisable(){Stop();}
    }
}
