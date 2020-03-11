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

call OpenRA.Utility.exe yr --import-translation-string chinese yrsc translation_fonts/FontsSetting.yaml
echo 正在启动 开源红警2尤里的复仇 ... 
cd ..
launch-yrsc
pause
exit /b

:noengine
echo 需要的引擎文件没有找到
echo 在mod根目录运行 `make all` 命令以便获取并且构建需要的文件，然后再试一次
pause
exit /b

:badconfig
echo 需要的mod.config变量缺失
echo 确保MOD_ID，ENGINE_VERSION以及ENGINE_DIRECTORY这三个变量
echo 存在于你的 mod.config (或者 user.config) 然后再试一次.
pause
exit /b
