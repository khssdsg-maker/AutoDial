using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

class AutoDialGUI : Form
{
    static string appDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"AutoDial");
    static string configPath = Path.Combine(appDir, "accounts.txt");
    static string settingsPath = Path.Combine(appDir, "settings.txt");

    ListBox lstAccounts;
    TextBox txtName, txtUser, txtPass;
    Label lblStatus;
    Button btnDial, btnDisconnect, btnSave, btnAdd, btnDelete;
    CheckBox chkAutoDial, chkSilent;
    ToolTip tip;
    List<string[]> accounts = new List<string[]>();

    public AutoDialGUI()
    {
        Text = "宽带自动拨号";
        Size = new Size(500, 520);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei", 10);
        tip = new ToolTip { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 10000 };

        Label l0 = new Label { Text = "账号列表:", Location = new Point(20, 15), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };
        lstAccounts = new ListBox { Location = new Point(20, 45), Width = 430, Height = 110, Font = new Font("Microsoft YaHei", 11) };
        lstAccounts.SelectedIndexChanged += LstAccounts_SelectedIndexChanged;
        tip.SetToolTip(lstAccounts, "选择要操作的宽带账号");

        btnAdd = new Button { Text = "新增账号", Location = new Point(20, 165), Width = 130, Height = 32 };
        btnAdd.Click += BtnAdd_Click;
        tip.SetToolTip(btnAdd, "添加一个新的宽带拨号账号");

        btnDelete = new Button { Text = "删除账号", Location = new Point(160, 165), Width = 130, Height = 32 };
        btnDelete.Click += BtnDelete_Click;
        tip.SetToolTip(btnDelete, "删除当前选中的宽带账号");

        Label l1 = new Label { Text = "连接名称:", Location = new Point(20, 210), AutoSize = true };
        txtName = new TextBox { Location = new Point(110, 207), Width = 340, Font = new Font("Microsoft YaHei", 11) };
        tip.SetToolTip(txtName, "Windows宽带连接名称，默认为 宽带连接");

        Label l2 = new Label { Text = "用户名:", Location = new Point(20, 250), AutoSize = true };
        txtUser = new TextBox { Location = new Point(110, 247), Width = 340, Font = new Font("Microsoft YaHei", 11) };
        tip.SetToolTip(txtUser, "宽带拨号的用户名，由运营商提供");

        Label l3 = new Label { Text = "密码:", Location = new Point(20, 290), AutoSize = true };
        txtPass = new TextBox { Location = new Point(110, 287), Width = 340, Font = new Font("Microsoft YaHei", 11), UseSystemPasswordChar = true };
        tip.SetToolTip(txtPass, "宽带拨号的密码，由运营商提供");

        Label l4 = new Label { Text = "设置:", Location = new Point(20, 330), AutoSize = true, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold) };

        chkAutoDial = new CheckBox { Text = "开机自动拨号", Location = new Point(20, 355), AutoSize = true };
        tip.SetToolTip(chkAutoDial, "开启后，每次开机登录Windows时自动执行拨号连接");

        chkSilent = new CheckBox { Text = "静默拨号（不弹出窗口）", Location = new Point(200, 355), AutoSize = true };
        tip.SetToolTip(chkSilent, "开启后，开机拨号时不会弹出软件界面，后台自动连接");
        chkSilent.CheckedChanged += ChkSilent_CheckedChanged;

        Label lblSilentTip = new Label { Text = "(需先开启开机自动拨号)", Location = new Point(200, 378), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Microsoft YaHei", 8) };

        lblStatus = new Label { Text = "状态: 未检测", Location = new Point(20, 410), AutoSize = true, ForeColor = Color.Gray };
        tip.SetToolTip(lblStatus, "显示当前的网络连接状态");

        btnDial = new Button { Text = "拨号连接", Location = new Point(20, 440), Width = 130, Height = 40, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnDial.Click += BtnDial_Click;
        tip.SetToolTip(btnDial, "使用当前选中的账号进行宽带拨号");

        btnDisconnect = new Button { Text = "断开连接", Location = new Point(160, 440), Width = 130, Height = 40 };
        btnDisconnect.Click += BtnDisconnect_Click;
        tip.SetToolTip(btnDisconnect, "断开当前的宽带连接");

        btnSave = new Button { Text = "保存设置", Location = new Point(310, 440), Width = 140, Height = 40, BackColor = Color.FromArgb(0, 153, 51), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnSave.Click += BtnSave_Click;
        tip.SetToolTip(btnSave, "保存所有配置和设置，开机拨号功能需保存后生效");

        Controls.AddRange(new Control[] { l0, lstAccounts, btnAdd, btnDelete, l1, txtName, l2, txtUser, l3, txtPass, l4, chkAutoDial, chkSilent, lblSilentTip, lblStatus, btnDial, btnDisconnect, btnSave });

        LoadAccounts();
        LoadSettings();
        Load += (s, e) => CheckStatus();
    }

    void ChkSilent_CheckedChanged(object s, EventArgs e)
    {
        if (chkSilent.Checked && !chkAutoDial.Checked) { chkSilent.Checked = false; MessageBox.Show("请先勾选「开机自动拨号」", "提示"); }
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
            }
        }
    }

    void LstAccounts_SelectedIndexChanged(object s, EventArgs e)
    {
        int i = lstAccounts.SelectedIndex;
        if (i >= 0 && i < accounts.Count) { txtName.Text = accounts[i][0]; txtUser.Text = accounts[i][1]; txtPass.Text = accounts[i][2]; }
    }

    void BtnAdd_Click(object s, EventArgs e) { txtName.Text = ""; txtUser.Text = ""; txtPass.Text = ""; txtName.Focus(); lstAccounts.SelectedIndex = -1; lblStatus.Text = "状态: 输入新账号后点保存"; lblStatus.ForeColor = Color.Orange; }

    void BtnDelete_Click(object s, EventArgs e)
    {
        int i = lstAccounts.SelectedIndex;
        if (i < 0) { MessageBox.Show("请先选择要删除的账号", "提示"); return; }
        if (MessageBox.Show("确定删除 \"" + accounts[i][0] + "\" ?", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            accounts.RemoveAt(i); SaveAccounts(); LoadAccounts();
            if (lstAccounts.Items.Count > 0) lstAccounts.SelectedIndex = 0; else { txtName.Text = ""; txtUser.Text = ""; txtPass.Text = ""; }
        }
    }

    void BtnSave_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请输入连接名称", "提示"); return; }
        int i = lstAccounts.SelectedIndex;
        if (i >= 0 && i < accounts.Count) accounts[i] = new string[] { txtName.Text, txtUser.Text, txtPass.Text };
        else accounts.Add(new string[] { txtName.Text, txtUser.Text, txtPass.Text });
        SaveAccounts(); SaveSettings(); LoadAccounts();
        lblStatus.Text = "状态: 配置已保存"; lblStatus.ForeColor = Color.Green;
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
        File.WriteAllText(settingsPath, "AutoDial=" + (chkAutoDial.Checked ? "1" : "0") + "\nSilent=" + (chkSilent.Checked ? "1" : "0"));
        string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");
        string destExe = Path.Combine(startupPath, "AutoDial.exe");
        string currentExe = Assembly.GetExecutingAssembly().Location;
        if (chkAutoDial.Checked) { if (!File.Exists(destExe)) File.Copy(currentExe, destExe, true); }
        else { if (File.Exists(destExe)) File.Delete(destExe); }
    }

    void BtnDial_Click(object s, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("请选择或输入账号", "提示"); return; }
        btnDial.Enabled = false; lblStatus.Text = "状态: 连接中..."; lblStatus.ForeColor = Color.Orange; Application.DoEvents();
        try
        {
            string args = "\"" + txtName.Text + "\"";
            if (!string.IsNullOrEmpty(txtUser.Text)) args += " \"" + txtUser.Text + "\" \"" + txtPass.Text + "\"";
            Process proc = Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = args, UseShellExecute = false, CreateNoWindow = true });
            proc.WaitForExit();
            lblStatus.Text = proc.ExitCode == 0 ? "状态: 已连接 - " + txtName.Text : "状态: 连接失败 (错误 " + proc.ExitCode + ")";
            lblStatus.ForeColor = proc.ExitCode == 0 ? Color.Green : Color.Red;
        }
        catch (Exception ex) { lblStatus.Text = "状态: 错误 - " + ex.Message; lblStatus.ForeColor = Color.Red; }
        btnDial.Enabled = true;
    }

    void BtnDisconnect_Click(object s, EventArgs e)
    {
        try { Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", Arguments = "\"" + txtName.Text + "\" /disconnect", UseShellExecute = false, CreateNoWindow = true }); lblStatus.Text = "状态: 已断开"; lblStatus.ForeColor = Color.Gray; } catch { }
    }

    void CheckStatus()
    {
        try { Process proc = Process.Start(new ProcessStartInfo { FileName = "rasdial.exe", UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true }); string o = proc.StandardOutput.ReadToEnd(); proc.WaitForExit(); if (o.Contains("已连接") || o.Contains("connected")) { lblStatus.Text = "状态: 已连接"; lblStatus.ForeColor = Color.Green; } } catch { }
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
            string currentExe = Assembly.GetExecutingAssembly().Location;
            if (currentExe != destExe) File.Copy(currentExe, destExe, true);
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            File.WriteAllText(settingsPath, "AutoDial=1\nSilent=0");
            MessageBox.Show("安装完成！开机将自动拨号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Application.EnableVisualStyles();
        Application.Run(new AutoDialGUI());
    }
}