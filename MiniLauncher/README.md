# MiniLauncher —— 极简 KartRider 登录器

只保留三个参数：**服务器IP / 服务器端口 / 角色名称**，其余逻辑与原版 Launcher_V2 完全一致。

## 编译（Windows 上操作）

1. 把整个 `MiniLauncher` 文件夹复制到 Windows 任意位置
2. 双击 `编译.bat`
3. 生成 `MiniLauncher.exe`（用系统自带 .NET Framework csc.exe，**不需要 Visual Studio / NuGet**）

## 使用

- 把 `MiniLauncher.exe` 放到游戏目录（和 `KartRider.exe` / `KartRider.pin` 同目录）
- 或先运行过原版登录器（程序会自动读注册表 `HKCU\Software\TCGame\kart\gamepath`）
- 填写三个参数，点「启动游戏」即可
- 参数会自动保存到同目录 `MiniLauncher.ini`，下次启动自动填充

## 传给 KartRider.exe 的内容（与原版一致）

1. **命令行**：`KartRider.exe TGC -region:3 -passport:<Base64 JSON>`
   - JSON = `{"Nickname":"角色名","ClientVersion":<PIN里的版本>,"CompileTime":"编译日期"}`
2. **PIN 文件**（`KartRider.pin`）：
   - 每个 AuthMethod 的 `LoginServers` 改写为 `服务器IP:端口`（IPv6 时写 `127.0.0.1`）
   - 移除 BML 里的 `NgsOn` 节点
3. **启动后**：后台 netstat 检测到游戏连上服务器，自动把备份的 `KartRider-bak.pin` 恢复回去（防服务端校验 PIN 被改）

## 说明

- 纯 .NET Framework 托管程序，不依赖 VC++ 运行库（Win11 LTSC 缺 VCRUNTIME140.dll 也能跑）
- 源文件为 C# 5 语法，兼容 Windows 自带 csc.exe；`InPacket.cs` 含 unsafe 代码，编译脚本已带 `/unsafe`
- 依赖源码从 Launcher_V2 原仓库复制（`KREncodedBlock.cs` 的 `Ionic.Zlib` 已替换为 .NET Framework 内置 `System.IO.Compression`，逻辑不变）
