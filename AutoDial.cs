using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

class AutoDialGUI : Form
{
    static string appDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"AutoDial");
    static string configPath = Path.Combine(appDir, "accounts.txt");
    static string settingsPath = Path.Combine(appDir, "settings.txt");
    static string phonebookPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Microsoft\Network\Connections\Pbk\rasphone.pbk");

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
    System.Windows.Forms.Timer statusTimer;
    DateTime? connectTime;
    bool isConnected = false;
    bool isDialing = false;

    public AutoDialGUI()
    {
        Text = "宽带自动拨号";
        Size = new Size(500, 580);
        ClientSize = new Size(480, 550);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei", 10);
        tip = new ToolTip { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 10000 };

        Label l0 = new Label { Text = "账号列表:", Location = new Point(20, 10), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };
        lstAccounts = new ListBox { Location = new Point(20, 35), Width = 440, Height = 80, Font = new Font("Microsoft YaHei", 11) };
        lstAccounts.SelectedIndexChanged += LstAccounts_SelectedIndexChanged;
        tip.SetToolTip(lstAccounts, "选择要操作的宽带账号");

        btnAdd = new Button { Text = "新增", Location = new Point(20, 120), Width = 70, Height = 28 };
        btnAdd.Click += BtnAdd_Click;

        btnDelete = new Button { Text = "删除", Location = new Point(100, 120), Width = 70, Height = 28 };
        btnDelete.Click += BtnDelete_Click;

        btnImport = new Button { Text = "导入", Location = new Point(180, 120), Width = 70, Height = 28 };
        btnImport.Click += BtnImport_Click;

        btnExport = new Button { Text = "导出", Location = new Point(260, 120), Width = 70, Height = 28 };
        btnExport.Click += BtnExport_Click;

        btnCreateConnection = new Button { Text = "创建系统连接", Location = new Point(340, 120), Width = 120, Height = 28, BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnCreateConnection.Click += BtnCreateConnection_Click;
        tip.SetToolTip(btnCreateConnection, "在Windows中创建PPPoE拨号连接（新设备必点）");

        btnDeleteConnection = new Button { Text = "删除系统连接", Location = new Point(340, 153), Width = 120, Height = 28 };
        btnDeleteConnection.Click += BtnDeleteConnection_Click;

        Label l1 = new Label { Text = "连接名称:", Location = new Point(20, 195), AutoSize = true };
        txtName = new TextBox { Location = new Point(110, 192), Width = 350, Font = new Font("Microsoft YaHei", 11) };

        Label l2 = new Label { Text = "用户名:", Location = new Point(20, 230), AutoSize = true };
        txtUser = new TextBox { Location = new Point(110, 227), Width = 350, Font = new Font("Microsoft YaHei", 11) };

        Label l3 = new Label { Text = "密码:", Location = new Point(20, 265), AutoSize = true };
        txtPass = new TextBox { Location = new Point(110, 262), Width = 350, Font = new Font("Microsoft YaHei", 11), UseSystemPasswordChar = true };

        Label l4 = new Label { Text = "设置:", Location = new Point(20, 305), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };

        chkAutoDial = new CheckBox { Text = "开机自动拨号", Location = new Point(20, 330), AutoSize = true };
        chkSilent = new CheckBox { Text = "静默拨号", Location = new Point(180, 330), AutoSize = true };
        chkAutoReconnect = new CheckBox { Text = "断线自动重连", Location = new Point(310, 330), AutoSize = true };

        Label l5 = new Label { Text = "连接状态:", Location = new Point(20, 365), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };

        lblStatus = new Label { Text = "未连接", Location = new Point(110, 367), AutoSize = true, ForeColor = Color.Gray };
        lblOnlineTime = new Label { Text = "在线时长: --:--:--", Location = new Point(20, 395), AutoSize = true };
        lblSpeed = new Label { Text = "网络速度: -- Mbps", Location = new Point(250, 395), AutoSize = true };

        btnDial = new Button { Text = "拨号连接", Location = new Point(20, 430), Width = 140, Height = 45, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };
        btnDial.Click += BtnDial_Click;

        btnDisconnect = new Button { Text = "断开连接", Location = new Point(170, 430), Width = 140, Height = 45 };
        btnDisconnect.Click += BtnDisconnect_Click;

        btnSave = new Button { Text = "保存设置", Location = new Point(320, 430), Width = 140, Height = 45, BackColor = Color.FromArgb(0, 153, 51), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };
        btnSave.Click += BtnSave_Click;

        Controls.AddRange(new Control[] {
            l0, lstAccounts, btnAdd, btnDelete, btnImport, btnExport,
            btnCreateConnection, btnDeleteConnection,
            l1, txtName, l2, txtUser, l3, txtPass,
            l4, chkAutoDial, chkSilent, chkAutoReconnect,
            l5, lblStatus, lblOnlineTime, lblSpeed,
            btnDial, btnDisconnect, btnSave
        });

        SetupTray();

        statusTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        statusTimer.Tick += StatusTimer_Tick;
        statusTimer.Start();

        LoadAccounts();
        LoadSettings();
        Load += (s, e) => CheckStatus();
        FormClosing += FormClosing_Handler;
        Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
    }

    bool ConnectionExistsInWindows(string name)
    {
        try
        {
            if (!File.Exists(phonebookPath)) return false;
            string content = File.ReadAllText(phonebookPath);
            return content.Contains("[" + name + "]");
        }
        catch { return false; }
    }

    void CreatePPPoEConnection(string name, string user, string pass)
    {
        if (string.IsNullOrEmpty(name)) { MessageBox.Show("请输入连接名称", "提示"); return; }

        if (ConnectionExistsInWindows(name))
        {
            MessageBox.Show("连接 \"" + name + "\" 已存在。\n\n如需修改用户名密码，请先删除旧连接再重新创建。", "提示");
            return;
        }

        try
        {
            // Create PPPoE connection entry in rasphone.pbk
            string entry = "\n[" + name + "]\n" +
                "Encoding=1\n" +
                "PBVersion=8\n" +
                "Type=5\n" +
                "AutoLogon=0\n" +
                "UseRasCredentials=1\n" +
                "DialParamsUID=" + new Random().Next(10000000, 99999999) + "\n" +
                "Guid={" + Guid.NewGuid().ToString("D").ToUpper() + "}\n" +
                "VpnStrategy=0\n" +
                "ExcludedProtocols=0\n" +
                "LcpExtensions=1\n" +
                "DataEncryption=8\n" +
                "SwCompression=0\n" +
                "NegotiateMultilinkAlways=0\n" +
                "SkipDoubleDialDialog=0\n" +
                "DialMode=0\n" +
                "OverridePref=15\n" +
                "RedialAttempts=3\n" +
                "RedialSeconds=60\n" +
                "IdleDisconnectSeconds=0\n" +
                "RedialOnLinkFailure=1\n" +
                "CallbackMode=0\n" +
                "CustomDialDll=\n" +
                "CustomDialFunc=\n" +
                "CustomRasDialDll=\n" +
                "ForceSecureCompartment=0\n" +
                "DisableIKENameEkuCheck=0\n" +
                "AuthenticateServer=0\n" +
                "ShareMsFilePrint=0\n" +
                "BindMsNetClient=0\n" +
                "SharedPhoneNumbers=0\n" +
                "GlobalDeviceSettings=0\n" +
                "PrerequisiteEntry=\n" +
                "PrerequisitePbk=\n" +
                "PreferredPort=PPPoE5-0\n" +
                "PreferredDevice=WAN Miniport (PPPOE)\n" +
                "PreferredBps=0\n" +
                "PreferredHwFlow=0\n" +
                "PreferredProtocol=0\n" +
                "PreferredCompression=0\n" +
                "PreferredSpeaker=0\n" +
                "PreferredMdmProtocol=0\n" +
                "PreviewUserPw=1\n" +
                "PreviewDomain=0\n" +
                "PreviewPhoneNumber=0\n" +
                "ShowDialingProgress=1\n" +
                "ShowMonitorIconInTaskBar=1\n" +
                "CustomAuthKey=0\n" +
                "AuthRestrictions=552\n" +
                "IpPrioritizeRemote=1\n" +
                "IpInterfaceMetric=0\n" +
                "IpHeaderCompression=0\n" +
                "IpAddress=0.0.0.0\n" +
                "IpDnsAddress=0.0.0.0\n" +
                "IpDns2Address=0.0.0.0\n" +
                "IpWinsAddress=0.0.0.0\n" +
                "IpWins2Address=0.0.0.0\n" +
                "IpAssign=1\n" +
                "IpNameAssign=1\n" +
                "IpDnsFlags=0\n" +
                "IpNBTFlags=0\n" +
                "TcpWindowSize=0\n" +
                "UseFlags=3\n" +
                "IpSecFlags=0\n" +
                "IpDnsSuffix=\n" +
                "Ipv6Assign=1\n" +
                "Ipv6Address=::\n" +
                "Ipv6PrefixLength=0\n" +
                "Ipv6PrioritizeRemote=1\n" +
                "Ipv6InterfaceMetric=0\n" +
                "Ipv6NameAssign=1\n" +
                "Ipv6DnsAddress=::\n" +
                "Ipv6Dns2Address=::\n" +
                "Ipv6Prefix=0000000000000000\n" +
                "Ipv6InterfaceId=0000000000000000\n" +
                "DisableClassBasedDefaultRoute=0\n" +
                "DisableMobility=0\n" +
                "NetworkOutageTime=0\n" +
                "IDI=\n" +
                "IDR=\n" +
                "ImsConfig=0\n" +
                "IdiType=0\n" +
                "IdrType=0\n" +
                "ProvisionType=0\n" +
                "PreSharedKey=\n" +
                "CacheCredentials=1\n" +
                "NumCustomPolicy=0\n" +
                "NumEku=0\n" +
                "UseMachineRootCert=0\n" +
                "Disable_IKEv2_Fragmentation=0\n" +
                "PlumbIKEv2TSAsRoutes=0\n" +
                "NumServers=0\n" +
                "RouteVersion=1\n" +
                "NumRoutes=0\n" +
                "NumNrptRules=0\n" +
                "AutoTiggerCapable=0\n" +
                "NumAppIds=0\n" +
                "NumClassicAppIds=0\n" +
                "SecurityDescriptor=\n" +
                "ApnInfoProviderId=\n" +
                "ApnInfoUsername=" + user + "\n" +
                "ApnInfoPassword=" + pass + "\n" +
                "ApnInfoAccessPoint=\n" +
                "ApnInfoAuthentication=1\n" +
                "ApnInfoCompression=0\n" +
                "DeviceComplianceEnabled=0\n" +
                "DeviceComplianceSsoEnabled=0\n" +
                "DeviceComplianceSsoEku=\n" +
                "DeviceComplianceSsoIssuer=\n" +
                "FlagsSet=0\n" +
                "Options=0\n" +
                "DisableDefaultDnsSuffixes=0\n" +
                "NumTrustedNetworks=0\n" +
                "NumDnsSearchSuffixes=0\n" +
                "PowershellCreatedProfile=0\n" +
                "ProxyFlags=0\n" +
                "ProxySettingsModified=0\n" +
                "ProvisioningAuthority=\n" +
                "AuthTypeOTP=0\n" +
                "GREKeyDefined=0\n" +
                "NumPerAppTrafficFilters=0\n" +
                "AlwaysOnCapable=0\n" +
                "DeviceTunnel=0\n" +
                "PrivateNetwork=0\n" +
                "ManagementApp=\n" +
                "\nNETCOMPONENTS=\n" +
                "ms_msclient=0\n" +
                "ms_server=0\n" +
                "\nMEDIA=rastapi\n" +
                "Port=PPPoE5-0\n" +
                "Device=WAN Miniport (PPPOE)\n" +
                "\nDEVICE=PPPoE\n" +
                "LastSelectedPhone=0\n" +
                "PromoteAlternates=0\n" +
                "TryNextAlternateOnFail=1\n";

            File.AppendAllText(phonebookPath, entry);

            if (ConnectionExistsInWindows(name))
            {
                MessageBox.Show("系统连接 \"" + name + "\" 创建成功！\n\n现在可以点击「拨号连接」了。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("创建失败，请重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        if (!ConnectionExistsInWindows(name))
        {
            MessageBox.Show("连接 \"" + name + "\" 不存在", "提示");
            return;
        }

        if (MessageBox.Show("确定要删除系统连接 \"" + name + "\" 吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            try
            {
                string content = File.ReadAllText(phonebookPath);
                // Find and remove the section
                int start = content.IndexOf("[" + name + "]");
                if (start >= 0)
                {
                    int end = content.IndexOf("\n[", start + 1);
                    if (end < 0) end = content.Length;
                    content = content.Remove(start, end - start);
                    File.WriteAllText(phonebookPath, content);
                    MessageBox.Show("已删除", "成功");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除失败: " + ex.Message, "错误");
            }
        }
    }

    void SetupTray()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("拨号", null, (s, e) => BtnDial_Click(s, e));
        trayMenu.Items.Add("断开", null, (s, e) => BtnDisconnect_Click(s, e));
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("退出", null, (s, e) => { Application.Exit(); });

        trayIcon = new NotifyIcon { Text = "宽带自动拨号", Icon = CreateTrayIcon(), ContextMenuStrip = trayMenu, Visible = true };
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
        g.FillEllipse(connected ? Brushes.Green : Brushes.Gray, 2, 2, 12, 12);
        g.DrawString(connected ? "C" : "N", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 3, 1);
        g.Dispose();
        trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
        trayIcon.Text = "宽带自动拨号 - " + (connected ? "已连接" : "未连接");
    }

    void FormClosing_Handler(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
        statusTimer.Stop();
        trayIcon.Visible = false;
    }

    string GetErrorDesc(int code)
    {
        switch (code)
        {
            case 691: return "用户名或密码错误";
            case 678: return "服务器无响应";
            case 680: return "无拨号音";
            case 631: return "用户主动断开";
            case 633: return "端口被占用";
            case 651: return "设备报告错误";
            case 718: return "连接超时";
            case 720: return "协议不支持";
            case 721: return "服务器拒绝连接";
            case 734: return "连接被中断";
            default: return "错误代码 " + code;
        }
    }

    void StatusTimer_Tick(object s, EventArgs e) { CheckStatus(); UpdateOnlineTime(); }

    void CheckStatus()
    {
        try
        {
            Process proc = Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true });
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            bool wasConnected = isConnected;
            isConnected = output.Contains("已连接") || output.Contains("connected");
            if (isConnected)
            {
                lblStatus.Text = "已连接"; lblStatus.ForeColor = Color.Green;
                if (!wasConnected && connectTime == null)
                {
                    connectTime = DateTime.Now;
                    if (WindowState == FormWindowState.Minimized)
                        trayIcon.ShowBalloonTip(3000, "连接成功", "宽带已连接", ToolTipIcon.Info);
                }
            }
            else
            {
                lblStatus.Text = "未连接"; lblStatus.ForeColor = Color.Gray;
                connectTime = null; lblSpeed.Text = "网络速度: -- Mbps";
                if (wasConnected && chkAutoReconnect.Checked && !isDialing) AutoReconnect();
            }
            UpdateTrayIcon(isConnected);
        }
        catch { lblStatus.Text = "检测失败"; lblStatus.ForeColor = Color.Red; }
    }

    void AutoReconnect()
    {
        if (accounts.Count == 0 || isDialing) return;
        int idx = lstAccounts.SelectedIndex >= 0 ? lstAccounts.SelectedIndex : 0;
        if (idx >= accounts.Count) idx = 0;
        string name = accounts[idx][0], user = accounts[idx][1], pass = accounts[idx][2];

        isDialing = true;
        lblStatus.Text = "重连中...";
        lblStatus.ForeColor = Color.Orange;

        Thread t = new Thread(() =>
        {
            int exitCode = -1;
            try
            {
                string args = "\"" + name + "\"";
                if (!string.IsNullOrEmpty(user)) args += " \"" + user + "\" \"" + pass + "\"";
                Process proc = Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = args, UseShellExecute = false, CreateNoWindow = true });
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            catch { exitCode = -1; }

            this.Invoke((MethodInvoker)delegate
            {
                if (exitCode == 0)
                {
                    connectTime = DateTime.Now;
                    lblStatus.Text = "已连接 - " + name;
                    lblStatus.ForeColor = Color.Green;
                }
                else
                {
                    lblStatus.Text = "重连失败 - " + GetErrorDesc(exitCode);
                    lblStatus.ForeColor = Color.Red;
                }
                isDialing = false;
            });
        });
        t.IsBackground = true;
        t.Start();
    }

    void UpdateOnlineTime()
    {
        if (connectTime.HasValue && isConnected)
        {
            TimeSpan ts = DateTime.Now - connectTime.Value;
            lblOnlineTime.Text = "在线时长: " + ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
        }
        else lblOnlineTime.Text = "在线时长: --:--:--";
    }

    void LoadAccounts()
    {
        accounts.Clear(); lstAccounts.Items.Clear();
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
        if (i >= 0 && i < accounts.Count) { txtName.Text = accounts[i][0]; txtUser.Text = accounts[i][1]; txtPass.Text = accounts[i][2]; }
    }

    void BtnAdd_Click(object s, EventArgs e) { txtName.Text = ""; txtUser.Text = ""; txtPass.Text = ""; txtName.Focus(); lstAccounts.SelectedIndex = -1; }

    void BtnDelete_Click(object s, EventArgs e)
    {
        int i = lstAccounts.SelectedIndex;
        if (i < 0) { MessageBox.Show("请先选择要删除的账号", "提示"); return; }
        if (MessageBox.Show("确定删除 \"" + accounts[i][0] + "\" ?", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            accounts.RemoveAt(i); SaveAccounts(); LoadAccounts();
            if (lstAccounts.Items.Count > 0) lstAccounts.SelectedIndex = 0;
            else { txtName.Text = ""; txtUser.Text = ""; txtPass.Text = ""; }
        }
    }

    void BtnCreateConnection_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtUser.Text))
        { MessageBox.Show("请先填写连接名称和用户名", "提示"); return; }
        CreatePPPoEConnection(txtName.Text, txtUser.Text, txtPass.Text);
    }

    void BtnDeleteConnection_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请输入要删除的连接名称", "提示"); return; }
        DeletePPPoEConnection(txtName.Text);
    }

    void BtnImport_Click(object s, EventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog { Filter = "配置文件 (*.txt;*.adial)|*.txt;*.adial" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            int count = 0;
            foreach (string line in File.ReadAllLines(ofd.FileName))
            {
                if (string.IsNullOrEmpty(line)) continue;
                string[] p = line.Split(new char[] { '|' }, 3);
                if (p.Length >= 1 && !string.IsNullOrEmpty(p[0]))
                {
                    bool exists = false;
                    foreach (string[] a in accounts) if (a[0] == p[0]) { exists = true; break; }
                    if (!exists) { accounts.Add(new string[] { p[0], p.Length >= 2 ? p[1] : "", p.Length >= 3 ? p[2] : "" }); count++; }
                }
            }
            SaveAccounts(); LoadAccounts();
            MessageBox.Show("导入成功！新增 " + count + " 个账号", "提示");
        }
    }

    void BtnExport_Click(object s, EventArgs e)
    {
        if (accounts.Count == 0) { MessageBox.Show("没有账号可以导出", "提示"); return; }
        SaveFileDialog sfd = new SaveFileDialog { Filter = "配置文件 (*.adial)|*.adial", FileName = "accounts_" + DateTime.Now.ToString("yyyyMMdd") + ".adial" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            List<string> lines = new List<string>();
            foreach (string[] a in accounts) lines.Add(a[0] + "|" + a[1] + "|" + a[2]);
            File.WriteAllLines(sfd.FileName, lines.ToArray());
            MessageBox.Show("导出成功！", "提示");
        }
    }

    void BtnSave_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请输入连接名称", "提示"); return; }
        int i = lstAccounts.SelectedIndex;
        if (i >= 0 && i < accounts.Count) accounts[i] = new string[] { txtName.Text, txtUser.Text, txtPass.Text };
        else accounts.Add(new string[] { txtName.Text, txtUser.Text, txtPass.Text });
        SaveAccounts(); SaveSettings(); LoadAccounts();
        lblStatus.Text = "已保存"; lblStatus.ForeColor = Color.Green;
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
        File.WriteAllText(settingsPath, "AutoDial=" + (chkAutoDial.Checked ? "1" : "0") + "\nSilent=" + (chkSilent.Checked ? "1" : "0") + "\nAutoReconnect=" + (chkAutoReconnect.Checked ? "1" : "0"));
        string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");
        string destExe = Path.Combine(startupPath, "AutoDial.exe");
        if (chkAutoDial.Checked) { if (!File.Exists(destExe)) File.Copy(Assembly.GetExecutingAssembly().Location, destExe, true); }
        else { if (File.Exists(destExe)) File.Delete(destExe); }
    }

    void BtnDial_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请选择或输入账号", "提示"); return; }
        if (isDialing) return;

        isDialing = true;
        btnDial.Enabled = false;
        btnDisconnect.Enabled = false;
        lblStatus.Text = "连接中...";
        lblStatus.ForeColor = Color.Orange;

        string name = txtName.Text;
        string user = txtUser.Text;
        string pass = txtPass.Text;

        Thread t = new Thread(() =>
        {
            int exitCode = -1;
            try
            {
                string args = "\"" + name + "\"";
                if (!string.IsNullOrEmpty(user)) args += " \"" + user + "\" \"" + pass + "\"";
                Process proc = Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = args, UseShellExecute = false, CreateNoWindow = true });
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            catch { exitCode = -1; }

            this.Invoke((MethodInvoker)delegate
            {
                if (exitCode == 0)
                {
                    connectTime = DateTime.Now;
                    isConnected = true;
                    lblStatus.Text = "已连接 - " + name;
                    lblStatus.ForeColor = Color.Green;
                    UpdateTrayIcon(true);
                }
                else
                {
                    lblStatus.Text = "失败 - " + GetErrorDesc(exitCode);
                    lblStatus.ForeColor = Color.Red;
                }
                btnDial.Enabled = true;
                btnDisconnect.Enabled = true;
                isDialing = false;
            });
        });
        t.IsBackground = true;
        t.Start();
    }

    void BtnDisconnect_Click(object s, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = "\"" + txtName.Text + "\" /disconnect", UseShellExecute = false, CreateNoWindow = true });
            isConnected = false; connectTime = null;
            lblStatus.Text = "已断开"; lblStatus.ForeColor = Color.Gray;
            lblOnlineTime.Text = "在线时长: --:--:--";
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
                foreach (string l in File.ReadAllLines(settingsPath)) if (l.StartsWith("Silent=")) silent = l.Substring(7) == "1";
                if (!silent) { Application.EnableVisualStyles(); Application.Run(new AutoDialGUI()); return; }
            }
            if (File.Exists(configPath))
            {
                string[] lines = File.ReadAllLines(configPath);
                if (lines.Length > 0)
                {
                    string[] p = lines[0].Split(new char[] { '|' }, 3);
                    string a = "\"" + p[0] + "\"";
                    if (p.Length >= 2 && !string.IsNullOrEmpty(p[1])) a += " \"" + p[1] + "\" \"" + p[2] + "\"";
                    Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = a, UseShellExecute = false, CreateNoWindow = true });
                }
            }
            return;
        }
        if (args.Length > 1 && args[1] == "install")
        {
            string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");
            string destExe = Path.Combine(startupPath, "AutoDial.exe");
            if (!File.Exists(destExe)) File.Copy(Assembly.GetExecutingAssembly().Location, destExe, true);
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            File.WriteAllText(settingsPath, "AutoDial=1\nSilent=0\nAutoReconnect=1");
            MessageBox.Show("安装完成！", "提示");
            return;
        }
        Application.EnableVisualStyles();
        Application.Run(new AutoDialGUI());
    }
}