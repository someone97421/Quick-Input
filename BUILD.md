# 构建说明

QuickInput 使用 .NET 10 SDK 构建。发布脚本会一次性生成两个 Windows x64 版本，并默认先清理 `artifacts` 目录里的旧产物。

## 一键发布

```powershell
.\scripts\publish.ps1
```

输出示例：

```text
artifacts\QuickInput-self-contained-win-x64-20260617-223533.exe
artifacts\QuickInput-framework-dependent-win-x64-20260617-223533.exe
```

## 产物区别

- `self-contained`：免安装 .NET 运行时，体积较大，适合直接分发给普通用户。
- `framework-dependent`：体积较小，需要目标机器已安装 .NET 10 Desktop Runtime x64。

## 保留旧产物

默认构建会清理旧版。如果需要保留旧产物：

```powershell
.\scripts\publish.ps1 -KeepOld
```

## 可选参数

```powershell
.\scripts\publish.ps1 -Configuration Release -Runtime win-x64
```

如果清理时报文件被占用，先从系统托盘退出正在运行的 QuickInput，再重新执行发布脚本。
