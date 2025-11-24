@echo off
REM LizardClient Release 完整构建和发布脚本
REM 执行完整的清理、构建和发布流程（Release 配置）

echo ========================================
echo    LizardClient Release 构建
echo ========================================
echo.
echo 此脚本将执行以下操作:
echo   1. 清理所有构建输出
echo   2. 恢复 NuGet 包
echo   3. 构建 Release 版本
echo   4. 发布单文件启动器
echo.

pause

where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    pwsh -ExecutionPolicy Bypass -File "%~dp0build.ps1" -All -Release
) else (
    powershell -ExecutionPolicy Bypass -File "%~dp0build.ps1" -All -Release
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Release 构建失败！
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Release 构建完成！
echo 输出位置: build\Release\publish\
echo ========================================
pause
