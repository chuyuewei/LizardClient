@echo off
REM LizardClient 更新服务器快速启动脚本

echo ============================================
echo    LizardClient Update Server Launcher
echo ============================================
echo.

REM 检查Go是否安装
where go >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Go未安装或不在PATH中
    echo 请从 https://golang.org/dl/ 下载安装Go
    pause
    exit /b 1
)

echo [INFO] Go版本:
go version
echo.

REM 切换到UpdateServer目录
cd /d "%~dp0UpdateServer"

echo [INFO] 启动更新服务器...
echo [INFO] 服务器地址: http://localhost:51000
echo.
echo 按 Ctrl+C 停止服务器
echo ============================================
echo.

go run main.go

pause
