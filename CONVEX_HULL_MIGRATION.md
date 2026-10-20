# 凸包场景迁移

源工程：`/Users/hk416/UnityProjects/ConvexHullProject`。
目标工程：VisualAlgo，Unity 6000.3.12f1。

## 打开方式

从 `00-StartMenu` 的“二维凸包 / 三维凸包”按钮进入，也可直接打开场景：

- `Assets/Scenes/ConvexHullDemo.unity`：二维点编辑、实时凸包和五种教学演示。
- `Assets/Scenes/ConvexHull3DDemo.unity`：三维点编辑、实时凸包和四种教学演示。

二维：左键拖动画布，点选后拖动点，右键打开新建/删除菜单，滚轮缩放；左下角问号可打开操作说明。
三维：右键按住并移动鼠标转向，WASD/QE 沿相机局部坐标移动；左键选点后拖动 XYZ 轴，Delete 删除。
顶部可随机生成点集并选择算法播放；底部可暂停、单步、停止和调速。

## 资源和依赖

- `Assets/ConvexHullDemo`、`Assets/ConvexHull3DDemo`：脚本、预制体、网格、材质、字体、UISetting 等。
- `Assets/Scripts/UIFramework`：凸包使用的 UIFramework（不导入示例场景）。它与 VisualAlgo 自有 UI 命名空间分离。
- `Assets/External`：原框架附带的 Signals 和 DOTween 依赖。
- 保留导入资源原 GUID，迁移前核对无路径与 GUID 冲突。
- 使用目标已有 URP 17.3.0、Input System 1.19.0、UGUI 2.0.0，无需更换包版本。

## 渲染兼容

`Assets/Settings/UniversalRP.asset` 的渲染器列表末尾增加 `Assets/ConvexHull3DDemo/HullForwardRenderer.asset`，索引为 1。
原 Renderer2D 仍为默认索引 0；凸包场景相机和两套 UI Frame 相机显式使用索引 1，以支持三维透明面和 URP UI 相机叠加。
不要删除该渲染器槽位，否则凸包相机会退回默认 2D 渲染器。

主菜单使用 UIFramework，复用原版背景与标题字，重制五个算法切角按钮及制作人、退出按钮；宽屏一排五个入口，窄窗口三加二排列。详见 `Assets/StartMenu/README.md`。两个场景追加到 Build Settings，原场景序号 0–3 保持不变。未构建 Player。

两个凸包场景通过 `HullPauseIntegration` 接入 `Assets/Perfabs/ESC Pause Panel.prefab`。ESC 打开或关闭场景暂停菜单，冻结相机、点输入和演示计时；恢复时保留演示原有播放/暂停状态。支持返回游戏、返回主菜单和退出。二维缩放灵敏度绑定原菜单滑条；三维无滚轮缩放，滑条禁用。

## 验证脚本

`AgentScripts/IntegrateConvexHulls.cs` 检查场景缺失脚本，并可重复配置渲染器。
`VerifyHull3D.cs`、`VerifyConvexHullDemo.cs` 检查算法与交互。
`VerifyPlayback3D.cs`、`VerifyMultiHullLifecycle.cs` 检查播放流程。
`VerifyVisualAlgoMigration.cs` 在主菜单 Play Mode 下检查原三个入口的跳转和默认 2D 渲染器。
验证脚本通过 unity-pipeline 的 `run_script` 执行，不放入 Assets，不参与游戏运行编译。

## 首次迁移验证结果（2026-10-20，主菜单接入前）

- 统一编译完成，无编译错误。
- 六个正式场景均可加载，没有 Missing Script。
- 二维算法：76,711 个断言；交互：1,076 个断言，包含 1/2/20/1000 随机点、删除、拖动、菜单、UI 隔离。
- 二维五种教学算法：0/1/2/20 点完整播放、停止、重播、退出清理，15,137 个断言通过。
- 三维算法：131 组数据、5,482,204 个断言通过；交互：1,227 个断言通过；1000 点连续修改平均重算约 1.13 ms（本机当次测试）。
- 三维四种教学算法：134 个断言通过；演示不修改点集、实时凸包及相机，暂停/继续/停止和清理正常。
- 实际截图检查两个场景：字体、统计、点、凸包及三维透明面正常。
- 从原主菜单分别进入排序、寻路、二叉树，再返回主菜单；保持原 Build Index 1/2/3，保持 Renderer2D，无新增场景运行错误。
- 与迁移前文件哈希核对：原有 Assets / Packages / ProjectSettings 中，只有 `Assets/Settings/UniversalRP.asset` 新增一个渲染器引用；其余原文件内容不变。迁移前已有的未提交改动保留。
- 旧的同步二维大规模动画测试曾使 CLI 超时；编辑器随后恢复，改为分帧、小规模完整播放后复测通过。该超时属于验证脚本，不作为 1000 点完整动画测试通过的依据。
- 已退出 Play Mode，恢复 `00-StartMenu` 场景，未构建 Player。

## 主菜单与 ESC 接入验证

验证脚本 `VerifyHullNavigation.Run` 覆盖两个入口、暂停 UI 射线、ESC 开关、冻结演示和相机、恢复原状态以及返回主菜单。`VerifyHullEscapeLive.Begin` 在正常 PlayerLoop 中注入 ESC，验证打开、关闭和重新打开。原三个菜单入口继续由 `VerifyVisualAlgoMigration.ExistingScenes` 回归检查。

接入完成后的复测：39 项入口与暂停检查通过；二维、三维均通过正常游戏帧 ESC 打开/关闭/重开测试；排序、寻路、二叉树原有入口回归通过。两个新场景的 Build Index 为 4、5，已保存到 ProjectSettings。退出 Play Mode 后恢复主菜单，并移除测试截图及无关字体缓存改动。
