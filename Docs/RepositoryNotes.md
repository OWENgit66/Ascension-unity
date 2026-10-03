# GitHub 上传说明

整理日期：2026-10-04。版本：V1.1 R1。

## 是否收官

当前作为游戏策划作品集Demo已完成核心开发：T07及V1.1 R1完成，正式180秒通关、Win/Lose/Restart/Pause均有实际Windows构建记录。最近验收为180.003708秒胜利、HP65/Qi16，构建0错误0警告。

修改后的真人盲测、展示GIF及更广泛的功法/随机种子平衡验证尚未完成；不将自动输入测试描述为真人通关。详见Playtest与BlindPlaytest。

## 上传这个文件夹

将 `Ascension-GitHub` 文件夹作为仓库根目录，上传里面的内容。GitHub仓库首页应直接显示README.md，旁边有UnityProject、Docs、Screenshots等目录，不要额外嵌套一层Ascension-GitHub。

```text
UnityProject/
  Assets/              # 完整源码、场景、游戏资源与.meta
  Packages/            # manifest与lock
  ProjectSettings/     # 项目设置及Unity版本
Docs/                  # 策划、平衡、Playtest、许可、验证脚本与证据
Screenshots/
README.md
THIRD_PARTY_LICENSES.md
TODO.json
.gitignore
```

UnityProject的166个文件已与当前开发项目逐文件SHA-256核对，一致。游戏源码、数值、视觉、音频没有修改。本次未重新Build或重跑游戏；保留的是此前已验收版本，整理仅影响仓库说明与文件选择。

上传副本约44MB，未初始化Git或连接远程，也未执行任何上传。请同时保留隐藏文件.gitignore。原开发项目仍保留，之后如继续开发，应选定一份作为主工作目录，避免两份分叉。

## 打开与构建

1. 安装Unity 2022.3.62f3和Windows Build Support。
2. Unity Hub添加本仓库下的UnityProject文件夹，等待UPM和Library恢复。
3. 打开Assets/Scenes/Ascension.unity，Play进入游戏。
4. 菜单Ascension > Build Windows Release，产物位于仓库根下Builds/Windows。

Unity需正常激活和下载官方依赖；无需GameFactory仓库、API Key或Python。Docs/Validation中的Python脚本仅用于开发验证，不影响Unity打开、运行或构建。

## 保留与排除

保留完整游戏资源（包含实际使用的WAV）、全部许可证和来源说明、策划文档、截图、JSON验收摘要及JSONL输入/事件轨迹。

排除Unity的Library/Temp/Logs/Obj/UserSettings、Builds、IDE临时文件；排除Docs/Evidence中的原始.log、测试混音.wav以及本机路径指针。历史文档中的这些文件名指向开发归档，并不表示它们包含在本上传包中。原始测试记录未被重新生成或改写；其中出现的旧绝对路径是历史环境信息，不是源码依赖。

Docs/Validation/audit_v11.py可核验已保留的V1.1原生记录。若重新使用play_native.py，需要按自己的运行日志位置创建Docs/Evidence/latest-runtime-path.txt；它是本机配置，不应入库。audit_independence.py属于早期独立化现场检查，引用原测试机器路径，作为历史脚本保留。

## 许可与可执行版

THIRD_PARTY_LICENSES.md、Docs/AssetSources.md及Docs/Licenses保留GameFactory Apache-2.0、模型/音效许可与署名。本次没有替项目自有代码选择新的开源许可证；公开源码不表示第三方和自有内容统一使用Apache-2.0。

此文件夹是源码仓库。若之后上传可下载的游戏版，应单独打包整个Windows构建目录并保留随附许可证，不要仅上传Ascension.exe，也不要将Unity Library缓存放入仓库。
