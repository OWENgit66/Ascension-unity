# T07 资产来源、许可与制作范围

2026-09-25。本清单随 Windows Build 分发。没有付费 Audio/3D API，没有 API Key 依赖。

## 第三方资产

| 用途 / Asset Name | 作者 / Source | License | 本项目修改 |
|---|---|---|---|
| 修士 Cultivator.fbx / Cultivator.png，原名 Monk.fbx / Monk_Texture.png | Quaternius，[RPG Character Pack](https://quaternius.com/packs/rpgcharacters.html) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 完整 1634 三角形模型、原贴图与 Idle/Run 动画；统一色调、尺度、原点与动画绑定。不是原创模型。 |
| Lightning，原名 thunder-seq.wav，作品 Thunder | Jerimee，[OpenGameArt](https://opengameart.org/content/thunder)；原作者注明基于 René Nyffenegger 的 cSound instrument，经其修改 | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | 去前导静音、裁成2.2秒、单声道24kHz、尾部衰减与音量调整。归属原作者，不暗示其认可本项目。 |
| Wind，原名 wind1.wav，作品 wind1 | Luke.RUSTLTD，[OpenGameArt](https://opengameart.org/content/wind1)；原页致谢 Andy Farnell 的教程 | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 取1.2秒、24kHz、淡入淡出与音量归一。来源为PureData合成风声，不宣称自然录音。 |
| Fire，原名 fire-1.wav，作品 Fire Crackling | AntumDeluge，[OpenGameArt](https://opengameart.org/content/fire-crackling) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 裁剪/延长至3.5秒、24kHz、淡入淡出与音量归一。 |
| Earth Crack，原名 Audio/impactMining_000.ogg，作品 Impact Sounds | Kenney，[资产页](https://kenney.nl/assets/impact-sounds) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 原始OGG用于岩石破裂短音，运行混音调音量。 |

独立版保留已导入的Monk模型/贴图、选定声音与原有生成图。未使用的Wizard等原始下载不复制。许可副本见Docs/Licenses/Quaternius.txt、Kenney.txt；原始下载包仍在原项目安全备份的SourceAssets中，开发和Build不需要它们。

## 本项目生成或编写

- Art/CloudSea.png：OpenAI imagegen 生成的雷云、云海和中国山峰背景，2026-09-25。导入图已保存在本Unity项目中，Build不读取Codex生成缓存。不是人工绘制，不将生成结果标成CC0。按低对比背景使用。
- PortfolioWorld.cs 和 shaders：法台阶层、八角灯柱、青金阵纹、石面材质、灵气珠、护盾壳、地裂碎岩与持续火区。所有装饰无Collider；火焰为低矮几何，不使用未审核的烟火预设。
- prepare_t07_assets.py：原创虚构术法/UI音色，Dash/Shield/Shield Absorb/Qi/Damage/Start/Stage/Victory/Defeat/Warning/Element共11个WAV。五声音阶铃音、泛音包络与短扫音，无外部音乐采样。
- PortfolioAudio.cs：游戏AudioSource软限幅和诊断音轨，只记录本游戏音频，不采集麦克风或其它应用。音轨/峰值不替代真人听感评价。
- 字体：系统Microsoft YaHei/SimHei/Arial，未复制或分发字体文件。非中文Windows需核对中文字体可用性。

## 框架来源

[GameFactory-3A](https://github.com/OpenDCAI/GameFactory-3A)，Copyright 2026 OpenDCAI，Apache License 2.0。独立副本许可全文见Docs/Licenses/GameFactory-3A-Apache-2.0.txt。A3GameRuntime与MediaDirector本地源码原样保留；UnityClient/EditorBridge已移除，Build使用Unity原生接口。

## 质量界限

T07目标是策划Demo美术层级：主要白盒角色、天空和地面已替换，玩法预警保留精确几何。修行者使用通用Idle/Run，不宣称有定制御剑/施法/布料动画。场景保持小型圆台；远山为背景绘景，不是可探索关卡。正式验证见Playtest.md。
