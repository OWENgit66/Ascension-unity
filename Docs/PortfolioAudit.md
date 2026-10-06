# Portfolio Polish 修改前审计

审计日期：2026-10-07。目标目录：`Ascension-GitHub/UnityProject`。本页记录修改前事实，不替代当前 [Playtest](Playtest.md)。

## 确认的当前行为

- `Resources/Balance.json` 正式阶段 35/40/45/60 秒，共 180 秒；Fast 为 10/12/14/54 秒，共 90 秒。
- `AscensionRuntime.Awake` 读取上述配置，场景序列化的旧默认字段不能直接当作运行时最终值。
- `AscensionView.OnGUI` 已删除常规 Strike 世界文字、倒计时以及左上最近落雷 HUD。普通/随机/连环/预测/追踪/封路的图形预警仍在；特殊机制提示和常规状态 HUD 保留。
- 模型及 Balance 未因去文字修改发生变化；上一次只删除 14 行呈现逻辑。
- 仓库当前无 Windows Build 和真实 Gameplay GIF。Screenshots 中是 T07/独立化历史画面，不可充当最新演示。

## 文档与证据冲突

| 原文件 | 问题 | 处理方向 |
| --- | --- | --- |
| README | 仍称小型 HUD 倒计时存在，以旧截图作为主要展示，容易将 R1 构建当最新源码 | 重写招聘者首页，区分版本与待发布状态 |
| GameDesign / Balance | 当前参数与多个历史版本混放，部分描述仍含旧文字提示 | 当前规格单独编写；整份原文保留 Archive |
| Playtest | 最新主要段落是 R1，缺去文字后的总览；旧 360 秒/102 秒结果混在长文里 | 当前证据矩阵与分层验收；旧全文归档 |
| PortfolioNotes | 六分钟、尚无真人反馈等旧口径 | 改为当前面试提纲，保留原文 |
| BlindPlaytest | 仍安排 T07 360 秒 | 改为当前 180 秒；未填真人结果继续空白 |
| RepositoryNotes | 未初始化 Git/未上传为旧状态 | 以本地真实 Git 状态说明；不推断远程是否已同步 |
| IndependenceReport | 360 秒独立化验证是历史事件 | 保留完整历史过程并加明显提示；当前构建指南另列 |
| TODO.json | V1.1 与 R1 均 done | 原样移动至 Archive/V1.1-Tasks.json，新增真实 ROADMAP |

## 验证归属

- T06 的 102/360 秒、T07 与独立化 360 秒均属历史。
- V1.1 固定圈 29.501 秒失败/组合策略 90.003 秒胜利，属于 V1.1 自动输入对照；不是人类通关率或严格单变量实验。
- R1 正式 180.003708 秒胜利及 122 次预警时序检查，属于此前 Windows Build。
- 2026-10-04 的最新纯文字删除只完成 Editor Fast 90 秒回归，不能冒充已重建的 Windows Release。
- 真人开发者反馈确实存在；独立真人盲测结果仍为 pending，模板样本量为 0。

## Git 起点与保留边界

HEAD：`a96d3ae`；此前为 `6056dd0`（v1.1）及 `58c1ce1`（v1.0）。只有这三条历史，不补造开发提交。

本轮开始前已有未提交修改：AscensionView.cs、Ascension.unity、Packages manifest/lock、GraphicsSettings、QualitySettings 和本机日志路径；另有去文字验证文件及 JobApplicationCase 未跟踪。不能把这些差异算成本轮新增，也不自动回退或代用户提交。

修改前完整 Docs、Assets、Packages、ProjectSettings 和根说明已备份到仓库外 `../Ascension-Releases/Portfolio-20261007-Baseline/`。本轮保持核心代码、Balance、资产和场景不变；构建若自动写回元数据，会与该基线核对。旧 Evidence 内容不能被构建入口覆盖后冒充历史结果。

## 修改前结论

当前规格以源码和 Balance 为准；无需更改 gameplay 来迁就旧文档。先同步当前/历史边界、案例与导航，再尝试当前构建、实机和真实录制。能否交付新 ZIP 与 GIF 以本轮实际结果为准。
