mkdir "%cd%\mods\ra2"

xcopy "%cd%\ra2\mods\ra2\OpenRA.Mods.RA2.dll" "%cd%\mods\ra2\OpenRA.Mods.RA2.dll" /V /Y

mkdir "%cd%\mods\common"

xcopy "%cd%\engine\mods\common\OpenRA.Mods.Cnc.dll" "%cd%\mods\common\OpenRA.Mods.Cnc.dll" /V /Y
xcopy "%cd%\engine\mods\common\OpenRA.Mods.Common.dll" "%cd%\mods\common\OpenRA.Mods.Common.dll" /V /Y