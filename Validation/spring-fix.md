# 弹簧压缩动画与弹跳修复

已实际打开用户图片 `D:/DevTools/temp/codex-clipboard-02921bd7-8a17-446a-8909-7f9e5b33321e.png`。并使用真实 2-1 的弹簧（x=3008、顶面 y=29）和旁边 160px 高墙构建回归场景。

## 原因与复现

旧代码落地时只写入 vy=4.5/6px，没有压缩/回弹状态，也没有重置此前下落的 jumpCut、jumpOrigin 与重力。此前处于下落状态时，即使按住跳跃仍走下落重力；实测普通/加强弹跳只有 25.4375/44.1875px。弹簧顶面与高墙顶部差 131px，无法借此越过。渲染状态始终只有一种高度。

新增 25 项专项后、修复前实际输出：

```text
MARIO_VALIDATION_FAIL levels=64 areas=136 nodes=83609 checks=2865 failed=17
```

## 修复行为

- 新增压缩、回弹、发射过程，约 12 个模拟步。普通 29px 弹簧压至 13px，再恢复原高。底座不动，顶部及玩家一起升降；中间弹簧纹理缩放，避免简单截断贴图。
- 普通踩踏自动弹起；落地预输入、按住跳跃或压缩期间补按跳跃会锁定加强弹跳，松开不会丢失。
- 发射明确重置起跳位置、jumpCut、重力与支撑关系，清空预输入和离地宽限，避免普通跳跃提前打断压缩或产生额外空跳。
- 普通/加强初速采用 7/12px 每步，重力 0x70，与本地 SMB2J 红弹簧参考参数一致；普通/加强高度实测为 59.5/170.625px。压缩时间、输入容错属于本工程实现，不宣称整套机制逐帧等同 NES，也未在本轮加入日版绿弹簧独立超高跳规则。
- 压缩时允许水平移动，跳下弹簧后不再被远程发射；同一弹簧不作为乘客的侧面墙，压缩高度取整数像素以避免微小嵌入。
- 暂停冻结弹簧与乘客，恢复后继续；死亡、换区、管道传送清除承载并复原弹簧。

## 验证

最终专项覆盖默认/严格原始模式、两档弹跳、压缩恢复与渲染状态、固定底座、真实 2-1 高墙路线、小/大/火焰形态的中途补按、方向输入、离开取消、暂停恢复、死亡/换区/传送清理与顶头碰撞。既有输入测试同步改为检查完整压缩后的发射，而非只在落地瞬间断言 vy>0。

首次修复验证的高墙路线已落在墙边（脚底 y=160、身体与墙有重叠），但测试提前停掉横向输入，没有满足“整个左边界进入墙面”的断言。已将路线改为达到安全高度后持续朝墙移动，最终重新验证，不将该中间失败当成成功。

日志：`_zcode_tmp/spring-before-validation.log`、`_zcode_tmp/spring-validation.log`、`_zcode_tmp/spring-final-validation.log`、`_zcode_tmp/spring-build.log`、`Validation/player-spring-fix.log`。实际 Player 专项记录、轨迹及动画截图保存在 `Validation/PlayerSpringFix/`。

最终实际输出：

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2878 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168979800
MARIO_SPRING_PASS checks=37 errors=0
MARIO_CONTROLS_PASS checks=92 errors=0
MARIO_PLATFORM_PASS checks=44 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

最终验证、构建、实际 Player 的退出码均为 0。旧玩法、动画、金币、朱盖姆、食人花、炮台、设置、特效和重开专项均通过。真实 2-1 路线最终落点为 x=3040.75、y=160。

已实际打开 `PlayerSpringFix/spring-0.png`、`spring-5.png`、`spring-11.png`、`spring-30.png`，检查压下、最低、恢复发射和高空姿态。CSV 轨迹中弹簧高度 26→13→29，发射步脚底 y=41、vy=11.5625，确认动画与物理同步。自动回归不是 64 关人工通关或物理手柄体验验收。
