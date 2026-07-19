@echo off
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set OUT=C:\Users\海辰\Desktop\AutoDial\AutoDial.exe

echo Compiling WPF application...

%CSC% /target:winexe /out:%OUT% ^
    /r:PresentationCore.dll ^
    /r:PresentationFramework.dll ^
    /r:WindowsBase.dll ^
    /r:System.Xaml.dll ^
    /r:System.dll ^
    /r:System.Windows.Forms.dll ^
    /r:System.Drawing.dll ^
    App.xaml.cs MainWindow.xaml.cs

if %errorlevel%==0 (
    echo Build successful!
) else (
    echo Build failed!
)

pause