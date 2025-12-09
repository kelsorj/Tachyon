SET CODE_PATH="C:\Code"
REM TRUNK_BIN_PATH ASSUMES WE ARE RUNNING THIS BAT FILE FROM THE TRUNK DIRECTORY
SET TRUNK_BIN_PATH="%CD%\vs2008\bin\Debug"

SET DATE_NOW="%date:~-4,4%-%date:~-10,2%-%date:~-7,2%"
SET RELEASE_PATH="%CODE_PATH%\Releases\%DATE_NOW%"

mkdir "%RELEASE_PATH%\bin"
mkdir "%RELEASE_PATH%\bin\config"
mkdir "%RELEASE_PATH%\bin\plugins"

xcopy "%TRUNK_BIN_PATH%\*" "%RELEASE_PATH%\bin\"
xcopy /E "%TRUNK_BIN_PATH%\config\*" "%RELEASE_PATH%\bin\config\"
xcopy /E "%TRUNK_BIN_PATH%\plugins\*" "%RELEASE_PATH%\bin\plugins\"

START create_shortcut_synapsis.vbs %DATE_NOW% %RELEASE_PATH%

