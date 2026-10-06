# Validation 工具

这些脚本用于开发核验，不是玩家数据或真人盲测。

- `play_native.py`：Python 标准库、localhost UDP 输入，读取原生诊断并选择移动/技能。A–E 分别覆盖步行观察、绕圈、频繁 Dash、频繁 Shield 和组合策略。观察策略能读精确危险坐标，不能代表人的反应能力。
- `audit_v11.py`：汇总已保存的 V1.1 结果，不会重新试玩或证明游戏好玩。
- `audit_independence.py`：历史独立化核验，适用范围以脚本断言与历史报告为准，不能替代当前版本构建。

## 重现一次自动输入

1. 用当前源码构建 Windows；启动时加 `-A3GameRuntimeInputPort=30131 --ascension-evidence <绝对JSONL路径>`，Formal 默认180秒。
2. 把 `Docs/Evidence/latest-runtime-path.txt` 暂时指向该 JSONL，保留原文件以便恢复。
3. 在仓库根运行 `python Docs/Validation/play_native.py E --label your-run`；看到 Ready 后在游戏点击 Start 或 Restart。
4. 检查实际 `started`、技能、拾取、阶段和 `won/lost` 事件，再判断成功；发送 UDP 成功本身不是游戏成功。
5. 将结果、输入、原生日志及 Build 指纹放入单独证据目录。不要覆盖历史文件或把结果改写成预期值。

脚本不会修改 HP、时间、技能费用或游戏程序集。暂停会延长墙钟时间，但正式游戏计时仍是180秒；工具总等待上限520秒。菜单/键盘和可视化回归仍需实际检查。
