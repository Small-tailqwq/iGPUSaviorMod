# iGPU Savior（一键马赛克和小窗插件）


[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net472)
[![BepInEx](https://img.shields.io/badge/BepInEx-Plugin-green.svg)](https://github.com/BepInEx/BepInEx)

一个用于游戏 《*放松时光：与你共享Lo-Fi故事*》 的性能优化和小窗 BepInEx 插件，不可以让你的土豆电脑的风扇不转，我试过了。但是可以给你提供一个不错的无边框小窗模式。

---

[![Chill with You](./header_schinese.jpg)](https://store.steampowered.com/app/3548580/)

> 「放松时光：与你共享Lo-Fi故事」是一个与喜欢写故事的女孩聪音一起工作的有声小说游戏。您可以自定义艺术家的原创乐曲、环境音和风景，以营造一个专注于工作的环境。在与聪音的关系加深的过程中，您可能会发现与她之间的特别联系。
---



> 所有代码均由 AI 编写，人工仅作反编译和排错处理。
> 这是我第二次用 AI 为 Unity 游戏做 MOD，有问题请反馈，虽然反馈了我也是再去找 AI 修就是了🫥

---

关联我写的第一个同步 mod：[Chill Env Sync](https://github.com/Small-tailqwq/RealTimeWeatherMod)


## ✨ 主要功能


- `F2` - 变成土豆/变成薯条
	- 让游戏变模糊，约等于设置里的渲染分辨率最低效果
- `F3` - 在无边框小窗和上一个窗口形态之间游龙切换


## ⚠️ 本项目可能有

- 💽**西伯利亚**：土豆只是一个比喻，与任何硬件、厂商、西伯利亚无关。请善待每一颗土豆🤎
- 💥**莫名冲突**：如果未来游戏更新或者 Mod 大井喷，本插件很可能与其他插件发生冲突。*因为我也知道我和 AI 配合写得很烂，届时还请别抱太大修复希望*
- 💸**仅供学习**：本插件采用了 MIT许可证，法律上来说你可以想干啥干啥，但是请**不要直接拿 DLL 去卖**。
- 😵‍💫**粗制滥造**：本插件完全由 AI 编写，可能存在各种问题和漏洞，请谨慎使用。*我完全不懂 C#，那你叫我怎么办嘛*
- 🤖**智械危机**：使用本插件可能会加速AI统治世界的进程，后果自负。（<---这段不是我自己写的）


## 🎮 支持的环境类型

### 土豆模式
一坨，没写好。每次打开游戏还会糊脸，哪个2b写的？

### 无边框小窗模式
暂时没发现啥问题，有问题提交，下班随缘处理。



### TODO

>  🗒️ 本项目的未来计划和待办清单

- [ ] 把 BUG 修好
- [ ] 添加一个进入小窗后，自动触发一次隐藏游戏 UI 的功能
- [ ] 写好 README
- [ ] 统一一下两个项目的细节，包括日志输出啥的
- [ ] 整理一下开发经验，虽然我完全不懂，但是 Gemini 比我懂，有些坑可以避

## 📦 安装方法

### 前置要求
- 游戏本体
- [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases) 或更高版本

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
# # 默认F3，能用就别乱改
PiPModeKey = F3

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

### v1.6.0
- 第一个发布版本，后续看看能不能优化一下土豆模式，不过游戏优化这块我还真不懂

详细更新日志请查看 [Git 提交记录](https://github.com/Small-tailqwq/RealTimeWeatherMod/commits/master)

## 🐛 已知问题

- 土豆模式没啥用
- 组合键拖动窗口时，偶发需要鼠标点击两次才会生效（~~那你用右键不就完事了~~）

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