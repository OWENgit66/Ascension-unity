# 仓库与交付说明

当前目录 `Ascension-GitHub` 是独立源码仓库根，Unity Hub 打开其中 `UnityProject`。本地配置了 `https://github.com/OWENgit66/Ascension-unity.git` 远程，本轮不 push，也不据远程配置推断 GitHub 页面已同步。

```text
README.md                 招聘者入口
ROADMAP.md                完成范围与真实待办
UnityProject/             Assets、Packages、ProjectSettings
Docs/                     当前规格、案例、验证指南
  Archive/                历史文档与原TODO任务记录
  Evidence/               按版本区分的真实证据
  Validation/             本地验证脚本
Screenshots/              标明版本的真实画面
THIRD_PARTY_LICENSES.md    来源与许可
```

Builds 与 Unity Library/Temp/Logs/Obj 等缓存不应入 Git。Assets 的 `.meta`、Packages manifest/lock 与 ProjectSettings 必须保留。发布游戏时打包整个 Windows 构建目录，不能只分发单个 EXE。

## 版本与 Git

原有本地历史只有 v1.0 发布、v1.1 提交与 main 合并等真实记录。本轮不重写历史、补造提交，也不把既有未提交的场景、包和显示设置差异算成此次新增。

本次文档、实际 Build/媒体结果与推荐 commit 划分见 [PortfolioPolishReport](PortfolioPolishReport.md)。当前文档和历史归档分别导航，原 TODO 已移动到 [Archive/V1.1-Tasks.json](Archive/V1.1-Tasks.json)。

## 本机与公开仓库的边界

- `Docs/Evidence/latest-runtime-path.txt` 是本机测试指针；即使被 ignore，已有跟踪状态仍需发布前单独审查。本轮不擅自清除你的跟踪记录。
- `play_native.py` 使用当前日志指针和本地端口；普通玩家无需 Python，运行和构建无需完整 GameFactory 仓库。
- `audit_independence.py` 引用旧测试机器路径，是历史现场脚本；不要当成任意新机器可用的一键验收。
- 第三方许可保留；没有为项目自有代码新增整体授权。

打开工程见 [BuildGuide](BuildGuide.md)，打包/上传见 [ReleaseChecklist](ReleaseChecklist.md)，原上传说明见 [Archive](Archive/RepositoryNotes-before-portfolio.md)。
