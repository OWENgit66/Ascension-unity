> Historical document — this describes an earlier version and should not be used as the current gameplay specification.
>
> Snapshot preserved before the 2026-10-07 portfolio polish. Original statements, results and links below describe the earlier source location; use [the archive index](README.md) and [current docs](../README.md) for navigation.

# 面试讲述提纲

建议展示顺序：30 秒题材与核心循环 → 一段实际游玩 → dominant strategy 迭代 → 资源/功法取舍 → 测试证据与局限。

## 1. 为什么选择渡劫题材

渡劫把“短时间生存、观察预警、承担风险、消耗有限资源”集中到一个明确目标。小型法台天然限制范围，雷风火地可以通过颜色、形状、方向与持续时间形成不同阅读任务。题材表达不依赖 NPC、剧情或开放世界的大量内容。

## 2. 核心玩法是什么

观察预警 → 选择路线 → 决定步行、Dash 或 Shield → 承担取气风险 → 为下一次压力预留 Qi → 通过四段难度并存活六分钟。胜利不是击杀敌人，而是在空间、时间与资源之间作连续取舍。

## 3. 最初出现了什么问题

普通追落点攻击容易被连续移动消解。玩家持续绕圈时，即使不认真看预警也可以稳定生存，技能和补给成为可忽略功能。不是“伤害低”这么简单，而是最优行动缺少变化。

## 4. 持续绕圈为何成为 dominant strategy

攻击主要指向过去位置，固定速度沿边缘运行既规避攻击，又避免复杂方向选择。没有足够的路线打断与补给成本，同一种输入解决了过多情境。

## 5. 如何解决

预测雷在短期未来位置预警，锁定后不追随；改变方向即可反制。封路电场要求改线；Final 风改变位置、火留下持续区域、地裂横截路线、散花雷要求选择夹角。预警先于伤害，同时限制组合数并采样逃生空间。固定半径绕圈在既有实测中第三阶段死亡；精确读取预警的自动策略仍可能无技能通关，这说明机制保留步行解法，也提醒我不能把自动测试当作普通玩家难度结论。

## 6. Qi 如何制造 meaningful choice

Dash 和 Shield 共享同一池 Qi。现在花气保住安全位置，会减少下一次承伤的预算；等落雷后补气，又可能错失安全窗口。中心与偏离当前路线的拾取提供恢复机会，避免只有惩罚没有恢复。用最低 Qi、消费/拾取事件与玩家实际取气路线评估压力。

## 7. 三功法的 trade-off

青云以低费短冷却 Dash 换取昂贵 Shield；玄武以高容量低费 Shield 换取昂贵撤离；九霄提高单颗 Qi 收益，但 Shield 时间更短、施放时机容错更低。参数直接作用于已有规则，没有增加独立技能或装备系统。三者均有完整可玩证据，单局结果不能用来宣称相同或不同通关率。

## 8. 真人测试后做了哪些修改

**尚未发生。** 当前仅完成开发者/自动输入验证和真人盲测模板。面试时明确说明这一点；收到真实反馈后，在 BlindPlaytest.md 填入原话与行为，再补“问题 → 假设 → 修改 → 复测结果”。不要将电脑精确读取预警的结果包装为用户调研。

## 9. 更多时间最值得做什么

先验证首次阅读负担与死因可理解性；再用多个真实玩家和多个种子调整 360 秒中段节奏与 Qi 恢复；随后完善修士的受击/施法动画与音效混音。保留小型竞技场范围，扩展前先证明现有选择有效。

## 制作贡献与工具说明

这是基于 GameFactory-3A 的 AI 辅助策划与 Unity Demo。应区分自己的目标定义、决策与验收工作，Codex 辅助的代码/文档实现，以及第三方资产。不要声称独立手工制作 Quaternius 角色或 OpenGameArt/Kenney 音效。云海背景为 OpenAI imagegen 生成；资产来源详见 AssetSources.md。核心框架与公开 UnityClient 保持原有架构。

可展示的证据：Balance.md 的参数、Playtest.md 的历次问题与修复、实际原生日志与屏幕截图、正式 Build。不要宣称 NUnit 已通过：本项目没有执行 Unity Test Framework。
