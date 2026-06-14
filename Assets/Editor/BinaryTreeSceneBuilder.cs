using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VisualAlgo.BinaryTree;
using VisualAlgo.Managers;
using VisualAlgo.Managers.UI;

namespace VisualAlgo.Editor
{
    /// <summary>
    /// 负责构建 BinaryTree 场景、预制体与新增 UI。
    /// </summary>
    public static class BinaryTreeSceneBuilder
    {
        /// <summary>
        /// 目标场景路径。
        /// </summary>
        private const string ScenePath = "Assets/Scenes/03-BinaryTree.unity";

        /// <summary>
        /// 节点预制体路径。
        /// </summary>
        private const string NodePrefabPath = "Assets/Perfabs/Binary Tree Node.prefab";

        /// <summary>
        /// 树预制体路径。
        /// </summary>
        private const string TreePrefabPath = "Assets/Perfabs/Binary Tree Group.prefab";

        /// <summary>
        /// 节点精灵路径。
        /// </summary>
        private const string SquareSpritePath = "Assets/Img/Square.png";

        /// <summary>
        /// 统一字体路径。
        /// </summary>
        private const string FontAssetPath = "Assets/Fonts/GenSenRounded2-M SDF.asset";

        /// <summary>
        /// 重建 BinaryTree 场景。
        /// </summary>
        /// <returns>执行结果。</returns>
        public static string RebuildScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null) return $"Scene not found: {ScenePath}";

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            TMP_DefaultControls.Resources resources = CreateTmpResources();

