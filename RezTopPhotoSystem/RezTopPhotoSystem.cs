using System.Drawing;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using K4WorldTextSharedAPI;
using MySqlConnector;
using WASDSharedAPI;

namespace RezTopPhotoSystem;

public sealed class RezTopPhotoSystem : BasePlugin
{
    public override string ModuleName => "RezTop PhotoSystem";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "RezTop";
    public override string ModuleDescription => "Admin-only large wall photo panels with MySQL persistence";

    private readonly PluginCapability<IK4WorldTextProvider> _worldTextCapability = new("k4-worldtext:sharedapi");
    private readonly PluginCapability<IWasdMenuManager> _menuCapability = new("wasdmenu:manager");
    private IK4WorldTextSharedAPI? _worldText;
    private IWasdMenuManager? _menus;
    private PhotoConfig _config = new();
    private string _loadedMap = "";
    private readonly Dictionary<long, int> _worldTextIds = new();

    public override void Load(bool hotReload)
    {
        LoadConfig();
        RegisterListener<Listeners.OnServerPrecacheResources>(manifest =>
        {
            foreach (var photo in _config.Photos)
                manifest.AddResource(photo.ModelPath);
        });

        AddCommand("css_photo", "Open RezTop PhotoSystem admin menu", (player, _) => OpenMenu(player));
        AddCommand("css_photo_add", "Open photo placement menu", (player, _) => OpenAddMenu(player));
        AddCommand("css_photo_list", "List photos on current map", (player, _) => OpenListMenu(player));
        AddCommand("css_photo_remove", "Open photo removal menu", (player, _) => OpenListMenu(player));

        RegisterEventHandler<EventRoundStart>((_, _) =>
        {
            var map = Server.MapName;
            if (!string.Equals(map, _loadedMap, StringComparison.OrdinalIgnoreCase))
            {
                _loadedMap = map;
                _ = LoadMapPhotosAsync(map);
            }
            return HookResult.Continue;
        });

        if (hotReload)
            Server.NextFrame(() => { _loadedMap = ""; _ = LoadMapPhotosAsync(Server.MapName); });
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _worldText = _worldTextCapability.Get()?.ForPlugin(this);
        _menus = _menuCapability.Get();
        if (_worldText == null)
            Logger.LogError("K4-WorldText-API capability k4-worldtext:sharedapi not found.");
        if (_menus == null)
            Logger.LogError("WASDMenuAPI capability wasdmenu:manager not found.");
    }

    public override void Unload(bool hotReload) => _worldText?.RemoveAll();

    private void LoadConfig()
    {
        var path = Path.Combine(ModuleDirectory, "config.json");
        if (!File.Exists(path))
            File.WriteAllText(path, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
        else
            _config = JsonSerializer.Deserialize<PhotoConfig>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PhotoConfig();
    }

    private bool IsAdmin(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid) return false;
        if (AdminManager.PlayerHasPermissions(player, _config.AdminPermission)) return true;
        player.PrintToChat("[RezTop] Нет доступа. Команда доступна только администраторам.");
        return false;
    }

    private void OpenMenu(CCSPlayerController? player)
    {
        if (!IsAdmin(player)) return;
        if (_menus == null) { player!.PrintToChat("[RezTop] WASDMenuAPI не загружен."); return; }
        var menu = _menus.CreateMenu("RezTop PhotoSystem");
        menu.Add("Добавить фотографию", (p, _) => OpenAddMenu(p));
        menu.Add("Фотографии на этой карте", (p, _) => OpenListMenu(p));
        menu.Add("Закрыть", (p, _) => _menus.CloseMenu(p));
        _menus.OpenMainMenu(player, menu);
    }

    private void OpenAddMenu(CCSPlayerController player)
    {
        if (!IsAdmin(player) || _menus == null) return;
        var menu = _menus.CreateMenu("Выбери большую фотографию");
        foreach (var photo in _config.Photos)
        {
            var selected = photo;
            menu.Add(selected.DisplayName, (p, _) => PlacePhoto(p, selected));
        }
        menu.Add("Назад", (p, _) => OpenMenu(p));
        _menus.OpenMainMenu(player, menu);
    }

