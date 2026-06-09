# QuickInput

QuickInput 是一个 Windows 悬浮输入框工具。运行后驻留在系统托盘，通过全局快捷键唤起悬浮输入框，在悬浮框中输入的内容会实时同步到打开前的目标输入框，再次按下快捷键退出。

## 功能

- 全局快捷键唤起/退出悬浮输入框
- WPF 置顶悬浮输入框，支持拖拽、缩放
- 悬浮窗可直接输入，并实时写回打开前捕获到的目标输入框
- 自动记忆上一次位置和大小
- 屏幕分辨率、多显示器变化后按虚拟桌面比例恢复并修正到可见区域
- 托盘右键菜单：显示/隐藏、设置快捷键、开机自启动、复位位置、重启、退出
- 快捷键设置窗口，支持自定义组合键
- 目标输入框同步：
  - 优先使用 UI Automation `ValuePattern.SetValue()` 写回
  - 对标准 Win32/RichEdit 编辑框使用 `WM_SETTEXT` 兜底
  - 支持 `TextPattern` 的控件可读取初始内容，但通常不能写回
  - 不使用剪贴板，不模拟粘贴

## 构建

需要安装 .NET 10 SDK。

```powershell
.\scripts\publish.ps1
```

发布产物：

```text
artifacts\QuickInput.exe
```

## 开发运行

```powershell
dotnet run -c Debug
```

## 默认快捷键

```text
Ctrl + Alt + Space
```

如果快捷键被其他程序占用，应用会继续驻留托盘，可通过托盘菜单重新设置。

## 兼容性说明

Windows 对跨进程输入有权限边界。普通权限运行的 QuickInput 无法读写管理员权限运行的目标程序，这是系统 UIPI 安全限制。对浏览器、Office、聊天软件、IDE 等复杂编辑器，同步能力取决于目标控件是否暴露可写的 UI Automation `ValuePattern`。如果目标控件只暴露 `TextPattern` 或完全不暴露文本接口，悬浮框仍可输入，但状态栏会提示无法写回目标。
