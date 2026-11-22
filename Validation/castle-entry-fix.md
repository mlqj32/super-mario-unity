# 通关行走与城堡入口修复

## 根因

用户两张截图已实际打开：`D:/DevTools/temp/codex-clipboard-639aee38-9f9e-4cf4-a38d-e6928cc4ab6e.png`、`D:/DevTools/temp/codex-clipboard-ba8c00b2-f789-4b77-ad62-78ac2689f27c.png`。

旧 Clear 阶段只在前 99 步直接给 X 加 1.25，并把 Y 减到 0；没有正常落地判定、水平速度、转向或门口检测。拔旗时保留的 airborne 状态让人物沿地面使用跳跃贴图。固定距离结束后人物停在任意位置，没有进入城堡并隐藏的状态。

## 修改

- 拔旗结束后转向右方，通过地形碰撞从旗台落地，设置实际行走速度，由现有连续跑步动画显示脚步。
- 从当前地图找到旗杆右侧地面的城堡门。初代使用 CastleDoor，排除大城堡高层装饰门窗；日版使用原始 CastleMetatiles 的 4b 门下半块，未重新导入关卡。
- 到达门中心后停止并隐藏角色，暂停时保持隐藏，进入下一关/重玩时清理入口状态。
- 有真实门的关卡必须走到门口，才能完成换关；音乐播放、时间计分、烟花结束条件继续生效。烟花也等待进门。
- 编辑者移动城堡后，角色按新位置行走；删除门后保留通关计时的退路，不因缺少入口永久卡住。
- 城堡内部斧头后的自动行走也使用正常落地/走路状态，保留原本音乐与下一关/结局流程。

## 测试

- 修复前：MARIO_CASTLE_ENTRY_FAIL checks=990 errors=330。
- 首轮专项修复后：MARIO_CASTLE_ENTRY_PASS checks=1320 errors=0，覆盖55个旗杆终点区域、三种体型，验证跑步帧、入口坐标、隐藏、暂停及下一关恢复。
- 最终 Unity：`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=5520 failed=0`。
- 原始电机参考：`MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0`。
- Windows 构建：`MARIO_BUILD_PASS errors=0 warnings=0 bytes=191851783`，已更新标准目录 `Builds/Windows/超级马里奥.exe`。上一轮临时目录 Windows-MotionFix 不含本次入口修复。
- 结构：`UNITY_YAML_PASS files=65 blocks=72 assetGuids=1938 errors=[]`。最终72个原关卡资产/猫里奥区域CSV的SHA256均保持不变。
- Windows 既有音乐专项：`MARIO_FINISH_AUDIO_PASS checks=119 errors=0`。
- Windows 入口专项：`MARIO_CASTLE_ENTRY_PASS checks=1327 errors=0`。
- 完整 Windows：`MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=122880 errors=0`；猫里奥：`SYOBON_PLAYER_PASS stages=4 areas=8 checks=82 errors=0`。本次 Unity 和 Player 最终进程退出码均为0。
- 实际打开检查 `Validation/PlayerCastleEntryFix/castle-entry-1-1-walk.png`、`castle-entry-1-1-inside.png`、`castle-entry-J-8-3-walk.png`、`castle-entry-J-8-3-inside.png`，可见跑步姿势及进门后角色隐藏。另保存初代1-3、日版1-1两组截图。
- 入口专项从旗杆附近布置角色再通过真实逻辑推进；全关卡压力测试含无敌，不等同于64关从头人工通关。

日志：`_zcode_tmp/castle-entry-before.log`、`castle-entry-after.log`、`castle-entry-validation-build.log`，以及 `Validation/player-castle-entry-fix.log`。

相关文件：MarioCastleEntry.cs、MarioCastleEntryAudit.cs、MarioCastleEntryValidation.cs，以及 MarioGame/MarioActors/MarioFinishAudio/MarioEffects 的流程接入。原有地图数据未覆盖。