    private void PlacePhoto(CCSPlayerController player, PhotoAsset photo)
    {
        if (!IsAdmin(player) || _worldText == null || player.PlayerPawn.Value == null) return;

        var pawn = player.PlayerPawn.Value;
        var origin = pawn.AbsOrigin;
        var eye = new Vector(origin.X, origin.Y, origin.Z + 64.0f);
        var trace = Trace.TraceShape(eye, pawn.EyeAngles, pawn);
        if (!trace.DidHit())
        {
            player.PrintToChat("[RezTop] Наведи прицел на стену и попробуй снова.");
            return;
        }

        var hit = trace.HitPoint;
        var normal = trace.Normal;
        var yaw = MathF.Atan2(normal.Y, normal.X) * (180.0f / MathF.PI);
        var position = new Vector(hit.X + normal.X * 1.5f, hit.Y + normal.Y * 1.5f, hit.Z + normal.Z * 1.5f);
        var angle = new QAngle(0, yaw, 0);

        try
        {
            var line = new TextLine
            {
                Text = " ",
                Color = Color.FromArgb(0, 0, 0, 0),
                FontSize = 1,
                Scale = 0.01f,
                ModelBackgroundEnabled = true,
                ModelBackgroundPath = photo.ModelPath,
                ModelBackgroundSurfaceScale = photo.Scale,
                ModelBackgroundDepth = 2,
                ModelBackgroundAutoCenter = true
            };
            var apiId = _worldText.AddWorldText(TextPlacement.Wall, line, position, angle);
            _ = SavePhotoAsync(Server.MapName, photo.Id, position, angle, player.SteamID, apiId, player);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create photo panel {PhotoId}", photo.Id);
            player.PrintToChat("[RezTop] Не удалось создать фото. Проверь модель и ресурсы Source 2.");
        }
    }

