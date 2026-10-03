# Ascension v1.0 源码独立化报告

状态：**完成**。独立Unity源码、直接Editor打开、场景/编译、Windows Release与正式360秒运行验证均通过。原项目和旧Release保留且未改动。

## 最小方案与边界

独立根目录 `Ascension/` 与 `GameFactory-3A/` 是同级目录。仅复制 Unity 的 Assets、Packages、ProjectSettings，以及游戏文档、精选截图和许可。没有符号链接、junction、指向原仓库的file包或Python import。游戏Mechanic、Presentation、Balance、资产及场景均不修改。

独立并不等于删除所有第三方代码：保留必要的Apache-2.0 Runtime本地副本，既能脱离仓库打开和Build，也避免为了清除框架名称改写游戏和音频逻辑。

## 依赖分类

| 分类 | 原先内容与实际用途 | 独立版处理 |
|---|---|---|
| 1. 仅开发阶段需要 | GameFactory Python UnityClient、生成管线、Agent Skills、模型服务、Tools/workflow.py、场景生成ComposeScene、EditorBridge任务投递 | 不复制；保留已生成资产和场景 |
| 2. Unity Runtime必须（当前源码耦合） | AscensionRuntime实现IA3GameEntityFactory，并使用Runtime/WorldSession/InputReceiver/EntityComponent；Presentation使用A3GameMediaDirector播放声音 | 原样本地保留Runtime共17个C#文件与asmdef、meta；依赖闭包内部使用其它数据类型和接口，不做破坏性裁剪 |
| 3. Build工具需要 | 旧BuildPlayer.cs依赖GameFactory3AEditorBridge.GetArgument，workflow.py依赖仓库Python导入路径 | 二者不复制；新增Editor-only StandaloneBuild，直接调用Unity BuildPipeline。也可使用Unity标准Build Settings |
| 4. 已不再需要 | 未被场景引用的A3GameBootstrap；空Imported目录；未使用glTFast；GameFactory与Ascension的NUnit测试目录及test-framework包 | 独立副本中排除。未删除原项目测试。保留已有URP和序列化渲染资产以维持画面 |
| 5. 应保留外部依赖 | Unity Editor/Build Support/License、官方UPM包及传递依赖、系统中文字体 | 通过安装/激活/UPM恢复，不复制Unity二进制、许可证凭据、PackageCache或系统字体 |

GameFactory Runtime输入接收器为原有功能，本地端口默认30030，可由命令行覆盖；普通键鼠游玩不需要Python或远端控制器。该功能为保持源码一致继续保留，并不访问GameFactory仓库路径。

## 保留文件与许可

[THIRD_PARTY_LICENSES.md](../THIRD_PARTY_LICENSES.md)逐一列出复制的GameFactory文件、源路径及Apache-2.0许可。`Docs/Evidence/dependency-copy-manifest.json`记录复制/排除清单及每个Runtime文件SHA-256。第三方模型和音频继续保留原有CC0/CC BY署名，不把第三方内容声明为自有作品。

## 打开和Build

1. 安装Unity **2022.3.62f3**及Windows Build Support，确保License有效。
2. Unity Hub选择Add project from disk，选本目录下 **UnityProject**，不是Ascension根目录。
3. 首次启动等待UPM还原和Library重新导入；首次机器需可访问Unity包源或具备合法包缓存。
4. 打开 `Assets/Scenes/Ascension.unity`，或菜单 `Ascension > Open Game Scene`。
5. 菜单 `Ascension > Build Windows Release` 输出至 `Builds/Windows/Ascension.exe`，默认正式360秒。菜单不会自动改写场景或数值。
6. 标准方式：File > Build Settings，Windows x86_64、只启用Ascension场景、取消Development Build，再Build。

命令行（Unity路径按自己的安装位置填写）：

```powershell
& "D:/unity/edit/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "E:/AI项目/Unity/Ascension/UnityProject" -buildTarget Win64 -executeMethod Ascension.Editor.StandaloneBuild.BuildWindows -logFile "E:/AI项目/Unity/Ascension/Docs/Evidence/build.log"
```

无需安装GameFactory、Python、生成模型或设置API Key。可选的Docs/Validation脚本仅用于本次证据采集，普通开发与Build不需要它。

## 不改变玩法的检查方法

复制前为原项目Assets/Packages/ProjectSettings与既有Windows Build生成SHA-256基线。独立版游戏代码、Balance、纹理、音频、模型、Shader、场景及ProjectSettings逐文件比对；允许的结构差异仅限移除未使用依赖/测试、UPM重新解析锁文件和新增Editor构建入口。实际原生Build再覆盖Movement、Qi、Dash、Shield、全部Hazard、Win/Lose。构建二进制可能因路径、时间及程序集裁剪变化而不同，不把二进制相同作为玩法一致条件。

原项目不会打开、构建、删除或改写。所有新日志与证据写在独立副本。Unity导入生成的Library/Temp/Logs/Obj最后关闭Editor后清理，.gitignore永久排除；Assets .meta、包锁和ProjectSettings保留。

