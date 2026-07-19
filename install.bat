@echo off
chcp 65001 >nul
title AutoDial 安装程序

echo ========================================
echo   AutoDial 宽带自动拨号工具 - 安装程序
echo ========================================
echo.

:: 安装目录
set "INSTALL_DIR=%LOCALAPPDATA%\AutoDial"
set "EXE_PATH=%INSTALL_DIR%\AutoDial.exe"
set "ICO_PATH=%INSTALL_DIR%\AutoDial.ico"

echo [1/4] 创建安装目录...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
echo   目录: %INSTALL_DIR%

echo.
echo [2/4] 下载程序文件...
echo   正在下载 AutoDial.exe...
curl -L -o "%EXE_PATH%" "https://github.com/khssdsg-maker/AutoDial/releases/download/v2.5/AutoDial.exe"
if %errorlevel% neq 0 (
    echo   下载失败！请检查网络连接。
    pause
    exit /b 1
)

echo   正在下载 AutoDial.ico...
curl -L -o "%ICO_PATH%" "https://github.com/khssdsg-maker/AutoDial/releases/download/v2.5/AutoDial.ico"
if %errorlevel% neq 0 (
    echo   图标下载失败，但程序仍可使用。
)

echo   下载完成！

echo.
echo [3/4] 创建桌面快捷方式...
echo Set WshShell = CreateObject("WScript.Shell") > "%TEMP%\create_shortcut.vbs"
echo Set Shortcut = WshShell.CreateShortcut("%USERPROFILE%\Desktop\宽带拨号.lnk") >> "%TEMP%\create_shortcut.vbs"
echo Shortcut.TargetPath = "%EXE_PATH%" >> "%TEMP%\create_shortcut.vbs"
echo Shortcut.IconLocation = "%ICO_PATH%,0" >> "%TEMP%\create_shortcut.vbs"
echo Shortcut.WorkingDirectory = "%INSTALL_DIR%" >> "%TEMP%\create_shortcut.vbs"
echo Shortcut.Save >> "%TEMP%\create_shortcut.vbs"
cscript //nologo "%TEMP%\create_shortcut.vbs"
del "%TEMP%\create_shortcut.vbs"
echo   快捷方式已创建！

echo.
echo [4/4] 安装完成！
echo.
echo ========================================
echo   安装成功！
echo   程序位置: %INSTALL_DIR%
echo   桌面快捷方式: 宽带拨号.lnk
echo ========================================
echo.
echo 按任意键打开软件...
pause >nul
start "" "%EXE_PATH%"