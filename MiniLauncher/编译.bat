@echo off
rem ============================================================
rem  MiniLauncher 编译脚本
rem  使用 Windows 自带的 .NET Framework 编译器 (csc.exe)
rem  无需安装 Visual Studio / NuGet 包
rem ============================================================
chcp 65001 >nul
cd /d "%~dp0"

rem ---- 定位 csc.exe 和框架目录（统一使用同一目录，避免重复引用）----
set "FWDIR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
set "CSC=%FWDIR%\csc.exe"
if not exist "%CSC%" (
    set "FWDIR=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
    set "CSC=%FWDIR%\csc.exe"
)
if not exist "%CSC%" (
    echo [ERROR] 找不到 csc.exe，请确认系统装有 .NET Framework 4.x
    pause
    exit /b 1
)

echo 编译器: %CSC%
echo.

"%CSC%" /nologo /target:winexe /unsafe /codepage:65001 /out:MiniLauncher.exe ^
    /r:"%FWDIR%\System.dll" ^
    /r:"%FWDIR%\System.Core.dll" ^
    /r:"%FWDIR%\System.Windows.Forms.dll" ^
    /r:"%FWDIR%\System.Drawing.dll" ^
    /r:"%FWDIR%\System.Xml.dll" ^
    /r:"%FWDIR%\System.IO.Compression.dll" ^
    *.cs

if %errorlevel%==0 (
    echo.
    echo ==========================================
    echo  编译成功: MiniLauncher.exe
    echo ==========================================
) else (
    echo.
    echo ==========================================
    echo  编译失败，请把上面的错误信息发出来
    echo ==========================================
)
if not defined CI pause