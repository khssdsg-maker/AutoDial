using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AutoDial
{
    public class App : Application
    {
        [System.STAThread]
        static void Main()
        {
            var app = new App();

            // 检查是否从启动项运行
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs\Startup\AutoDial.exe");

            if (exePath.Equals(startupPath, StringComparison.OrdinalIgnoreCase))
            {
                // 从启动项运行，检查是否静默模式
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"AutoDial\settings.txt");

                if (File.Exists(settingsPath))
                {
                    string[] lines = File.ReadAllLines(settingsPath);
                    bool silent = false;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Silent=") && line.Substring(7) == "1")
                        {
                            silent = true;
                            break;
                        }
                    }

                    if (silent)
                    {
                        // 静默模式：直接拨号，不显示窗口
                        string configPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            @"AutoDial\accounts.txt");

                        if (File.Exists(configPath))
                        {
                            string[] accountLines = File.ReadAllLines(configPath);
                            if (accountLines.Length > 0)
                            {
                                string[] parts = accountLines[0].Split(new char[] { '|' }, 3);
                                if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0]))
                                {
                                    string name = parts[0];
                                    string user = parts.Length >= 2 ? parts[1] : "";
                                    string pass = parts.Length >= 3 ? parts[2] : "";

                                    string args = "\"" + name + "\"";
                                    if (!string.IsNullOrEmpty(user))
                                        args += " \"" + user + "\" \"" + pass + "\"";

                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "rasdial.exe",
                                        Arguments = args,
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    });
                                }
                            }
                        }
                        app.Shutdown();
                        return;
                    }
                }
            }

            // 正常模式：显示窗口
            app.Run(new MainWindow());
        }
    }
}