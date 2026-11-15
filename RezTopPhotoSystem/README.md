# RezTop PhotoSystem 1.0

CS2 / CounterStrikeSharp plugin. Uses the real K4-WorldText-API v2 shared interface and WASDMenuAPI.

## What it does
- Admin-only `!photo`, `!photo_add`, `!photo_list`, `!photo_remove`.
- WASDMenuAPI menu.
- Places a large wall panel at the surface the admin is aiming at.
- Uses K4-WorldText-API's `TextLine.ModelBackground*` feature to render a textured Source 2 model behind an invisible text line.
- Saves placements by map in MySQL and restores them when that map starts.
- Per-plugin K4 scope; unload removes this plugin's active world texts.

## Important: images must be compiled Source 2 assets
A raw JPG/PNG uploaded by FTP cannot be displayed as a world texture by CounterStrikeSharp or K4-WorldText-API. The K4 API displays a `.vmdl` model as the panel background. Each photo therefore needs a compiled flat-panel model and its compiled material/texture resources. Upload the model and all dependent resources to the server's game content and make them available to clients (Workshop/FastDL/server resource distribution as appropriate).

Example model path in config: `models/reztop/photos/reztop_poster.vmdl`.
The model should be a flat rectangular quad, with its front face oriented along the model's expected forward axis, UV-mapped to the photo texture, and centered at its origin. The plugin precaches configured model paths via `OnServerPrecacheResources`.

## Build
Requirements: .NET 10 SDK, CounterStrikeSharp API 1.0.369+, MySqlConnector, K4 WorldText API v2, WASDMenuAPI.

1. Create `lib/` beside the csproj.
2. Copy the exact shared assemblies from the server/builds:
   - `K4-WorldText-SharedAPI.dll` from K4-WorldText-API v2
   - `WASDSharedAPI.dll` from WASDMenuAPI
3. `dotnet restore`
4. `dotnet build -c Release`
5. Copy `bin/Release/net10.0/RezTopPhotoSystem.dll` and `config.json` into `game/csgo/addons/counterstrikesharp/plugins/RezTopPhotoSystem/`.

Do not copy the two shared API DLLs into the plugin folder: install their own plugins/shared assemblies as their projects specify.

## Config
Set MySQL connection string and add each compiled photo model to `Photos`. Every model path must be precached and downloadable by clients. `Scale` is uniform model scale; use a large panel model authored at the desired aspect ratio.

## Permissions
Default permission: `@css/generic`. Grant it to admins in CounterStrikeSharp's admins configuration. Change `AdminPermission` to a custom permission if desired.

## Database
The plugin creates `reztop_photo_placements` automatically. Each row stores map, photo ID, transform, admin SteamID, and creation time.

## Commands
- `!photo` — open admin menu
- `!photo_add` — open photo picker
- `!photo_list` / `!photo_remove` — open placement list; select an entry to remove it

## Notes
- This source targets K4 WorldText API v2 (capability `k4-worldtext:sharedapi`, provider then `ForPlugin(this)`) and WASD capability `wasdmenu:manager`.
- K4 world text IDs are runtime-only; MySQL stores placement data and the plugin recreates panels after map changes.
- Test the panel orientation and offset on your map. The model's authored forward axis affects how it faces the wall.
- This source has not been built against your exact server binaries yet. Match the shared API DLL versions to the installed plugins before deployment.
