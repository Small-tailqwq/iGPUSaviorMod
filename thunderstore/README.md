# iGPU Savior（性能和体验优化插件）


[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net472)
[![BepInEx](https://img.shields.io/badge/BepInEx-Plugin-green.svg)](https://github.com/BepInEx/BepInEx)

一个用于游戏 《*放松时光：与你共享Lo-Fi故事*》 的性能和体验优化 BepInEx 插件，不可以让你的土豆电脑的风扇不转，我试过了。但是可以给你提供一个不错的镜像、小窗、竖屏模式。

---

[![Chill with You](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/header_schinese.jpg)](https://store.steampowered.com/app/3548580/)

> 「放松时光：与你共享Lo-Fi故事」是一个与喜欢写故事的女孩聪音一起工作的有声小说游戏。您可以自定义艺术家的原创乐曲、环境音和风景，以营造一个专注于工作的环境。在与聪音的关系加深的过程中，您可能会发现与她之间的特别联系。
---



> 所有代码均由 AI 编写，人工仅作反编译和排错处理。
> 这是我第二次用 AI 为 Unity 游戏做 MOD，有问题请反馈，虽然反馈了我也是再去找 AI 修就是了🫥

---

关联我写的第一个同步 mod：[Chill Env Sync](https://github.com/Small-tailqwq/RealTimeWeatherMod)


## 快速演示

![Steam 论坛演示](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/ptt4.png)
<p align="center"><i>在 Steam 社区发帖的同时，用小窗观看游戏画面</i></p>

#### ⚡ 性能优化
显著降低游戏资源占用，让你的电脑更流畅：

**内存占用对比：**

![优化前内存占用](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/mmr1.png)
<p align="center"><i>优化前：内存占用 1,134.8 MB</i></p>

![优化后内存占用](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/mmr2.png)
<p align="center"><i>优化后：内存占用 459.5 MB</i></p>

- 优化前：1,134.8 MB
- 优化后：459.5 MB
- **节省约 60% 内存占用** 💾

**GPU 占用对比：**

![优化前 GPU 占用](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/ptt3.png)
<p align="center"><i>优化前：3D 占用率 43%，持续高负载</i></p>

![优化后 GPU 占用](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/ptt2.png)
<p align="center"><i>优化后：3D 占用率 16%，显著降低</i></p>

- 优化前：3D 占用率 43%，持续高负载
- 优化后：3D 占用率 16%，显著降低
- **减少约 63% 的 GPU 负担** 🎮

> **⚠️ 性能优化原理说明：**  
> 测试环境为 AMD 4800H 笔记本核显；小窗模式默认分辨率为原始分辨率的 1/3，*减少约 67% 的渲染像素*



## ✨ 主要功能


- `F2` - 变成土豆/变成薯条
	- 让游戏变模糊，约等于设置里的渲染分辨率最低效果
- `F3` - 在无边框小窗和上一个窗口形态之间游龙切换
- `F4` - 切换摄像机镜像模式（左右翻转画面）
	- 视觉、输入、音频完全镜像，沉浸式体验
	- 自适应窗口大小变化，无需手动调整
- `F5` - 切换竖屏优化模式（增大竖屏视角）
	- 可在配置中设置启动时自动启用
	- 支持在游戏内 MOD 设置中开关"竖优自启动"


## ⚠️ 本项目可能有

- ⛄**西伯利亚**：土豆只是一个比喻，与任何硬件、厂商、西伯利亚无关。请善待每一颗土豆🤎
- 💥**莫名冲突**：如果未来游戏更新或者 Mod 大井喷，本插件很可能与其他插件发生冲突。*因为我也知道我和 AI 配合写得很烂，届时还请别抱太大修复希望*
- 💸**仅供学习**：本插件采用了 MIT许可证，法律上来说你可以想干啥干啥，但是请**不要直接拿 DLL 去卖**。
- 😵‍💫**粗制滥造**：本插件完全由 AI 编写，可能存在各种问题和漏洞，请谨慎使用。*我完全不懂 C#，那你叫我怎么办嘛*
- 🤖**智械危机**：使用本插件可能会加速AI统治世界的进程，后果自负。（<---这段不是我自己写的）


## 🎮 支持的环境类型

### 土豆模式
一坨，没写好。优化微乎其微，而且画面还会变得很糊，唯一的作用就是到时候绑定窗口焦点，实现无缝切换：
后台不可见时，将画面变成马赛克模式，减少显卡占用，聚焦或者置顶时不自动应用。

### 无边框小窗模式
暂时没发现啥问题，有问题提交，下班随缘处理。
这玩意真没啥问题吧



### TODO

>  🗒️ 本项目的未来计划和待办清单

- [ ] 把 BUG 修好
- [ ] 添加一个进入小窗后，自动触发一次隐藏游戏 UI 的功能
  - [ ] 这个要查看第一个 mod 的代码，抄袭一下
- [ ] 写好 README
- [ ] 统一一下两个项目的细节，包括日志输出啥的
- [ ] 整理一下开发经验，虽然我完全不懂，但是 Gemini 比我懂，有些坑可以避

## 📦 安装方法

### 前置要求
- 游戏本体
- [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases) 版本，别下 6.0

### 安装步骤

1. 确保已正确安装 BepInEx 框架
2. 将下载好的 `iGPU.Savior.dll` 放入 `BepInEx/plugins/` 目录
3. 启动游戏，插件将自动加载
4. 编辑配置文件调整各种快捷键

## ⚙️ 配置说明

首次运行后，配置文件将生成在 `BepInEx/config/chillwithyou.potatomode`

### 配置项说明

```ini
[Hotkeys]

## 切换土豆模式的按键
# Setting type: KeyCode
# Default value: F2
# 默认F2，能用就别乱改
PotatoModeKey = F2

## 切换画中画小窗的按键
# Setting type: KeyCode
# Default value: F3
# 默认F3，能用就别乱改
PiPModeKey = F3

## 切换摄像机镜像的按键(左右翻转画面)
# Setting type: KeyCode
# Default value: F4
# 默认F4，镜像模式包含视觉、输入、音频完全翻转
CameraMirrorKey = F4

## 切换竖屏优化的按键(方便调试参数)
# Setting type: KeyCode
# Default value: F5
# 默认F5，能用就别乱改
PortraitModeKey = F5

[Camera]

## 启动时是否自动启用摄像机镜像(默认关闭,建议先用UE Explorer测试)
# Setting type: Boolean
# Default value: false
EnableMirrorOnStart = false

## 启动时是否自动启用竖屏优化(默认关闭,如启用会在游戏初始化后自动激活)
# Setting type: Boolean
# Default value: false
# 注意：此功能会在场景加载后15秒自动启用，确保游戏完全初始化
EnablePortraitMode = false

[Window]

## 小窗缩放比例
# Setting type: WindowScaleRatio
# Default value: OneThird
# 分别是三分之一，四分之一，五分之一。默认根据屏幕大小自动计算
# Acceptable values: OneThird, OneFourth, OneFifth
ScaleRatio = OneThird

## 拖动方式
# Setting type: DragMode
# Default value: Ctrl_LeftClick
# 分别是 Ctrl+左键拖动，Alt+左键拖动，右键拖动（个人推荐右键，最顺手）
# Acceptable values: Ctrl_LeftClick, Alt_LeftClick, RightClick_Hold
DragMethod = RightClick_Hold
```

## 🚀 使用方法

### 基础使用

直接看 BILIBILI 视频：[时间、天气与土豆](https://www.bilibili.com/video/BV1JXSiB4EP1)

## 🔧 技术细节

- **框架**：BepInEx 5.x
- **目标框架**：.NET Framework 4.7.2
- **使用技术/工具**：
  - 反射（用于访问游戏内部系统）
  - 各种大语言模型

## 📝 版本历史
> 注：版本号为 AI 自己写的，不关我的事

### v1.7.3（最新版本）- 多语言与小窗优化版
- 🌍 **多语言系统重构**：
  - ✨ **极简多语言支持**：内置了简体中文、繁体中文（回落到简中）、日文、英文的本地化支持。
  - 🐛 **语言切换 Bug 修复**：修复了在游戏内切换语言时，“MOD 设置”标签变回 "Credits" 或者内容不更新的问题。
  - 🔄 **动态刷新**：所有 MOD 设置项现已支持动态语言切换，无需重启游戏即可实时更新文本和字体。
- 🔌 **第三方 MOD 对接升级**：
  - 🛠️ **新增多语言 API**：`ModSettingsManager` 新增 `RegisterTranslation` 方法，允许第三方 MOD 注册自己的翻译 Key。
  - ✅ **向后兼容**：旧版对接代码依然有效，但建议更新以支持多语言切换（详见文档或示例）。
- 🪟 **小窗模式优化**：
  - 💾 **记忆位置**：优化了 F3 小窗的位置记忆逻辑，修复了从全屏切回小窗时位置重置的问题。
  - 🔐 **登录验证修复**：修复了 F3 切换时可能导致的登录窗口异常或状态丢失问题。

### v1.7.2 - 竖屏优化优化版
- 🐛 **重大 Bug 修复**：彻底解决竖屏优化自动启用时的相机位置错误问题
  - 📍 **问题根源**：启动时延迟太短（0.5秒），导致保存了游戏初始化时的默认相机位置 `(0,1,-10)` 而非实际位置
  - ⏱️ **解决方案**：采用与镜像模式相同的延迟机制，改为场景加载后 15 秒再启用，确保游戏完全初始化
  - 💾 **参数保留机制**：Toggle 关闭时保留已保存的原始参数，重新启用时无需重新保存，避免在竖屏状态下误保存参数
  - 🔄 **智能初始化**：首次启用时检查是否已有保存的参数，避免重复保存
- ⚙️ **新增配置项**：
  - GUI 设置中新增"竖优自启动"开关，可在游戏内直接调整
  - 配置文件新增 `EnablePortraitMode` 选项（默认关闭，建议手动测试后再开启）
- 📊 **改进日志**：优化竖屏优化相关的日志输出，方便问题追踪

### v1.7.1
- 添加了设置中直接调整 MOD 配置的接口，支持其他 MOD 接入  
  - 目前接入的 MOD 有：[Chill Env Sync](https://github.com/Small-tailqwq/RealTimeWeatherMod)  
- 添加了竖屏优化，支持在竖屏状态通过 F5 打开或关闭，开启时可以获得更大的视角  
  - 不建议在竖屏工作时开启，因为竖屏工作是特写🥰  
- 添加了小窗模式，支持通过 F3 打开或关闭，开启时强制置顶，可以通过组合键或者鼠标右键的形式进行移动  
  - 未来可能会加入竖屏小窗功能，类似微信电话浮窗

### v1.7.0
- ✨ **镜像模式重大改进**：完全重写镜像实现，修复所有已知问题
  - 🎨 **修复光照撕裂**：改用 RenderTexture + UV 翻转方案，不再破坏角色法线和蒙皮权重
  - 🖱️ **智能输入映射**：鼠标点击自动适配镜像画面，点击 UI 和 3D 场景时坐标分别处理
  - 🎧 **音频声道交换**：镜像模式下自动交换左右声道，视听完全一致
  - 🖼️ **自动分辨率适配**：窗口大小变化时自动重建 RenderTexture，无蓝屏问题
  - 🔧 **资源管理优化**：正确释放 RenderTexture/Canvas/Material，无内存泄漏
- 🎯 **新增功能**：
  - `F4` - 切换摄像机镜像模式（左右翻转画面）
  - 配置项：`CfgEnableMirror` - 启动时是否自动启用镜像（默认关闭）
- 🐛 **Bug 修复**：
  - 修复镜像模式下点击事件穿透问题（UI 可正常点击）
  - 修复窗口调整大小时画面变蓝的问题
  - 修复鼠标输入无限递归导致的死锁问题

### v1.6.0
- 第一个发布版本，后续看看能不能优化一下土豆模式，不过游戏优化这块我还真不懂

详细更新日志请查看 [Git 提交记录](https://github.com/Small-tailqwq/iGPUSaviorMod/commits/master)

## 🐛 已知问题

- 土豆模式没啥用
- 组合键拖动窗口时，偶发需要鼠标点击两次才会生效（~~那你用右键不就完事了~~）
- ~~镜像模式下光照撕裂问题~~ ✅ v1.7.0 已修复
- ~~镜像模式下点击事件错位~~ ✅ v1.7.0 已修复
- ~~调整窗口大小后画面变蓝~~ ✅ v1.7.0 已修复
- ~~竖屏优化自启动时相机位置错误~~ ✅ v1.7.2 已修复

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 关于反馈

如需反馈问题，请先确保问题“可复现”，并开启调试日志（BepInEx/config/BepInEx.cfg 中将 `Logging.Console` 设置为 `true`）。  
这时，游戏启动时会在控制台输出详细日志，有助于定位问题。

## 📄 许可证

本项目采用 **MIT 许可证** 开源。

**⚠️ 重要声明**：
- ✅ 可以自由使用、修改和分发
- ✅ 可以用于个人学习和研究
- 使用本软件产生的任何后果由使用者自行承担

详见 [LICENSE](LICENSE) 文件。

## 👨‍💻 作者

- GitHub: [@Small-tailqwq](https://github.com/Small-tailqwq)

## 🙏 致谢

- BepInEx 团队
- Google Gemini3Pro



---

**免责声明**：本插件仅供学习交流使用，请勿用于商业用途。使用本插件产生的任何问题与作者无关。


> 我不爱上班