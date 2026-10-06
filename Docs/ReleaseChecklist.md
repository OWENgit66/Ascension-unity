# V1.1 Portfolio Windows Release

状态日期：2026-10-07。

## 当前包

- 文件：`Ascension-v1.1-Portfolio-Windows.zip`
- 本机位置：`E:/AI项目/Unity/Ascension-Releases/Ascension-v1.1-Portfolio-Windows.zip`，位于Git仓库外。
- 大小：34,048,697字节，约32.47MiB；154个ZIP条目。
- SHA-256：`5e26cc627964956ba6a5d90fa9ef0cd7b9869a40c229ae95cd227f0754657015`
- 版本口径：V1.1 R1 + 常规雷击去文字；本轮仅Portfolio整理，未改游戏规则或数值。
- Unity2022.3.62f3，Windows x64 Release，Formal180秒 / Fast90秒。

## 已完成

- [x] 从当前源码重新Build，0错误/0警告；[Build报告](Evidence/portfolio-20261007/build.json)。
- [x] 新EXE启动、开始、Movement、Qi、Dash、Shield、Pause、Restart、Win/Lose基础验证；[Playtest](Playtest.md)。
- [x] 当前完整正式局：玄武、seed24130、180.003秒Won，HP100/Qi56；明确属于自动策略。
- [x] ZIP保留EXE、Data、MonoBleedingEdge、UnityPlayer/CrashHandler和所需运行文件。
- [x] 随附README.txt、ReleaseInfo.json、THIRD_PARTY_LICENSES.md、AssetSources、GameFactory/Quaternius/Kenney许可证及复制来源清单。
- [x] 排除`UnityProject_BurstDebugInformation_DoNotShip`、Unity缓存、测试日志和原始混音录音；原构建目录未删。
- [x] ZIP CRC检查、逐文件SHA-256与已测试Build一致；[包记录](Evidence/portfolio-20261007/release-package.json)、[文件清单](Evidence/portfolio-20261007/build-manifest.json)。

## 人工发布前

- [ ] 审阅本轮文档/素材diff和此前未提交的游戏/UI/MCP配置改动，再创建能对应源码的提交与tag；建议版本名`v1.1-portfolio`，当前未创建tag。
- [ ] 在另一台Windows机器完整解压测试：启动、中文字体、分辨率、音量、输入与首次体验。不要只拷贝EXE。
- [ ] 由项目作者确认后创建GitHub Release，上传此ZIP；本轮没有push、上传或发布。
- [ ] Release说明附版本、Controls、Formal/Fast时长、Win/Lose条件、第三方许可和已知限制。
- [ ] 实际上传成功后，再把README的“尚未发布”替换为真实下载URL；不要提前造链接。
- [ ] 安排[真人盲测](BlindPlaytest.md)，如有修改重新Build、更新指纹、录像与验证，不复用旧验收冒充新版本。

## 应公开说明的限制

独立真人盲测样本0；自动输入能读取精确危险坐标，不能代表玩家通关率或乐趣。本轮是单功法/单种子基础回归，尚未完成全部功法多种子覆盖。小型单人竞技场原型；镜头边缘危险、无文字预警理解和Qi压力待真人反馈。中文使用系统字体，跨机器表现需确认。GIF约5fps、无音轨，不是性能帧率。

## 许可与命令行问题

本包运行不需要Unity Hub、MCP、GameFactory仓库或AI API Key；重新开发/构建需要合法Unity Editor。曾遇到命令行Editor未取得许可，改由Hub打开后成功构建；不需要因为该错误重新购买Personal或“激活MCP”。

项目没有在本轮新增全仓库开源许可；第三方许可按原作者条款分别保留，不能把Apache或CC0标签当作全项目授权。