## 导入时发现并处理的差异

首次GUI打开后Unity将GraphicsSettings.lightsUseColorTemperature写成true，并同步QualitySettings的MSAA。核对原/新Library中的URP源码：构造器按管线资产的msaaSampleCount同步AA，渲染时启用色温，两边代码相同。关闭Editor后恢复原序列化文件，以原始设置重新Batch Build，所有ProjectSettings再次逐字节一致。没有增加运行时钩子改变视觉；日后GUI打开仍可能出现同类URP自动序列化差异，应核对管线资产而非视为玩法调优。

移除未使用glTFast后UPM曾自动把Burst解析为1.8.21；最终manifest明确固定原项目实际版本1.8.24，重新构建。Unity 2022.3.62f3将manifest中的URP 14.0.11内置版本映射为实际14.0.12，原项目与独立副本相同。实际包文件比较见package-comparison.json：所有保留包与原项目实际缓存中的同版本包内容一致。

GUI首次打开还生成了PackageManagerSettings.asset（仅官方registry及Editor展开状态）和URPProjectSettings.asset（材质迁移版本7），保留为Unity原生Editor元数据。它们不修改运行时画面或数值。原项目已有的ProjectSettings文件全部相同。

上次用户Esc中断的测试不计作通过；本次使用新的runtime-final.jsonl。窗口授权恢复后已实际查看独立Editor与Ascension场景，截图见Screenshots/independent-editor.png。

## 验证结果

验收全部通过，见 [independence-acceptance.json](Evidence/independence-acceptance.json)。真人盲测不是本次独立化验证，结果仍保持原模板pending。

### 最终独立构建实测

- 直接Unity Batch Build：Succeeded，0 errors / 0 warnings；11.402秒构建，Unity 2022.3.62f3。
- 正式模式360.0008秒胜利，玄武HP100/Qi80，最低Qi0；Dash34、Shield16、拾取61、吸收5次。
- 普通/连环/追踪/预测/封路、风/火/地/散花与三个Final组合全部实际发生，3次转段，9道神雷齐全。
- 77.0193秒Pause/Resume核心状态完全一致；胜利后实际点击Restart恢复HP100、Qi60、时间0、零危险。
- 测试输入改用Python标准库发送原Runtime协议，测试策略沿用T07。它只发送移动、镜头、技能输入，没有改血量、时间、资源、伤害；不依赖UnityClient或GameFactory Python模块。这不是人类盲测。
- 原T07玄武回归360.0022秒HP100/Qi60（Dash37/Shield17/拾取65）；独立回归的输入到达时机、帧时间和路线不同，因此不把逐局计数不同误判为数值修改。相同游戏代码/资源/配置哈希，加上技能费用和全部阶段事件审计，是本次一致性证据。

- 静止承伤死亡：19.5077秒，HP0，死因普通雷；死亡后Restart再次恢复初始状态。实体Q输入Qi60→36，护盾容量60。截图见Screenshots/independent-final-lose.png、restart.png、shield.png。
- 127个消费/拾取/护盾事件逐项核对通过。运行日志无Exception、Shader error、缺失音频或端口冲突。
- 原项目与旧Release共358个基线文件SHA-256均未变化。复制到独立版的游戏源码、资源、场景、原ProjectSettings全部逐字节相同。
- 游戏输入、音频仍使用原有本地Runtime；17个C#文件及asmdef与上游源文件一致。.meta沿用原Ascension生成的GUID，其哈希与上游若不同不代表改写Runtime代码。
- 原生证据/源码与Build最终指纹：Docs/Evidence/independent-final-fingerprint.json。

## 最终目录与清理

```text
Ascension/
├── UnityProject/
│   ├── Assets/
│   ├── Packages/
│   └── ProjectSettings/
├── Docs/
│   ├── IndependenceReport.md
│   ├── Licenses/
│   ├── Evidence/
│   └── Validation/       # 可选审计脚本，不参与编译或Build
├── Screenshots/
├── README.md
├── THIRD_PARTY_LICENSES.md
├── .gitignore
└── Builds/Windows/      # 已验证的新Release，可再生成、不入源码包
```

Unity Editor已正常关闭，Library、Logs、UserSettings已删除；Temp/Obj不存在，也不会进入源码包。没有符号链接、原仓库file包或运行/构建代码中的原仓库绝对路径。UnityProject最终只保留三个必需目录。独立源码ZIP不含Builds和Unity缓存；诊断游戏混音WAV不进入源码ZIP，游戏Assets中的正式Audio全部保留。

结论：**源码和开发流程已经脱离GameFactory-3A仓库结构**。复制/移动整个独立Ascension目录后，可用匹配Unity版本直接打开和重新Build。仍需外部安装Unity及官方包和有效License，保留Apache-2.0 Runtime和资产署名；“独立”不表示抹去第三方来源。没有改Gameplay、数值、UI、视觉、Audio，未增加新玩法。
