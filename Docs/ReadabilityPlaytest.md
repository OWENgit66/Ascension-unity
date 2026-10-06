# 常规雷击预警可读性调整

> Historical document — this describes an earlier version and should not be used as the current gameplay specification.
>
> 以下保留2026-10-04 Editor回归原记录。“未重新Build”仅指当时；后续Windows重新构建结果见[当前Playtest](Playtest.md)。

日期：2026-10-04  
项目：`E:/AI项目/Unity/Ascension-GitHub/UnityProject`  
Unity：2022.3.62f3

## 修改

问题：常规雷击的黑色文字框和倒计时遮挡场景预警。

修改：仅从 `Assets/Ascension/Presentation/AscensionView.cs` 的 `OnGUI()` 删除 14 行绘制逻辑：

- HUD 中“雷劫预警 · N处 / 最近落雷 X.Xs”的统计、黑色底框及文本。
- 遍历 `State.Strikes` 绘制落雷、随机雷、连环雷、追踪/锁定、预测、封路及九天神雷名称与倒计时的世界空间文字。

保留 HUD、阶段与操作提示、特殊天劫文字，以及全部地面图形预警。`WorldWarning` 辅助方法仍供特殊机制使用。

没有新增动画。原有圆环线宽随 `age / warning` 从 0.16 增至 0.30 的动画原样保留。落点、半径、颜色、伤害、预警时间、技能与资源配置均未修改。

## 验证

通过本地 Unity MCP 进入实际 Editor Play Mode，使用现有 Fast 配置（90 秒）、玄武功法与现有输入测试脚本。输入脚本只提供移动、镜头、Dash 和 Shield 输入；没有修改 HP、Qi、伤害或模拟时间。Fast 仅为本次运行时选择，没有保存到场景或正式配置。

实际画面观察及截图：

| 雷击 | 游戏时间 | 结果 | 证据 |
| --- | --- | --- | --- |
| 普通雷 | 2.00 秒 | 圆圈保留，无常规文字框 | [截图](Evidence/readability-normal.png) |
| 随机雷 | 4.52 秒 | 圆圈保留，无常规文字框 | [截图](Evidence/readability-random.png) |
| 连环雷 | 11.50 秒 | 连续圆圈保留，无常规文字框 | [截图](Evidence/readability-chain.png) |
| 预测雷 | 17.10 秒 | 预测落点圆圈保留，无常规文字框 | [截图](Evidence/readability-predictive.png) |
| 追踪雷 | 25.90 秒 | 玩家附近追踪圆圈保留，无常规文字框 | [截图](Evidence/readability-tracking.png) |

截图中的水劫、风劫、球形闪电等特殊机制文字为保留内容。HP / Qi、阶段、剩余时间、功法、Dash / Shield 与操作提示正常显示。

干净启动的完整测试于 90.000 秒到达 `Won`：HP 100、Qi 20、Dash 11 次、Shield 4 次，记录到 49 次雷击事件。最后读取 Unity Console：0 Error。最终胜利状态由运行日志确认；最后取图时 Windows 已显示锁屏，未取得胜利界面截图。

运行日志：[readability-editor-clean.jsonl](Evidence/readability-editor-clean.jsonl)。采样记录中没有发现早于 warning 时刻的 impact。警告时间及伤害范围保持不变的主要依据为源码差异核对：修改前后对 Mechanic / Presentation 下 74 个文件进行 SHA-256 比较，仅 `Presentation/AscensionView.cs` 变化，差异仅为上述 14 行删除。Mechanic 文件及全部图形预警更新代码未变。

## 测试过程中的异常

首次 Play 受到延迟脚本编译 / Domain Reload 干扰，运行时 State 变为 null，并出现已有 `DrawElements / LateUpdate` 空引用错误，随后 MCP 断开。该次运行不计作通过。停止 Play、等待编译结束后清理 Console 并重新开始完整测试；重新开始后的测试没有复现 Error。本次没有扩展修改运行时热重载逻辑。

本任务未重新构建 Windows EXE；验证对象为修改后的 Unity 源码和 Editor Play Mode。
