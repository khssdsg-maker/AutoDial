using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AutoDial
{
    public partial class MainWindow : Window
    {
        static string appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"AutoDial");
        static string configPath = Path.Combine(appDir, "accounts.txt");
        static string settingsPath = Path.Combine(appDir, "settings.txt");
        static string windowSettingsPath = Path.Combine(appDir, "window.txt");
        static string phonebookPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Network\Connections\Pbk\rasphone.pbk");
        static string currentVersion = "2.5";

        List<string[]> accounts = new List<string[]>();
        DispatcherTimer statusTimer;
        DateTime? connectTime;
        bool isConnected = false;
        bool isDialing = false;
        System.Windows.Forms.NotifyIcon trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowPosition();

            statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            statusTimer.Tick += StatusTimer_Tick;
            statusTimer.Start();

            LoadAccounts();
            LoadSettings();
            CheckStatus();
            SetupTray();
            CheckForUpdate();
        }

        void LoadWindowPosition()
        {
            try
            {
                if (File.Exists(windowSettingsPath))
                {
                    string[] lines = File.ReadAllLines(windowSettingsPath);
                    if (lines.Length >= 2)
                    {
                        double left = double.Parse(lines[0]);
                        double top = double.Parse(lines[1]);
                        if (left >= 0 && top >= 0 && left < SystemParameters.PrimaryScreenWidth && top < SystemParameters.PrimaryScreenHeight)
                        {
                            Left = left;
                            Top = top;
                            WindowStartupLocation = WindowStartupLocation.Manual;
                        }
                    }
                }
            }
            catch { }
        }

        void SaveWindowPosition()
        {
            try
            {
                if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
                File.WriteAllLines(windowSettingsPath, new string[] { Left.ToString(), Top.ToString() });
            }
            catch { }
        }

        void CheckForUpdate()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        string latest = wc.DownloadString("https://raw.githubusercontent.com/khssdsg-maker/AutoDial/main/version.txt");
                        latest = latest.Trim();
                        if (latest != currentVersion)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (MessageBox.Show("发现新版本 " + latest + "，是否前往下载？", "更新", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                {
                                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/khssdsg-maker/AutoDial/releases", UseShellExecute = true });
                                }
                            });
                        }
                    }
                }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        void SetupTray()
        {
            var trayMenu = new System.Windows.Forms.ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, (s, e) => { Show(); WindowState = WindowState.Normal; });
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("拨号", null, (s, e) => Dial());
            trayMenu.Items.Add("断开", null, (s, e) => Disconnect());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("退出", null, (s, e) => { SaveWindowPosition(); Application.Current.Shutdown(); });

            trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "宽带自动拨号",
                Icon = System.Drawing.SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => { Show(); WindowState = WindowState.Normal; };
        }

        void UpdateTrayIcon(bool connected)
        {
            trayIcon.Text = "宽带自动拨号 - " + (connected ? "已连接" : "未连接");
        }

        void ShowTrayBalloon(string title, string msg)
        {
            trayIcon.ShowBalloonTip(3000, title, msg, System.Windows.Forms.ToolTipIcon.Info);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveWindowPosition();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnClosed(e);
            Application.Current.Shutdown();
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
                    lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4, 0xC, 0xAF));
                    statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x4, 0xC, 0xAF));
                    if (!wasConnected && connectTime == null)
                    {
                        connectTime = DateTime.Now;
                        if (WindowState == WindowState.Minimized)
                            ShowTrayBalloon("连接成功", "宽带已连接");
                    }
                }
                else
                {
                    lblStatus.Text = "未连接";
                    lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
                    statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
                    connectTime = null;

                    if (wasConnected && chkAutoReconnect.IsChecked == true && !isDialing)
                        AutoReconnect();
                }

                UpdateTrayIcon(isConnected);
            }
            catch
            {
                lblStatus.Text = "检测失败";
                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
            }
        }

        void AutoReconnect()
        {
            if (accounts.Count == 0 || isDialing) return;
            int idx = lstAccounts.SelectedIndex >= 0 ? lstAccounts.SelectedIndex : 0;
            if (idx >= accounts.Count) idx = 0;

            string name = accounts[idx][0], user = accounts[idx][1], pass = accounts[idx][2];
            isDialing = true;
            lblStatus.Text = "重连中...";

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

                Dispatcher.Invoke(() =>
                {
                    if (exitCode == 0)
                    {
                        connectTime = DateTime.Now;
                        lblStatus.Text = "已连接 - " + name;
                        lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4, 0xC, 0xAF));
                        statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x4, 0xC, 0xAF));
                        ShowTrayBalloon("重连成功", "宽带已自动重新连接");
                    }
                    else
                    {
                        lblStatus.Text = "重连失败 - " + GetErrorDesc(exitCode);
                        lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
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
                lblOnlineTime.Text = ts.Hours.ToString("D2") + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
            }
            else
                lblOnlineTime.Text = "--:--:--";
        }

        string GetErrorDesc(int code)
        {
            switch (code)
            {
                case 691: return "用户名或密码错误";
                case 678: return "服务器无响应";
                case 680: return "无拨号音";
                case 631: return "用户主动断开";
                case 651: return "设备报告错误";
                case 718: return "连接超时";
                case 720: return "协议不支持";
                case 721: return "服务器拒绝连接";
                case 734: return "连接被中断";
                default: return "错误代码 " + code;
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
                    if (line.StartsWith("AutoDial=")) chkAutoDial.IsChecked = line.Substring(9) == "1";
                    if (line.StartsWith("Silent=")) chkSilent.IsChecked = line.Substring(7) == "1";
                    if (line.StartsWith("AutoReconnect=")) chkAutoReconnect.IsChecked = line.Substring(14) == "1";
                }
            }
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
            File.WriteAllText(settingsPath,
                "AutoDial=" + (chkAutoDial.IsChecked == true ? "1" : "0") + "\n" +
                "Silent=" + (chkSilent.IsChecked == true ? "1" : "0") + "\n" +
                "AutoReconnect=" + (chkAutoReconnect.IsChecked == true ? "1" : "0"));

            string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs\Startup");
            string destExe = Path.Combine(startupPath, "AutoDial.exe");
            if (chkAutoDial.IsChecked == true)
            {
                if (!File.Exists(destExe)) File.Copy(Assembly.GetExecutingAssembly().Location, destExe, true);
            }
            else
            {
                if (File.Exists(destExe)) File.Delete(destExe);
            }
        }

        string GetCurrentPassword()
        {
            return isPasswordVisible ? txtPassVisible.Text : txtPass.Password;
        }

        void lstAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = lstAccounts.SelectedIndex;
            if (i >= 0 && i < accounts.Count)
            {
                txtName.Text = accounts[i][0];
                txtUser.Text = accounts[i][1];
                txtPass.Password = accounts[i][2];
                txtPassVisible.Text = accounts[i][2];
            }
        }

        void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            txtName.Text = ""; txtUser.Text = ""; txtPass.Password = ""; txtPassVisible.Text = "";
            txtName.Focus();
            lstAccounts.SelectedIndex = -1;
        }

        void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            int i = lstAccounts.SelectedIndex;
            if (i < 0) { MessageBox.Show("请先选择要删除的账号"); return; }
            if (MessageBox.Show("确定删除 \"" + accounts[i][0] + "\" ?", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                accounts.RemoveAt(i);
                SaveAccounts();
                LoadAccounts();
            }
        }

        void BtnCreateConnection_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtUser.Text))
            { MessageBox.Show("请先填写连接名称和用户名"); return; }

            string name = txtName.Text;
            if (File.Exists(phonebookPath) && File.ReadAllText(phonebookPath).Contains("[" + name + "]"))
            {
                MessageBox.Show("系统连接 \"" + name + "\" 已存在");
                return;
            }

            try
            {
                string entry = "\n[" + name + "]\nEncoding=1\nPBVersion=8\nType=5\nAutoLogon=0\nUseRasCredentials=1\n" +
                    "DialParamsUID=" + new Random().Next(10000000, 99999999) + "\nGuid={" + Guid.NewGuid().ToString("D").ToUpper() + "}\n" +
                    "VpnStrategy=0\nExcludedProtocols=0\nLcpExtensions=1\nDataEncryption=8\nSwCompression=0\n" +
                    "NegotiateMultilinkAlways=0\nSkipDoubleDialDialog=0\nDialMode=0\nOverridePref=15\nRedialAttempts=3\n" +
                    "RedialSeconds=60\nIdleDisconnectSeconds=0\nRedialOnLinkFailure=1\nCallbackMode=0\nCustomDialDll=\n" +
                    "CustomDialFunc=\nCustomRasDialDll=\nForceSecureCompartment=0\nDisableIKENameEkuCheck=0\n" +
                    "AuthenticateServer=0\nShareMsFilePrint=0\nBindMsNetClient=0\nSharedPhoneNumbers=0\n" +
                    "GlobalDeviceSettings=0\nPrerequisiteEntry=\nPrerequisitePbk=\nPreferredPort=PPPoE5-0\n" +
                    "PreferredDevice=WAN Miniport (PPPOE)\nPreferredBps=0\nPreferredHwFlow=0\nPreferredProtocol=0\n" +
                    "PreferredCompression=0\nPreferredSpeaker=0\nPreferredMdmProtocol=0\nPreviewUserPw=1\n" +
                    "PreviewDomain=0\nPreviewPhoneNumber=0\nShowDialingProgress=1\nShowMonitorIconInTaskBar=1\n" +
                    "CustomAuthKey=0\nAuthRestrictions=552\nIpPrioritizeRemote=1\nIpInterfaceMetric=0\n" +
                    "IpHeaderCompression=0\nIpAddress=0.0.0.0\nIpDnsAddress=0.0.0.0\nIpDns2Address=0.0.0.0\n" +
                    "IpWinsAddress=0.0.0.0\nIpWins2Address=0.0.0.0\nIpAssign=1\nIpNameAssign=1\nIpDnsFlags=0\n" +
                    "IpNBTFlags=0\nTcpWindowSize=0\nUseFlags=3\nIpSecFlags=0\nIpDnsSuffix=\nIpv6Assign=1\n" +
                    "Ipv6Address=::\nIpv6PrefixLength=0\nIpv6PrioritizeRemote=1\nIpv6InterfaceMetric=0\n" +
                    "Ipv6NameAssign=1\nIpv6DnsAddress=::\nIpv6Dns2Address=::\nIpv6Prefix=0000000000000000\n" +
                    "Ipv6InterfaceId=0000000000000000\nDisableClassBasedDefaultRoute=0\nDisableMobility=0\n" +
                    "NetworkOutageTime=0\nIDI=\nIDR=\nImsConfig=0\nIdiType=0\nIdrType=0\nProvisionType=0\n" +
                    "PreSharedKey=\nCacheCredentials=1\nNumCustomPolicy=0\nNumEku=0\nUseMachineRootCert=0\n" +
                    "Disable_IKEv2_Fragmentation=0\nPlumbIKEv2TSAsRoutes=0\nNumServers=0\nRouteVersion=1\n" +
                    "NumRoutes=0\nNumNrptRules=0\nAutoTiggerCapable=0\nNumAppIds=0\nNumClassicAppIds=0\n" +
                    "SecurityDescriptor=\nApnInfoProviderId=\nApnInfoUsername=" + txtUser.Text + "\nApnInfoPassword=" + GetCurrentPassword() + "\n" +
                    "ApnInfoAccessPoint=\nApnInfoAuthentication=1\nApnInfoCompression=0\nDeviceComplianceEnabled=0\n" +
                    "DeviceComplianceSsoEnabled=0\nDeviceComplianceSsoEku=\nDeviceComplianceSsoIssuer=\nFlagsSet=0\n" +
                    "Options=0\nDisableDefaultDnsSuffixes=0\nNumTrustedNetworks=0\nNumDnsSearchSuffixes=0\n" +
                    "PowershellCreatedProfile=0\nProxyFlags=0\nProxySettingsModified=0\nProvisioningAuthority=\n" +
                    "AuthTypeOTP=0\nGREKeyDefined=0\nNumPerAppTrafficFilters=0\nAlwaysOnCapable=0\nDeviceTunnel=0\n" +
                    "PrivateNetwork=0\nManagementApp=\n\nNETCOMPONENTS=\nms_msclient=0\nms_server=0\n\n" +
                    "MEDIA=rastapi\nPort=PPPoE5-0\nDevice=WAN Miniport (PPPOE)\n\nDEVICE=PPPoE\n" +
                    "LastSelectedPhone=0\nPromoteAlternates=0\nTryNextAlternateOnFail=1\n";

                File.AppendAllText(phonebookPath, entry);

                // Auto-add account to list
                bool exists = false;
                foreach (string[] a in accounts) if (a[0] == name) { exists = true; break; }
                if (!exists)
                {
                    accounts.Add(new string[] { name, txtUser.Text, GetCurrentPassword() });
                    SaveAccounts();
                    LoadAccounts();
                    lstAccounts.SelectedIndex = lstAccounts.Items.Count - 1;
                }

                MessageBox.Show("系统连接创建成功！\n已自动添加到账户列表。", "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建失败: " + ex.Message);
            }
        }

        void BtnDeleteConnection_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请输入连接名称"); return; }
            string name = txtName.Text;

            if (!File.Exists(phonebookPath) || !File.ReadAllText(phonebookPath).Contains("[" + name + "]"))
            { MessageBox.Show("系统连接不存在"); return; }

            if (MessageBox.Show("确定删除系统连接 \"" + name + "\" 吗？\n\n对应的账户也会被删除。", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete system connection
                    string content = File.ReadAllText(phonebookPath);
                    int start = content.IndexOf("[" + name + "]");
                    int end = content.IndexOf("\n[", start + 1);
                    if (end < 0) end = content.Length;
                    content = content.Remove(start, end - start);
                    File.WriteAllText(phonebookPath, content);

                    // Auto-delete account from list
                    for (int i = accounts.Count - 1; i >= 0; i--)
                    {
                        if (accounts[i][0] == name)
                        {
                            accounts.RemoveAt(i);
                            break;
                        }
                    }
                    SaveAccounts();
                    LoadAccounts();

                    MessageBox.Show("已删除系统连接和对应账户", "成功");
                }
                catch (Exception ex) { MessageBox.Show("删除失败: " + ex.Message); }
            }
        }

        void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "配置文件 (*.txt;*.adial)|*.txt;*.adial" };
            if (ofd.ShowDialog() == true)
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
                MessageBox.Show("导入成功！新增 " + count + " 个账号");
            }
        }

        void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (accounts.Count == 0) { MessageBox.Show("没有账号可以导出"); return; }
            var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "配置文件 (*.adial)|*.adial", FileName = "accounts_" + DateTime.Now.ToString("yyyyMMdd") + ".adial" };
            if (sfd.ShowDialog() == true)
            {
                List<string> lines = new List<string>();
                foreach (string[] a in accounts) lines.Add(a[0] + "|" + a[1] + "|" + a[2]);
                File.WriteAllLines(sfd.FileName, lines.ToArray());
                MessageBox.Show("导出成功！");
            }
        }

        void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请输入连接名称"); return; }
            int i = lstAccounts.SelectedIndex;
            if (i >= 0 && i < accounts.Count) accounts[i] = new string[] { txtName.Text, txtUser.Text, GetCurrentPassword() };
            else accounts.Add(new string[] { txtName.Text, txtUser.Text, GetCurrentPassword() });
            SaveAccounts(); SaveSettings(); LoadAccounts();
            lblStatus.Text = "已保存";
        }

        void BtnDial_Click(object sender, RoutedEventArgs e) { Dial(); }

        void Dial()
        {
            if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请选择或输入账号"); return; }
            if (isDialing) return;

            isDialing = true;
            btnDial.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
            lblStatus.Text = "连接中...";

            string name = txtName.Text, user = txtUser.Text, pass = GetCurrentPassword();

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

                Dispatcher.Invoke(() =>
                {
                    if (exitCode == 0)
                    {
                        connectTime = DateTime.Now; isConnected = true;
                        lblStatus.Text = "已连接 - " + name;
                        lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4, 0xC, 0xAF));
                        statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x4, 0xC, 0xAF));
                        UpdateTrayIcon(true);
                        ShowTrayBalloon("连接成功", name + " 已连接");
                    }
                    else
                    {
                        lblStatus.Text = "失败 - " + GetErrorDesc(exitCode);
                        lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
                        ShowTrayBalloon("连接失败", GetErrorDesc(exitCode));
                    }
                    btnDial.IsEnabled = true;
                    btnDisconnect.IsEnabled = true;
                    isDialing = false;
                });
            });
            t.IsBackground = true;
            t.Start();
        }

        void BtnDisconnect_Click(object sender, RoutedEventArgs e) { Disconnect(); }

        void Disconnect()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = "\"" + txtName.Text + "\" /disconnect", UseShellExecute = false, CreateNoWindow = true });
                isConnected = false; connectTime = null;
                lblStatus.Text = "已断开";
                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
                statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
                UpdateTrayIcon(false);
                ShowTrayBalloon("已断开", "宽带连接已断开");
            }
            catch { }
        }

        void BtnChangelog_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window
            {
                Title = "更新日志 - AutoDial",
                Width = 500,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                FontFamily = new FontFamily("Microsoft YaHei"),
                ResizeMode = ResizeMode.NoResize
            };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(20) };

            var log = @"v2.5 (2025-07-19)
