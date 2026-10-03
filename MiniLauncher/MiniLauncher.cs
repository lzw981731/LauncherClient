// MiniLauncher —— 极简 KartRider 登录器（含账号注册/登录）
// 三个参数：服务器IP / 服务器端口 / 角色名称
// 传给 KartRider.exe 的内容与原版 Launcher_V2 完全一致：
//   1. 命令行: KartRider.exe TGC -region:3 -passport:<Base64 JSON>
//   2. PIN 文件: 登录服务器 LoginServers 改写 + 移除 NgsOn
//   3. 启动后: TCP 检测到连接即恢复原始 PIN 文件
// 新增：账号注册/登录（HTTP API :39314），登录后自动填入绑定昵称
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

        // 账号系统控件
        private TabControl tabMode;
        private TabPage tabDirect;
        private TabPage tabAccount;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtRegUser;
        private TextBox txtRegPass;
        private TextBox txtRegNick;
        private Button btnLogin;
        private Button btnRegister;
        private Label lblAccountStatus;

        // 账号 API 基地址（从 IP+端口 推导）
        private string AccountApiBase
        {
            get
            {
                string ip = txtIp.Text.Trim();
                string portStr = txtPort.Text.Trim();
                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(portStr))
                    return "";
                // 账号 API 端口 = 游戏端口 + 3
                ushort port;
                if (ushort.TryParse(portStr, out port))
                    return string.Format("http://{0}:{1}", ip, port + 3);
                return "";
            }
        }

        public MiniLauncherForm()
        {
            Text = "KartRider 登录器 Mini";
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(460, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            int y = 16;

            txtIp = NewTextBox();
            y = AddRow("服务器IP:", txtIp, y, 200);

            txtPort = NewTextBox();
            y = AddRow("服务器端口:", txtPort, y, 100);

            // 模式切换 TabControl
            tabMode = new TabControl();
            tabMode.Location = new Point(16, y);
            tabMode.Size = new Size(ClientSize.Width - 32, 200);
            tabMode.Font = Font;

            // ---- 直接登录 Tab ----
            tabDirect = new TabPage("直接登录");
            tabDirect.Font = Font;
            {
                int ty = 12;
                Label lb = new Label();
                lb.Text = "输入角色名称直接进入游戏（无需账号）";
                lb.AutoSize = true;
                lb.Location = new Point(12, ty);
                lb.ForeColor = Color.Gray;
                tabDirect.Controls.Add(lb);
                ty += 28;

                Label lbName = new Label();
                lbName.Text = "角色名称:";
                lbName.AutoSize = true;
                lbName.Location = new Point(12, ty + 3);
                tabDirect.Controls.Add(lbName);

                txtName = NewTextBox();
                txtName.Location = new Point(100, ty);
                txtName.Size = new Size(200, 24);
                tabDirect.Controls.Add(txtName);
            }
            tabMode.TabPages.Add(tabDirect);

            // ---- 账号登录 Tab ----
            tabAccount = new TabPage("账号登录");
            tabAccount.Font = Font;
            {
                int ty = 10;

                // 登录区
                Label lbLogin = new Label();
                lbLogin.Text = "── 登录 ──";
                lbLogin.AutoSize = true;
                lbLogin.Location = new Point(12, ty);
                lbLogin.Font = new Font(Font.FontFamily, 8f, FontStyle.Bold);
                tabAccount.Controls.Add(lbLogin);
                ty += 22;

                Label lbUser = new Label();
                lbUser.Text = "账号:";
                lbUser.AutoSize = true;
                lbUser.Location = new Point(12, ty + 3);
                tabAccount.Controls.Add(lbUser);

                txtUsername = NewTextBox();
                txtUsername.Location = new Point(70, ty);
                txtUsername.Size = new Size(150, 24);
                tabAccount.Controls.Add(txtUsername);
                ty += 30;

                Label lbPass = new Label();
                lbPass.Text = "密码:";
                lbPass.AutoSize = true;
                lbPass.Location = new Point(12, ty + 3);
                tabAccount.Controls.Add(lbPass);

                txtPassword = NewTextBox();
                txtPassword.Location = new Point(70, ty);
                txtPassword.Size = new Size(150, 24);
                txtPassword.UseSystemPasswordChar = true;
                tabAccount.Controls.Add(txtPassword);

                btnLogin = new Button();
                btnLogin.Text = "登录";
                btnLogin.Size = new Size(60, 26);
                btnLogin.Location = new Point(230, ty - 1);
                btnLogin.Font = new Font(Font.FontFamily, 8f);
                btnLogin.Click += delegate { DoLogin(); };
                tabAccount.Controls.Add(btnLogin);
                ty += 36;

                // 注册区
                Label lbReg = new Label();
                lbReg.Text = "── 注册 ──";
                lbReg.AutoSize = true;
                lbReg.Location = new Point(12, ty);
                lbReg.Font = new Font(Font.FontFamily, 8f, FontStyle.Bold);
                tabAccount.Controls.Add(lbReg);
                ty += 22;

                Label lbRU = new Label();
                lbRU.Text = "账号:";
                lbRU.AutoSize = true;
                lbRU.Location = new Point(12, ty + 3);
                tabAccount.Controls.Add(lbRU);

                txtRegUser = NewTextBox();
                txtRegUser.Location = new Point(70, ty);
                txtRegUser.Size = new Size(150, 24);
                tabAccount.Controls.Add(txtRegUser);
                ty += 30;

                Label lbRP = new Label();
                lbRP.Text = "密码:";
                lbRP.AutoSize = true;
                lbRP.Location = new Point(12, ty + 3);
                tabAccount.Controls.Add(lbRP);

                txtRegPass = NewTextBox();
                txtRegPass.Location = new Point(70, ty);
                txtRegPass.Size = new Size(150, 24);
                txtRegPass.UseSystemPasswordChar = true;
                tabAccount.Controls.Add(txtRegPass);
                ty += 30;

                Label lbRN = new Label();
                lbRN.Text = "昵称:";
                lbRN.AutoSize = true;
                lbRN.Location = new Point(12, ty + 3);
                tabAccount.Controls.Add(lbRN);

                txtRegNick = NewTextBox();
                txtRegNick.Location = new Point(70, ty);
                txtRegNick.Size = new Size(150, 24);
                tabAccount.Controls.Add(txtRegNick);

                btnRegister = new Button();
                btnRegister.Text = "注册";
                btnRegister.Size = new Size(60, 26);
                btnRegister.Location = new Point(230, ty - 1);
                btnRegister.Font = new Font(Font.FontFamily, 8f);
                btnRegister.Click += delegate { DoRegister(); };
                tabAccount.Controls.Add(btnRegister);
            }
            tabMode.TabPages.Add(tabAccount);

            Controls.Add(tabMode);
            y += tabMode.Height + 8;

            // 账号状态栏
            lblAccountStatus = new Label();
            lblAccountStatus.Text = "";
            lblAccountStatus.AutoSize = true;
            lblAccountStatus.Location = new Point(16, y);
            lblAccountStatus.Font = new Font(Font.FontFamily, 8f);
            lblAccountStatus.ForeColor = Color.DarkGreen;
            Controls.Add(lblAccountStatus);
            y += 20;

            btnStart = new Button();
            btnStart.Text = "启动游戏";
            btnStart.Size = new Size(130, 34);
            btnStart.Location = new Point(165, y + 2);
            btnStart.Font = Font;
            btnStart.Click += delegate { StartClicked(); };
            Controls.Add(btnStart);

            y += 46;

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Location = new Point(16, y);
            txtLog.Size = new Size(ClientSize.Width - 32, ClientSize.Height - y - 16);
            txtLog.Font = new Font("Consolas", 9f);
            Controls.Add(txtLog);

            // Tab 切换时同步 txtName
            tabMode.SelectedIndexChanged += delegate
            {
                if (tabMode.SelectedTab == tabDirect)
                {
                    lblAccountStatus.Text = "";
                }
            };

            LoadConfig();
            Log("就绪。选择「直接登录」或「账号登录」模式。");
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

        // ============ 账号 API ============

        private void DoLogin()
        {
            string apiBase = AccountApiBase;
            if (string.IsNullOrEmpty(apiBase))
            {
                Log("错误：请先填写服务器IP和端口");
                return;
            }

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Log("错误：请输入账号和密码");
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "登录中...";

            string json = string.Format("{{\"username\":\"{0}\",\"password\":\"{1}\"}}",
                JsonEscape(username), JsonEscape(password));

            Thread t = new Thread(delegate()
            {
                try
                {
                    string result = HttpPost(apiBase + "/login", json);
                    Log("登录响应: " + result);

                    // 简易 JSON 解析（不依赖 Newtonsoft/System.Text.Json）
                    if (ContainsValue(result, "\"ok\"", "true"))
                    {
                        string nickname = ExtractString(result, "nickname");
                        string token = ExtractString(result, "token");
                        Log("登录成功！绑定昵称: " + nickname);

                        SetAccountStatus("已登录: " + username + " → " + nickname);

                        // 自动填入昵称到直接登录框
                        SetTextSafe(txtName, nickname);
                        SetTextSafe(txtUsername, username);

                        // 保存账号信息到配置
                        SaveAccountConfig(username, nickname);
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        Log("登录失败: " + msg);
                        SetAccountStatus("登录失败");
                    }
                }
                catch (Exception ex)
                {
                    Log("登录请求失败: " + ex.Message);
                    SetAccountStatus("连接失败");
                }
                finally
                {
                    SetButtonSafe(btnLogin, true, "登录");
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void DoRegister()
        {
            string apiBase = AccountApiBase;
            if (string.IsNullOrEmpty(apiBase))
            {
                Log("错误：请先填写服务器IP和端口");
                return;
            }

            string username = txtRegUser.Text.Trim();
            string password = txtRegPass.Text.Trim();
            string nickname = txtRegNick.Text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(nickname))
            {
                Log("错误：请填写注册账号、密码和昵称");
                return;
            }

            btnRegister.Enabled = false;
            btnRegister.Text = "注册中...";

            string json = string.Format("{{\"username\":\"{0}\",\"password\":\"{1}\",\"nickname\":\"{2}\"}}",
                JsonEscape(username), JsonEscape(password), JsonEscape(nickname));

            Thread t = new Thread(delegate()
            {
                try
                {
                    string result = HttpPost(apiBase + "/register", json);
                    Log("注册响应: " + result);

                    if (ContainsValue(result, "\"ok\"", "true"))
                    {
                        Log("注册成功！账号: " + username + " 昵称: " + nickname);
                        SetAccountStatus("注册成功，请登录");

                        // 自动填入登录框
                        SetTextSafe(txtUsername, username);
                        SetTextSafe(txtPassword, "");
                        SetTextSafe(txtName, nickname);

                        // 保存账号信息
                        SaveAccountConfig(username, nickname);
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        Log("注册失败: " + msg);
                        SetAccountStatus("注册失败");
                    }
                }
                catch (Exception ex)
                {
                    Log("注册请求失败: " + ex.Message);
                    SetAccountStatus("连接失败");
                }
                finally
                {
                    SetButtonSafe(btnRegister, true, "注册");
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// 简易 HTTP POST（.NET Framework 4.x 兼容）
        /// </summary>
        private static string HttpPost(string url, string jsonBody)
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonBody);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.ContentLength = body.Length;
            req.Timeout = 10000; // 10秒超时
            using (Stream stream = req.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream respStream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(respStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// 简易 JSON 值检查（避免引入 JSON 库）
        /// </summary>
        private static bool ContainsValue(string json, string key, string value)
        {
            return json.IndexOf(key + ":" + value) >= 0 || json.IndexOf(key + " : " + value) >= 0;
        }

        /// <summary>
        /// 简易 JSON 字符串提取
        /// </summary>
        private static string ExtractString(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0)
            {
                search = "\"" + key + "\" : \"";
                idx = json.IndexOf(search, StringComparison.Ordinal);
            }
            if (idx < 0) return "";
            idx += search.Length;
            int end = json.IndexOf('"', idx);
            if (end < 0) return "";
            // 处理转义字符
            string raw = json.Substring(idx, end - idx);
            return raw.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private void SetAccountStatus(string text)
        {
            if (lblAccountStatus.InvokeRequired)
            {
                try { lblAccountStatus.BeginInvoke(new Action<string>(SetAccountStatus), text); } catch { }
            }
            else
            {
                lblAccountStatus.Text = text;
            }
        }

        private void SetTextSafe(TextBox tb, string text)
        {
            if (tb.InvokeRequired)
            {
                try { tb.BeginInvoke(new Action<TextBox, string>(SetTextSafe), tb, text); } catch { }
            }
            else
            {
                tb.Text = text;
            }
        }

        private void SetButtonSafe(Button btn, bool enabled, string text)
        {
            if (btn.InvokeRequired)
            {
                try { btn.BeginInvoke(new Action<Button, bool, string>(SetButtonSafe), btn, enabled, text); } catch { }
            }
            else
            {
                btn.Enabled = enabled;
                btn.Text = text;
            }
        }

        // ============ 启动 ============

        private void StartClicked()
        {
            if (!btnStart.Enabled)
                return;
            btnStart.Enabled = false;

            // 根据当前 Tab 决定昵称来源
            string nickname;
            if (tabMode.SelectedTab == tabAccount)
            {
                // 账号模式：使用登录后填入的昵称
                nickname = txtName.Text.Trim();
                if (string.IsNullOrEmpty(nickname))
                {
                    Log("错误：请先登录账号，或切换到「直接登录」模式");
                    SetButtonEnabled();
                    return;
                }
            }
            else
            {
                // 直接登录模式
                nickname = txtName.Text.Trim();
            }

            SaveConfig(txtIp.Text.Trim(), txtPort.Text.Trim(), nickname);

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
                string nickname;
                ushort serverPort;

                if (tabMode.SelectedTab == tabAccount)
                {
                    nickname = txtName.Text.Trim();
                }
                else
                {
                    nickname = txtName.Text.Trim();
                }

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

        /// <summary>
        /// 保存账号信息到配置（账号名+绑定昵称，不保存密码）
        /// </summary>
        private void SaveAccountConfig(string username, string nickname)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniLauncher.ini");
                string content = "[Launcher]\r\n" +
                    "ServerIP=" + txtIp.Text.Trim() + "\r\n" +
                    "ServerPort=" + txtPort.Text.Trim() + "\r\n" +
                    "Nickname=" + nickname + "\r\n" +
                    "Username=" + username + "\r\n";
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
                    else if (key == "Username")
                        txtUsername.Text = value;
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