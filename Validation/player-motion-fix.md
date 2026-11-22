# 马里奥移动画面排查与修复

## 已证实的问题

- 用户截图 `D:/DevTools/temp/codex-clipboard-f8d34353-3e3a-4b26-ae9d-bb3dc0a0ea2c.png` 已打开检查。截取 (86,29)-(246,221)，与 `Assets/Resources/Pixels/0136.png` 大马里奥跳跃帧按 X=10、Y=6 最近邻放大后的 30,720 像素完全相同，差异为 0。这一张图没有混合旧帧，存在横向拉伸。保比例可在设置选“原始比例”；未覆盖用户主动保存的比例偏好。
- 旧逻辑角色 X 使用 Round，相机 X 使用 Floor。即便跟随距离严格为 112，角色屏幕坐标仍在 110/111（朝左为 126/127）间变化。
- 旧跑步帧用全局 frame 除以速度决定的 3/6。跨速度阈值会改变整个时间轴，产生倒序、跳帧、仅一逻辑帧的姿势。真实走/跑转换测试在四个时间起点都复现。

## 修改

- `MarioPresentation.cs`：统一按物体相对相机的屏幕位置取整，再换回世界坐标；角色和场景使用同一规则。碰撞、速度、跳跃轨迹不变。
- 跑步使用独立且只由逻辑步推进的循环计时。加速只改变推进速度，保留当前循环相位。换区、离地、停步等重置；暂停、额外重绘、补算多步不会乱序。
- `MarioPresentationAudit.cs`：跟随位置、四组走跑转换、暂停、重绘频率与连续画面专项。Windows 连续帧检查读取 LateUpdate 实际产出的整张目标纹理，包含透明区和旧位置，防止只检查新贴图有色像素而漏掉残影。
- 旧 Editor 跑步贴图检查由手动改全局 frame 改为真实逻辑推进；隔离火棒/碰撞，避免图像夹具在移动中受伤。
- 构建支持可选输出目录。原游戏仍运行时，修正版单独放入 `Builds/Windows-MotionFix`，不关闭用户游戏。

## 验证记录

- 修复前：`MARIO_PRESENTATION_FAIL checks=18 errors=14`。
- 初次专项修复后：`MARIO_PRESENTATION_PASS checks=18 errors=0`。
- 最终 Unity：`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=4197 failed=0`。
- 原始电机参考：`MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0`。
- 构建：`MARIO_BUILD_PASS errors=0 warnings=0 bytes=191843079`。
- Windows 移动画面：`MARIO_PRESENTATION_PASS checks=867 renderedFrames=576 errors=0`；三个体型、两个朝向，共 576 个真实渲染帧。全帧 RGB 与背景/单张源贴图合成结果相同，无旧位置残留。
- 既有角色动画成品检查：`MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0`。
- Unity 结构：`UNITY_YAML_PASS files=65 blocks=72 assetGuids=1935 errors=[]`；72 个地图/区域文件 SHA256 全部保持不变。
- 已实际打开 `Validation/PlayerMotionFix/motion-34.png`、`motion-contact-sheet.png`，跳跃/跑步贴图完整，按原比例显示。
- 完整 Windows 成品：`MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=122880 errors=0`；猫里奥：`SYOBON_PLAYER_PASS stages=4 areas=8 checks=82 errors=0`。全部既有专项通过，最终 Unity 和 Player 进程退出码均为 0。

日志：`_zcode_tmp/presentation-before.log`、`presentation-after.log`、`presentation-validation-build-final.log`、`Validation/player-motion-fix.log`。第一次全量运行因旧图像夹具移动后遭火棒伤害而失败，隔离夹具后重新运行通过；一次重启验证因前一个 batch 实例仍运行被 Unity 拒绝，结束自建 batch 后已重跑成功。

只依据真实截图和自动化画面/逻辑验证，不把单张截图当作显示器动态响应测试。
