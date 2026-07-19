using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class AutoDialGUI : Form
{
    static string appDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"AutoDial");
    static string configPath = Path.Combine(appDir, "accounts.txt");
    static string settingsPath = Path.Combine(appDir, "settings.txt");

    ListBox lstAccounts;
    TextBox txtName, txtUser, txtPass;
    Label lblStatus, lblOnlineTime, lblSpeed;
    Button btnDial, btnDisconnect, btnSave, btnAdd, btnDelete, btnImport, btnExport;
    Button btnCreateConnection, btnDeleteConnection;
    CheckBox chkAutoDial, chkSilent, chkAutoReconnect;
    ToolTip tip;
    NotifyIcon trayIcon;
    ContextMenuStrip trayMenu;
    List<string[]> accounts = new List<string[]>();
    Timer statusTimer;
    DateTime? connectTime;
    bool isConnected = false;
    bool isDialing = false;

    // Windows API for creating dial-up connections
    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint RasSetEntryProperties(
        string lpszPhonebook,
        string lpszEntry,
        IntPtr lprasentry,
        int cb,
        IntPtr lprasstats,
        IntPtr lpdwreserved);

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode)]
    static extern uint RasDeleteEntry(string lpszPhonebook, string lpszEntry);

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode)]
    static extern uint RasValidateEntryName(string lpszPhonebook, string lpszEntry);

    public AutoDialGUI()
    {
        Text = "宽带自动拨号";
        Size = new Size(500, 680);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei", 10);
        tip = new ToolTip { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 10000 };

        // Account list
        Label l0 = new Label { Text = "账号列表:", Location = new Point(20, 15), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };
        lstAccounts = new ListBox { Location = new Point(20, 45), Width = 430, Height = 90, Font = new Font("Microsoft YaHei", 11) };
        lstAccounts.SelectedIndexChanged += LstAccounts_SelectedIndexChanged;
        tip.SetToolTip(lstAccounts, "选择要操作的宽带账号");

        btnAdd = new Button { Text = "新增", Location = new Point(20, 145), Width = 70, Height = 28 };
        btnAdd.Click += BtnAdd_Click;
        tip.SetToolTip(btnAdd, "添加一个新的宽带账号");

        btnDelete = new Button { Text = "删除", Location = new Point(100, 145), Width = 70, Height = 28 };
        btnDelete.Click += BtnDelete_Click;
        tip.SetToolTip(btnDelete, "删除当前选中的账号");

        btnImport = new Button { Text = "导入", Location = new Point(180, 145), Width = 70, Height = 28 };
        btnImport.Click += BtnImport_Click;
        tip.SetToolTip(btnImport, "从文件导入账号配置");

        btnExport = new Button { Text = "导出", Location = new Point(260, 145), Width = 70, Height = 28 };
        btnExport.Click += BtnExport_Click;
        tip.SetToolTip(btnExport, "将账号配置导出到文件");

        // Windows connection management
        Label lConn = new Label { Text = "系统连接:", Location = new Point(340, 130), AutoSize = true, Font = new Font("Microsoft YaHei", 9) };

        btnCreateConnection = new Button { Text = "创建连接", Location = new Point(340, 145), Width = 110, Height = 28 };
        btnCreateConnection.Click += BtnCreateConnection_Click;
        tip.SetToolTip(btnCreateConnection, "在Windows中创建宽带拨号连接（新设备必点）");

        btnDeleteConnection = new Button { Text = "删除系统连接", Location = new Point(340, 178), Width = 110, Height = 28 };
        btnDeleteConnection.Click += BtnDeleteConnection_Click;
        tip.SetToolTip(btnDeleteConnection, "从Windows中删除宽带拨号连接");

        // Edit fields
        Label l1 = new Label { Text = "连接名称:", Location = new Point(20, 215), AutoSize = true };
        txtName = new TextBox { Location = new Point(110, 212), Width = 340, Font = new Font("Microsoft YaHei", 11) };
        tip.SetToolTip(txtName, "宽带连接名称，例如：宽带连接");

        Label l2 = new Label { Text = "用户名:", Location = new Point(20, 255), AutoSize = true };
        txtUser = new TextBox { Location = new Point(110, 252), Width = 340, Font = new Font("Microsoft YaHei", 11) };
        tip.SetToolTip(txtUser, "宽带拨号的用户名，由运营商提供");

        Label l3 = new Label { Text = "密码:", Location = new Point(20, 295), AutoSize = true };
        txtPass = new TextBox { Location = new Point(110, 292), Width = 340, Font = new Font("Microsoft YaHei", 11), UseSystemPasswordChar = true };
        tip.SetToolTip(txtPass, "宽带拨号的密码，由运营商提供");

        // Settings section
        Label l4 = new Label { Text = "设置:", Location = new Point(20, 335), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };

        chkAutoDial = new CheckBox { Text = "开机自动拨号", Location = new Point(20, 360), AutoSize = true };
        tip.SetToolTip(chkAutoDial, "开启后，每次开机登录Windows时自动执行拨号连接");

        chkSilent = new CheckBox { Text = "静默拨号", Location = new Point(200, 360), AutoSize = true };
        tip.SetToolTip(chkSilent, "开机拨号时不弹出界面窗口，后台自动连接");

        chkAutoReconnect = new CheckBox { Text = "断线自动重连", Location = new Point(340, 360), AutoSize = true };
        tip.SetToolTip(chkAutoReconnect, "检测到断线后自动重新拨号连接");

        // Status section
        Label l5 = new Label { Text = "连接状态:", Location = new Point(20, 400), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };

        lblStatus = new Label { Text = "未连接", Location = new Point(110, 402), AutoSize = true, ForeColor = Color.Gray };
        tip.SetToolTip(lblStatus, "当前网络连接状态");

        lblOnlineTime = new Label { Text = "在线时长: --:--:--", Location = new Point(20, 430), AutoSize = true };
        tip.SetToolTip(lblOnlineTime, "当前连接已持续的时间");

        lblSpeed = new Label { Text = "网络速度: -- Mbps", Location = new Point(250, 430), AutoSize = true };
        tip.SetToolTip(lblSpeed, "当前网络连接速度");

        // Action buttons
        btnDial = new Button { Text = "拨号连接", Location = new Point(20, 470), Width = 130, Height = 40, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnDial.Click += BtnDial_Click;
        tip.SetToolTip(btnDial, "使用当前选中的账号进行宽带拨号");

        btnDisconnect = new Button { Text = "断开连接", Location = new Point(160, 470), Width = 130, Height = 40 };
        btnDisconnect.Click += BtnDisconnect_Click;
        tip.SetToolTip(btnDisconnect, "断开当前的宽带连接");

        btnSave = new Button { Text = "保存设置", Location = new Point(310, 470), Width = 140, Height = 40, BackColor = Color.FromArgb(0, 153, 51), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnSave.Click += BtnSave_Click;
        tip.SetToolTip(btnSave, "保存所有配置和设置");

        Controls.AddRange(new Control[] {
            l0, lstAccounts, btnAdd, btnDelete, btnImport, btnExport,
            lConn, btnCreateConnection, btnDeleteConnection,
            l1, txtName, l2, txtUser, l3, txtPass,
            l4, chkAutoDial, chkSilent, chkAutoReconnect,
            l5, lblStatus, lblOnlineTime, lblSpeed,
            btnDial, btnDisconnect, btnSave
        });

        SetupTray();

        statusTimer = new Timer { Interval = 3000 };
        statusTimer.Tick += StatusTimer_Tick;
        statusTimer.Start();

        LoadAccounts();
        LoadSettings();
        Load += (s, e) => CheckStatus();
        FormClosing += FormClosing_Handler;
        Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
    }

    bool ConnectionExists(string name)
    {
        try
        {
            uint result = RasValidateEntryName(null, name);
            return result == 0; // ERROR_SUCCESS means entry exists
        }
        catch { return false; }
    }

    void CreatePPPoEConnection(string name, string user, string pass)
    {
        if (string.IsNullOrEmpty(name)) { MessageBox.Show("请输入连接名称", "提示"); return; }

        if (ConnectionExists(name))
        {
            MessageBox.Show("连接 \"" + name + "\" 已存在，无需重复创建", "提示");
            return;
        }

        try
        {
            // Create RASENTRY structure
            int entrySize = 1024; // Large enough buffer
            IntPtr entryPtr = Marshal.AllocHGlobal(entrySize);

            // Zero out the buffer
            byte[] buffer = new byte[entrySize];
            Marshal.Copy(buffer, 0, entryPtr, entrySize);

            // Set dwSize (offset 0, 4 bytes)
            Marshal.WriteInt32(entryPtr, 0, entrySize);

            // Set dwfOptions (offset 4, 4 bytes)
            // RASEO_IpHeaderCompression = 0x00010000
            // RASEO_RemoteDefaultGateway = 0x00000004
            Marshal.WriteInt32(entryPtr, 4, 0x00010004);

            // Set szEntryName (offset 76, 256 chars = 512 bytes for Unicode)
            byte[] nameBytes = System.Text.Encoding.Unicode.GetBytes(name + "\0");
            Marshal.Copy(nameBytes, 0, entryPtr + 76, Math.Min(nameBytes.Length, 512));

            // Set szDeviceType (offset 88 + 256*2 = 600, 16 chars = 32 bytes)
            byte[] devType = System.Text.Encoding.Unicode.GetBytes("PPPoE\0");
            Marshal.Copy(devType, 0, entryPtr + 600, Math.Min(devType.Length, 32));

            // Set szDeviceName (offset 632, 128 chars = 256 bytes)
            byte[] devName = System.Text.Encoding.Unicode.GetBytes("WAN Miniport (PPPOE)\0");
            Marshal.Copy(devName, 0, entryPtr + 632, Math.Min(devName.Length, 256));

            // Set szUserName (offset 888, 256 chars = 512 bytes)
            byte[] userBytes = System.Text.Encoding.Unicode.GetBytes(user + "\0");
            Marshal.Copy(userBytes, 0, entryPtr + 888, Math.Min(userBytes.Length, 512));

            // Set szPassword (offset 1400, 256 chars = 512 bytes)
            byte[] passBytes = System.Text.Encoding.Unicode.GetBytes(pass + "\0");
            Marshal.Copy(passBytes, 0, entryPtr + 1400, Math.Min(passBytes.Length, 512));

            // Set szDomain (offset 1912, 16 chars = 32 bytes)
            byte[] domain = System.Text.Encoding.Unicode.GetBytes("*\0");
            Marshal.Copy(domain, 0, entryPtr + 1912, Math.Min(domain.Length, 32));

            // Call RasSetEntryProperties
            uint result = RasSetEntryProperties(null, name, entryPtr, entrySize, IntPtr.Zero, IntPtr.Zero);

            Marshal.FreeHGlobal(entryPtr);

            if (result == 0)
            {
                MessageBox.Show("系统连接 \"" + name + "\" 创建成功！\n\n现在可以点击「拨号连接」了。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("创建失败，错误码: " + result + "\n\n请尝试在Windows设置中手动创建连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("创建连接时出错: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void DeletePPPoEConnection(string name)
    {
        if (string.IsNullOrEmpty(name)) { MessageBox.Show("请输入要删除的连接名称", "提示"); return; }

        if (!ConnectionExists(name))
        {
            MessageBox.Show("连接 \"" + name + "\" 不存在", "提示");
            return;
        }

        if (MessageBox.Show("确定要从系统中删除连接 \"" + name + "\" 吗？\n\n注意：这只会删除Windows中的拨号连接，不会删除软件中的账号配置。", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            try
            {
                uint result = RasDeleteEntry(null, name);
                if (result == 0)
                {
                    MessageBox.Show("系统连接 \"" + name + "\" 已删除", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("删除失败，错误码: " + result, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除时出错: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    void SetupTray()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("拨号连接", null, (s, e) => BtnDial_Click(s, e));
        trayMenu.Items.Add("断开连接", null, (s, e) => BtnDisconnect_Click(s, e));
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("退出", null, (s, e) => { Application.Exit(); });

        trayIcon = new NotifyIcon
        {
            Text = "宽带自动拨号 - 未连接",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        trayIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
    }

    Icon CreateTrayIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.FillEllipse(Brushes.Gray, 2, 2, 12, 12);
        g.DrawString("N", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 3, 1);
        g.Dispose();
        return Icon.FromHandle(bmp.GetHicon());
    }

    void UpdateTrayIcon(bool connected)
    {
        Bitmap bmp = new Bitmap(16, 16);
        Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        if (connected)
        {
            g.FillEllipse(Brushes.Green, 2, 2, 12, 12);
            g.DrawString("C", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 3, 1);
            trayIcon.Text = "宽带自动拨号 - 已连接";
        }
        else
        {
            g.FillEllipse(Brushes.Gray, 2, 2, 12, 12);
            g.DrawString("N", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 3, 1);
            trayIcon.Text = "宽带自动拨号 - 未连接";
        }
        g.Dispose();
        trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
    }

    void ShowTrayBalloon(string title, string message, ToolTipIcon icon)
    {
        trayIcon.ShowBalloonTip(3000, title, message, icon);
    }

    void FormClosing_Handler(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        statusTimer.Stop();
        statusTimer.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
    }

    string GetErrorDescription(int code)
    {
        switch (code)
        {
            case 600: return "端口已打开 - 端口已被其他程序占用";
            case 601: return "端口句柄无效 - 设备不存在或未安装";
            case 602: return "端口已打开 - 端口已被占用";
            case 603: return "端口太小 - 缓冲区不足";
            case 604: return "信息不正确 - 参数错误";
            case 605: return "无法设置端口信息 - 权限不足";
            case 606: return "端口未连接 - 设备未连接";
            case 607: return "事件无效 - 无效的事件类型";
            case 608: return "设备不存在 - 设备未找到";
            case 609: return "设备类型不存在 - 设备类型错误";
            case 610: return "设备缓冲区太小 - 缓冲区不足";
            case 611: return "线路未配置 - 线路未设置";
            case 612: return "线路正在使用 - 线路被占用";
            case 619: return "端口未连接 - 端口未建立连接";
            case 620: return "无法确定端点 - 无法识别目标";
            case 621: return "无法打开电话簿 - 配置文件错误";
            case 622: return "无法加载电话簿 - 配置加载失败";
            case 623: return "找不到电话簿条目 - 连接不存在";
            case 624: return "无法更新电话簿 - 写入配置失败";
            case 625: return "找不到电话簿字符串 - 配置项缺失";
            case 628: return "端口已断开 - 连接已关闭";
            case 629: return "端口被断开 - 连接被远程关闭";
            case 630: return "端口硬件已断开 - 网线已拔出";
            case 631: return "端口已中断 - 用户主动断开";
            case 632: return "结构体大小错误 - 程序内部错误";
            case 633: return "端口正在使用 - 端口被其他程序占用";
            case 634: return "无法验证地址 - 服务器不可达";
            case 635: return "需要未知错误 - 未知问题";
            case 645: return "适配器未连接 - 网卡未连接";
            case 646: return "身份验证失败 - 身份验证不通过";
            case 647: return "启用身份验证失败 - 认证配置错误";
            case 648: return "密码过期 - 密码已失效";
            case 649: return "未授权 - 账号无拨号权限";
            case 650: return "网络不可达 - 无法访问网络";
            case 651: return "调制解调器/设备报告错误 - 设备故障";
            case 652: return "未收到回复信号 - 服务器无响应";
            case 666: return "调制解调器未响应 - 设备无应答";
            case 676: return "线路忙 - 电话线被占用";
            case 678: return "无应答 - 服务器未响应";
            case 679: return "无法检测载波 - 物理连接失败";
            case 680: return "无拨号音 - 未检测到电话线信号";
            case 691: return "用户名或密码错误 - 认证失败";
            case 692: return "设备硬件故障 - 网卡或调制解调器故障";
            case 718: return "超时 - 连接超时无响应";
            case 720: return "无PPP协议 - 服务器不支持PPP";
            case 721: return "远程计算机无响应 - 服务器拒绝连接";
            case 732: return "协议不一致 - 协议协商失败";
            case 734: return "PPP链接控制协议终止 - 连接被中断";
            case 736: return "远程计算机中断连接 - 服务器主动断开";
            case 738: return "服务器无法接受请求 - 服务器负载过重";
            case 739: return "无法接受B密码 - 认证方式不支持";
            case 740: return "无效的拨号参数 - 参数配置错误";
            case 741: return "本地加密类型不支持 - 加密方式不兼容";
            case 742: return "远程加密类型不支持 - 服务器加密不兼容";
            case 743: return "远程计算机需要加密 - 服务器要求加密";
            case 752: return "脚本语法错误 - 脚本配置错误";
            case 753: return "脚本无法中断 - 脚本执行异常";
            case 754: return "脚本执行失败 - 脚本运行出错";
            case 755: return "无法自动拨号 - 自动连接失败";
            case 756: return "已自动拨号 - 正在自动重连中";
            case 757: return "无法打开共享访问 - 共享配置错误";
            case 758: return "共享访问已启用 - 共享功能冲突";
            case 760: return "无法启用路由 - 路由配置错误";
            case 761: return "共享访问启用时无法启用路由 - 功能冲突";
            case 763: return "无法启用共享访问 + 路由 - 配置冲突";
            case 764: return "未安装共享访问 - 共享组件缺失";
            case 765: return "无法设置Internet共享 - 共享配置失败";
            case 766: return "找不到证书 - 证书缺失或损坏";
            case 767: return "共享访问失败 - 共享功能异常";
            case 768: return "身份验证时出现意外错误 - 认证过程异常";
            case 769: return "目标无法到达 - 网络不通";
            case 770: return "远程计算机拒绝拨入 - 服务器拒绝访问";
            case 771: return "尝试连接失败 - 连接建立失败";
            case 772: return "远程计算机的网络硬件不兼容 - 设备不支持";
            case 773: return "远程计算机的连接已断开 - 连接被中断";
            case 774: return "临时故障 - 网络暂时不可用";
            case 775: return "调制解调器(或其他连接设备)的呼叫被拒绝 - 被服务器拒绝";
            case 776: return "调制解调器(或其他连接设备)无法拨出 - 无法发起连接";
            case 777: return "调制解调器(或其他连接设备)报告的故障 - 设备异常";
            case 778: return "无法验证服务器的身份 - 证书验证失败";
            default: return "未知错误 (代码 " + code + ")";
        }
    }

    void StatusTimer_Tick(object sender, EventArgs e)
    {
        CheckStatus();
        UpdateOnlineTime();
    }

    void CheckStatus()
    {
        try
        {
            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = "rasdial.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            bool wasConnected = isConnected;
            isConnected = output.Contains("已连接") || output.Contains("connected");

            if (isConnected)
            {
                lblStatus.Text = "已连接";
                lblStatus.ForeColor = Color.Green;
                if (!wasConnected && connectTime == null)
                {
                    connectTime = DateTime.Now;
                    if (WindowState == FormWindowState.Minimized)
                        ShowTrayBalloon("连接成功", "宽带已成功连接", ToolTipIcon.Info);
                }
                UpdateSpeed();
            }
            else
            {
                lblStatus.Text = "未连接";
                lblStatus.ForeColor = Color.Gray;
                connectTime = null;
                lblSpeed.Text = "网络速度: -- Mbps";

                if (wasConnected && chkAutoReconnect.Checked && !isDialing)
                {
                    AutoReconnect();
                }
            }

            UpdateTrayIcon(isConnected);
        }
        catch
        {
            lblStatus.Text = "检测失败";
            lblStatus.ForeColor = Color.Red;
        }
    }

    void AutoReconnect()
    {
        if (accounts.Count == 0) return;

        int idx = lstAccounts.SelectedIndex >= 0 ? lstAccounts.SelectedIndex : 0;
        if (idx >= accounts.Count) idx = 0;

        string name = accounts[idx][0];
        string user = accounts[idx][1];
        string pass = accounts[idx][2];

        lblStatus.Text = "自动重连中...";
        lblStatus.ForeColor = Color.Orange;
        Application.DoEvents();

        try
        {
            string args = "\"" + name + "\"";
            if (!string.IsNullOrEmpty(user)) args += " \"" + user + "\" \"" + pass + "\"";

            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = "rasdial.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                connectTime = DateTime.Now;
                lblStatus.Text = "已连接 - " + name;
                lblStatus.ForeColor = Color.Green;
                if (WindowState == FormWindowState.Minimized)
                    ShowTrayBalloon("重连成功", "宽带已自动重新连接", ToolTipIcon.Info);
            }
            else
            {
                lblStatus.Text = "重连失败 - " + GetErrorDescription(proc.ExitCode);
                lblStatus.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "重连错误 - " + ex.Message;
            lblStatus.ForeColor = Color.Red;
        }
    }

    void UpdateOnlineTime()
    {
        if (connectTime.HasValue && isConnected)
        {
            TimeSpan ts = DateTime.Now - connectTime.Value;
            lblOnlineTime.Text = "在线时长: " + ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
        }
        else
        {
            lblOnlineTime.Text = "在线时长: --:--:--";
        }
    }

    void UpdateSpeed()
    {
        try
        {
            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            foreach (string line in output.Split('\n'))
            {
                if (line.Contains("接收速率") || line.Contains("Receive rate"))
                {
                    string speed = line.Split(new char[] { ':' }, 2)[1].Trim();
                    lblSpeed.Text = "网络速度: " + speed;
                    return;
                }
            }
            lblSpeed.Text = "网络速度: -- Mbps";
        }
        catch
        {
            lblSpeed.Text = "网络速度: -- Mbps";
        }
    }

    void LoadAccounts()
    {
        accounts.Clear();
        lstAccounts.Items.Clear();
        if (File.Exists(configPath))
        {
            foreach (string line in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrEmpty(line)) continue;
                string[] p = line.Split(new char[] { '|' }, 3);
                if (p.Length >= 1 && !string.IsNullOrEmpty(p[0]))
                {
                    accounts.Add(new string[] { p[0], p.Length >= 2 ? p[1] : "", p.Length >= 3 ? p[2] : "" });
                    lstAccounts.Items.Add(p[0] + "  (" + (p.Length >= 2 ? p[1] : "") + ")");
                }
            }
        }
        if (lstAccounts.Items.Count > 0) lstAccounts.SelectedIndex = 0;
    }

    void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            foreach (string line in File.ReadAllLines(settingsPath))
            {
                if (line.StartsWith("AutoDial=")) chkAutoDial.Checked = line.Substring(9) == "1";
                if (line.StartsWith("Silent=")) chkSilent.Checked = line.Substring(7) == "1";
                if (line.StartsWith("AutoReconnect=")) chkAutoReconnect.Checked = line.Substring(14) == "1";
            }
        }
    }

    void LstAccounts_SelectedIndexChanged(object s, EventArgs e)
    {
        int i = lstAccounts.SelectedIndex;
        if (i >= 0 && i < accounts.Count)
        {
            txtName.Text = accounts[i][0];
            txtUser.Text = accounts[i][1];
            txtPass.Text = accounts[i][2];
        }
    }

    void BtnAdd_Click(object s, EventArgs e)
    {
        txtName.Text = ""; txtUser.Text = ""; txtPass.Text = "";
        txtName.Focus();
        lstAccounts.SelectedIndex = -1;
        lblStatus.Text = "输入新账号后点保存";
        lblStatus.ForeColor = Color.Orange;
    }

    void BtnDelete_Click(object s, EventArgs e)
    {
        int i = lstAccounts.SelectedIndex;
        if (i < 0) { MessageBox.Show("请先选择要删除的账号", "提示"); return; }
        if (MessageBox.Show("确定删除 \"" + accounts[i][0] + "\" ?", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            accounts.RemoveAt(i); SaveAccounts(); LoadAccounts();
            if (lstAccounts.Items.Count > 0) lstAccounts.SelectedIndex = 0;
            else { txtName.Text = ""; txtUser.Text = ""; txtPass.Text = ""; }
        }
    }

    void BtnCreateConnection_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text))
        {
            MessageBox.Show("请先填写连接名称、用户名和密码", "提示");
            return;
        }
        CreatePPPoEConnection(txtName.Text, txtUser.Text, txtPass.Text);
    }

    void BtnDeleteConnection_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text))
        {
            MessageBox.Show("请输入要删除的连接名称", "提示");
            return;
        }
        DeletePPPoEConnection(txtName.Text);
    }

    void BtnImport_Click(object s, EventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog
        {
            Title = "导入账号配置",
            Filter = "配置文件 (*.txt;*.adial)|*.txt;*.adial|所有文件 (*.*)|*.*"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string[] lines = File.ReadAllLines(ofd.FileName);
                int count = 0;
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    string[] p = line.Split(new char[] { '|' }, 3);
                    if (p.Length >= 1 && !string.IsNullOrEmpty(p[0]))
                    {
                        bool exists = false;
                        foreach (string[] acc in accounts)
                            if (acc[0] == p[0]) { exists = true; break; }
                        if (!exists)
                        {
                            accounts.Add(new string[] { p[0], p.Length >= 2 ? p[1] : "", p.Length >= 3 ? p[2] : "" });
                            count++;
                        }
                    }
                }
                SaveAccounts(); LoadAccounts();
                MessageBox.Show("导入成功！新增 " + count + " 个账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    void BtnExport_Click(object s, EventArgs e)
    {
        if (accounts.Count == 0) { MessageBox.Show("没有账号可以导出", "提示"); return; }

        SaveFileDialog sfd = new SaveFileDialog
        {
            Title = "导出账号配置",
            Filter = "配置文件 (*.adial)|*.adial|文本文件 (*.txt)|*.txt",
            FileName = "AutoDial_accounts_" + DateTime.Now.ToString("yyyyMMdd") + ".adial"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (string[] acc in accounts)
                    lines.Add(acc[0] + "|" + acc[1] + "|" + acc[2]);
                File.WriteAllLines(sfd.FileName, lines.ToArray());
                MessageBox.Show("导出成功！共 " + accounts.Count + " 个账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    void BtnSave_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请输入连接名称", "提示"); return; }
        int i = lstAccounts.SelectedIndex;
        if (i >= 0 && i < accounts.Count)
            accounts[i] = new string[] { txtName.Text, txtUser.Text, txtPass.Text };
        else
            accounts.Add(new string[] { txtName.Text, txtUser.Text, txtPass.Text });
        SaveAccounts(); SaveSettings(); LoadAccounts();
        lblStatus.Text = "配置已保存"; lblStatus.ForeColor = Color.Green;
        MessageBox.Show("设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void SaveAccounts()
    {
        if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
        List<string> lines = new List<string>();
        foreach (string[] a in accounts) lines.Add(a[0] + "|" + a[1] + "|" + a[2]);
        File.WriteAllLines(configPath, lines.ToArray());
    }

    void SaveSettings()
    {
        if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
        string content = "AutoDial=" + (chkAutoDial.Checked ? "1" : "0") + "\n";
        content += "Silent=" + (chkSilent.Checked ? "1" : "0") + "\n";
        content += "AutoReconnect=" + (chkAutoReconnect.Checked ? "1" : "0");
        File.WriteAllText(settingsPath, content);

        string startupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs\Startup");
        string destExe = Path.Combine(startupPath, "AutoDial.exe");
        string currentExe = Assembly.GetExecutingAssembly().Location;

        if (chkAutoDial.Checked)
        {
            if (!File.Exists(destExe)) File.Copy(currentExe, destExe, true);
        }
        else
        {
            if (File.Exists(destExe)) File.Delete(destExe);
        }
    }

    void BtnDial_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请选择或输入账号", "提示"); return; }
        isDialing = true;
        btnDial.Enabled = false;
        lblStatus.Text = "连接中...";
        lblStatus.ForeColor = Color.Orange;
        Application.DoEvents();

        try
        {
            string args = "\"" + txtName.Text + "\"";
            if (!string.IsNullOrEmpty(txtUser.Text))
                args += " \"" + txtUser.Text + "\" \"" + txtPass.Text + "\"";

            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = "rasdial.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                connectTime = DateTime.Now;
                isConnected = true;
                lblStatus.Text = "已连接 - " + txtName.Text;
                lblStatus.ForeColor = Color.Green;
                UpdateTrayIcon(true);
                if (WindowState == FormWindowState.Minimized)
                    ShowTrayBalloon("连接成功", "宽带已成功连接", ToolTipIcon.Info);
            }
            else
            {
                lblStatus.Text = "连接失败 - " + GetErrorDescription(proc.ExitCode);
                lblStatus.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "错误 - " + ex.Message;
            lblStatus.ForeColor = Color.Red;
        }
        btnDial.Enabled = true;
        isDialing = false;
    }

    void BtnDisconnect_Click(object s, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "rasdial.exe",
                Arguments = "\"" + txtName.Text + "\" /disconnect",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            isConnected = false;
            connectTime = null;
            lblStatus.Text = "已断开";
            lblStatus.ForeColor = Color.Gray;
            lblOnlineTime.Text = "在线时长: --:--:--";
            lblSpeed.Text = "网络速度: -- Mbps";
            UpdateTrayIcon(false);
        }
        catch { }
    }

    static void Main()
    {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Length > 1 && args[1] == "silent")
        {
            if (File.Exists(settingsPath))
            {
                bool silent = false;
                foreach (string l in File.ReadAllLines(settingsPath))
                    if (l.StartsWith("Silent=")) silent = l.Substring(7) == "1";

                if (!silent)
                {
                    Application.EnableVisualStyles();
                    Application.Run(new AutoDialGUI());
                    return;
                }
            }

            if (File.Exists(configPath))
            {
                string[] lines = File.ReadAllLines(configPath);
                if (lines.Length > 0)
                {
                    string[] p = lines[0].Split(new char[] { '|' }, 3);
                    string a = "\"" + p[0] + "\"";
                    if (p.Length >= 2 && !string.IsNullOrEmpty(p[1]))
                        a += " \"" + p[1] + "\" \"" + p[2] + "\"";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "rasdial.exe",
                        Arguments = a,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            return;
        }

        if (args.Length > 1 && args[1] == "install")
        {
            string startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs\Startup");
            string destExe = Path.Combine(startupPath, "AutoDial.exe");
            string currentExe = Assembly.GetExecutingAssembly().Location;
            if (currentExe != destExe) File.Copy(currentExe, destExe, true);

            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            File.WriteAllText(settingsPath, "AutoDial=1\nSilent=0\nAutoReconnect=1");
            MessageBox.Show("安装完成！开机将自动拨号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.Run(new AutoDialGUI());
    }
}