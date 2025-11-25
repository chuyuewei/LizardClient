# LizardClient 更新服务器配置脚本
# 用途：快速配置客户端的更新服务器地址

param(
    [string]$ServerUrl = "http://localhost:51000",
    [switch]$UseLocalhost,
    [switch]$Help
)

# 显示帮助信息
if ($Help) {
    Write-Host @"
LizardClient 更新服务器配置脚本

用法:
    .\configure-update-server.ps1 [-ServerUrl <url>] [-UseLocalhost]

参数:
    -ServerUrl <url>    指定更新服务器URL (默认: http://localhost:51000)
    -UseLocalhost       使用本地服务器 (相当于 -ServerUrl http://localhost:51000)
    -Help               显示此帮助信息

示例:
    .\configure-update-server.ps1
    .\configure-update-server.ps1 -UseLocalhost
    .\configure-update-server.ps1 -ServerUrl https://updates.lizardclient.com

"@
    exit 0
}

# 使用本地服务器
if ($UseLocalhost) {
    $ServerUrl = "http://localhost:51000"
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   LizardClient 更新服务器配置" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 查找配置文件
$configPaths = @(
    "$env:APPDATA\.lizardclient\config.json",
    ".\config.json",
    ".\src\LizardClient.Launcher\bin\Debug\net10.0-windows\config.json",
    ".\src\LizardClient.Launcher\bin\Release\net10.0-windows\config.json"
)

$configFile = $null
foreach ($path in $configPaths) {
    if (Test-Path $path) {
        $configFile = $path
        break
    }
}

if (-not $configFile) {
    # 如果没有找到配置文件，创建一个新的
    $defaultConfigPath = "$env:APPDATA\.lizardclient"
    if (-not (Test-Path $defaultConfigPath)) {
        New-Item -ItemType Directory -Path $defaultConfigPath -Force | Out-Null
    }
    $configFile = "$defaultConfigPath\config.json"
    
    Write-Host "未找到现有配置文件，将创建新配置:" -ForegroundColor Yellow
    Write-Host "  -> $configFile" -ForegroundColor Gray
    Write-Host ""
    
    # 创建默认配置
    $config = @{
        ConfigVersion = "1.0.0"
        PreferredLanguage = "zh-CN"
        Theme = "Dark"
        EnableHardwareAcceleration = $true
        EnableAutoUpdate = $true
        EnableTelemetry = $false
        DownloadThreads = 4
        UpdateChannel = "Stable"
        AutoDownloadUpdates = $false
        UpdateCheckInterval = 24
        UpdateServerUrl = $ServerUrl
        MaxDownloadSpeed = 0
        Performance = @{
            FpsBoostLevel = 2
            MemoryOptimizationLevel = 2
            EnableFastLoading = $true
            MaxMemoryMB = 2048
        }
        Preferences = @{
            MinimizeToTray = $true
            StartWithSystem = $false
            CloseAfterGameLaunch = $false
            ShowNews = $true
            Hotkeys = @{
                OpenModMenu = "F6"
                ToggleZoom = "C"
                Freelook = "LeftAlt"
            }
        }
    }
} else {
    Write-Host "找到配置文件:" -ForegroundColor Green
    Write-Host "  -> $configFile" -ForegroundColor Gray
    Write-Host ""
    
    # 读取现有配置
    try {
        $configJson = Get-Content $configFile -Raw -Encoding UTF8
        $config = $configJson | ConvertFrom-Json -AsHashtable
    } catch {
        Write-Host "配置文件格式错误，将创建新配置" -ForegroundColor Yellow
        $config = @{
            ConfigVersion = "1.0.0"
            EnableAutoUpdate = $true
            DownloadThreads = 4
            UpdateChannel = "Stable"
            AutoDownloadUpdates = $false
            UpdateCheckInterval = 24
            UpdateServerUrl = $ServerUrl
            MaxDownloadSpeed = 0
        }
    }
}

# 更新服务器URL
$oldUrl = $config.UpdateServerUrl
$config.UpdateServerUrl = $ServerUrl

# 保存配置
try {
    $configJson = $config | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($configFile, $configJson, [System.Text.Encoding]::UTF8)
    
    Write-Host "配置已更新:" -ForegroundColor Green
    Write-Host "  旧地址: $oldUrl" -ForegroundColor Gray
    Write-Host "  新地址: $ServerUrl" -ForegroundColor Cyan
    Write-Host ""
    
    # 显示其他更新相关设置
    Write-Host "当前更新设置:" -ForegroundColor Yellow
    Write-Host "  更新频道: $($config.UpdateChannel)" -ForegroundColor Gray
    Write-Host "  自动更新: $(if ($config.EnableAutoUpdate) { '启用' } else { '禁用' })" -ForegroundColor Gray
    Write-Host "  自动下载: $(if ($config.AutoDownloadUpdates) { '启用' } else { '禁用' })" -ForegroundColor Gray
    Write-Host "  检查间隔: $($config.UpdateCheckInterval) 小时" -ForegroundColor Gray
    Write-Host "  下载线程: $($config.DownloadThreads)" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "✓ 配置完成!" -ForegroundColor Green
    
    # 测试连接
    Write-Host ""
    Write-Host "测试服务器连接..." -ForegroundColor Yellow
    try {
        $testUrl = "$ServerUrl/health"
        $response = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ 服务器连接成功!" -ForegroundColor Green
        } else {
            Write-Host "⚠ 无法连接到更新服务器 (状态码: $($response.StatusCode))" -ForegroundColor Yellow
            Write-Host "  请确保更新服务器正在运行" -ForegroundColor Gray
        }
    } catch {
        Write-Host "⚠ 无法连接到更新服务器" -ForegroundColor Yellow
        Write-Host "  请确保更新服务器正在运行" -ForegroundColor Gray
        Write-Host "  启动服务器: cd UpdateServer && go run main.go" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "✗ 保存配置失败: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
