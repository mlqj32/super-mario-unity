# 刺猬云速度与抖动修复

## 原因与修改

旧逻辑在玩家速度超过 0.5 时，直接切换到玩家面朝方向前方 64 像素的目标；低于阈值则切换到另一个正弦巡游目标。位置直接按每帧 1.75/3.5 像素追赶，没有加减速过程，贴图又随瞬时速度正负翻转。停止和转向时会出现目标跳变、急刹与快速翻面。

- 用连续权重将小范围慢巡游过渡到追赶目标，去除速度阈值处的目标跳变。
- 从玩家实际水平位移估计移动速度，避免玩家被墙或相机边界挡住时仍按输入速度向前追赶。
- 保留云朵速度，每个 60 Hz 模拟步的速度改变量不超过 0.1；正常追赶上限由 3.5 降至 3.1，静止附近上限 1.1。
- 巡游范围由 56 缩至 24、周期放慢；近乎静止时保留原朝向，避免翻面抖动。
- 出场高度平滑靠近浮动高度；离场同样先减速再转向。投蛋、击杀后补位、去重与停止追踪条件保留。

## 实测

同一静止巡游测试中，最大速度由约 1.232 降至 0.260 像素/步；启停/反向速度突变量由 4.25 降至 0.1。小输入变化测试中贴图转向次数由 4 降至 1。

- 修复前专项：MARIO_LAKITU_FAIL checks=27 errors=7，见 _zcode_tmp/lakitu-motion-before.log。
- 修复后增加两侧受阻测试：MARIO_LAKITU_PASS checks=29 errors=0，见 _zcode_tmp/lakitu-motion-after.log。
- Windows 构建：MARIO_BUILD_PASS errors=0 warnings=0 bytes=191864583，见 _zcode_tmp/lakitu-motion-build.log。
- Windows 运行：MARIO_LAKITU_PASS checks=30 errors=0，进程退出 0。覆盖持续奔跑、反向、离屏追回、投蛋/准备姿势、重生、去重、停止标记、6 张真实地图及连续渲染位置。
- PlayerLakituMotionFix/lakitu-motion.csv 保存 600 帧启停/反向数据；实测 maxDeltaV=0.100001（浮点误差），最大屏幕位移 4 像素/步，无翻面位置跳变。
- 72 份关卡/区域 SHA256 未变，见 lakitu-motion-map-integrity.json。本轮使用刺猬云专项验证，没有重新运行全部游戏专项。

已实际打开 PlayerLakituMotionFix/lakitu-motion-119.png、lakitu-motion-599.png、lakitu-chase-420.png。运动连续性由逐帧轨迹检查，静态截图仅验证绘制及位置。

用户旧游戏在构建前已关闭，最新文件直接更新到 D:/Unity/project/超级马里奥/Builds/Windows/超级马里奥.exe，没有生成独立临时版本。
