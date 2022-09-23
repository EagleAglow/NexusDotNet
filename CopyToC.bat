@echo off
echo This will copy the executable files
echo from the Release folder to folder
echo C:/NXSNXS.  Use control-C to cancel.
pause
cd ./bin/Release
xcopy *.exe C:\NXSNXS /f /i /y
xcopy *.manifest C:\NXSNXS /f /i /y
xcopy *.application C:\NXSNXS /f /i /y
xcopy *.config C:\NXSNXS /f /i /y
xcopy *.dll C:\NXSNXS /f /i /y
xcopy LICENSE.txt C:\NXSNXS /f /i /y
pause
