# 全工程核查与修复（2026-10-17）

本轮审查了角色运动与碰撞、敌人／道具／平台、死亡和通关状态、音频、渲染、菜单输入、设置持久化、关卡预览与资源引用。使用现有完整套件，加上新的边界复现和混合输入压力测试。

## 已确认与修复

1. **通关阶段暂停入口不一致**：普通游戏可以按暂停，但旗杆、断桥和结算阶段的实际按键入口不接受暂停；手柄断线／窗口失焦也漏掉这些阶段和死亡阶段。统一判断仍在模拟的状态，暂停后恢复原阶段；死亡时 Start 快速复活的既有行为保留。
2. **尸体在死亡／通关阶段冻结**：这些阶段只推进 Effect，未推进死亡敌人。现在继续其死亡运动与清理；已被击倒的库巴不再额外执行断桥下落，避免每帧移动两次。
3. **天平断绳后未负重一侧悬空**：其速度为 0，切换到 falling 标记后仍不移动。断绳时两侧均取得向下速度，测试覆盖两种对象排列顺序。
4. **水下初始重力错误**：进入水下时仍使用陆地重力，只有第一次划水后才变成水下参数。现在进入区域立即设置水下上升／下落重力，覆盖全部 11 个水下入口。
5. **紧张音乐状态丢失**：低于 100 秒时，吃星、星星结束或切换区域会把音乐恢复到正常速度。现在选择音乐时从剩余时间恢复速度，新关／复活恢复正常速度。
6. **分数弹字被爆炸遮住**：文字渲染路径固定层级 3，未跟随普通 Effect 的层级修复。得分文字单独使用层级 5，像素检查确认其能覆盖爆炸；空文字清除旧贴图，文字切换清除旧翻转／缩放状态。
7. **死亡音效未清理先前音效**：死亡时只停止 BGM，先前变身等声音可能继续叠播。现在先停止旧音频，再播放死亡音效；成品测试检查旧声音采样被复位。
8. **关卡编辑器预览与运行不一致**：CustomText 不走文字渲染，参考贴图使用默认尺寸。预览现在显示文字并使用编辑后的宽高；未初始化节点的选中 Gizmo 增加空值保护。

## 回归范围

- 新增 `MarioBoundaryAudit`：修复前 `MARIO_BOUNDARY_FAIL checks=62 errors=40`；修复后专项 `MARIO_BOUNDARY_PASS checks=62 errors=0`。
- 为 64 关各增加 1200 帧可重复混合输入，检查暂停恢复、死亡重生和位置／速度数值稳定性，共 76,800 帧。
- 完整验证前后比较 64 份关卡资源的内存序列化结果，另核对磁盘 SHA-256，防止运行检查修改可编辑地图。
- 原有 64 关启动、区域、传送、分段迷宫、通关、操作、敌人、动画像素、特效、设置、手柄、尸体和受伤容错检查继续运行。
- 运行 Unity YAML、对象引用和资产 GUID 结构检查。

本次不是 64 关逐关人工无伤通关，也不代表已证明不存在任何其他缺陷；虚拟手柄检查不等同于实体手柄兼容性验收。

## 实际验证输出

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=3932 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
UNITY_YAML_PASS files=65 blocks=72 assetGuids=1735 localReferences=5 externalReferences=69 errors=[]
MARIO_MAP_INTEGRITY_PASS files=64 changed=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=170230727
MARIO_BOUNDARY_PASS checks=128 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=122880 errors=0
```

上述命令退出码均为 0。最终记录位于 `_zcode_tmp/full-audit-validation.log`、`_zcode_tmp/full-audit-build-final.log`、`Validation/player-full-audit-final.log`、`Validation/structure.json` 和 `Validation/full-audit-map-integrity.json`。原始运动轨迹误差结论仅适用于底层原始电机，不代表整款复刻的 NES 逐帧一致性。

Windows 成品中，既有动画、游戏逻辑、金币与中文、刺猬云、食人花、炮台、设置、特效、重开、操作、平台、弹簧、迷宫、菜单、通关音频、尸体和接触伤害专项全部通过。122,880 帧为每关既有 720 帧与新增 1200 帧的合计，不含其他专项的模拟帧。

第一轮成品测试的音频前置条件失败：短声音在检查时没有活动采样，因此该轮不能证明音频清理有效。最终测试先等待压力测试产生的延迟销毁结束，再持续播放旧声音并在限定时间内确认活动采样，防止短音效自然结束造成假通过。最终实际输出为 `PASS sound cleanup fixture has active samples clip=Powerup samples=470 playing=True`，随后 `PASS death clears earlier sound playback oldClipSamples=0`。

已实际查看最终截图 `Validation/PlayerFullAuditFinal/score-over-explosion.png`，白色“100”显示在爆炸前方。Windows 成品位于 `Builds/Windows/超级马里奥.exe`；运行时需保留同目录的数据和依赖文件。
