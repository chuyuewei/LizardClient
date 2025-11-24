@echo off
REM LizardClient 快速构建脚本
REM 此脚本调用 PowerShell 构建脚本

echo ========================================
echo    LizardClient 快速构建
echo ========================================
echo.

REM 检查 PowerShell 是否可用
where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo 使用 PowerShell 7+
    pwsh -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
) else (
    echo 使用 Windows PowerShell
    powershell -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo 构建失败！错误代码: %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo 构建成功完成！
pause
