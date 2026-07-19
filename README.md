# AutoDial - 宽带自动拨号工具

<p align="center">
  <img src="AutoDial.ico" width="128" height="128">
</p>

<p align="center">
  <a href="https://khssdsg-maker.github.io/AutoDial/">官方网站</a> |
  <a href="https://github.com/khssdsg-maker/AutoDial/releases">下载</a> |
  <a href="#安装方法">安装方法</a> |
  <a href="#更新日志">更新日志</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-2.5-blue" alt="Version">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-brightgreen" alt="Platform">
  <img src="https://img.shields.io/badge/license-MIT-orange" alt="License">
</p>

现代化 WPF 界面的 Windows 宽带自动拨号工具，支持多账号管理、开机自动拨号、静默拨号、断线自动重连等功能。

## 功能特性

### 核心功能
- **多账号管理** - 支持添加、删除、切换多个宽带账号
- **一键拨号** - 选择账号后一键连接宽带
- **创建系统连接** - 直接在Windows中创建PPPoE拨号连接，无需手动设置
- **删除系统连接** - 从Windows中删除不需要的拨号连接
- **开机自动拨号** - 开机登录后自动执行拨号
- **静默拨号** - 后台自动拨号，不弹出界面窗口
- **断线自动重连** - 检测到断线后自动重新拨号连接

### 界面功能
- **WPF现代化界面** - 卡片式布局，圆角阴影效果
- **软件图标** - 紫蓝渐变设计，专业软件风格
- **密码显示/隐藏** - 点击眼睛按钮切换密码可见性
- **连接状态监控** - 实时显示连接状态、在线时长
- **系统托盘** - 最小化到系统托盘，不占任务栏
- **托盘通知** - 拨号成功/失败自动弹出通知
- **窗口位置记忆** - 下次打开在相同位置

### 辅助功能
- **错误码解释** - 显示中文错误说明，而非错误代码
- **导入/导出配置** - 支持备份和迁移账号设置
- **自动更新检测** - 启动时检查GitHub新版本
- **更新日志** - 点击标题栏按钮查看历史版本
- **连接账户联动** - 创建/删除系统连接时自动同步账户列表

## 安装方法

### 方法一：PowerShell一键安装（推荐）

以管理员身份运行 PowerShell，执行：

```powershell
irm https://raw.githubusercontent.com/khssdsg-maker/AutoDial/main/install.ps1 | iex
```

### 方法二：CMD一键安装

以管理员身份运行 CMD，执行：

```cmd
curl -o %TEMP%\install.bat https://raw.githubusercontent.com/khssdsg-maker/AutoDial/main/install.bat && %TEMP%\install.bat
```

### 方法三：Winget安装（Windows 11）

```powershell
winget install khssdsg-maker.AutoDial
```

### 方法四：手动下载安装

1. 下载 [AutoDial.exe](https://github.com/khssdsg-maker/AutoDial/releases/download/v2.5/AutoDial.exe) 和 [AutoDial.ico](https://github.com/khssdsg-maker/AutoDial/releases/download/v2.5/AutoDial.ico)
2. 将两个文件放在同一目录
3. 双击 `AutoDial.exe` 运行

### 方法五：开机自启动

1. 运行软件
2. 勾选「开机自动拨号」
3. 点击「保存设置」

## 使用方法

### 新设备首次使用（推荐）
1. 运行 `AutoDial.exe`
2. 输入连接名称、用户名、密码
3. 点击「创建系统连接」（自动创建Windows连接并添加到账户列表）
4. 点击「拨号连接」即可上网

### 直接使用（已有Windows连接）
1. 运行 `AutoDial.exe`
2. 输入或选择已有账号
3. 点击「拨号连接」

### 设置开机自动拨号
1. 勾选「开机自动拨号」
2. 点击「保存设置」

### 设置静默拨号
1. 先勾选「开机自动拨号」
2. 再勾选「静默拨号」
3. 点击「保存设置」

### 密码管理
1. 输入密码后，点击右侧眼睛按钮可显示密码
2. 再次点击可隐藏密码

### 导入/导出配置
1. 点击「导出」保存账号到文件
2. 在新电脑上点击「导入」选择文件

## 系统要求

- Windows 10 / 11
- .NET Framework 4.0（Windows 默认自带）

## 编译方法

```batch
C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:AutoDial.exe /r:PresentationCore.dll /r:PresentationFramework.dll /r:WindowsBase.dll /r:System.Xaml.dll /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll App.xaml.cs MainWindow.xaml.cs MainWindow.g.cs
```

## 文件说明

### 程序文件
- `AutoDial.exe` - 主程序
- `AutoDial.ico` - 软件图标

### 安装脚本
- `install.ps1` - PowerShell一键安装脚本
- `install.bat` - CMD一键安装脚本

### 网站文件
- `index.html` - 官方网站页面

### 源代码文件
- `App.xaml.cs` - 应用程序入口
- `MainWindow.xaml.cs` - 主窗口逻辑
- `MainWindow.g.cs` - 主窗口界面定义

### 配置文件（运行时生成）
- `%APPDATA%\AutoDial\accounts.txt` - 账号配置
- `%APPDATA%\AutoDial\settings.txt` - 设置文件
- `%APPDATA%\AutoDial\window.txt` - 窗口位置记录

## 更新日志

### v2.5 (2025-07-19)
- 添加软件图标（紫蓝渐变设计）
- 标题栏显示更新日志按钮
- 添加官方网站和一键安装脚本

### v2.4 (2025-07-19)
- 新增更新日志窗口
- 点击标题栏按钮查看历史版本

### v2.3 (2025-07-19)
- 新增密码显示/隐藏切换按钮
- 新增鼠标悬停提示

### v2.2 (2025-07-19)
- 新增托盘气泡通知（拨号成功/失败/断开）
- 新增窗口位置记忆功能
- 新增自动更新检测（从GitHub检查新版本）
- 优化：创建系统连接后自动添加到账户列表
- 优化：删除系统连接时自动删除对应账户

### v2.1 (2025-07-19)
- 修复界面布局问题，添加滚动支持

### v2.0 (2025-07-19)
- 升级为WPF现代化界面
- 卡片式布局，圆角阴影效果

### v1.8 (2025-07-19)
- 异步拨号，解决界面卡顿问题

### v1.7 (2025-07-19)
- 重写创建连接功能，直接写入电话簿文件

### v1.4 (2025-07-01)
- 新增创建/删除系统连接功能

### v1.3 (2025-07-01)
- 新增系统托盘功能
- 新增错误码中文解释
- 新增导入/导出配置功能

### v1.2 (2025-07-01)
- 新增断线自动重连功能
- 新增连接状态实时监控

### v1.1 (2025-07-01)
- 新增多账号管理功能

### v1.0 (2025-06-14)
- 初始版本发布

## 常见问题

### Q: 软件无法运行？
A: 确保已安装 .NET Framework 4.0（Windows 10/11 默认自带）

### Q: 拨号失败？
A: 检查连接名称、用户名、密码是否正确，或查看错误提示

### Q: 如何取消开机自启动？
A: 在软件中取消勾选「开机自动拨号」→ 保存设置

### Q: 如何备份配置？
A: 点击「导出」保存账号文件，在新电脑点击「导入」恢复

### Q: 图标不显示？
A: 确保 `AutoDial.ico` 与 `AutoDial.exe` 在同一目录

## 许可证

MIT License

## 作者

khssdsg-maker