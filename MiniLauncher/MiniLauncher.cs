// MiniLauncher v4 —— KartRider �账号登录器
// 配置文件读取 IP/端口，弹窗提示，注册/创角分离
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
        // 登录区
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Button btnStart;
        private LinkLabel linkRegister;
        private Button btnSettings;
        private Label lblStatus;
        private Label lblServerInfo;

        // 注册区（覆盖面板）
        private Panel panelRegister;
        private TextBox txtRegUser;
        private TextBox txtRegPass;
        private Button btnRegConfirm;
        private Button btnRegCancel;
        private Label lblRegHint;

        // 服务器配置（从 MiniLauncher.ini 读取）
        private string _serverIP = "";
        private ushort _serverPort = 39311;

        // 登录状态
        private string _boundNickname = "";
        private string _loggedInUser = "";
        private string _loginToken = "";

        // 账号 API 基地址
        private string AccountApiBase
        {
            get
            {
                if (string.IsNullOrEmpty(_serverIP)) return "";
                return string.Format("http://{0}:{1}", _serverIP, _serverPort + 3);
            }
        }

        public MiniLauncherForm()
        {
            Text = "KartRider 登录器";
            Font = new Font("Microsoft YaHei", 9f);
            ClientSize = new Size(360, 310);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            int y = 16;

            // ---- 服务器信息（只读显示）----
            lblServerInfo = new Label();
            lblServerInfo.Font = new Font(Font.FontFamily, 8f);
            lblServerInfo.ForeColor = Color.Gray;
            lblServerInfo.Location = new Point(24, y);
            lblServerInfo.Size = new Size(ClientSize.Width - 48, 18);
            lblServerInfo.AutoSize = false;
            Controls.Add(lblServerInfo);
            y += 22;

            // ---- 分隔线 ----
            Panel sep0 = new Panel();
            sep0.Height = 1;
            sep0.BackColor = Color.LightGray;
            sep0.Location = new Point(24, y);
            sep0.Size = new Size(ClientSize.Width - 48, 1);
            Controls.Add(sep0);
            y += 8;

            // ---- 账号登录 ----
            Label lbAccount = new Label();
            lbAccount.Text = "账号登录";
            lbAccount.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            lbAccount.Location = new Point(24, y);
            lbAccount.AutoSize = true;
            Controls.Add(lbAccount);
            y += 30;

            txtUser = NewTextBox();
            y = AddRow("账号:", txtUser, y, 200);

            txtPass = NewTextBox();
            txtPass.UseSystemPasswordChar = true;
            y = AddRow("密码:", txtPass, y, 200);
            y += 6;

            // 登录按钮
            btnLogin = new Button();
            btnLogin.Text = "登 录";
            btnLogin.Size = new Size(200, 32);
            btnLogin.Location = new Point(80, y);
            btnLogin.Font = new Font(Font.FontFamily, 10f);
            btnLogin.Click += delegate { DoLogin(); };
            Controls.Add(btnLogin);
            y += 42;

            // 启动按钮
            btnStart = new Button();
            btnStart.Text = "▶ 启动游戏";
            btnStart.Size = new Size(200, 36);
            btnStart.Location = new Point(80, y);
            btnStart.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            btnStart.BackColor = Color.FromArgb(76, 175, 80);
            btnStart.ForeColor = Color.White;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += delegate { StartClicked(); };
            Controls.Add(btnStart);
            y += 46;

            // 底部：注册链接 + 设置按钮
            linkRegister = new LinkLabel();
            linkRegister.Text = "注册新账号";
            linkRegister.Font = new Font(Font.FontFamily, 9f);
            linkRegister.Location = new Point(24, y);
            linkRegister.Size = new Size(90, 24);
            linkRegister.LinkColor = Color.SteelBlue;
            linkRegister.LinkClicked += delegate { ShowRegisterPanel(); };
            Controls.Add(linkRegister);

            btnSettings = new Button();
            btnSettings.Text = "设置";
            btnSettings.Size = new Size(50, 26);
            btnSettings.Location = new Point(ClientSize.Width - 78, y);
            btnSettings.Font = new Font(Font.FontFamily, 8.5f);
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.BackColor = Color.FromArgb(240, 240, 240);
            btnSettings.Click += delegate { ShowSettingsDialog(); };
            Controls.Add(btnSettings);
            y += 32;

            // �状态标签
            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Font = new Font(Font.FontFamily, 8f);
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(24, y);
            lblStatus.Size = new Size(ClientSize.Width - 48, 20);
            Controls.Add(lblStatus);

            // ---- 注册面板（初始隐藏）----
            panelRegister = new Panel();
            panelRegister.Location = new Point(0, 0);
            panelRegister.Size = new Size(ClientSize.Width, ClientSize.Height);
            panelRegister.BackColor = Color.White;
            panelRegister.Visible = false;
            Controls.Add(panelRegister);
            panelRegister.BringToFront();

            int ry = 60;

            Label lbRegTitle = new Label();
            lbRegTitle.Text = "注册新账号";
            lbRegTitle.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            lbRegTitle.Location = new Point(80, ry);
            lbRegTitle.AutoSize = true;
            panelRegister.Controls.Add(lbRegTitle);
            ry += 40;

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
            ry += 36;

            lblRegHint = new Label();
            lblRegHint.Text = "注册后需创建角色才能进入游戏";
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

            // 加载配置
            LoadConfig();
            UpdateServerInfo();

            // 检查服务器配置
            if (string.IsNullOrEmpty(_serverIP))
            {
                BeginInvoke(new Action(delegate
                {
                    MessageBox.Show("未配置服务器地址，请在设置中填写服务器IP和端口。",
                        "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ShowSettingsDialog();
                }));
            }
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

        private void UpdateServerInfo()
        {
            if (string.IsNullOrEmpty(_serverIP))
                lblServerInfo.Text = "服务器: 未配置";
            else
                lblServerInfo.Text = string.Format("服务器: {0}:{1}", _serverIP, _serverPort);
        }

        // ============ 设置对话框 ============

        private void ShowSettingsDialog()
        {
            Form dlg = new Form();
            dlg.Text = "服务器设置";
            dlg.Font = new Font("Microsoft YaHei", 9f);
            dlg.ClientSize = new Size(320, 150);
            dlg.FormBorderStyle = FormBorderStyle.FixedSingle;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ShowInTaskbar = false;

            Label lbIP = new Label();
            lbIP.Text = "服务器IP:";
            lbIP.AutoSize = true;
            lbIP.Location = new Point(20, 20);
            dlg.Controls.Add(lbIP);

            TextBox txtIP = new TextBox();
            txtIP.Text = _serverIP;
            txtIP.Location = new Point(100, 17);
            txtIP.Size = new Size(200, 24);
            dlg.Controls.Add(txtIP);

            Label lbPort = new Label();
            lbPort.Text = "端口:";
            lbPort.AutoSize = true;
            lbPort.Location = new Point(20, 54);
            dlg.Controls.Add(lbPort);

            TextBox txtPort = new TextBox();
            txtPort.Text = _serverPort.ToString();
            txtPort.Location = new Point(100, 51);
            txtPort.Size = new Size(80, 24);
            dlg.Controls.Add(txtPort);

            Button btnOK = new Button();
            btnOK.Text = "确定";
            btnOK.Size = new Size(100, 30);
            btnOK.Location = new Point(60, 100);
            btnOK.Click += delegate
            {
                string ip = txtIP.Text.Trim();
                string portStr = txtPort.Text.Trim();
                ushort port;
                if (string.IsNullOrEmpty(ip))
                {
                    MessageBox.Show("请输入服务器IP", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ushort.TryParse(portStr, out port))
                {
                    MessageBox.Show("端口格式错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _serverIP = ip;
                _serverPort = port;
                UpdateServerInfo();
                SaveConfig();
                dlg.Close();
            };
            dlg.Controls.Add(btnOK);

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Size = new Size(80, 30);
            btnCancel.Location = new Point(180, 100);
            btnCancel.Click += delegate { dlg.Close(); };
            dlg.Controls.Add(btnCancel);

            dlg.AcceptButton = btnOK;
            dlg.CancelButton = btnCancel;

            dlg.ShowDialog(this);
        }

        // ============ 注册面板显隐 ============

        private void ShowRegisterPanel()
        {
            txtRegUser.Text = "";
            txtRegPass.Text = "";
            lblRegHint.Text = "注册后需创建角色才能进入游戏";
            lblRegHint.ForeColor = Color.Gray;
            panelRegister.Visible = true;
            txtRegUser.Focus();
        }

        private void HideRegisterPanel()
        {
            panelRegister.Visible = false;
        }

        // ============ 创建角色对话框 ============

        private void PromptCreateCharacter()
        {
            DialogResult result = MessageBox.Show(
                "当前账号没有角色，是否创建角色？\n\n角色名 = 游戏内显示名称，创建后不可修改",
                "创建角色", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ShowCreateCharDialog();
            }
        }

        private void ShowCreateCharDialog()
        {
            Form dlg = new Form();
            dlg.Text = "创建角色";
            dlg.Font = new Font("Microsoft YaHei", 9f);
            dlg.ClientSize = new Size(320, 170);
            dlg.FormBorderStyle = FormBorderStyle.FixedSingle;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ShowInTaskbar = false;

            Label lbHint = new Label();
            lbHint.Text = "请输入角色名（游戏内显示名称，创建后不可修改）";
            lbHint.Font = new Font(dlg.Font.FontFamily, 8.5f);
            lbHint.ForeColor = Color.Gray;
            lbHint.Location = new Point(20, 16);
            lbHint.Size = new Size(280, 30);
            dlg.Controls.Add(lbHint);

            Label lbName = new Label();
            lbName.Text = "角色名:";
            lbName.AutoSize = true;
            lbName.Location = new Point(20, 60);
            dlg.Controls.Add(lbName);

            TextBox txtName = new TextBox();
            txtName.Location = new Point(80, 57);
            txtName.Size = new Size(220, 24);
            txtName.MaxLength = 16;
            dlg.Controls.Add(txtName);

            Button btnOK = new Button();
            btnOK.Text = "确定";
            btnOK.Size = new Size(100, 30);
            btnOK.Location = new Point(60, 110);
            btnOK.Click += delegate
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("角色名不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (name.Length > 16)
                {
                    MessageBox.Show("角色名最长16字符", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                dlg.Tag = name;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            dlg.Controls.Add(btnOK);

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Size = new Size(80, 30);
            btnCancel.Location = new Point(180, 110);
            btnCancel.Click += delegate { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
            dlg.Controls.Add(btnCancel);

            dlg.AcceptButton = btnOK;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string nickname = dlg.Tag as string;
                if (!string.IsNullOrEmpty(nickname))
                {
                    DoCreateCharacter(nickname);
                }
            }
        }

        // ============ 账号 API ============

        private void DoLogin()
        {
            string apiBase = AccountApiBase;
            if (string.IsNullOrEmpty(apiBase))
            {
                MessageBox.Show("未配置服务器地址，请点击「设置」配置。", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string username = txtUser.Text.Trim();
            string password = txtPass.Text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入账号和密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "登录中...";
            lblStatus.Text = "正在登录...";

            string json = string.Format("{{\"username\":\"{0}\",\"password\":\"{1}\"}}",
                JsonEscape(username), JsonEscape(password));

            Thread t = new Thread(delegate()
            {
                try
                {
                    string result = HttpPost(apiBase + "/login", json);

                    if (ContainsValue(result, "\"ok\"", "true"))
                    {
                        string nickname = ExtractString(result, "nickname");
                        bool hasNickname = ContainsValue(result, "\"hasNickname\"", "true");
                        string token = ExtractString(result, "token");

                        _boundNickname = nickname;
                        _loggedInUser = username;
                        _loginToken = token;
                        SaveConfig();

                        if (hasNickname)
                        {
                            SetTextSafe(lblStatus, "已登录: " + username + " → " + nickname);
                        }
                        else
                        {
                            SetTextSafe(lblStatus, "已登录: " + username + "（无角色）");
                            BeginInvoke(new Action(delegate
                            {
                                PromptCreateCharacter();
                            }));
                        }
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        SetTextSafe(lblStatus, "登录失败");
                        BeginInvoke(new Action(delegate
                        {
                            MessageBox.Show(msg, "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    SetTextSafe(lblStatus, "连接失败");
                    BeginInvoke(new Action(delegate
                    {
                        MessageBox.Show("连接失败: " + ex.Message, "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
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
                BeginInvoke(new Action(delegate
                {
                    MessageBox.Show("未配置服务器地址，请点击「设置」配置。", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }

            string username = txtRegUser.Text.Trim();
            string password = txtRegPass.Text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblRegHint.Text = "请填写账号和密码";
                lblRegHint.ForeColor = Color.Red;
                return;
            }
            if (password.Length < 4)
            {
                lblRegHint.Text = "密码至少4位";
                lblRegHint.ForeColor = Color.Red;
                return;
            }

            btnRegConfirm.Enabled = false;
            btnRegConfirm.Text = "注册中...";
            lblRegHint.Text = "正在注册...";
            lblRegHint.ForeColor = Color.Gray;

            // 注册不传昵称（先注册，登录后创角）
            string json = string.Format("{{\"username\":\"{0}\",\"password\":\"{1}\"}}",
                JsonEscape(username), JsonEscape(password));

            Thread t = new Thread(delegate()
            {
                try
                {
                    string result = HttpPost(apiBase + "/register", json);

                    if (ContainsValue(result, "\"ok\"", "true"))
                    {
                        SetTextSafe(txtUser, username);
                        SetTextSafe(txtPass, "");
                        SetTextSafe(lblStatus, "注册成功: " + username);
                        SaveConfig();

                        Thread.Sleep(500);
                        HideRegisterPanelSafe();

                        BeginInvoke(new Action(delegate
                        {
                            MessageBox.Show("注册成功！请登录账号后创建角色。",
                                "注册成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        SetTextSafe(lblRegHint, "注册失败: " + msg);
                        SetColorSafe(lblRegHint, Color.Red);
                    }
                }
                catch (Exception ex)
                {
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

        private void DoCreateCharacter(string nickname)
        {
            string apiBase = AccountApiBase;
            if (string.IsNullOrEmpty(apiBase) || string.IsNullOrEmpty(_loginToken))
            {
                MessageBox.Show("请先登录账号", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblStatus.Text = "正在创建角色...";

            string json = string.Format("{{\"token\":\"{0}\",\"nickname\":\"{1}\"}}",
                JsonEscape(_loginToken), JsonEscape(nickname));

            Thread t = new Thread(delegate()
            {
                try
                {
                    string result = HttpPost(apiBase + "/create-character", json);

                    if (ContainsValue(result, "\"ok\"", "true"))
                    {
                        _boundNickname = nickname;
                        SetTextSafe(lblStatus, "角色创建成功: " + nickname);
                        SaveConfig();

                        BeginInvoke(new Action(delegate
                        {
                            MessageBox.Show("角色创建成功！角色名: " + nickname + "\n现在可以启动游戏了。",
                                "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    else
                    {
                        string msg = ExtractString(result, "msg");
                        SetTextSafe(lblStatus, "角色创建失败");
                        BeginInvoke(new Action(delegate
                        {
                            MessageBox.Show("角色创建失败: " + msg, "错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    SetTextSafe(lblStatus, "连接失败");
                    BeginInvoke(new Action(delegate
                    {
                        MessageBox.Show("连接失败: " + ex.Message, "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
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

            if (string.IsNullOrEmpty(_boundNickname))
            {
                if (string.IsNullOrEmpty(_loggedInUser))
                    MessageBox.Show("请先登录账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("当前账号没有角色，请先创建角色", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            SaveConfig();

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
                string serverIP = _serverIP;
                ushort serverPort = _serverPort;

                if (string.IsNullOrEmpty(serverIP))
                {
                    BeginInvoke(new Action(delegate
                    {
                        MessageBox.Show("未配置服务器地址", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                SetTextSafe(lblStatus, "正在查找游戏目录...");
                string root = FindKartRiderDirectory();
                if (root == null)
                {
                    BeginInvoke(new Action(delegate
                    {
                        MessageBox.Show("找不到 KartRider.exe / KartRider.pin\n\n请把 MiniLauncher.exe 放到游戏目录；或先运行过原版登录器（注册表 TCGame\\kart\\gamepath）",
                            "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                string pinFile = Path.Combine(root, "KartRider.pin");
                string pinFileBak = Path.Combine(root, "KartRider-bak.pin");

                if (File.Exists(pinFileBak))
                {
                    if (File.Exists(pinFile)) File.Delete(pinFile);
                    File.Move(pinFileBak, pinFile);
                }

                PINFile val = new PINFile(pinFile);
                ushort clientVersion = val.Header.MinorVersion;

                File.Copy(pinFile, pinFileBak, true);

                string ip = IsIPv6(serverIP) ? "127.0.0.1" : serverIP;
                if (val.AuthMethods != null)
                {
                    foreach (PINFile.AuthMethod am in val.AuthMethods)
                    {
                        am.LoginServers.Clear();
                        am.LoginServers.Add(new PINFile.IPEndPoint { IP = ip, Port = serverPort });
                    }
                }

                RemoveNgsOn(val);

                File.WriteAllBytes(pinFile, val.GetEncryptedData());

                string json = string.Format(
                    "{{\"Nickname\":\"{0}\",\"ClientVersion\":{1},\"CompileTime\":\"{2}\"}}",
                    JsonEscape(nickname), clientVersion, CompileTime.Time);
                string passport = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                string args = string.Format("TGC -region:3 -passport:{0}", passport);

                SetTextSafe(lblStatus, "正在启动 KartRider.exe...");

                ProcessStartInfo psi = new ProcessStartInfo("KartRider.exe", args)
                {
                    WorkingDirectory = Path.GetFullPath(root),
                    UseShellExecute = true
                };
                Process proc = Process.Start(psi);
                int pid = proc.Id;
                proc.Dispose();

                SetTextSafe(lblStatus, string.Format("KartRider.exe 已启动 (PID={0})，等待连接...", pid));

                bool restored = false;
                for (int i = 0; i < 30; i++)
                {
                    Thread.Sleep(1000);
                    if (CheckTcpConnection(pid, ip, serverPort))
                    {
                        if (RestorePinFile(pinFile, pinFileBak))
                        {
                            SetTextSafe(lblStatus, "已连接服务器，PIN 已恢复");
                            restored = true;
                            break;
                        }
                    }
                }
                if (!restored)
                    SetTextSafe(lblStatus, "30秒内未检测到连接（下次启动会自动还原 PIN）");
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(delegate
                {
                    MessageBox.Show("启动失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
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

        private void SaveConfig()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniLauncher.ini");
                string content = "[Launcher]\r\n" +
                    "ServerIP=" + _serverIP + "\r\n" +
                    "ServerPort=" + _serverPort + "\r\n" +
                    "Username=" + _loggedInUser + "\r\n" +
                    "Nickname=" + _boundNickname + "\r\n";
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
                    if (key == "ServerIP") _serverIP = value;
                    else if (key == "ServerPort") { ushort p; if (ushort.TryParse(value, out p)) _serverPort = p; }
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
    }
}