# 当前源码打开与构建

## 打开

1. 安装 Unity **2022.3.62f3** 与 Windows Build Support，并在 Unity Hub 使用有效 Editor 许可。
2. Hub 添加仓库下的 `UnityProject/`，而不是仓库根目录。
3. 等待 UPM 与 Library 恢复；当前包包括 Unity 官方依赖及后续安装的 Unity MCP Git 包，首次还原可能需要网络。
4. 打开 `Assets/Scenes/Ascension.unity`，也可使用菜单 `Ascension > Open Game Scene`。编辑模式下相机由运行时创建，因此 Game 页的 No cameras rendering 不单独证明运行错误。
5. Play 后从游戏菜单开始。默认 Formal180秒；Fast90秒仅用于开发覆盖。

## 构建

退出 Play，使用 `Ascension > Build Windows Release`。已有入口检查场景、运行规则断言并调用 BuildPipeline，输出：

`Builds/Windows/Ascension.exe`

构建入口会写 `Docs/Evidence/build.json`、`scene.json` 及模型检查 JSON。需要保留历史结果时，先备份这些文件；将本次输出保存到独立的日期目录，再恢复历史原件，不能覆盖旧证据后继续引用旧版本。

也可以使用 Unity 标准 Build Settings，Windows x86_64、包含 Ascension 场景、关闭 Development Build；这种路径不会自动执行本项目菜单中的全部模型检查。

## 命令行与许可排查

命令行可使用 `-batchmode -projectPath <UnityProject绝对路径> -buildTarget Win64 -executeMethod Ascension.Editor.StandaloneBuild.BuildWindows -logFile <日志路径>`；Editor 路径按本机安装位置填写。

若命令行显示未取得有效许可，但 Hub 已列出 Personal，应先尝试从 Hub 正常打开同版本 Editor。一次命令行错误不能证明 Personal 已失效，更不是 MCP 需要激活。不要混用已有 Editor 与另一进程同时打开同一项目。

## 运行与验证

完整复制 Windows 目录后启动 EXE，默认180秒。开发参数 `--ascension-fast` 切换90秒，`--ascension-seed <整数>` 设置种子，`--ascension-evidence <绝对JSONL路径>` 输出诊断。普通玩家不需要这些参数。

检查 Launch、Start、Movement、Dash、Shield、Pause、Restart、Win/Lose 与错误日志，结果按构建指纹记录在 [Playtest](Playtest.md)。本地输入脚本只作开发验证，不代表真人试玩。

构建和运行不需要 GameFactory Python、UnityClient 或 AI API Key。分发包保留完整 Data/Player 文件和[许可证](../THIRD_PARTY_LICENSES.md)，步骤见 [ReleaseChecklist](ReleaseChecklist.md)。
