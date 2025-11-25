# LizardClient 构建说明

## 快速开始

### 方式 1: 使用批处理脚本（推荐）

**开发构建（Debug）**
```bash
.\build.bat
```

**完整 Release 构建**
```bash
.\build-release.bat
```

### 方式 2: 使用 PowerShell 脚本

**基本构建**
```powershell
.\build.ps1
```

**完整构建流程**
```powershell
.\build.ps1 -All
```

**Release 构建**
```powershell
.\build.ps1 -All -Release
```

**仅清理**
```powershell
.\build.ps1 -Clean
```

**仅发布**
```powershell
.\build.ps1 -Publish
```

## 构建选项说明

| 选项 | 说明 |
|------|------|
| `-Clean` | 清理所有 bin 和 obj 目录 |
| `-Restore` | 恢复 NuGet 包依赖 |
| `-Build` | 编译解决方案 |
| `-Publish` | 发布为单文件可执行程序 |
| `-Release` | 使用 Release 配置（默认为 Debug） |
| `-All` | 执行所有步骤（清理 + 恢复 + 构建 + 发布） |

## 输出目录

- **Debug 构建**: `build/Debug/`
- **Release 构建**: `build/Release/`
- **发布输出**: `build/{Configuration}/publish/`

## 手动构建命令

如果你更喜欢使用原生的 dotnet CLI：

```bash
# 恢复包
dotnet restore

# 构建
dotnet build --configuration Release

# 发布启动器
dotnet publish src/LizardClient.Launcher/LizardClient.Launcher.csproj `
    --configuration Release `
    --output ./publish `
    --runtime win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true
```

## 运行项目

**开发模式**
```bash
dotnet run --project src/LizardClient.Launcher/LizardClient.Launcher.csproj
```

**Release 模式**
```bash
cd build/Release/publish
.\LizardClient.Launcher.exe
```

## 故障排除

### PowerShell 执行策略错误

如果遇到"无法加载文件，因为在此系统上禁止运行脚本"错误：

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

### 构建失败

1. 确保已安装 .NET 10 SDK
2. 检查网络连接（NuGet 还原需要网络）
3. 尝试手动清理：`.\build.ps1 -Clean`
4. 检查是否有文件被占用（关闭 Visual Studio / Rider）

## 系统要求

- **操作系统**: Windows 10/11 (64-bit)
- **.NET SDK**: .NET 10.0 或更高版本
- **PowerShell**: 5.1 或更高版本（推荐 PowerShell 7+）
- **磁盘空间**: 至少 500 MB（包含构建输出）

## 持续集成 (CI)

对于 CI/CD 环境，推荐使用：

```bash
.\build.ps1 -All -Release
```

这将确保干净的、完整的 Release 构建。
