using System;
using System.Collections.Generic;
using UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ConvexHullDemo3D {
    [DefaultExecutionOrder(-50)]
    public sealed class ConvexHull3DController : MonoBehaviour {
        public const string MenuId="HullContextMenu3D";
        public HullContextMenu3D ContextMenu {get;private set;}
        public bool IsMenuOpen=>ContextMenu!=null&&ContextMenu.IsVisible;
        int? contextId,rightPointId;Vector2 rightStart;bool rightPending;
        public const string ToolbarId="HullToolbar3D";
        public Camera worldCamera;
        public UISetting uiSetting;
        public Transform pointsRoot;
        public HullPoint3D pointPrefab;
        public HullGizmo3D gizmo;
        public HullSurface3D surface;
        public HullPlayback3D playback;
        public bool IsTeaching {get;private set;}
        public HullPlaybackPanel3D PlaybackPanel {get;private set;}
        public List<HullPoint3D> previewPoints=new List<HullPoint3D>();
        [Header("World-unit appearance")]
        [Min(.01f)] public float pointDiameter=.3f;
        [Min(.001f)] public float edgeWidth=.018f;
        public Color innerColor=new Color(.32f,.40f,.49f),hullColor=new Color(.10f,.38f,.80f),selectionColor=new Color(1,.55f,.08f);
        public Color faceColor=new Color(.23f,.52f,.86f,.18f),edgeColor=new Color(.12f,.33f,.64f,.65f);
        [Header("Interaction")]
        [Min(.01f)] public float hitRadius=.24f,axisLength=1.5f,axisWidth=.035f,axisHitRadius=.13f;
        [Min(.01f)] public float moveSpeed=6,lookSensitivity=.12f,addDistance=8;
        [Min(.01f)] public float randomNear=6,randomFar=14;
        [Range(.00000001f,.0001f)] public float relativeTolerance=.0000001f;
        public Vector3[] initialPoints={new Vector3(-3,-3,-3),new Vector3(3,-3,-3),new Vector3(3,3,-3),new Vector3(-3,3,-3),new Vector3(-3,-3,3),new Vector3(3,-3,3),new Vector3(3,3,3),new Vector3(-3,3,3),Vector3.zero,new Vector3(1,1,1),new Vector3(-1,1,-1),new Vector3(1,-1,0)};
        public sealed class Point {public int Id{get;internal set;}public Vector3 Position{get;internal set;}public HullPoint3D View{get;internal set;}}
        readonly List<Point> points=new List<Point>();readonly Stack<HullPoint3D> pool=new Stack<HullPoint3D>();
        readonly List<Vector3> coordinates=new List<Vector3>();readonly HashSet<Vector3> hullPositions=new HashSet<Vector3>();
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();readonly ConvexHull3D.Solver solver=new ConvexHull3D.Solver();
        UIFrame frame;bool dirty=true,flyActive;int nextId=1,dragAxis=-1;int? selected;
        Plane dragPlane;Vector3 dragStart,dragAnchor;float yaw,pitch;
        public IReadOnlyList<Point> Points=>points;
        public HullResult3D Hull{get;private set;}
        public HullToolbar3D Toolbar{get;private set;}
        public int? SelectedId=>selected;
        public int DragAxis=>dragAxis;
        public bool IsFlying=>flyActive;
        public int HullRevision{get;private set;}
        public double LastComputeMilliseconds{get;private set;}
        public event Action Changed;
        static bool Finite(Vector3 p)=>!(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z));
        void Awake(){Cursor.visible=true;Cursor.lockState=CursorLockMode.None;foreach(var view in previewPoints){view.gameObject.SetActive(false);pool.Push(view);}foreach(var p in initialPoints)AddPoint(p,false);FlushChanges();SyncAngles();gizmo.gameObject.SetActive(false);}
        void Start(){frame=uiSetting.CreateUIInstance();Toolbar=frame.GetComponentInChildren<HullToolbar3D>(true);Toolbar.Bind(this);ContextMenu=frame.GetComponentInChildren<HullContextMenu3D>(true);if(ContextMenu)ContextMenu.Bind(this);frame.ShowPanel(ToolbarId);if(playback!=null){PlaybackPanel=frame.GetComponentInChildren<HullPlaybackPanel3D>(true);PlaybackPanel.Bind(playback,worldCamera);playback.Changed+=RefreshPlaybackPanel;}}
        void RefreshPlaybackPanel(){if(frame==null||PlaybackPanel==null)return;if(playback.IsActive){if(!PlaybackPanel.IsVisible)frame.ShowPanel(PlaybackPanel.ScreenId);}else if(PlaybackPanel.IsVisible)PlaybackPanel.Hide();}
        public void SetTeachingMode(bool active){CloseMenu();rightPending=false;if(active&&Toolbar!=null)Toolbar.countInput.DeactivateInputField();IsTeaching=active;dragAxis=-1;selected=null;surface.gameObject.SetActive(!active);UpdateAppearance();UpdateGizmo();Changed?.Invoke();}
        void OnDestroy(){if(playback!=null)playback.Changed-=RefreshPlaybackPanel;}
        void SyncAngles(){yaw=worldCamera.transform.eulerAngles.y;pitch=worldCamera.transform.eulerAngles.x;if(pitch>180)pitch-=360;}
        bool InputFocused=>EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.GetComponentInParent<InputField>() is InputField input&&input.isFocused;
        // 场景暂停与算法自身暂停分开，恢复后不会丢失演示会话。
        public bool IsScenePaused { get; set; }
        public bool UsesPauseMenu { get; set; }
        void Update(){
            if (IsScenePaused || (UsesPauseMenu && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)) return;
            var mouse=Mouse.current;var keyboard=Keyboard.current;if(mouse==null)return;Vector2 screen=mouse.position.ReadValue();
            if(mouse.rightButton.wasPressedThisFrame)BeginRight(screen);
            Vector3 movement=Vector3.zero;if(keyboard!=null&&!InputFocused){movement.x=(keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0);movement.y=(keyboard.eKey.isPressed?1:0)-(keyboard.qKey.isPressed?1:0);movement.z=(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0);}
            if(mouse.rightButton.isPressed){ContinueRight(screen,movement);if(flyActive&&!InputFocused)Fly(mouse.delta.ReadValue(),movement,Time.unscaledDeltaTime);}
            if(mouse.rightButton.wasReleasedThisFrame)EndRight(screen);
            if(mouse.leftButton.wasPressedThisFrame&&IsMenuOpen&&!ContextMenu.Contains(screen))CloseMenu();
            if(!flyActive){if(mouse.leftButton.wasPressedThisFrame)BeginLeft(screen);if(mouse.leftButton.isPressed)Drag(screen);if(mouse.leftButton.wasReleasedThisFrame)EndDrag();}
            if(keyboard!=null&&!InputFocused&&keyboard.escapeKey.wasPressedThisFrame&&IsTeaching)playback.Stop();
            if(keyboard!=null&&!IsTeaching&&!InputFocused&&keyboard.deleteKey.wasPressedThisFrame&&selected.HasValue)RemovePoint(selected.Value);
        }
        void LateUpdate(){FlushChanges();UpdateGizmo();}
        public bool PointerOverUI(Vector2 screen){if(EventSystem.current==null)return false;uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},uiHits);return uiHits.Count>0;}
        // 在任意节点上右键单击选中并打开菜单；拖动超过 4 像素或按移动键则继续相机漫游。
        public void BeginRight(Vector2 screen){
            rightPending=false;if(IsScenePaused||PointerOverUI(screen)||InputFocused||dragAxis>=0||!worldCamera.pixelRect.Contains(screen))return;
            CloseMenu();rightStart=screen;rightPointId=HitTest(screen);
            if(!IsTeaching&&rightPointId.HasValue)rightPending=true;else BeginFly(screen);
        }
        public void ContinueRight(Vector2 screen,Vector3 movement){if(!rightPending)return;if((screen-rightStart).sqrMagnitude>16||movement.sqrMagnitude>0){rightPending=false;BeginFly(rightStart);}}
        public void EndRight(Vector2 screen){bool open=rightPending&&(screen-rightStart).sqrMagnitude<=16&&HitTest(screen)==rightPointId;rightPending=false;EndFly();if(open)OpenContextMenu(screen);}
        public bool OpenContextMenu(Vector2 screen){
            if(IsScenePaused||IsTeaching||ContextMenu==null||PointerOverUI(screen))return false;
            var hit=HitTest(screen);if(!hit.HasValue)return false;
            EndFly();dragAxis=-1;Select(hit);contextId=hit;frame.ShowPanel(MenuId);ContextMenu.PositionAt(screen);return true;
        }
        public void CloseMenu(){contextId=null;if(IsMenuOpen)frame.HidePanel(MenuId);}
        public void DeleteContextPoint(){if(IsScenePaused||IsTeaching){CloseMenu();return;}var id=contextId;CloseMenu();if(id.HasValue)RemovePoint(id.Value);}
        public void BeginFly(Vector2 screen){if(dragAxis>=0||PointerOverUI(screen)||InputFocused||!worldCamera.pixelRect.Contains(screen))return;SyncAngles();flyActive=true;}
        public void EndFly(){flyActive=false;}
        public void Fly(Vector2 delta,Vector3 localMovement,float seconds){if(!flyActive||dragAxis>=0||InputFocused)return;yaw+=delta.x*lookSensitivity;pitch=Mathf.Clamp(pitch-delta.y*lookSensitivity,-89,89);worldCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);worldCamera.transform.position+=worldCamera.transform.TransformDirection(Vector3.ClampMagnitude(localMovement,1))*moveSpeed*Mathf.Max(0,seconds);}
        public void CancelGestures(){flyActive=false;dragAxis=-1;rightPending=false;CloseMenu();}
        void OnApplicationFocus(bool focus){if(!focus)CancelGestures();}
        void OnDisable(){CancelGestures();}
        public int? HitTest(Vector2 screen){Ray ray=worldCamera.ScreenPointToRay(screen);int? best=null;float nearest=float.PositiveInfinity;float radius=Mathf.Max(hitRadius,pointDiameter*.5f);
            foreach(var p in points){var offset=ray.origin-p.Position;float b=Vector3.Dot(offset,ray.direction);float discriminant=b*b-offset.sqrMagnitude+radius*radius;if(discriminant<0)continue;float distance=-b-Mathf.Sqrt(discriminant);if(distance<0)distance=-b+Mathf.Sqrt(discriminant);if(distance<0)continue;
                bool tied=Mathf.Abs(distance-nearest)<.00001f;if(distance<nearest-.00001f||(tied&&(p.Id==selected||(best!=selected&&(!best.HasValue||p.Id>best.Value))))){best=p.Id;nearest=distance;}}
            return best;
        }
        public void Select(int? id){if(IsTeaching)return;CloseMenu();selected=Find(id)!=null?id:null;UpdateAppearance();UpdateGizmo();Changed?.Invoke();}
        public void BeginLeft(Vector2 screen){if(IsTeaching)return;if(flyActive||PointerOverUI(screen)||!worldCamera.pixelRect.Contains(screen))return;var point=Find(selected);
            if(point!=null){UpdateGizmo();int axis=gizmo.Hit(worldCamera.ScreenPointToRay(screen),point.Position,axisLength,axisHitRadius);if(axis>=0&&BeginAxisDrag(axis,screen))return;}
            Select(HitTest(screen));
        }
        public bool BeginAxisDrag(int axis,Vector2 screen){if(IsTeaching)return false;var point=Find(selected);if(point==null||axis<0||axis>2||flyActive)return false;UpdateGizmo();if(!gizmo.Available[axis])return false;
            Vector3 direction=HullGizmo3D.Axis(axis),view=(worldCamera.transform.position-point.Position).normalized;
            Vector3 normal=view-direction*Vector3.Dot(view,direction);if(normal.sqrMagnitude<.01f)return false;
            dragPlane=new Plane(normal.normalized,point.Position);Ray ray=worldCamera.ScreenPointToRay(screen);if(!dragPlane.Raycast(ray,out float enter))return false;
            dragAxis=axis;dragStart=point.Position;dragAnchor=ray.GetPoint(enter);return true;
        }
        public void Drag(Vector2 screen){if(dragAxis<0)return;var point=Find(selected);if(point==null){EndDrag();return;}Ray ray=worldCamera.ScreenPointToRay(screen);if(!dragPlane.Raycast(ray,out float enter))return;
            Vector3 axis=HullGizmo3D.Axis(dragAxis);Vector3 position=dragStart+axis*Vector3.Dot(ray.GetPoint(enter)-dragAnchor,axis);if(Finite(position))MovePoint(point.Id,position);
        }
        public void EndDrag(){dragAxis=-1;UpdateGizmo();}
        void UpdateGizmo(){if(IsTeaching){gizmo.gameObject.SetActive(false);return;}var point=Find(selected);if(point==null){gizmo.gameObject.SetActive(false);return;}gizmo.Display(point.Position,worldCamera,Mathf.Max(.4f,axisLength),Mathf.Max(.005f,axisWidth),dragAxis);}
        Point Find(int? id){if(!id.HasValue)return null;foreach(var p in points)if(p.Id==id)return p;return null;}
        public Point AddPoint(Vector3 position,bool select=true){if(IsTeaching)return null;if(!Finite(position))throw new ArgumentException("Point position must be finite.");CancelGestures();var view=pool.Count>0?pool.Pop():Instantiate(pointPrefab,pointsRoot);view.gameObject.SetActive(true);view.transform.position=position;view.transform.localScale=Vector3.one*pointDiameter;
            var point=new Point{Id=nextId++,Position=position,View=view};view.name="Point "+point.Id;points.Add(point);dirty=true;if(select)Select(point.Id);return point;
        }
        public Point AddInFront()=>AddPoint(worldCamera.transform.position+worldCamera.transform.forward*Mathf.Max(.1f,addDistance));
        public void MovePoint(int id,Vector3 position){if(IsTeaching)return;if(!Finite(position))throw new ArgumentException("Point position must be finite.");var p=Find(id);if(p==null||p.Position==position)return;p.Position=position;p.View.transform.position=position;dirty=true;}
        public void RemovePoint(int id){if(IsTeaching)return;CancelGestures();var point=Find(id);if(point==null)return;points.Remove(point);point.View.gameObject.SetActive(false);pool.Push(point.View);if(selected==id)selected=null;dirty=true;UpdateGizmo();}
        public void ClearPoints(){if(IsTeaching)return;CancelGestures();foreach(var p in points){p.View.gameObject.SetActive(false);pool.Push(p.View);}points.Clear();selected=null;dirty=true;UpdateGizmo();}
        public Rect RandomScreenRect(){Rect rect=worldCamera.pixelRect;float scale=Toolbar!=null?Toolbar.GetComponentInParent<Canvas>().scaleFactor:1;float margin=28*scale;float top=Toolbar!=null?Toolbar.ScreenBottom:rect.yMax;return Rect.MinMaxRect(rect.xMin+margin,rect.yMin+margin,rect.xMax-margin,top-margin);}
        public bool Randomize(string text,out string error){if(IsTeaching){error="演示期间不可修改点集";return false;}if(!int.TryParse(text,out int count)||count<1||count>1000){error="请输入 1–1000 的整数";return false;}Rect area=RandomScreenRect();if(area.width<1||area.height<1){error="窗口太小，请扩大窗口";return false;}
            float near=Mathf.Max(worldCamera.nearClipPlane+pointDiameter,randomNear),far=Mathf.Max(near+.1f,randomFar);far=Mathf.Min(far,worldCamera.farClipPlane-pointDiameter);if(far<=near){error="随机深度超出相机可见范围";return false;}
            ClearPoints();for(int i=0;i<count;i++){float z=UnityEngine.Random.Range(near,far);AddPoint(worldCamera.ScreenToWorldPoint(new Vector3(UnityEngine.Random.Range(area.xMin,area.xMax),UnityEngine.Random.Range(area.yMin,area.yMax),z)),false);}error="";return true;
        }
        public void FlushChanges(){if(IsTeaching||!dirty)return;coordinates.Clear();foreach(var p in points)coordinates.Add(p.Position);var watch=System.Diagnostics.Stopwatch.StartNew();var result=solver.Compute(coordinates,relativeTolerance);watch.Stop();LastComputeMilliseconds=watch.Elapsed.TotalMilliseconds;Hull=result;HullRevision++;dirty=false;
            hullPositions.Clear();foreach(var v in result.Vertices)hullPositions.Add(v);surface.Display(result,faceColor,edgeColor,edgeWidth);UpdateAppearance();Changed?.Invoke();}
        void UpdateAppearance(){foreach(var p in points){p.View.transform.localScale=Vector3.one*pointDiameter;p.View.SetColor(IsTeaching?innerColor:p.Id==selected?selectionColor:hullPositions.Contains(p.Position)?hullColor:innerColor);}}
        void OnValidate(){pointDiameter=Mathf.Max(.01f,pointDiameter);axisLength=Mathf.Max(.4f,axisLength);axisWidth=Mathf.Max(.005f,axisWidth);moveSpeed=Mathf.Max(.01f,moveSpeed);if(Application.isPlaying){dirty=true;UpdateAppearance();}}
    }
}
