@echo off
rem ============================================================
rem  MiniLauncher 编译脚本
rem  使用 Windows 自带的 .NET Framework 编译器 (csc.exe)
rem  无需安装 Visual Studio / NuGet 包
rem ============================================================
chcp 65001 >nul
cd /d "%~dp0"

rem ---- 定位 csc.exe（优先 64 位）----
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [ERROR] 找不到 csc.exe，请确认系统装有 .NET Framework 4.x
    pause
    exit /b 1
)

rem ---- 定位参考程序集目录 ----
set "REF=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if exist "%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8" set "REF=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if exist "%ProgramFiles%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8" set "REF=%ProgramFiles%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"

echo 编译器: %CSC%
echo 引用目录: %REF%
echo.

"%CSC%" /nologo /target:winexe /unsafe /codepage:65001 /out:MiniLauncher.exe ^
    /r:"%REF%\System.dll" ^
    /r:"%REF%\System.Core.dll" ^
    /r:"%REF%\System.Windows.Forms.dll" ^
    /r:"%REF%\System.Drawing.dll" ^
    /r:"%REF%\System.Xml.dll" ^
    /r:"%REF%\System.IO.Compression.dll" ^
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
