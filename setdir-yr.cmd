mkdir "%cd%\mods\ra2"

xcopy "%cd%\ra2\mods\ra2\*" "%cd%\mods\ra2\" /V /Y /S /H

mkdir "%cd%\mods\common"

xcopy "%cd%\engine\mods\common\OpenRA.Mods.Cnc.dll" "%cd%\mods\common\OpenRA.Mods.Cnc.dll" /V /Y
xcopy "%cd%\engine\mods\common\OpenRA.Mods.Common.dll" "%cd%\mods\common\OpenRA.Mods.Common.dll" /V /Y

mkdir "%cd%\mods\modcontent"

xcopy "%cd%\ra2\mods\modcontent\*"  "%cd%\mods\modcontent\" /V /Y /S /H