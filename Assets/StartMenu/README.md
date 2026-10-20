# VisualAlgo 开始界面

打开 `Assets/Scenes/00-StartMenu.unity` 运行。五个主要按钮进入序列演绎、路径探索、二叉树、二维凸包、三维凸包；下方两个次要按钮打开制作人信息和退出确认。

## 风格与排布

复用原项目的 `Assets/Img/BackGround.png` 和 `Assets/Img/Titles/Title4.png`，保持黑白笔刷背景、原版标题字。五个场景按钮使用切角边框、金属色描边、彩色顶部识别条和算法图形。制作人、退出和弹窗操作使用独立的简洁细边框按钮；关闭按钮仅显示叉号。前三个场景恢复原有柱形排序、路径绘制和二叉树调整悬停动画，按钮本体不缩放；两个凸包入口无图形或缩放动画，仅保留按钮颜色反馈。无需额外图片素材。

- 宽屏设计尺寸 1920×1080：五个主要入口并排，制作人和退出位于下方。
- 宽高比小于 1.4 时采用 1440×1620：三个主要入口在第一排，两个凸包入口居中放在第二排。
- 背景始终等比居中裁切；标题保持原图片比例。按钮边框使用 UI 网格绘制，缩放不拉伸切角线宽。

## 编辑与框架

`StartMenuSetting.asset` 注册 `StartMenuPanel`、`StartMenuDialog`；通过 UISetting.CreateUIInstance、UIFrame.ShowPanel 和 PanelController 管理生命周期。`Prefabs/StartMenuFrame.prefab` 包含 URP Overlay UI 相机与唯一 EventSystem。

`Prefabs/StartMenuPanel.prefab` 保存背景、标题、七个按钮及绑定。可在 Inspector 修改文字、颜色和图形；`LaunchButtonGraphic` 绘制新按钮边框，`MenuGraphic` 绘制算法符号。字体复用 `Assets/ConvexHullDemo/Fonts/GenSenRounded2-M.ttc`。

`StartMenuResponsive` 等待框架将面板挂到 Canvas 后再解析实际比例，并切换上述布局。修改位置需同步其布局参数。`StartMenuDialog` 显示时阻止底层交互，但不对背景或底层按钮作灰化处理；ESC 可关闭，退出需要确认。原场景顺序、算法功能及凸包暂停菜单不变。

## 备份与制作

网页卡片风格的完整版本已提交到 `backup/start-menu-cards-20261020`，提交 `c66dae5`。之后使用 Git 从 `aedeb35` 恢复原开始场景，确认原背景与标题，再通过 Pipeline 重制按钮和场景。

`AgentScripts/BuildUnifiedStartMenu.cs` 的 `BuildUnifiedStartMenu.Run` 是当前可重复制作入口，会覆盖本界面 Prefab 和主菜单场景；再次运行前先保存场景，并将人工美术改动同步到该脚本。备份分支保留上一版制作脚本和资源。

## 验证

编译通过。Play Mode 下执行 `AgentScripts/VerifyUnifiedStartMenu.cs` 的 `VerifyUnifiedStartMenu.Run`：检查七个按钮的屏幕边界、宽窄布局、真实 UI 射线点击、五个场景跳转与返回、制作人弹窗、退出确认和交互隔离。

2026-10-20 最终宽屏、800×900 窄屏各 72 项通过；已截图检查 `Preview.png` 和 `PreviewNarrow.png`，最终 Console 无功能错误。退出 Play Mode 并恢复测试前 Game View 设置，未构建 Player。

`VerifyMenuHover.Run` 已验证原有三个动画进入/恢复、凸包无缩放动画、弹窗底层保持原色且阻止输入、关闭按钮无背景及小按钮采用独立样式。最新导航回归 72 项通过。
