for /F "tokens=1-3 delims=:." %%a in ("%TIME%") do set HR=%%a& set MIN=%%b& set SEC=%%c
if "%HR:~0,1%"==" " set HR=0%HR:~1,1%
set MYTIME=%HR%%MIN%%SEC%
echo %MYTIME%
ren c:\installers\HiGIntegration\Installer.msi HiGIntegration_%DATE:~10,4%-%DATE:~4,2%-%DATE:~7,2%_%MYTIME%.msi