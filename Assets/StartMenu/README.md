# VisualAlgo 开始界面

打开 `Assets/Scenes/00-StartMenu.unity` 运行。五张实验卡片分别进入排序、路径探索、二叉树、二维凸包和三维凸包；侧栏保留关于/制作人与退出确认。

## 资源与维护

- `StartMenuSetting.asset`：UIFramework 注册，启动通过 UISetting.CreateUIInstance 与 UIFrame.ShowPanel。
- `Prefabs/StartMenuFrame.prefab`：URP Overlay UI 相机、CanvasScaler，参考尺寸 1440×900、Expand 适配。
- `Prefabs/StartMenuPanel.prefab`：正式主界面，可在编辑器调整文字、卡片颜色和图形。
- `Prefabs/StartMenuDialog.prefab`：关于与退出确认，显示时阻止底层交互，ESC 可关闭。
- `Scripts/StartMenuResponsive.cs`：窗口宽高比小于 1.3 时切换两列紧凑排版；宽屏恢复原布局。修改结构时需同步其节点名称和紧凑布局。
- `Scripts/MenuGraphic.cs`：条形图、路径网格、二叉树与凸包线图的 UGUI 网格绘制，无额外贴图依赖。
- 字体复用 `Assets/ConvexHullDemo/Fonts/GenSenRounded2-M.ttc`。

全部 Prefab 和 UISetting 已持久化。原场景顺序、算法逻辑与凸包暂停菜单保持原有接入。本界面无需补充图片素材。

## 重建与验证

通过 unity-pipeline 执行 `AgentScripts/BuildUnifiedStartMenu.cs` 的 `BuildUnifiedStartMenu.Run` 可重新制作界面。重建会覆盖本界面的 Prefab 与主菜单场景，应在确实需要重建时执行，并先保存场景。后续人工美术调整需同步到制作脚本才能在再次重建后保留。

Play Mode 下执行 `AgentScripts/VerifyUnifiedStartMenu.cs` 的 `VerifyUnifiedStartMenu.Run`：检查真实 UI 射线点击、五个场景跳转和返回、制作人弹窗、退出确认及底层交互隔离。2026-10-20 宽屏与 800×900 各 42 项检查通过，已截图检查布局。未构建 Player。
