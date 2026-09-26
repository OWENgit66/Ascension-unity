# 第三方许可与署名

本独立副本沿用 Ascension v1.0 的资产和音频，没有重新授权第三方作品。游戏专有代码不因依赖 Apache-2.0 自动成为 Apache 授权。

## GameFactory-3A 源码

来源：https://github.com/OpenDCAI/GameFactory-3A
来源仓库提交：d8e543492a1aac8a857c5f1dcadf06bb16c99a4e
Copyright 2026 OpenDCAI。许可：Apache License 2.0，全文见 [许可证](Docs/Licenses/GameFactory-3A-Apache-2.0.txt)。

复制现有项目中的 `Assets/A3GameRuntime/Runtime/`，17个C#文件及程序集定义逐字节保留，未改命名、逻辑或版权声明。对应路径及SHA-256见 [复制清单](Docs/Evidence/dependency-copy-manifest.json)。同时保留其package.json元数据及Unity .meta GUID；没有复制上游测试框架、Python SDK、模型、生成管线或Agent Skills。

| 独立项目文件（UnityProject/） | 上游来源（GameFactory-3A/） | 修改 |
|---|---|---|
| `Assets/A3GameRuntime/Runtime/A3GameControlBinding.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameControlBinding.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameControllerState.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameControllerState.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameControlMode.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameControlMode.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameEntitySnapshot.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameEntitySnapshot.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameEntitySpawnRequest.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameEntitySpawnRequest.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameIdentityComponent.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameIdentityComponent.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameLocomotionState.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameLocomotionState.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameMediaDirector.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameMediaDirector.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameParticipantInfo.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameParticipantInfo.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameRuntime.asmdef` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameRuntime.asmdef` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameRuntimeEntityComponent.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameRuntimeEntityComponent.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameRuntimeInputReceiver.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameRuntimeInputReceiver.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameRuntimeInputState.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameRuntimeInputState.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameRuntimeSubsystem.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameRuntimeSubsystem.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/A3GameWorldSessionSubsystem.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\A3GameWorldSessionSubsystem.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/IA3GameControllableEntity.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\IA3GameControllableEntity.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/IA3GameEntityFactory.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\IA3GameEntityFactory.cs` | 无；Apache-2.0 |
| `Assets/A3GameRuntime/Runtime/IA3GameRuntimeMessageHandler.cs` | `engine_adapters\unity3d\plugin\A3GameRuntime\Runtime\IA3GameRuntimeMessageHandler.cs` | 无；Apache-2.0 |

## 模型、图像和音频

详细作品名、作者、修改方式和原始链接见 [AssetSources](Docs/AssetSources.md)。

| 内容 | 来源/作者 | 许可 |
|---|---|---|
| Monk修士模型、纹理、Idle/Run | Quaternius / RPG Character Pack | CC0 1.0；Docs/Licenses/Quaternius.txt |
| Thunder / thunder-seq.wav | Jerimee，基于René Nyffenegger的cSound instrument | CC BY 3.0；已裁剪、单声道转换、衰减与归一，不暗示作者认可本项目 |
| wind1.wav | Luke.RUSTLTD | CC0 1.0 |
| Fire Crackling / fire-1.wav | AntumDeluge | CC0 1.0 |
| impactMining_000.ogg / Impact Sounds | Kenney | CC0 1.0；Docs/Licenses/Kenney.txt |
| 云海雷云背景 | OpenAI imagegen生成 | 非人工原创/非CC0声明；保留现有生成资产 |
| 11个术法/UI WAV、法台与几何表现 | 本项目编写/合成 | 非第三方采样；沿用原项目 |

Thunder许可原文：https://creativecommons.org/licenses/by/3.0/
CC0原文：https://creativecommons.org/publicdomain/zero/1.0/

## 外部开发依赖

Unity Editor及Windows Build Support需合法独立安装和激活，按Unity条款使用，不复制安装目录或License凭据。Unity官方UPM包依manifest和lock从Unity registry解析，保留各包自己的LICENSE.md；不复制Library/PackageCache冒充项目自有代码。URP及其Core/ShaderGraph/Burst/Mathematics等传递依赖由UPM管理；项目未改变渲染管线。

系统Microsoft YaHei/SimHei/Arial字体未复制或分发，沿用运行系统字体。未来分发字体须单独确认授权。独立化不需要GameFactory的API Key、账户或生成服务。
