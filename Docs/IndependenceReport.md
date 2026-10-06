# 源码独立化说明与历史报告入口

当前源码可脱离完整 GameFactory 仓库结构，通过 Unity 打开、开发和构建。独立不等于零第三方依赖：保留必要的 A3GameRuntime 本地源码、Unity UPM 依赖和资产署名。

完整独立化过程、文件比对和当时的正式360秒验证保留在 [Historical IndependenceReport](Archive/IndependenceReport-before-portfolio.md)。这些历史结果不冒充当前180秒去文字版本的构建。

## 保留与移除

- 保留17个 GameFactory Runtime C#文件、程序集定义和 `.meta`，输入/实体接入及音频沿用现有框架。
- 不依赖原仓库 Python SDK、生成管线、模型服务、Agent Skills 或 EditorBridge。
- 构建使用 [StandaloneBuild](../UnityProject/Assets/Ascension/Editor/StandaloneBuild.cs) 和 Unity 原生 BuildPipeline。
- Unity Editor、Build Support、许可及 UPM 包仍是外部开发条件；后续安装的 Unity MCP 为 Editor 工具，不是自研系统。

[第三方文件与许可清单](../THIRD_PARTY_LICENSES.md) · [当前打开/构建指南](BuildGuide.md) · [当前验证状态](Playtest.md)