- 添加软件图标（紫蓝渐变设计）
- 标题栏显示更新日志按钮

v2.4 (2025-07-19)
- 新增更新日志窗口
- 点击标题栏按钮查看历史版本

v2.3 (2025-07-19)
- 新增密码显示/隐藏切换按钮
- 新增鼠标悬停提示

v2.2 (2025-07-19)
- 新增托盘气泡通知（拨号成功/失败/断开）
- 新增窗口位置记忆功能
- 新增自动更新检测（从GitHub检查新版本）
- 优化：创建系统连接后自动添加到账户列表
- 优化：删除系统连接时自动删除对应账户

v2.1 (2025-07-19)
- 修复界面布局问题，添加滚动支持

v2.0 (2025-07-19)
- 升级为WPF现代化界面
- 卡片式布局，圆角阴影效果

v1.8 (2025-07-19)
- 异步拨号，解决界面卡顿问题

v1.7 (2025-07-19)
- 重写创建连接功能，直接写入电话簿文件

v1.4 (2025-07-01)
- 新增创建/删除系统连接功能

v1.3 (2025-07-01)
- 新增系统托盘功能
- 新增错误码中文解释
- 新增导入/导出配置功能

v1.2 (2025-07-01)
- 新增断线自动重连功能
- 新增连接状态实时监控

v1.1 (2025-07-01)
- 新增多账号管理功能

v1.0 (2025-06-14)
- 初始版本发布";

            string[] lines = log.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("v"))
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = trimmed,
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
                        Margin = new Thickness(0, 12, 0, 6)
                    });
                }
                else if (trimmed.StartsWith("-"))
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = trimmed,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                        Margin = new Thickness(8, 2, 0, 2),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }

            scrollViewer.Content = stack;
            win.Content = scrollViewer;
            win.ShowDialog();
        }
    }
}