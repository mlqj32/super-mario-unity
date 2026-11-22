# 怪物尸体中途消失修复

日期：2026-10-17。

## 已复现原因

`MarioActors.TickActor` 原先让所有死亡状态共用 `age > 40` 的清理条件。以 60 Hz 计算，尸体在约 0.68 秒（第 41 帧）被删除，不检查是否还在画面里。高处打飞的板栗仔在消失时，身体顶部仍位于世界坐标 y=151；库巴为 y=167。地面敌人也可能在屏幕底边仍留有部分贴图时被删除。

此外，活体敌人通常在层级 1、食人花在 -1，而水管在 2。尸体沿用这个层级，会在下落穿过水管时被遮挡。

## 改动

- 踩扁板栗仔（状态 900）继续短暂显示后清理。
- 打飞／翻转下落的尸体（状态 1000）持续按重力运动；直到整个身体顶部低于 y=-64 才清理。固定游戏画面的底边为 y=-32，因此有 32 像素余量，不再靠固定寿命截断动画。
- 下落尸体使用层级 3，爆炸、金币和分数等特效使用层级 4；地形、水管与活体食人花维持原有遮挡关系。完整回归曾抓到尸体与爆炸同层导致的像素遮挡，已通过分层修正。
- 使用原有的失效对象回收机制，避免持续保留已落出画面的对象。

## 回归范围与证据

新增 `MarioCorpseAudit`，接入完整 Editor 验证和 Windows Player smoke：155 项检查，覆盖 14 种敌人、三种高度、四种场景配色、火球／龟壳／无敌星／顶砖／踩踏实际调用路径、画面像素、暂停恢复、无伤害、无重复得分、回收及踩扁尸体寿命。

修复前：`_zcode_tmp/corpse-before.log`，`MARIO_CORPSE_FAIL checks=155 errors=98`。

专项修复后：`_zcode_tmp/corpse-focused.log`，`MARIO_CORPSE_PASS checks=155 errors=0`。

完整 Unity 检查：`_zcode_tmp/corpse-validation-final.log`，`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=3391 failed=0`；`MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0`。进程退出码 0。

Windows 构建：`_zcode_tmp/corpse-build.log`，`MARIO_BUILD_PASS errors=0 warnings=0 bytes=170206599`。进程退出码 0。

实际 Windows 成品：`Validation/player-corpse-fix.log`，`MARIO_CORPSE_PASS checks=155 errors=0`；`MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0`。进程退出码 0。既有特效 386 项、食人花 46 项、通关音乐 119 项回归同时通过。

已人工查看 `Validation/PlayerCorpseFix/corpse-fall-41.png` 与 `corpse-fall-55.png`：倒置板栗仔在旧删除时刻仍清晰可见，随后继续下落；`corpse-fall-70.png` 中已落出画面。

这些是自动化场景与像素回归，不代表 64 关逐关人工通关，亦不宣称尸体运动与 NES 逐帧一致。
