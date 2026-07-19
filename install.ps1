# AutoDial 一键安装脚本
# 以管理员身份运行 PowerShell 执行此脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AutoDial 宽带自动拨号工具 - 安装程序" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 安装目录
$installDir = "$env:LOCALAPPDATA\AutoDial"
$exePath = "$installDir\AutoDial.exe"
$icoPath = "$installDir\AutoDial.ico"
$shortcutPath = "$env:USERPROFILE\Desktop\宽带拨号.lnk"

# 创建安装目录
Write-Host "[1/4] 创建安装目录..." -ForegroundColor Yellow
if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
}
Write-Host "  目录: $installDir" -ForegroundColor Green

# 下载文件
Write-Host "[2/4] 下载程序文件..." -ForegroundColor Yellow
try {
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri "https://github.com/khssdsg-maker/AutoDial/releases/download/v2.5/AutoDial.exe" -OutFile $exePath
    Invoke-WebRequest -Uri "https://github.com/khssdsg-maker/AutoDial/releases/download/v2.5/AutoDial.ico" -OutFile $icoPath
    Write-Host "  下载完成!" -ForegroundColor Green
} catch {
    Write-Host "  下载失败: $_" -ForegroundColor Red
    exit 1
}

# 创建桌面快捷方式
Write-Host "[3/4] 创建桌面快捷方式..." -ForegroundColor Yellow
$ws = New-Object -ComObject WScript.Shell
$shortcut = $ws.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.IconLocation = "$icoPath,0"
$shortcut.WorkingDirectory = $installDir
$shortcut.Save()
Write-Host "  快捷方式: $shortcutPath" -ForegroundColor Green

# 完成
Write-Host "[4/4] 安装完成!" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  安装成功！" -ForegroundColor Green
Write-Host "  程序位置: $installDir" -ForegroundColor White
Write-Host "  桌面快捷方式: 宽带拨号.lnk" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "按任意键打开软件..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Start-Process $exePath