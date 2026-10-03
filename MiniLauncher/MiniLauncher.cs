// MiniLauncher —— KartRider 账号登录器
// 账号注册/登录（HTTP API :39314），登录后自动使用绑定昵称启动游戏
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
        // 服务器设置
        private TextBox txtIp;
        private TextBox txtPort;

        // 登录区
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Button btnStart;
        private LinkLabel linkRegister;

        // 注册区（覆盖在登录区上方）
        private Panel panelRegister;
        private TextBox txtRegUser;
        private TextBox txtRegPass;
        private TextBox txtRegNick;
        private Button btnRegConfirm;
        private Button btnRegCancel;
        private Label lblRegHint;

        // 日志
        private TextBox txtLog;

        // 登录后绑定的昵称
        private string _boundNickname = "";
        private string _loggedInUser = "";

        // 账号 API 基地址
        private string AccountApiBase
        {
            get
            {
                string ip = txtIp.Text.Trim();
                string portStr = txtPort.Text.Trim();
                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(portStr)) return "";
                ushort port;
                if (ushort.TryParse(portStr, out port))
                    return string.Format("http://{0}:{1}", ip, port + 3);
                return "";
            }
        }

        public MiniLauncherForm()
        {
            Text = "KartRider 登录器";
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(400, 480);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            int y = 14;

            // ---- 服务器设置 ----
            Label lbServer = new Label();
            lbServer.Text = "服务器设置";
            lbServer.Font = new Font(Font.FontFamily, 8f, FontStyle.Bold);
            lbServer.ForeColor = Color.Gray;
            lbServer.Location = new Point(16, y);
            lbServer.AutoSize = true;
            Controls.Add(lbServer);
            y += 20;

            txtIp = NewTextBox();
            y = AddRow("IP:", txtIp, y, 170);

            txtPort = NewTextBox();
            txtPort.Text = "39311";
            y = AddRow("端口:", txtPort, y, 80);
            y += 6;

            // ---- 分隔线 ----
            Panel sep1 = new Panel();
            sep1.Height = 1;
            sep1.BackColor = Color.LightGray;
            sep1.Location = new Point(16, y);
            sep1.Size = new Size(ClientSize.Width - 32, 1);
            Controls.Add(sep1);
            y += 10;

            // ---- 账号登录 ----
            Label lbAccount = new Label();
            lbAccount.Text = "账号登录";
            lbAccount.Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold);
            lbAccount.Location = new Point(16, y);
            lbAccount.AutoSize = true;
            Controls.Add(lbAccount);
            y += 28;

            txtUser = NewTextBox();
            y = AddRow("账号:", txtUser, y, 200);

            txtPass = NewTextBox();
            txtPass.UseSystemPasswordChar = true;
            y = AddRow("密码:", txtPass, y, 200);
            y += 4;

            // 登录按钮
            btnLogin = new Button();
            btnLogin.Text = "登 录";
            btnLogin.Size = new Size(200, 32);
            btnLogin.Location = new Point(100, y);
            btnLogin.Font = new Font(Font.FontFamily, 10f);
            btnLogin.Click += delegate { DoLogin(); };
            Controls.Add(btnLogin);
            y += 40;

            // 注册链接（右下角）
            linkRegister = new LinkLabel();
            linkRegister.Text = "注册新账号";
            linkRegister.Font = new Font(Font.FontFamily, 9f);
            linkRegister.Location = new Point(ClientSize.Width - 100, y - 30);
            linkRegister.Size = new Size(90, 24);
            linkRegister.LinkColor = Color.SteelBlue;
            linkRegister.LinkClicked += delegate { ShowRegisterPanel(); };
            Controls.Add(linkRegister);
            y += 8;

            // ---- 分隔线 ----
            Panel sep2 = new Panel();
            sep2.Height = 1;
            sep2.BackColor = Color.LightGray;
            sep2.Location = new Point(16, y);
            sep2.Size = new Size(ClientSize.Width - 32, 1);
            Controls.Add(sep2);
            y += 10;

            // ---- 启动按钮 ----
            btnStart = new Button();
            btnStart.Text = "▶ 启动游戏";
            btnStart.Size = new Size(200, 38);
            btnStart.Location = new Point(100, y);
            btnStart.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            btnStart.BackColor = Color.FromArgb(76, 175, 80);
            btnStart.ForeColor = Color.White;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += delegate { StartClicked(); };
            Controls.Add(btnStart);
            y += 50;

            // ---- 日志 ----
            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Location = new Point(16, y);
            txtLog.Size = new Size(ClientSize.Width - 32, ClientSize.Height - y - 14);
            txtLog.Font = new Font("Consolas", 8.5f);
            Controls.Add(txtLog);

            // ---- 注册面板（初始隐藏，覆盖在登录区上方）----
            panelRegister = new Panel();
            panelRegister.Location = new Point(0, 0);
            panelRegister.Size = new Size(ClientSize.Width, ClientSize.Height);
            panelRegister.BackColor = Color.White;
            panelRegister.Visible = false;
            Controls.Add(panelRegister);
            // 确保在最上层
            panelRegister.BringToFront();

            int ry = 60;

            Label lbRegTitle = new Label();
            lbRegTitle.Text = "注册新账号";
            lbRegTitle.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            lbRegTitle.Location = new Point(80, ry);
            lbRegTitle.AutoSize = true;
            panelRegister.Controls.Add(lbRegTitle);
            ry += 36;

            Label lbRU = new Label();
            lbRU.Text = "账号:";
            lbRU.AutoSize = true;
            lbRU.Location = new Point(40, ry + 3);
            panelRegister.Controls.Add(lbRU);

            txtRegUser = NewTextBox();
            txtRegUser.Location = new Point(100, ry);
            txtRegUser.Size = new Size(200, 24);
            panelRegister.Controls.Add(txtRegUser);
            ry += 32;

            Label lbRP = new Label();
            lbRP.Text = "密码:";
            lbRP.AutoSize = true;
            lbRP.Location = new Point(40, ry + 3);
            panelRegister.Controls.Add(lbRP);

            txtRegPass = NewTextBox();
            txtRegPass.Location = new Point(100, ry);
            txtRegPass.Size = new Size(200, 24);
            txtRegPass.UseSystemPasswordChar = true;
            panelRegister.Controls.Add(txtRegPass);
            ry += 32;

            Label lbRN = new Label();
            lbRN.Text = "昵称:";
            lbRN.AutoSize = true;
            lbRN.Location = new Point(40, ry + 3);
            panelRegister.Controls.Add(lbRN);

            txtRegNick = NewTextBox();
            txtRegNick.Location = new Point(100, ry);
            txtRegNick.Size = new Size(200, 24);
            panelRegister.Controls.Add(txtRegNick);
            ry += 30;

            lblRegHint = new Label();
            lblRegHint.Text = "昵称 = 游戏内角色名，注册后不可修改";
            lblRegHint.Font = new Font(Font.FontFamily, 8f);
            lblRegHint.ForeColor = Color.Gray;
            lblRegHint.AutoSize = true;
            lblRegHint.Location = new Point(40, ry);
            panelRegister.Controls.Add(lblRegHint);
            ry += 30;

            btnRegConfirm = new Button();
            btnRegConfirm.Text = "确认注册";
            btnRegConfirm.Size = new Size(120, 30);
            btnRegConfirm.Location = new Point(60, ry);
            btnRegConfirm.Font = new Font(Font.FontFamily, 9f);
            btnRegConfirm.Click += delegate { DoRegister(); };
            panelRegister.Controls.Add(btnRegConfirm);

            btnRegCancel = new Button();
            btnRegCancel.Text = "取消";
            btnRegCancel.Size = new Size(80, 30);
            btnRegCancel.Location = new Point(200, ry);
            btnRegCancel.Font = new Font(Font.FontFamily, 9f);
            btnRegCancel.Click += delegate { HideRegisterPanel(); };
            panelRegister.Controls.Add(btnRegCancel);

            LoadConfig();
            Log("就绪。请登录账号后启动游戏。");
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
            lb.Location = new Point(40, y + 3);
            Controls.Add(lb);

            input.Location = new Point(100, y);
            input.Size = new Size(width, 24);
            Controls.Add(input);
            return y + 30;
        }

        // ============ 注册面板显隐 ============

        private void ShowRegisterPanel()
        {
            txtRegUser.Text = "";
            txtRegPass.Text = "";
            txtRegNick.Text = "";
            lblRegHint.Text = "昵称 = 游戏内角色名，注册后不可修改";
            lblRegHint.ForeColor = Color.Gray;
            panelRegister.Visible = true;
            txtRegUser.Focus();
        }

        private void HideRegisterPanel()
        {
            panelRegister.Visible = false;
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

            string username = txtUser.Text.Trim();
            string password = txtPass.Text.Trim();
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

                    if (ContainsValue(result, "\"ok\"", "true"))
                    {
                        string nickname = ExtractString(result, "nickname");
                        _boundNickname = nickname;
                        _loggedInUser = username;
                        Log("登录成功！绑定角色: " + nickname);
                        SetTextSafe(lblRegHint, "✓ 已登录: " + username + " → " + nickname);
                        SetColorSafe(lblRegHint, Color.DarkGreen);
                        SaveConfig(txtIp.Text.Trim(), txtPort.Text.Trim(), username, nickname);
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        Log("登录失败: " + msg);
                    }
                }
                catch (Exception ex)
                {
                    Log("登录请求失败: " + ex.Message);
                }
                finally
                {
                    SetButtonSafe(btnLogin, true, "登 录");
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
                lblRegHint.Text = "请填写所有字段";
                lblRegHint.ForeColor = Color.Red;
                return;
            }
            if (password.Length < 4)
            {
                lblRegHint.Text = "密码至少4位";
                lblRegHint.ForeColor = Color.Red;
                return;
            }
            if (nickname.Length > 16)
            {
                lblRegHint.Text = "昵称最长16字符";
                lblRegHint.ForeColor = Color.Red;
                return;
            }

            btnRegConfirm.Enabled = false;
            btnRegConfirm.Text = "注册中...";

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
                        Log("注册成功！账号: " + username + " 角色: " + nickname);
                        _boundNickname = nickname;
                        _loggedInUser = username;
                        SetTextSafe(lblRegHint, "✓ 注册成功！角色: " + nickname);
                        SetColorSafe(lblRegHint, Color.DarkGreen);
                        SetTextSafe(txtUser, username);
                        SetTextSafe(txtPass, "");
                        SaveConfig(txtIp.Text.Trim(), txtPort.Text.Trim(), username, nickname);

                        // 注册成功后自动关闭注册面板
                        Thread.Sleep(800);
                        HideRegisterPanelSafe();
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        Log("注册失败: " + msg);
                        SetTextSafe(lblRegHint, "注册失败: " + msg);
                        SetColorSafe(lblRegHint, Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    Log("注册请求失败: " + ex.Message);
                    SetTextSafe(lblRegHint, "连接失败: " + ex.Message);
                    SetColorSafe(lblRegHint, Color.Red);
                }
                finally
                {
                    SetButtonSafe(btnRegConfirm, true, "确认注册");
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
            req.Timeout = 10000;
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

        private static bool ContainsValue(string json, string key, string value)
        {
            return json.IndexOf(key + ":" + value) >= 0 || json.IndexOf(key + " : " + value) >= 0;
        }

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
            string raw = json.Substring(idx, end - idx);
            return raw.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private void SetTextSafe(Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                try { ctrl.BeginInvoke(new Action<Control, string>(SetTextSafe), ctrl, text); } catch { }
            }
            else
            {
                ctrl.Text = text;
            }
        }

        private void SetColorSafe(Control ctrl, Color color)
        {
            if (ctrl.InvokeRequired)
            {
                try { ctrl.BeginInvoke(new Action<Control, Color>(SetColorSafe), ctrl, color); } catch { }
            }
            else
            {
                ctrl.ForeColor = color;
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

        private void HideRegisterPanelSafe()
        {
            if (panelRegister.InvokeRequired)
            {
                try { panelRegister.BeginInvoke(new Action(HideRegisterPanelSafe)); } catch { }
            }
            else
            {
                panelRegister.Visible = false;
            }
        }

        // ============ 启动游戏 ============

        private void StartClicked()
        {
            if (!btnStart.Enabled) return;

            // 必须先登录
            if (string.IsNullOrEmpty(_boundNickname))
            {
                Log("请先登录账号，或注册新账号");
                return;
            }

            btnStart.Enabled = false;

            SaveConfig(txtIp.Text.Trim(), txtPort.Text.Trim(), _loggedInUser, _boundNickname);

            string nickname = _boundNickname;
            Thread t = new Thread(delegate() { LaunchGame(nickname); });
            t.IsBackground = true;
            t.Name = "LaunchThread";
            t.Start();
        }

        private void LaunchGame(string nickname)
        {
            try
            {
                string serverIP = txtIp.Text.Trim();
                string serverPortStr = txtPort.Text.Trim();
                ushort serverPort;

                if (string.IsNullOrEmpty(serverIP) || string.IsNullOrEmpty(serverPortStr))
                {
                    Log("错误：请填写服务器IP和端口");
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

                if (File.Exists(pinFileBak))
                {
                    if (File.Exists(pinFile)) File.Delete(pinFile);
                    File.Move(pinFileBak, pinFile);
                    Log("已还原上次残留的 PIN 备份");
                }

                PINFile val = new PINFile(pinFile);
                ushort clientVersion = val.Header.MinorVersion;

                File.Copy(pinFile, pinFileBak, true);
                Log("已备份 PIN -> KartRider-bak.pin");

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

                RemoveNgsOn(val);

                File.WriteAllBytes(pinFile, val.GetEncryptedData());
                Log("PIN 文件已写回");

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

                Log("等待游戏连接服务器...");
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
                    Log("30 秒内未检测到连接（下次启动会自动还原 PIN）");
            }
            catch (Exception ex)
            {
                Log("启动失败: " + ex.Message);
            }
            finally
            {
                SetButtonSafe(btnStart, true, "▶ 启动游戏");
            }
        }

        // ============ PIN 处理 ============

        private void RemoveNgsOn(PINFile val)
        {
            if (val.BmlObjects == null) return;
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
                if (string.IsNullOrEmpty(pinFileBak) || !File.Exists(pinFileBak)) return false;
                if (File.Exists(pinFile)) File.Delete(pinFile);
                File.Move(pinFileBak, pinFile);
                return true;
            }
            catch { return false; }
        }

        // ============ TCP 检测 ============

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
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    string pidTag = " " + processId;
                    string epTag = serverIP + ":" + serverPort;
                    foreach (string line in output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Contains(pidTag) && line.Contains("ESTABLISHED") && line.Contains(epTag))
                            return true;
                    }
                }
            }
            catch { }
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
            catch { }
            return null;
        }

        // ============ 配置保存（MiniLauncher.ini, UTF-8） ============

        private void SaveConfig(string ip, string port, string username, string nickname)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniLauncher.ini");
                string content = "[Launcher]\r\n" +
                    "ServerIP=" + ip + "\r\n" +
                    "ServerPort=" + port + "\r\n" +
                    "Username=" + username + "\r\n" +
                    "Nickname=" + nickname + "\r\n";
                File.WriteAllText(path, content, Encoding.UTF8);
            }
            catch { }
        }

        private void LoadConfig()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniLauncher.ini");
                if (!File.Exists(path)) return;
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    if (key == "ServerIP") txtIp.Text = value;
                    else if (key == "ServerPort") txtPort.Text = value;
                    else if (key == "Username") txtUser.Text = value;
                    else if (key == "Nickname") { _boundNickname = value; }
                }
            }
            catch { }
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
            if (s == null) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append(string.Format("\\u{0:X4}", (int)c));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private void Log(string msg)
        {
            if (txtLog == null || txtLog.IsDisposed) return;
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n";
            if (txtLog.InvokeRequired)
            {
                try { txtLog.BeginInvoke(new Action<string>(AppendLog), line); } catch { }
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