    private async Task SavePhotoAsync(string map, string photoId, Vector pos, QAngle ang, ulong steamId, int apiId, CCSPlayerController player)
    {
        try
        {
            await using var db = new MySqlConnection(_config.MySql.ConnectionString);
            await db.OpenAsync();
            await using var cmd = db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO reztop_photo_placements
                (map_name, photo_id, pos_x, pos_y, pos_z, pitch, yaw, roll, created_by)
                VALUES (@map,@photo,@x,@y,@z,@pitch,@yaw,@roll,@admin);
                SELECT LAST_INSERT_ID();
                """;
            cmd.Parameters.AddWithValue("@map", map);
            cmd.Parameters.AddWithValue("@photo", photoId);
            cmd.Parameters.AddWithValue("@x", pos.X); cmd.Parameters.AddWithValue("@y", pos.Y); cmd.Parameters.AddWithValue("@z", pos.Z);
            cmd.Parameters.AddWithValue("@pitch", ang.X); cmd.Parameters.AddWithValue("@yaw", ang.Y); cmd.Parameters.AddWithValue("@roll", ang.Z);
            cmd.Parameters.AddWithValue("@admin", steamId);
            var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            Server.NextFrame(() =>
            {
                _worldTextIds[id] = apiId;
                if (player.IsValid) player.PrintToChat($"[RezTop] Фото размещено. ID: {id}");
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not save photo placement to MySQL.");
            Server.NextFrame(() =>
            {
                try { _worldText?.RemoveWorldText(apiId); } catch { }
                if (player.IsValid) player.PrintToChat("[RezTop] Ошибка MySQL: фото не сохранено.");
            });
        }
    }

    private async Task LoadMapPhotosAsync(string map)
    {
        if (_worldText == null) return;
        try
        {
            await using var db = new MySqlConnection(_config.MySql.ConnectionString);
            await db.OpenAsync();
            await using (var create = db.CreateCommand())
            {
                create.CommandText = """
                    CREATE TABLE IF NOT EXISTS reztop_photo_placements (
                      id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                      map_name VARCHAR(128) NOT NULL,
                      photo_id VARCHAR(64) NOT NULL,
                      pos_x FLOAT NOT NULL, pos_y FLOAT NOT NULL, pos_z FLOAT NOT NULL,
                      pitch FLOAT NOT NULL, yaw FLOAT NOT NULL, roll FLOAT NOT NULL,
                      created_by BIGINT UNSIGNED NOT NULL,
                      created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                      INDEX idx_map (map_name)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                    """;
                await create.ExecuteNonQueryAsync();
            }

            var rows = new List<Placement>();
            await using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = "SELECT id, photo_id, pos_x, pos_y, pos_z, pitch, yaw, roll FROM reztop_photo_placements WHERE map_name=@map";
                cmd.Parameters.AddWithValue("@map", map);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    rows.Add(new Placement(reader.GetInt64(0), reader.GetString(1),
                        reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4),
                        reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7)));
            }

            foreach (var row in rows)
            {
                var photo = _config.Photos.FirstOrDefault(x => x.Id.Equals(row.PhotoId, StringComparison.OrdinalIgnoreCase));
                if (photo == null) { Logger.LogWarning("Photo asset {PhotoId} is missing from config.", row.PhotoId); continue; }
                var line = new TextLine
                {
                    Text = " ", Color = Color.FromArgb(0, 0, 0, 0), FontSize = 1, Scale = 0.01f,
                    ModelBackgroundEnabled = true, ModelBackgroundPath = photo.ModelPath,
                    ModelBackgroundSurfaceScale = photo.Scale, ModelBackgroundDepth = 2
                };
                Server.NextFrame(() =>
                {
                    var id = _worldText!.AddWorldText(TextPlacement.Wall, line,
                        new Vector(row.X, row.Y, row.Z), new QAngle(row.Pitch, row.Yaw, row.Roll));
                    _worldTextIds[row.Id] = id;
                });
            }
            Logger.LogInformation("RezTop PhotoSystem: loaded {Count} panels for map {Map}", rows.Count, map);
        }
        catch (Exception ex) { Logger.LogError(ex, "Failed loading photos for map {Map}", map); }
    }

    private void OpenListMenu(CCSPlayerController player)
    {
        if (!IsAdmin(player) || _menus == null) return;
        _ = QueryPlacementsAsync(player, Server.MapName);
    }

    private async Task QueryPlacementsAsync(CCSPlayerController player, string map)
    {
        try
        {
            await using var db = new MySqlConnection(_config.MySql.ConnectionString);
            await db.OpenAsync();
            await using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT id, photo_id FROM reztop_photo_placements WHERE map_name=@map ORDER BY id";
            cmd.Parameters.AddWithValue("@map", map);
            var items = new List<(long Id, string PhotoId)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) items.Add((reader.GetInt64(0), reader.GetString(1)));
            Server.NextFrame(() =>
            {
                if (!player.IsValid || _menus == null) return;
                var menu = _menus.CreateMenu("Фотографии этой карты");
                foreach (var item in items)
                {
                    var current = item;
                    menu.Add($"Удалить #{current.Id} ({current.PhotoId})", (p, _) => DeletePhoto(p, current.Id, map));
                }
                menu.Add("Назад", (p, _) => OpenMenu(p));
                _menus.OpenMainMenu(player, menu);
            });
        }
        catch (Exception ex) { Logger.LogError(ex, "Could not query photo list."); }
    }

    private void DeletePhoto(CCSPlayerController player, long id, string map)
    {
        if (!IsAdmin(player)) return;
        _ = DeletePhotoAsync(player, id, map);
    }

    private async Task DeletePhotoAsync(CCSPlayerController player, long id, string map)
    {
        try
        {
            await using var db = new MySqlConnection(_config.MySql.ConnectionString);
            await db.OpenAsync();
            await using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM reztop_photo_placements WHERE id=@id AND map_name=@map";
            cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@map", map);
            var affected = await cmd.ExecuteNonQueryAsync();
            Server.NextFrame(() =>
            {
                if (_worldTextIds.Remove(id, out var apiId))
                    try { _worldText?.RemoveWorldText(apiId); } catch { }
                player.PrintToChat(affected > 0 ? $"[RezTop] Фото #{id} удалено." : "[RezTop] Фото не найдено.");
                OpenListMenu(player);
            });
        }
        catch (Exception ex) { Logger.LogError(ex, "Could not delete photo placement."); }
    }

    private sealed class PhotoConfig
    {
        public string AdminPermission { get; set; } = "@css/generic";
        public MySqlConfig MySql { get; set; } = new();
        public List<PhotoAsset> Photos { get; set; } = new();
    }
    private sealed class MySqlConfig
    {
        public string ConnectionString { get; set; } = "Server=127.0.0.1;Port=3306;Database=reztop;User ID=USERNAME;Password=PASSWORD;";
    }
    private sealed class PhotoAsset
    {
        public string Id { get; set; } = "photo1";
        public string DisplayName { get; set; } = "Photo 1";
        public string ModelPath { get; set; } = "models/reztop/photos/photo1.vmdl";
        public float Scale { get; set; } = 1.0f;
    }
    private sealed record Placement(long Id, string PhotoId, float X, float Y, float Z, float Pitch, float Yaw, float Roll);
}