            EnsureMainCamera();
            EnsureEventSystem();
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null) return "Canvas not found in BinaryTree scene.";

            DestroyChildIfExists(canvasObject.transform, "BinaryTree Global Controls");
            DestroyChildIfExists(canvasObject.transform, "Selected BinaryTree Controls");
            DestroyRootIfExists("Binary Tree Systems");
            DestroyChildIfExists(canvasObject.transform, "BinaryTree UI Controller");

            BinaryTreeNodeView nodePrefab = BuildNodePrefab(squareSprite, fontAsset);
            BinaryTreeController treePrefab = BuildTreePrefab();

            GameObject systemsObject = new("Binary Tree Systems");
            Transform treesRoot = new GameObject("Trees Root").transform;
            treesRoot.SetParent(systemsObject.transform, false);
            BinaryTreeFactory treeFactory = GetOrAddComponent<BinaryTreeFactory>(systemsObject);
            BinaryTreeComparisonManager comparisonManager = GetOrAddComponent<BinaryTreeComparisonManager>(systemsObject);
            BinaryTreeInputInteractor inputInteractor = GetOrAddComponent<BinaryTreeInputInteractor>(systemsObject);

            GameObject globalPanelObject = canvasObject.transform.Find("Control Panel")?.gameObject;
            if (globalPanelObject == null) return "Control Panel not found in BinaryTree scene.";
            PanelController panelController = globalPanelObject.GetComponent<PanelController>();
            GlobalControlsBundle globalControls = CreateGlobalControls(globalPanelObject.transform, resources, fontAsset);
            SelectedControlsBundle selectedControls = CreateSelectedControls(canvasObject.transform, resources, fontAsset);

            GameObject uiControllerObject = CreateUiObject("BinaryTree UI Controller", canvasObject.transform);
            BinaryTreeUIController uiController = GetOrAddComponent<BinaryTreeUIController>(uiControllerObject);

            SetPrivateObjectField(treeFactory, "treePrefab", treePrefab);
            SetPrivateObjectField(treeFactory, "treesRoot", treesRoot);

            SetPrivateObjectField(comparisonManager, "treeFactory", treeFactory);
            SetPrivateObjectField(comparisonManager, "treePrefab", treePrefab);
            SetPrivateObjectField(comparisonManager, "nodePrefab", nodePrefab);
            SetPrivateObjectField(comparisonManager, "treesRoot", treesRoot);
            SetPrivateFloatField(comparisonManager, "treeSpacing", 8f);
            SetPrivateFloatField(comparisonManager, "stepInterval", 0.45f);
            SetPrivateEnumField(comparisonManager, "newTreeMode", 0);

            SetPrivateObjectField(inputInteractor, "comparisonManager", comparisonManager);

            SetPrivateObjectField(uiController, "comparisonManager", comparisonManager);
            SetPrivateObjectField(uiController, "globalPanel", panelController);
            SetPrivateObjectField(uiController, "selectedTreePanel", selectedControls.PanelRect);
            SetPrivateObjectField(uiController, "intervalInput", globalControls.IntervalInput);
            SetPrivateObjectField(uiController, "newTreeModeDropdown", globalControls.NewTreeModeDropdown);
            SetPrivateObjectField(uiController, "globalTargetInput", globalControls.TargetInput);
            SetPrivateObjectField(uiController, "globalInsertListInput", globalControls.InsertListInput);
            SetPrivateObjectField(uiController, "globalNewValueInput", globalControls.NewValueInput);
            SetPrivateObjectField(uiController, "addTreeButton", globalControls.AddTreeButton);
            SetPrivateObjectField(uiController, "globalSearchButton", globalControls.SearchButton);
            SetPrivateObjectField(uiController, "globalInsertButton", globalControls.InsertButton);
            SetPrivateObjectField(uiController, "globalUpdateButton", globalControls.UpdateButton);
            SetPrivateObjectField(uiController, "globalDeleteButton", globalControls.DeleteButton);
            SetPrivateObjectField(uiController, "pauseResumeButton", globalControls.PauseResumeButton);
            SetPrivateObjectField(uiController, "stopButton", globalControls.StopButton);
            SetPrivateObjectField(uiController, "pauseResumeButtonText", globalControls.PauseResumeButtonText);
            SetPrivateObjectField(uiController, "selectedTreeTitleText", selectedControls.TitleText);
            SetPrivateObjectField(uiController, "selectedTreeModeDropdown", selectedControls.ModeDropdown);
            SetPrivateObjectField(uiController, "selectedTargetInput", selectedControls.TargetInput);
            SetPrivateObjectField(uiController, "selectedInsertListInput", selectedControls.InsertListInput);
            SetPrivateObjectField(uiController, "selectedNewValueInput", selectedControls.NewValueInput);
            SetPrivateObjectField(uiController, "selectedSearchButton", selectedControls.SearchButton);
            SetPrivateObjectField(uiController, "selectedInsertButton", selectedControls.InsertButton);
            SetPrivateObjectField(uiController, "selectedUpdateButton", selectedControls.UpdateButton);
            SetPrivateObjectField(uiController, "selectedDeleteButton", selectedControls.DeleteButton);
            SetPrivateObjectField(uiController, "clearSelectedTreeButton", selectedControls.ClearButton);

            GameObject firstTreeObject = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab.gameObject);
            firstTreeObject.name = "Binary Tree 1";
            firstTreeObject.transform.SetParent(treesRoot, false);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return "BinaryTree scene rebuilt successfully.";
        }

        /// <summary>
        /// 创建 TMP 控件资源。
        /// </summary>
        /// <returns>控件资源。</returns>
        private static TMP_DefaultControls.Resources CreateTmpResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }

        /// <summary>
        /// 确保主相机带有 CameraManager。
        /// </summary>
        private static void EnsureMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 8f;
            mainCamera.backgroundColor = Color.white;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            GetOrAddComponent<CameraManager>(mainCamera.gameObject);
        }

        /// <summary>
        /// 确保事件系统存在。
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (GameObject.Find("EventSystem") != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>
        /// 创建左侧全局控制内容。
        /// </summary>
        /// <param name="controlPanelTransform">现有控制面板节点。</param>
        /// <param name="resources">TMP 资源。</param>
        /// <param name="fontAsset">统一字体。</param>
        /// <returns>控件集合。</returns>
        private static GlobalControlsBundle CreateGlobalControls(Transform controlPanelTransform, TMP_DefaultControls.Resources resources, TMP_FontAsset fontAsset)
        {
            GameObject root = CreateUiObject("BinaryTree Global Controls", controlPanelTransform);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-24f, -36f));

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 26, 18);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel("Title", root.transform, "二叉树", 34, fontAsset, TextAlignmentOptions.Center);

            GameObject intervalSection = CreateSection(root.transform, "Interval Section");
            CreateLabel("Interval Label", intervalSection.transform, "操作间隔", 24, fontAsset);
            TMP_InputField intervalInput = CreateInputField("间隔 Input", intervalSection.transform, "秒", new Vector2(280f, 42f), TMP_InputField.ContentType.DecimalNumber, resources, fontAsset);

            GameObject modeSection = CreateSection(root.transform, "Mode Section");
            CreateLabel("Mode Label", modeSection.transform, "新建树模式", 24, fontAsset);
            TMP_Dropdown newTreeModeDropdown = CreateDropdown("新建树模式 Dropdown", modeSection.transform, new Vector2(280f, 42f), resources, fontAsset);

            GameObject valueSection = CreateSection(root.transform, "Value Section");
            CreateLabel("Value Label", valueSection.transform, "全局操作值", 24, fontAsset);
            TMP_InputField targetInput = CreateInputField("目标值 Input", valueSection.transform, "目标值", new Vector2(280f, 42f), TMP_InputField.ContentType.IntegerNumber, resources, fontAsset);
            BinaryTreeInsertListInput insertListInput = CreateInsertListInput("插入列表控件", valueSection.transform, new Vector2(280f, 42f), resources, fontAsset);
            TMP_InputField newValueInput = CreateInputField("新值 Input", valueSection.transform, "新值", new Vector2(280f, 42f), TMP_InputField.ContentType.IntegerNumber, resources, fontAsset);

            GameObject controlSection = CreateSection(root.transform, "Control Section");
            Button pauseResumeButton = CreateButton("暂停恢复 Button", controlSection.transform, "暂停", new Vector2(280f, 44f), resources, fontAsset);
            TMP_Text pauseResumeButtonText = pauseResumeButton.GetComponentInChildren<TMP_Text>(true);
            Button stopButton = CreateButton("停止 Button", controlSection.transform, "停止", new Vector2(280f, 44f), resources, fontAsset);

            GameObject actionSection = CreateSection(root.transform, "Action Section");
            Button addTreeButton = CreateButton("新建树 Button", actionSection.transform, "新建树", new Vector2(280f, 44f), resources, fontAsset);
            Button searchButton = CreateButton("全局查找 Button", actionSection.transform, "全局查找", new Vector2(280f, 44f), resources, fontAsset);
            Button insertButton = CreateButton("全局插入 Button", actionSection.transform, "全局插入", new Vector2(280f, 44f), resources, fontAsset);
            Button updateButton = CreateButton("全局修改 Button", actionSection.transform, "全局修改", new Vector2(280f, 44f), resources, fontAsset);
            Button deleteButton = CreateButton("全局删除 Button", actionSection.transform, "全局删除", new Vector2(280f, 44f), resources, fontAsset);

            return new GlobalControlsBundle
            {
                IntervalInput = intervalInput,
                NewTreeModeDropdown = newTreeModeDropdown,
                TargetInput = targetInput,
                InsertListInput = insertListInput,
                NewValueInput = newValueInput,
                PauseResumeButton = pauseResumeButton,
                PauseResumeButtonText = pauseResumeButtonText,
                StopButton = stopButton,
                AddTreeButton = addTreeButton,
                SearchButton = searchButton,
                InsertButton = insertButton,
                UpdateButton = updateButton,
                DeleteButton = deleteButton
            };
        }

        /// <summary>
        /// 创建顶部单树控制面板。
        /// </summary>
        /// <param name="canvasTransform">Canvas 根节点。</param>
        /// <param name="resources">TMP 资源。</param>
        /// <param name="fontAsset">统一字体。</param>
        /// <returns>控件集合。</returns>
        private static SelectedControlsBundle CreateSelectedControls(Transform canvasTransform, TMP_DefaultControls.Resources resources, TMP_FontAsset fontAsset)
        {
            GameObject panel = CreateUiObject("Selected BinaryTree Controls", canvasTransform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            SetRect(panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(420f, 300f));
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.11f, 0.94f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text titleText = CreateLabel("当前树 Text", panel.transform, string.Empty, 28, fontAsset).GetComponent<TMP_Text>();
            CreateLabel("Single Mode Label", panel.transform, "树模式", 22, fontAsset);
            TMP_Dropdown modeDropdown = CreateDropdown("当前树模式 Dropdown", panel.transform, new Vector2(360f, 42f), resources, fontAsset);
            CreateLabel("Single Value Label", panel.transform, "单树操作值", 22, fontAsset);
            TMP_InputField targetInput = CreateInputField("当前树目标值 Input", panel.transform, "目标值", new Vector2(360f, 42f), TMP_InputField.ContentType.IntegerNumber, resources, fontAsset);
            BinaryTreeInsertListInput insertListInput = CreateInsertListInput("当前树插入列表控件", panel.transform, new Vector2(360f, 42f), resources, fontAsset);
            TMP_InputField newValueInput = CreateInputField("当前树新值 Input", panel.transform, "新值", new Vector2(360f, 42f), TMP_InputField.ContentType.IntegerNumber, resources, fontAsset);
            Button searchButton = CreateButton("当前树查找 Button", panel.transform, "查找", new Vector2(360f, 42f), resources, fontAsset);
            Button insertButton = CreateButton("当前树插入 Button", panel.transform, "插入", new Vector2(360f, 42f), resources, fontAsset);
            Button updateButton = CreateButton("当前树修改 Button", panel.transform, "修改", new Vector2(360f, 42f), resources, fontAsset);
            Button deleteButton = CreateButton("当前树删除 Button", panel.transform, "删除", new Vector2(360f, 42f), resources, fontAsset);
            Button clearButton = CreateButton("清空当前树 Button", panel.transform, "清空当前树", new Vector2(360f, 42f), resources, fontAsset);

            panel.SetActive(false);
            return new SelectedControlsBundle
            {
                PanelRect = panelRect,
                TitleText = titleText,
                ModeDropdown = modeDropdown,
                TargetInput = targetInput,
                InsertListInput = insertListInput,
                NewValueInput = newValueInput,
                SearchButton = searchButton,
                InsertButton = insertButton,
                UpdateButton = updateButton,
                DeleteButton = deleteButton,
                ClearButton = clearButton
            };
        }

        /// <summary>
        /// 构建二叉树节点预制体。
        /// </summary>
        /// <param name="sprite">节点使用的精灵。</param>
        /// <param name="fontAsset">统一字体。</param>
        /// <returns>节点视图组件。</returns>
        private static BinaryTreeNodeView BuildNodePrefab(Sprite sprite, TMP_FontAsset fontAsset)
        {
            GameObject root = new("Binary Tree Node");
            Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite nodeSprite = circleSprite != null ? circleSprite : sprite;

            GameObject body = new("Body");
            body.transform.SetParent(root.transform, false);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = nodeSprite;
            bodyRenderer.color = new Color(0.92f, 0.95f, 0.98f, 1f);
            bodyRenderer.sortingOrder = 10;
            body.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

            GameObject selection = new("Selection");
            selection.transform.SetParent(root.transform, false);
            SpriteRenderer selectionRenderer = selection.AddComponent<SpriteRenderer>();
            selectionRenderer.sprite = nodeSprite;
            selectionRenderer.color = new Color(0.18f, 0.52f, 0.96f, 0.28f);
            selectionRenderer.sortingOrder = 11;
            selection.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            selectionRenderer.enabled = false;

            GameObject label = new("Label");
            label.transform.SetParent(root.transform, false);
            TextMeshPro text = label.AddComponent<TextMeshPro>();
            text.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
            text.fontSize = 4.2f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.sortingOrder = 12;
            text.rectTransform.sizeDelta = new Vector2(3f, 1.4f);

            root.AddComponent<BinaryTreeNodeView>();
            PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<BinaryTreeNodeView>(NodePrefabPath);
        }

        /// <summary>
        /// 构建树预制体。
        /// </summary>
        /// <returns>树控制器组件。</returns>
        private static BinaryTreeController BuildTreePrefab()
        {
            GameObject root = new("Binary Tree Group");
            root.AddComponent<BinaryTreeController>();
            new GameObject("Nodes").transform.SetParent(root.transform, false);
            new GameObject("Edges").transform.SetParent(root.transform, false);
            new GameObject("Border").transform.SetParent(root.transform, false);
            new GameObject("Title").AddComponent<TextMeshPro>().transform.SetParent(root.transform, false);
            PrefabUtility.SaveAsPrefabAsset(root, TreePrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<BinaryTreeController>(TreePrefabPath);
        }

        /// <summary>
        /// 创建通用 UI 对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="parent">父节点。</param>
        /// <returns>创建的对象。</returns>
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        /// <summary>
        /// 创建竖向布局区域。
        /// </summary>
        /// <param name="parent">父节点。</param>
        /// <param name="name">对象名称。</param>
        /// <returns>区域对象。</returns>
        private static GameObject CreateSection(Transform parent, string name)
        {
            GameObject section = CreateUiObject(name, parent);
            VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            return section;
        }

        /// <summary>
        /// 创建标签文本。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="text">文本。</param>
        /// <param name="fontSize">字号。</param>
        /// <param name="fontAsset">字体资源。</param>
        /// <param name="alignment">对齐方式。</param>
        /// <returns>标签对象。</returns>
        private static GameObject CreateLabel(string name, Transform parent, string text, int fontSize, TMP_FontAsset fontAsset, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject labelObject = CreateUiObject(name, parent);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = alignment;
            ApplyFont(label, fontAsset, fontSize, Color.white, FontStyles.Normal);
            LayoutElement layoutElement = labelObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = fontSize + 10f;
            return labelObject;
        }

        /// <summary>
        /// 创建按钮。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="text">按钮文本。</param>
        /// <param name="size">尺寸。</param>
        /// <param name="resources">TMP 资源。</param>
        /// <param name="fontAsset">字体资源。</param>
        /// <returns>按钮组件。</returns>
        private static Button CreateButton(string name, Transform parent, string text, Vector2 size, TMP_DefaultControls.Resources resources, TMP_FontAsset fontAsset)
        {
            GameObject buttonObject = TMP_DefaultControls.CreateButton(resources);
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = buttonObject.GetComponent<Image>();
            if (image != null) image.color = new Color(0.22f, 0.28f, 0.35f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.28f, 0.35f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.35f, 0.44f, 1f);
            colors.pressedColor = new Color(0.14f, 0.18f, 0.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                label.alignment = TextAlignmentOptions.Center;
                ApplyFont(label, fontAsset, 28, Color.white, FontStyles.Normal);
            }

            LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(buttonObject);
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            return button;
        }

        /// <summary>
        /// 创建输入框。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="placeholder">占位文本。</param>
        /// <param name="size">尺寸。</param>
        /// <param name="contentType">输入类型。</param>
        /// <param name="resources">TMP 资源。</param>
        /// <param name="fontAsset">字体资源。</param>
        /// <returns>输入框组件。</returns>
        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholder, Vector2 size, TMP_InputField.ContentType contentType, TMP_DefaultControls.Resources resources, TMP_FontAsset fontAsset)
        {
            GameObject inputObject = TMP_DefaultControls.CreateInputField(resources);
            inputObject.name = name;
            inputObject.transform.SetParent(parent, false);

            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = inputObject.GetComponent<Image>();
            if (image != null) image.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            TMP_InputField inputField = inputObject.GetComponent<TMP_InputField>();
            inputField.contentType = contentType;
            TMP_Text[] texts = inputObject.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++) ApplyFont(texts[i], fontAsset, 26, Color.white, FontStyles.Normal);
            if (inputField.textComponent != null) inputField.textComponent.alignment = TextAlignmentOptions.Center;
            if (inputField.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = placeholder;
                placeholderText.alignment = TextAlignmentOptions.Center;
                ApplyFont(placeholderText, fontAsset, 24, new Color(0.72f, 0.72f, 0.72f, 1f), FontStyles.Italic);
            }

            LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(inputObject);
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            return inputField;
        }

        /// <summary>
        /// 创建支持动态添加输入框的插入列表控件。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="inputSize">输入框尺寸。</param>
        /// <param name="resources">TMP 资源。</param>
        /// <param name="fontAsset">字体资源。</param>
        /// <returns>插入列表控件组件。</returns>
        private static BinaryTreeInsertListInput CreateInsertListInput(string name, Transform parent, Vector2 inputSize, TMP_DefaultControls.Resources resources, TMP_FontAsset fontAsset)
        {
            GameObject root = CreateUiObject(name, parent);
            VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 6f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            Button addButton = CreateButton("Add Button", root.transform, "添加", new Vector2(inputSize.x, 40f), resources, fontAsset);
            Button removeButton = CreateButton("Remove Button", root.transform, "删除最后一个", new Vector2(inputSize.x, 40f), resources, fontAsset);
            GameObject listRootObject = CreateUiObject("List Root", root.transform);
            VerticalLayoutGroup listLayout = listRootObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 6f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            ContentSizeFitter listFitter = listRootObject.AddComponent<ContentSizeFitter>();
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            TMP_InputField templateInput = CreateInputField("Template Input", listRootObject.transform, "插入值", inputSize, TMP_InputField.ContentType.IntegerNumber, resources, fontAsset);
            BinaryTreeInsertListInput listInput = root.AddComponent<BinaryTreeInsertListInput>();

            SerializedObject serializedObject = new(listInput);
            serializedObject.FindProperty("listRoot").objectReferenceValue = listRootObject.GetComponent<RectTransform>();
            serializedObject.FindProperty("addButton").objectReferenceValue = addButton;
            serializedObject.FindProperty("removeButton").objectReferenceValue = removeButton;
            serializedObject.FindProperty("inputTemplate").objectReferenceValue = templateInput;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return listInput;
        }

        /// <summary>
        /// 创建下拉框。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="size">尺寸。</param>
        /// <param name="resources">TMP 资源。</param>
        /// <param name="fontAsset">字体资源。</param>
        /// <returns>下拉框组件。</returns>
        private static TMP_Dropdown CreateDropdown(string name, Transform parent, Vector2 size, TMP_DefaultControls.Resources resources, TMP_FontAsset fontAsset)
        {
            GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(resources);
            dropdownObject.name = name;
            dropdownObject.transform.SetParent(parent, false);

            RectTransform rect = dropdownObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = dropdownObject.GetComponent<Image>();
            if (image != null) image.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("朴素二叉搜索树"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("平衡二叉树"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("红黑树"));

            TMP_Text[] texts = dropdownObject.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++) ApplyFont(texts[i], fontAsset, 24, Color.white, FontStyles.Normal);
            if (dropdown.captionText != null) dropdown.captionText.alignment = TextAlignmentOptions.Center;

            LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(dropdownObject);
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            return dropdown;
        }

        /// <summary>
        /// 为 TMP 文本组件统一应用字体配置。
        /// </summary>
        /// <param name="text">目标文本。</param>
        /// <param name="fontAsset">字体资源。</param>
        /// <param name="fontSize">字号。</param>
        /// <param name="color">颜色。</param>
        /// <param name="fontStyle">样式。</param>
        private static void ApplyFont(TMP_Text text, TMP_FontAsset fontAsset, float fontSize, Color color, FontStyles fontStyle)
        {
            if (text == null) return;
            if (fontAsset != null) text.font = fontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = fontStyle;
        }

        /// <summary>
        /// 设置 RectTransform 参数。
        /// </summary>
        /// <param name="rect">目标 RectTransform。</param>
        /// <param name="anchorMin">最小锚点。</param>
        /// <param name="anchorMax">最大锚点。</param>
        /// <param name="pivot">轴心。</param>
        /// <param name="anchoredPosition">锚定坐标。</param>
        /// <param name="sizeDelta">尺寸。</param>
        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// 删除同名根对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        private static void DestroyRootIfExists(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root != null) Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 删除父节点下的指定子对象。
        /// </summary>
        /// <param name="parent">父节点。</param>
        /// <param name="childName">子对象名称。</param>
        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            if (parent == null) return;
            Transform child = parent.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        /// <summary>
        /// 获取或添加指定组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="gameObject">目标对象。</param>
        /// <returns>组件引用。</returns>
        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null) component = gameObject.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// 写入私有对象引用字段。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <param name="value">写入值。</param>
        private static void SetPrivateObjectField(Object target, string fieldName, Object value)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 写入私有浮点字段。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <param name="value">写入值。</param>
        private static void SetPrivateFloatField(Object target, string fieldName, float value)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 写入私有枚举字段。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <param name="value">写入枚举索引。</param>
        private static void SetPrivateEnumField(Object target, string fieldName, int value)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null) return;
            property.enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 全局控件引用集合。
        /// </summary>
        private sealed class GlobalControlsBundle
        {
            public TMP_InputField IntervalInput;
            public TMP_Dropdown NewTreeModeDropdown;
            public TMP_InputField TargetInput;
            public BinaryTreeInsertListInput InsertListInput;
            public TMP_InputField NewValueInput;
            public Button PauseResumeButton;
            public TMP_Text PauseResumeButtonText;
            public Button StopButton;
            public Button AddTreeButton;
            public Button SearchButton;
            public Button InsertButton;
            public Button UpdateButton;
            public Button DeleteButton;
        }

        /// <summary>
        /// 单树控件引用集合。
        /// </summary>
        private sealed class SelectedControlsBundle
        {
            public RectTransform PanelRect;
            public TMP_Text TitleText;
            public TMP_Dropdown ModeDropdown;
            public TMP_InputField TargetInput;
            public BinaryTreeInsertListInput InsertListInput;
            public TMP_InputField NewValueInput;
            public Button SearchButton;
            public Button InsertButton;
            public Button UpdateButton;
            public Button DeleteButton;
            public Button ClearButton;
        }
    }
}
