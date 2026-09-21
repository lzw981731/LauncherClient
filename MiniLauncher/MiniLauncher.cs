// MiniLauncher —— 极简 KartRider 登录器
// 只保留三个参数：服务器IP / 服务器端口 / 角色名称
// 传给 KartRider.exe 的内容与原版 Launcher_V2 完全一致：
//   1. 命令行: KartRider.exe TGC -region:3 -passport:<Base64 JSON>
//   2. PIN 文件: 登录服务器 LoginServers 改写 + 移除 NgsOn
//   3. 启动后: TCP 检测到连接即恢复原始 PIN 文件
// 编译: 双击 编译.bat（Windows 自带 .NET Framework csc.exe，无需 Visual Studio）

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using KartRider.Common.Data;

namespace KartRider
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MiniLauncherForm());
        }
    }

    public class MiniLauncherForm : Form
    {
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtName;
        private TextBox txtLog;
        private Button btnStart;

        public MiniLauncherForm()
        {
            Text = "KartRider 登录器 Mini";
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(430, 400);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            int y = 16;

            txtIp = NewTextBox();
            y = AddRow("服务器IP:", txtIp, y, 180);

            txtPort = NewTextBox();
            y = AddRow("服务器端口:", txtPort, y, 80);

            txtName = NewTextBox();
            y = AddRow("角色名称:", txtName, y, 180);

            btnStart = new Button();
            btnStart.Text = "启动游戏";
            btnStart.Size = new Size(130, 34);
            btnStart.Location = new Point(150, y + 4);
            btnStart.Font = Font;
            btnStart.Click += delegate { StartClicked(); };
            Controls.Add(btnStart);

            y += 50;

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Location = new Point(16, y);
            txtLog.Size = new Size(ClientSize.Width - 32, ClientSize.Height - y - 16);
            txtLog.Font = new Font("Consolas", 9f);
            Controls.Add(txtLog);

            LoadConfig();
            Log("就绪。填写 服务器IP / 端口 / 角色名称 后点击启动。");
        }

        private TextBox NewTextBox()
        {
            TextBox tb = new TextBox();
            tb.Font = Font;
            return tb;
        }

        private int AddRow(string label, TextBox input, int y, int width)
        {
            Label lb = new Label();
            lb.Text = label;
            lb.AutoSize = true;
            lb.Location = new Point(16, y + 3);
            Controls.Add(lb);

            input.Location = new Point(118, y);
            input.Size = new Size(width, 24);
            Controls.Add(input);
            return y + 34;
        }

        // ============ 启动 ============

        private void StartClicked()
        {
            if (!btnStart.Enabled)
                return;
            btnStart.Enabled = false;

            SaveConfig(txtIp.Text.Trim(), txtPort.Text.Trim(), txtName.Text.Trim());

            Thread t = new Thread(LaunchGame);
            t.IsBackground = true;
            t.Name = "LaunchThread";
            t.Start();
        }

        private void LaunchGame()
        {
            try
            {
                string serverIP = txtIp.Text.Trim();
                string serverPortStr = txtPort.Text.Trim();
                string nickname = txtName.Text.Trim();
                ushort serverPort;

                if (serverIP.Length == 0 || serverPortStr.Length == 0 || nickname.Length == 0)
                {
                    Log("错误：请填写 服务器IP / 服务器端口 / 角色名称");
                    return;
                }
                if (!ushort.TryParse(serverPortStr, out serverPort))
                {
                    Log("错误：服务器端口必须是 0-65535 的数字");
                    return;
                }

                string root = FindKartRiderDirectory();
                if (root == null)
                {
                    Log("错误：找不到 KartRider.exe / KartRider.pin");
                    Log("  请把 MiniLauncher.exe 放到游戏目录；或先运行过原版登录器（注册表 TCGame\\kart\\gamepath）");
                    return;
                }

                string pinFile = Path.Combine(root, "KartRider.pin");
                string pinFileBak = Path.Combine(root, "KartRider-bak.pin");

                // 上次启动残留的备份：先还原，保证 PIN 从原版开始改
                if (File.Exists(pinFileBak))
                {
                    if (File.Exists(pinFile))
                        File.Delete(pinFile);
                    File.Move(pinFileBak, pinFile);
                    Log("已还原上次残留的 PIN 备份");
                }

                PINFile val = new PINFile(pinFile);
                ushort clientVersion = val.Header.MinorVersion;

                File.Copy(pinFile, pinFileBak, true);
                Log("已备份 PIN -> KartRider-bak.pin");

                // 改写登录服务器（与原版一致：IPv6 时游戏连 127.0.0.1）
                string ip = IsIPv6(serverIP) ? "127.0.0.1" : serverIP;
                if (val.AuthMethods != null)
                {
                    foreach (PINFile.AuthMethod am in val.AuthMethods)
                    {
                        am.LoginServers.Clear();
                        am.LoginServers.Add(new PINFile.IPEndPoint { IP = ip, Port = serverPort });
                    }
                }
                Log(string.Format("已写入登录服务器: {0}:{1}", ip, serverPort));

                // 移除 NgsOn（与原版默认设置 NgsOn=false 一致）
                RemoveNgsOn(val);

                File.WriteAllBytes(pinFile, val.GetEncryptedData());
                Log("PIN 文件已写回");

                // 构造 passport（字段顺序与原版 Helper.cs 一致）
                string json = string.Format(
                    "{{\"Nickname\":\"{0}\",\"ClientVersion\":{1},\"CompileTime\":\"{2}\"}}",
                    JsonEscape(nickname), clientVersion, CompileTime.Time);
                string passport = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                string args = string.Format("TGC -region:3 -passport:{0}", passport);
                Log("命令行: KartRider.exe " + args);

                ProcessStartInfo psi = new ProcessStartInfo("KartRider.exe", args)
                {
                    WorkingDirectory = Path.GetFullPath(root),
                    UseShellExecute = true
                };
                Process proc = Process.Start(psi);
                int pid = proc.Id;
                Log(string.Format("KartRider.exe 已启动, PID={0}", pid));
                proc.Dispose();

                // 后台检测 TCP 连接，连上后恢复原始 PIN（与原版一致）
                Log("等待游戏连接服务器，连接后自动恢复原始 PIN ...");
                bool restored = false;
                for (int i = 0; i < 30; i++)
                {
                    Thread.Sleep(1000);
                    if (CheckTcpConnection(pid, ip, serverPort))
                    {
                        if (RestorePinFile(pinFile, pinFileBak))
                        {
                            Log("已检测到连接，PIN 已恢复为原版");
                            restored = true;
                            break;
                        }
                    }
                }
                if (!restored)
                    Log("30 秒内未检测到连接（游戏可能仍在启动；下次启动会自动还原 PIN）");
            }
            catch (Exception ex)
            {
                Log("启动失败: " + ex.Message);
            }
            finally
            {
                SetButtonEnabled();
            }
        }

        private void SetButtonEnabled()
        {
            try
            {
                btnStart.BeginInvoke(new Action(delegate { btnStart.Enabled = true; }));
            }
            catch
            {
            }
        }

        // ============ PIN 处理 ============

        private void RemoveNgsOn(PINFile val)
        {
            if (val.BmlObjects == null)
                return;

            foreach (BmlObject bml in val.BmlObjects)
            {
                if (bml.Name == "extra" && bml.SubObjects != null)
                {
                    for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                    {
                        if (bml.SubObjects[i].Item1 == "NgsOn")
                        {
                            bml.SubObjects.RemoveAt(i);
                            Log("已移除 PIN 中的 NgsOn");
                            break;
                        }
                    }
                }
            }
        }

        private bool RestorePinFile(string pinFile, string pinFileBak)
        {
            try
            {
                if (string.IsNullOrEmpty(pinFileBak) || !File.Exists(pinFileBak))
                    return false;
                if (File.Exists(pinFile))
                    File.Delete(pinFile);
                File.Move(pinFileBak, pinFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============ TCP 检测（对齐原版精确匹配逻辑） ============

        private bool CheckTcpConnection(int processId, string serverIP, int serverPort)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process netstatProcess = Process.Start(psi))
                {
                    string output = netstatProcess.StandardOutput.ReadToEnd();
                    netstatProcess.WaitForExit();

                    string pidTag = " " + processId;
                    string epTag = serverIP + ":" + serverPort;
                    string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.Contains(pidTag) && line.Contains("ESTABLISHED") && line.Contains(epTag))
                            return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        // ============ 目录定位 ============

        private string FindKartRiderDirectory()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(appDir, "KartRider.exe")) && File.Exists(Path.Combine(appDir, "KartRider.pin")))
                return appDir;

            try
            {
                string gp = (string)Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\TCGame\kart", "gamepath", null);
                if (!string.IsNullOrEmpty(gp) &&
                    File.Exists(Path.Combine(gp, "KartRider.exe")) &&
                    File.Exists(Path.Combine(gp, "KartRider.pin")))
                    return Path.GetFullPath(gp);
            }
            catch
            {
            }
            return null;
        }

        // ============ 配置保存（MiniLauncher.ini, UTF-8） ============

        private void SaveConfig(string ip, string port, string name)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniLauncher.ini");
                string content = "[Launcher]\r\n" +
                    "ServerIP=" + ip + "\r\n" +
                    "ServerPort=" + port + "\r\n" +
                    "Nickname=" + name + "\r\n";
                File.WriteAllText(path, content, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private void LoadConfig()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniLauncher.ini");
                if (!File.Exists(path))
                    return;

                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0)
                        continue;
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    if (key == "ServerIP")
                        txtIp.Text = value;
                    else if (key == "ServerPort")
                        txtPort.Text = value;
                    else if (key == "Nickname")
                        txtName.Text = value;
                }
            }
            catch
            {
            }
        }

        // ============ 工具 ============

        private static bool IsIPv6(string ip)
        {
            IPAddress addr;
            return IPAddress.TryParse(ip, out addr) &&
                addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        }

        private static string JsonEscape(string s)
        {
            if (s == null)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                            sb.Append(string.Format("\\u{0:X4}", (int)c));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private void Log(string msg)
        {
            if (txtLog == null || txtLog.IsDisposed)
                return;

            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n";
            if (txtLog.InvokeRequired)
            {
                try
                {
                    txtLog.BeginInvoke(new Action<string>(AppendLog), line);
                }
                catch
                {
                }
            }
            else
            {
                AppendLog(line);
            }
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText(line);
        }
    }
}
