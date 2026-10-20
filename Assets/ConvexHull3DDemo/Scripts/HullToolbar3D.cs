using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;
namespace ConvexHullDemo3D {
    public sealed class HullToolbar3D : PanelController {
        public Text pointCount,hullCount,message;
        public InputField countInput;
        public Button addButton,randomButton,playButton;
        public Dropdown algorithmDropdown;
        public RectTransform card;
        public HullOrientationIndicator3D orientationIndicator;
        ConvexHull3DController demo;
        public void Bind(ConvexHull3DController value){if(demo!=null)demo.Changed-=Refresh;demo=value;if(orientationIndicator!=null)orientationIndicator.Bind(demo.worldCamera,demo.gizmo);demo.Changed+=Refresh;if(demo.playback!=null)demo.playback.Changed+=Refresh;Refresh();}
        protected override void AddListeners(){addButton.onClick.AddListener(Add);randomButton.onClick.AddListener(Randomize);if(playButton!=null)playButton.onClick.AddListener(Play);if(algorithmDropdown!=null)algorithmDropdown.onValueChanged.AddListener(Algorithm);}
        protected override void RemoveListeners(){addButton.onClick.RemoveListener(Add);randomButton.onClick.RemoveListener(Randomize);if(playButton!=null)playButton.onClick.RemoveListener(Play);if(algorithmDropdown!=null)algorithmDropdown.onValueChanged.RemoveListener(Algorithm);if(demo!=null){demo.Changed-=Refresh;if(demo.playback!=null)demo.playback.Changed-=Refresh;}}
        void Add(){demo.AddInFront();message.text="";}
        void Randomize(){demo.Randomize(countInput.text,out var error);message.text=error;}
        void Play(){if(demo.playback!=null){demo.playback.Play(out var error);message.text=error;}}
        void Algorithm(int value){if(demo!=null&&demo.playback!=null)demo.playback.SelectAlgorithm(value);}
        public void Refresh(){if(demo==null)return;pointCount.text=demo.Points.Count.ToString();hullCount.text=(demo.Hull?.Vertices.Length??0).ToString();bool locked=demo.IsTeaching;countInput.interactable=addButton.interactable=randomButton.interactable=!locked;if(playButton!=null)playButton.interactable=!locked;if(algorithmDropdown!=null){algorithmDropdown.interactable=!locked;if(demo.playback!=null)algorithmDropdown.SetValueWithoutNotify((int)demo.playback.SelectedAlgorithm);}if(demo.playback!=null&&!locked&&demo.playback.Message.StartsWith("演示中止"))message.text=demo.playback.Message;}
        public float ScreenBottom {get{var corners=new Vector3[4];card.GetWorldCorners(corners);return RectTransformUtility.WorldToScreenPoint(GetComponentInParent<Canvas>().worldCamera,corners[0]).y;}}
    }
}
