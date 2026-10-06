# 当前 Playtest Summary

状态日期：2026-10-07。V1.1 R1 + 常规雷击去文字，Formal **180秒** / Fast **90秒**。本轮只整理作品集、构建与验证，没有修改核心机制、Balance.json、视觉资产或音频。

## 本轮当前 Windows Build

Unity **2022.3.62f3**，Windows x64 Release，Editor菜单构建成功，**0错误 / 0警告**，27.308秒。已有12组模型检查通过；它们不是12名玩家或12次正常通关。[Build结果](Evidence/portfolio-20261007/build.json)

命令行启动曾未取得Editor许可；Hub显示Personal，经Hub打开同版Editor后成功构建。这不是MCP激活问题，也不能据此断言Personal失效。

| Smoke项目 | 实际证据与结果 |
| --- | --- |
| Launch / Start | 1280×720窗口启动，菜单显示正式180秒；点击开始进入Playing |
| Movement / Qi | 原生输入改变位置，完整局43次拾取、HUD变化；另补测Shift+W键盘位移 |
| Dash / Shield | 完整局26次Dash、10次Shield、7次吸收；另用Shift+W/Q验证键盘输入 |
| Pause / Resume | P键暂停在13.887秒，间隔截图均02:47；恢复事件仍13.887秒 |
| Hazards / Stages | 普通、随机、连环、预测、封路、追踪事件；四阶段及风/火/地/水/散花/球形闪电事件 |
| Win | 玄武、seed24130、策略E，180.003秒Won，HP100/Qi56，九道神雷完成 |
| Lose | 胜利后Restart，不移动/不用技能，19.308秒Lost，HP0，最后伤害普通雷 |
| Restart | 胜利后、失败后均重置elapsed0/HP100/Qi60，保留玄武 |
| Warning / HUD | 地面圈、技能与阶段HUD可见；普通雷即时黑框未出现，特殊天劫文字保留 |
| Errors | 本轮Player日志未检出Exception/Error/Crash，流程未阻塞；Editor BuildReport 0错误 |

完整局最低Qi为0，133个快照低于Dash费用。**最终满血不等于没有危险，技能使用次数也不等于每次都必要。** 这只是固定种子的自动策略烟测，不能推导真人通关率、整体平衡或所有种子公平。

[正式局结果](Evidence/portfolio-20261007/v11-portfolio-formal-E-1791301557269200200-result.json) · [原始运行事件](Evidence/portfolio-20261007/runtime-formal.jsonl) · [核验摘要](Evidence/portfolio-20261007/validation-summary.json) · [当前20秒实机GIF](../Screenshots/portfolio-20261007-gameplay.gif)

## 版本与方法边界

| 版本 / 方法 | 有效结论与限制 |
| --- | --- |
| 本轮当前Windows、自动输入+Computer Use | 上表完整流程及短键盘检查；不是独立真人测试 |
| 2026-10-04去文字源码、Editor Fast | 普通/随机/连环/预测/追踪圈继续显示、90秒通关；当时未重建Windows，见[ReadabilityPlaytest](ReadabilityPlaytest.md) |
| V1.1 Fast策略比较 | 固定绕圈29.501秒失败；组合策略90.003秒胜利、HP63、最低Qi0；不是严格单变量实验 |
| R1历史Windows Formal | 180.004秒胜利，HP65/Qi16，27Dash/8Shield；不是本轮Build |
| 更早T04–T07 / 独立化 | 仅适用于当时360秒等规格，保留[完整历史](Archive/Playtest-before-portfolio.md) |
| 开发者自测 | 用户Play Mode后提出局长、预警和文字遮挡反馈；不是独立多人调研 |
| 独立真人盲测 | [模板](BlindPlaytest.md)已准备，**样本0，结果pending** |

## 问题 → 假设 → 修改 → 结果

**问题：** 固定绕圈较稳定，技能和Qi可被忽略。**假设：** 预测落点、封路和偏离路线的补给会要求转向及主动风险。**修改：** 前序版本调整这些机制，再以元素组合扩展后期空间任务；本轮不改数值。**结果：** 已留档V1.1 Fast固定圈失败、组合策略通关；历史精确观察步行策略也曾通关。结论限于“固定圈在已测情境不再占优”，不是所有无技能玩法必须失败。详见[策划案例](PortfolioCaseStudy.md)及[历史Balance](Archive/Balance-before-portfolio.md)。

**可读性迭代：** 用户指出即时雷击文字遮挡场景；前序调整删除绘制分支，保留圈动画与判定。10月4日Editor与本轮Windows有回归证据，尚无独立玩家首次体验结果。

## 当前限制

自动策略能读取精确坐标，不能证明好玩或易懂。本轮未重跑全策略、三功法、多种子矩阵。旧Editor域重载错误本轮未复现，不声称已修复。下一步用真人模板记录无文字预警、死因、Qi取舍和屏外危险理解，再决定改动。
