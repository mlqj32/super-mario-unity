# 1-2 循环升降平台传送修复

## 已复现的原因

在真实 1-2 地下区域，x=2192 有两块下降平台，x=2448 有两块上升平台。平台越过边界后循环回到另一端。旧逻辑在位移过大时仅解除当前承载关系，却继续使用 `Min(本帧平台顶面, 上帧平台顶面)` 做落地检测。

下降平台从底部回到顶部时，该检测把整个高度范围当成可落地范围：马里奥从 y=0/32/80/128 被直接拉到 y=184，正在搭乘同列另一块平台时也会被抢走。既有测试只检查了上升平台回到底部，没有覆盖下降方向或非乘客。

加入测试后、修复前的实际 Unity 输出：

```text
MARIO_VALIDATION_FAIL levels=64 areas=136 nodes=83609 checks=2840 failed=16
```

失败明细在 `_zcode_tmp/platform-before-validation.log`。其中周期采样的马里奥单步垂直位移达到 120px，平台底部乘客从 -31.75 被拉到 184。

## 修复

循环复位时明确设置运行时 `platformWrapped` 标记；承载关系立即解除，并且该平台在本模拟步不参与马里奥的落地判定。下一模拟步清除标记，恢复正常承载和落地碰撞。连续升降、横向移动、普通平台和跳跃参数不需要调整。

修复作用于所有共用该循环平台逻辑的关卡；参考 JSON 中有 14 关的 51 个此类平台节点，不仅是 1-2。没有改动关卡资源或重新导入地图。

## 验证范围

新增 `MarioPlatformAudit`，使用真实 1-2 区域中的电梯，运行 44 项检查：

- 上升/下降循环，默认响应模式与严格原始模式。
- 非乘客在四个高度经过、原平台乘客脱离、站在固定台阶上。
- 同列双平台、反转对象遍历顺序，确保复位平台不抢乘客。
- 复位下一帧恢复落地检测；连续 40 帧正常搭乘和主动跳离。
- 双向各 900 步周期采样，覆盖多次复位并约束单步位移。

测试夹具只改变运行时对象，不修改 LevelData。周期采样为受控碰撞测试，不声称真人完成 1-2 或全部关卡通关。

修复后 Unity 日志：`_zcode_tmp/platform-validation.log`；构建日志：`_zcode_tmp/platform-build.log`；实际 Player 日志：`Validation/player-platform-fix.log`。Windows 专项明细及轨迹输出到 `Validation/PlayerPlatformFix/`。

修复后 Unity 检查中，44 项平台专项全部通过；下降平台循环 4 次、上升平台循环 3 次，两组采样的马里奥最大单步位移均为 1px。

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2840 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168972120
MARIO_PLATFORM_PASS checks=44 errors=0
MARIO_CONTROLS_PASS checks=91 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

实际 Windows Player 也通过全部平台专项及既有玩法、动画、特效、设置和重开回归。修复后验证、构建、Player 进程退出码均为 0。已打开检查 `PlayerPlatformFix/elevator-wrap-0.png` 和 `elevator-wrap-1.png`：平台在顶部重新出现，马里奥保持自然下落。实际轨迹为平台顶面 -31.75→184 时，马里奥脚底 64→63，没有随平台传送。
