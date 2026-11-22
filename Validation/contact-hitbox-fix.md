# 跳跃擦边受伤判定调整

日期：2026-10-17。

## 原因和行为

原逻辑直接以马里奥的 12×16（大个子站立 12×32）物理矩形与敌人的完整矩形相交决定伤害。实测上升时仅有 0.25 像素重叠也会死亡／掉体型，没有区分踩头范围与受伤范围。这是判定过严，非 Unity Collider 缩放异常。

新增 `MarioContact.cs`，为接触伤害单独定义区域：

- 马里奥的受伤框每边内收 2px：小个子／蹲下为 8×12，大个子站立为 8×28，居中于原物理框。
- 普通 16px 敌人的伤害框每边内收 2px；8px 火焰每边内收 1px，保留危险核心。自定义尺寸按尺寸的 1/8 内收，上限 2px。
- 先用原较宽范围判踩头，再用较小范围判伤害。脚边压到可踩敌人、龟壳停转／踢出、无敌星攻击保留原有范围。
- 上升时深度撞进敌人、尖刺及火焰接触仍有伤害；水下不允许踩死敌人。擦边容错不等于跳起时无敌。
- 地形碰撞、起跳速度、金币／道具拾取、火球和龟壳对敌人的攻击范围均不因受伤框缩小而改变。

这是当前原生版本的操作宽容度调整，不宣称接触判定与 NES 逐帧一致。

## 测试

新增 `MarioContactAudit`，通过生产 `TickActor` / `Step` 路径检测：三种体型、蹲下、左右边缘、上下边缘、精确边界、上升擦边、下降踩头、移动龟壳、8px 火焰、水下、无敌星、伤害无敌帧及拾取。

修复前首版 238 项中 75 项失败：`_zcode_tmp/contact-before.log`，`MARIO_CONTACT_FAIL checks=238 errors=75`。

修复后首版专项全部通过：`_zcode_tmp/contact-focused.log`，`MARIO_CONTACT_PASS checks=238 errors=0`。随后增加精确边界及小火焰／龟壳／拾取用例。

旧水下测试原来要求刚擦到鱿鱼的第一帧即死亡，现在继续模拟至身体重叠，仍要求受伤且不能踩死鱿鱼。

最终专项增加至 295 项。完整 Unity 回归日志 `_zcode_tmp/contact-validation-final.log`：

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=3686 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
```

进程退出码 0。运动参考只针对原始电机，不将其结论用于本次受伤容错改动。

截图复核还发现测试场景的水下标记影响了后续踩头截图；已改用独立 `AreaData`，避免修改共享关卡资源，并增加两项成品截图前的实际判定断言。磁盘上 1-1 的两处 `underwater` 均为 0。修正隔离后再次执行完整验证，上述结果仍全部通过。

最终 Windows 构建 `_zcode_tmp/contact-build-final.log`：`MARIO_BUILD_PASS errors=0 warnings=0 bytes=170214791`，退出码 0。

最终成品运行 `Validation/player-contact-final.log`：

```text
MARIO_CONTACT_PASS checks=297 errors=0
MARIO_CORPSE_PASS checks=155 errors=0
MARIO_FINISH_AUDIO_PASS checks=119 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

进程退出码 0。已实际查看最终 `Validation/PlayerContactFinal/contact-rising-graze-safe.png` 和 `contact-edge-stomp.png`：前者擦边未受伤，后者板栗仔压扁、显示 100 分且马里奥回弹。截图是定点测试场景；不是逐关人工体验验收。旧 `PlayerContactFix` 为测试隔离修正前的中间结果，不作为最终截图证据。
