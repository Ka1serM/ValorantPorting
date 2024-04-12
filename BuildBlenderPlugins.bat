@ECHO off
set base_dir=%cd%

for /d %%a in (BlenderPlugins\ValorantPortingBlender*) do (
	echo %%a
	xcopy "%base_dir%\BlenderPlugins\PSA_PSK_Import\" "%%a" /h /i /c /k /e /r /y
	"C:\Program Files\7-zip\7z.exe" a %%~na.zip %base_dir%\%%a > nul
	echo Created "%%a.zip"
)