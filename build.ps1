# LizardClient 一键构建脚本
# 用法: .\build.ps1 [选项]
# 选项: -Clean, -Restore, -Build, -Publish, -Release, -All

param(
    [switch]$Clean,    # 清理构建输出
    [switch]$Restore,  # 恢复 NuGet 包
    [switch]$Build,    # 构建项目
    [switch]$Publish,  # 发布项目
    [switch]$Release,  # 使用 Release 配置
    [switch]$All       # 执行所有步骤
)

# 颜色输出函数
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Success { Write-ColorOutput Green $args }
function Write-Info { Write-ColorOutput Cyan $args }
function Write-Warning { Write-ColorOutput Yellow $args }
function Write-Error { Write-ColorOutput Red $args }

# 项目根目录
$ProjectRoot = $PSScriptRoot
$SolutionFile = Join-Path $ProjectRoot "LizardClient.sln"

# 配置
$Configuration = if ($Release) { "Release" } else { "Debug" }
$OutputDir = Join-Path $ProjectRoot "build" $Configuration

Write-Info "========================================="
Write-Info "   LizardClient 构建脚本"
Write-Info "========================================="
Write-Info "配置: $Configuration"
Write-Info "输出目录: $OutputDir"
Write-Info ""

# 如果没有指定任何选项，默认执行 Build
if (-not ($Clean -or $Restore -or $Build -or $Publish -or $All)) {
    $Build = $true
}

# -All 选项启用所有步骤
if ($All) {
    $Clean = $true
    $Restore = $true
    $Build = $true
    $Publish = $true
}

# 步骤 1: 清理
if ($Clean) {
    Write-Info "[1/4] 清理构建输出..."
    
    try {
        # 清理 bin 和 obj 目录
        Get-ChildItem -Path $ProjectRoot -Include bin,obj -Recurse -Directory | 
            ForEach-Object { 
                Write-Host "  删除: $($_.FullName)"
                Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            }
        
        # 清理输出目录
        if (Test-Path $OutputDir) {
            Remove-Item $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        
        Write-Success "✓ 清理完成"
    }
    catch {
        Write-Error "✗ 清理失败: $_"
        exit 1
    }
    Write-Info ""
}

# 步骤 2: 恢复 NuGet 包
if ($Restore) {
    Write-Info "[2/4] 恢复 NuGet 包..."
    
    try {
        dotnet restore $SolutionFile --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore 失败" }
        Write-Success "✓ 恢复完成"
    }
    catch {
        Write-Error "✗ 恢复失败: $_"
        exit 1
    }
    Write-Info ""
}

# 步骤 3: 构建
if ($Build) {
    Write-Info "[3/4] 构建解决方案..."
    
    try {
        dotnet build $SolutionFile `
            --configuration $Configuration `
            --no-restore `
            --verbosity minimal
        
        if ($LASTEXITCODE -ne 0) { throw "dotnet build 失败" }
        Write-Success "✓ 构建完成"
        
        # 收集构建产物到 build/Debug 或 build/Release
        Write-Info "收集构建产物..."
        
        # 创建输出目录
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        
        # 复制所有项目的构建输出
        $projects = @(
            "LizardClient.Core",
            "LizardClient.Launcher", 
            "LizardClient.Injection",
            "LizardClient.ModSystem",
            "LizardClient.Game"
        )
        
        foreach ($project in $projects) {
            $projectName = $project
            $binPath = Join-Path $ProjectRoot "src\$projectName\bin\$Configuration"
            
            if (Test-Path $binPath) {
                $targetFrameworkDirs = Get-ChildItem $binPath -Directory
                foreach ($tfDir in $targetFrameworkDirs) {
                    $destPath = Join-Path $OutputDir $projectName
                    New-Item -ItemType Directory -Path $destPath -Force | Out-Null
                    
                    Copy-Item -Path "$($tfDir.FullName)\*" -Destination $destPath -Recurse -Force
                    Write-Host "  已复制: $projectName -> $destPath"
                }
            }
        }
        
        Write-Success "✓ 构建产物已收集到: $OutputDir"
    }
    catch {
        Write-Error "✗ 构建失败: $_"
        exit 1
    }
    Write-Info ""
}

# 步骤 4: 发布
if ($Publish) {
    Write-Info "[4/4] 发布启动器..."
    
    $LauncherProject = Join-Path $ProjectRoot "src\LizardClient.Launcher\LizardClient.Launcher.csproj"
    $PublishDir = Join-Path $OutputDir "publish"
    
    try {
        # 发布为 Windows 单文件应用
        dotnet publish $LauncherProject `
            --configuration $Configuration `
            --output $PublishDir `
            --runtime win-x64 `
            --self-contained false `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            --verbosity minimal
        
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败" }
        
        Write-Success "✓ 发布完成"
        Write-Info "发布目录: $PublishDir"
        
        # 列出发布的文件
        Write-Info "`n发布文件列表:"
        Get-ChildItem $PublishDir -File | ForEach-Object {
            $size = "{0:N2} MB" -f ($_.Length / 1MB)
            Write-Host "  - $($_.Name) ($size)"
        }
    }
    catch {
        Write-Error "✗ 发布失败: $_"
        exit 1
    }
    Write-Info ""
}

# 完成
Write-Success "========================================="
Write-Success "   构建成功完成！"
Write-Success "========================================="

if ($Publish) {
    Write-Info "`n运行启动器:"
    Write-Info "  cd $PublishDir"
    Write-Info "  .\LizardClient.Launcher.exe"
}
