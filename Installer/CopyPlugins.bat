
REM copy ".\TestInstallerHelper\bin\Release\DIDE.Installer.dll" .
if "%PROGRAMFILES(X86)%"=="" goto :x86
goto :x64
:x86
	copy CLR.dll "%PROGRAMFILES%\NSIS\Plugins\"
	copy nsisunz.dll "%PROGRAMFILES%\NSIS\Plugins\"
	copy inetc.dll "%PROGRAMFILES%\NSIS\Plugins\"
	copy NsisUrlLib.dll "%PROGRAMFILES%\NSIS\Plugins\"
	goto done
:x64
	xcopy CLR.dll "%PROGRAMFILES(X86)%\NSIS\Plugins\"
	xcopy nsisunz.dll "%PROGRAMFILES(X86)%\NSIS\Plugins\"
	xcopy inetc.dll "%PROGRAMFILES(X86)%\NSIS\Plugins\"
	xcopy NsisUrlLib.dll "%PROGRAMFILES(X86)%\NSIS\Plugins\"
:done

rem copy .\libraries\*.nsh  "%PROGRAMFILES%\NSIS\Include\"
pause