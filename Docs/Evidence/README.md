# Evidence：如何解读

证据按实际版本和测试方法使用，不能将自动输入、模型断言或历史 Build 当作当前真人测试。

| 范围 | 用途与边界 |
| --- | --- |
| [portfolio-20261007](portfolio-20261007/) | 本轮当前源码的 Editor 构建、Windows 正式局、菜单与暂停截图；最终判定见 [Playtest](../Playtest.md) |
| `readability-*.png`、`readability-editor-clean.jsonl`、`v11-readability-clean-*-result.json` | 2026-10-04 去文字版本 Editor Fast 回归，不是当时重建的 Windows EXE |
| `v11-*-result.json` / `*-native.jsonl` / `*-inputs.jsonl` | V1.1 / R1 原生自动输入结果、状态事件和输入轨迹；以各文件的 profile/seed/label 为准 |
| `t06*` / `t065*` / `t07*`、`independence*` | 历史调优、视觉及独立化证据，不代表当前 180 秒源码 |
| 根目录 `build.json` / `scene.json` / `*-model-checks.json` | 历史文件已保留，本轮构建输出另存日期目录 |

`latest-runtime-path.txt` 是本机工具临时定位文件，不是版本证明。日期目录中的 `validation-summary.json`、源码/构建哈希和原始日志一起界定本次验证范围。记录中的绝对路径与机器时间可能来自不同运行环境，不能据此推算开发工时。

独立真人盲测结果尚未收集；[BlindPlaytest](../BlindPlaytest.md)只有模板。完整历史叙述见[归档 Playtest](../Archive/Playtest-before-portfolio.md)。
