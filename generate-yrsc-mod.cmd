@echo off
setlocal EnableDelayedExpansion

FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))
set MOD_SEARCH_PATHS=%~dp0mods,./mods

if "!MOD_ID!" == "" goto badconfig
if "!ENGINE_VERSION!" == "" goto badconfig
if "!ENGINE_DIRECTORY!" == "" goto badconfig

title OpenRA.Utility.exe %MOD_ID%

set TEMPLATE_DIR=%CD%
if not exist %ENGINE_DIRECTORY%\OpenRA.Game.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine
cd %ENGINE_DIRECTORY%

call OpenRA.Utility.exe yr --import-translation-string chinese yrsc
pause
exit /b

:noengine
echo Required engine files not found.
echo Run `make all` in the mod directory to fetch and build the required files, then try again.
pause
exit /b

:badconfig
echo Required mod.config variables are missing.
echo Ensure that MOD_ID ENGINE_VERSION and ENGINE_DIRECTORY are
echo defined in your mod.config (or user.config) and try again.
pause
exit /b