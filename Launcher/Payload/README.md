# MMOnsterpatch Launcher Payload

This folder is copied beside the published launcher and staged into the Monsterpatch game folder only while Play Online is running.

Included runtime payload:
- `Root/winhttp.dll`
- `Root/doorstop_config.ini`
- `Root/.doorstop_version`
- `BepInEx/core/*` from BepInEx win_x64 5.4.23.5

Build-time payload:
- `BepInEx/patchers/MMOnsterpatchOfficialServerPatcher.dll` is copied here automatically by `Source/build_all.bat`.

The final player-facing release should be the published launcher folder, including this Payload folder with the compiled patcher DLL present.
