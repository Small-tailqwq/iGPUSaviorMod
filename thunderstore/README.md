# iGPU Savior（性能和体验优化插件）

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net472)
[![BepInEx](https://img.shields.io/badge/BepInEx-Plugin-green.svg)](https://github.com/BepInEx/BepInEx)
[![opencode](https://github.com/anomalyco/opencode/raw/dev/packages/console/app/src/asset/logo-ornate-dark.svg)](https://github.com/anomalyco/opencode)

一个用于游戏 《*放松时光：与你共享Lo-Fi故事*》 的性能和体验优化 BepInEx 插件。可以降低资源占用，并提供镜像、小窗、竖屏等增强体验模式。

---

[![Chill with You](https://raw.githubusercontent.com/Small-tailqwq/iGPUSaviorMod/refs/heads/master/img/header_schinese.jpg)](https://store.steampowered.com/app/3548580/)

> 「放松时光：与你共享Lo-Fi故事」是一个与喜欢写故事的女孩聪音一起工作的有声小说游戏。您可以自定义艺术家的原创乐曲、环境音和风景，以营造一个专注于工作的环境。在与聪音的关系加深的过程中，您可能会发现与她之间的特别联系。

---

> 所有代码均由 AI 编写，人工仅作反编译和排错处理。

关联我写的第一个同步 mod：[Chill Env Sync](https://github.com/Small-tailqwq/RealTimeWeatherMod)

## ✨ 主要功能

- `F2` - 切换土豆模式（降低画质，减少占用）
  - 三级模式系统：正常 / 土豆(手动F2) / 后台(失焦自动)
- `F3` - 切换无边框小窗模式
  - 记忆位置和窗口尺寸，可配置自动隐藏游戏 GUI
- `F4` - 切换摄像机镜像模式（左右翻转画面）
  - 视觉、输入、音频完全镜像，沉浸式体验
  - 自适应窗口大小变化，无需手动调整
- `F5` - 切换竖屏优化模式（增大竖屏视角）
  - 可在配置中设置启动时自动启用
  - 支持在游戏内 MOD 设置中开关"竖优自启动"

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
PotatoModeKey = F2

## 切换画中画小窗的按键
# Setting type: KeyCode
# Default value: F3
PiPModeKey = F3

## 切换摄像机镜像的按键(左右翻转画面)
# Setting type: KeyCode
# Default value: F4
CameraMirrorKey = F4

## 切换竖屏优化的按键(方便调试参数)
# Setting type: KeyCode
# Default value: F5
PortraitModeKey = F5

[Camera]

## 启动时是否自动启用摄像机镜像(默认关闭)
# Setting type: Boolean
# Default value: false
EnableMirrorOnStart = false

## 启动时是否自动启用竖屏优化(默认关闭)
# Setting type: Boolean
# Default value: false
EnablePortraitMode = false

[Performance]

## 窗口失焦时自动省电(限帧10fps+渲染分辨率0.1+关闭阴影)；小窗模式始终豁免
# Setting type: Boolean
# Default value: true
EnableBackgroundOptimization = true

[Window]

## 小窗初始缩放比例（之后会优先恢复上次自由尺寸和形状）
# Setting type: WindowScaleRatio
# Default value: OneThird
# Acceptable values: OneThird, OneFourth, OneFifth
ScaleRatio = OneThird

## 拖动方式
# Setting type: DragMode
# Default value: Ctrl_LeftClick
# Acceptable values: Ctrl_LeftClick, Alt_LeftClick, RightClick_Hold
DragMethod = RightClick_Hold

## 进入小窗时自动隐藏GUI，退出时恢复
# Setting type: Boolean
# Default value: false
AutoHideGuiInPiP = false
```

## 🎮 使用方法

### 基础使用
- 观看演示视频：[时间、天气与土豆](https://www.bilibili.com/video/BV1JXSiB4EP1)
- 使用快捷键切换不同模式（F2-F5）

## 🔧 技术细节

- **框架**：BepInEx 5.x
- **目标框架**：.NET Framework 4.7.2
- **使用技术/工具**：
  - 反射（用于访问游戏内部系统）
  - 各种大语言模型

## 📝 版本历史

详细更新日志请查看 [GitHub 版本历史](https://github.com/Small-tailqwq/iGPUSaviorMod/releases)

### v1.9.1（最新版本）- MOD 设置条件可见性 API
- ✨ **条件可见性 API**：第三方 MOD 可通过 `visibleWhen` 声明配置项联动显隐。
- 🔄 **实时依赖刷新**：配置项变更后自动刷新依赖项显示状态。
- ✅ **稳定性加固**：补充条件工厂、求值器与边界测试，配置错误 fail-open 限流警告。
- 🐛 **缺陷修复**：修复设置界面输入框行距异常。
- 📚 **文档更新**：完善 README、接入指南与版本同步脚本说明。

### v1.9.0 - 三级渲染模式与设置面板架构重构
- 🥔 **三级渲染模式**：正常 / 土豆(F2) / 后台(失焦自动)，三级独立保存/恢复渲染参数
  - 后台省电优化：窗口失焦自动限帧 10fps + 渲染分辨率 0.1 + 关闭阴影和垂直同步
  - PiP 豁免：小窗期间跳过后台优化，退出后按焦点状态恢复
- 🪟 **小窗模式增强**：记忆位置和窗口尺寸、窗口子类化重写
  - 新增 `AutoHideGuiInPiP` 配置项
- 🎨 **MOD 设置面板重构**：从 Credits 切换到 General 模板，引入共用样式系统
- 🐛 服装悄悄话空值防护、日志级别统一降噪

### v1.8.1 - 原版设置激活态修复与发版流程改进
- 🐛 **设置界面修复**：修复安装 mod 后原版设置 ON/OFF 激活高亮丢失的问题。
- ✅ **回归测试**：新增字段筛选回归测试。
- 🧩 **版本管理统一**：新增 `version.json` + `scripts/sync-version.ps1`。

### v1.8.0 - 笔记导出与缺陷修复版
- 📝 **笔记多选导出**：支持多选笔记导出为 UTF-8 with BOM 的 `.txt` 文件。
- 🗑️ **删除确认**：笔记删除增加确认弹窗。
- 🐛 **缺陷修复**：修复镜像模式 UI 按钮错误激活。
- 📚 **文档整顿**：重组文档目录。

### v1.7.4 - UI 架构自适应修复与优化版
- 🐛 **UI 布局崩溃修复**：为动态文字组件正确挂载字体资源上下文
- 🎨 **无缝兼容原版界面**：启用自适应弹性伸缩特性
- ⚙️ **标签竞态死结修复**：重写标签高亮和面板切换判定系统
- 🎛️ **设置控件样式统一**：竖屏模式控件过渡至原生下拉菜单

## 🐛 已知问题

- 土豆模式优化微乎其微，画面会变糊
- 组合键拖动窗口时，偶发需要鼠标点击两次才会生效（建议改用右键拖动）

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 关于反馈
如需反馈问题，请先确保问题"可复现"，并开启调试日志（BepInEx/config/BepInEx.cfg 中将 `Logging.Console` 设置为 `true`）。

## 📄 许可证

本项目采用 **MIT 许可证** 开源。

**⚠️ 重要声明**：
- ✅ 可以自由使用、修改和分发
- ✅ 可以用于个人学习和研究
- ⚠️ 使用本软件产生的任何后果由使用者自行承担

## 👨‍💻 作者

- GitHub: [@Small-tailqwq](https://github.com/Small-tailqwq)

## 🙏 致谢

- BepInEx 团队
- Google Gemini 3 Pro

---

**免责声明**：本插件仅供学习交流使用，请勿用于商业用途。使用本插件产生的任何问题与作者无关。
