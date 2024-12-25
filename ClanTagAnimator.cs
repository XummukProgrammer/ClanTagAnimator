using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using CSSharpUtils.Extensions;
using System.Text.Json.Serialization;

namespace ClanTagAnimator;

public class Constants
{
    public const string PluginName = "Clan Tag Animator";
    public const string PluginVersion = "1.0.0";
    public const int MaxPlayers = 64;
}

public class Player
{
    private CCSPlayerController? _controller;
    private string? _clanTag;
    private int _index;

    public bool IsValid => _controller != null;

    public void Init(CCSPlayerController? controller, string? clanTag)
    {
        _controller = controller;
        _clanTag = clanTag;
        _index = 0;
    }

    public void Deinit()
    {
        _controller = null;
        _clanTag = null;
        _index = 0;
    }

    public void Update()
    {
        ClanTagChange();
        IncrementIndex();
    }

    private void ClanTagChange()
    {
        if (_clanTag != null && _clanTag.Length > 0)
        {
            string clanTag = "";

            for (int i = 0; i < 10; i++)
            {
                int index = i + _index;
                if (index >= _clanTag?.Length)
                {
                    index -= _clanTag?.Length ?? 0;
                }

                clanTag += _clanTag?[index];
            }

            _controller?.SetClantag(clanTag);
        }
    }

    private void IncrementIndex()
    {
        _index++;

        if (_index >= _clanTag?.Length)
        {
            _index = 0;
        }
    }
}

public class PlayersManager
{
    private Player[] _players = new Player[Constants.MaxPlayers];
    private ConfigModel? _config;

    public void Init()
    {
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = new Player();
        }
    }

    public void SetConfig(ConfigModel? config)
    {
        _config = config;
    }

    public void Deinit()
    {
    }

    public void Update()
    {
        foreach (var player in _players)
        {
            player.Update();
        }
    }

    public void AddPlayer(CCSPlayerController? controller)
    {
        if (controller == null)
        {
            return;
        }

        string? clanTag = null;
        var playerModel = GetPlayerModel(controller?.AuthorizedSteamID?.SteamId2 ?? "");
        if (playerModel != null)
        {
            var clanTagModel = GetClanTagModel(playerModel.ClanTagID!);
            if (clanTagModel != null)
            {
                clanTag = clanTagModel.Visual!;
            }
        }

        GetPlayer(controller).Init(controller, clanTag);
    }

    public void RemovePlayer(CCSPlayerController? controller)
    {
        if (controller == null)
        {
            return;
        }

        GetPlayer(controller).Deinit();
    }

    public Player GetPlayer(CCSPlayerController controller)
    {
        return _players[controller.Slot];
    }

    public PlayerModel? GetPlayerModel(string steamID)
    {
        return _config?.Players.Find(player => player.SteamID == steamID);
    }

    public ClanTagModel? GetClanTagModel(string id)
    {
        return _config?.ClanTags.Find(clanTag => clanTag.ID == id);
    }
}

public class ClanTagAnimator : BasePlugin, IPluginConfig<ConfigModel>
{
    public override string ModuleName => Constants.PluginName;

    public override string ModuleVersion => Constants.PluginVersion;

    public ConfigModel Config { get; set; }

    private PlayersManager _playersManager = new();

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        _playersManager.Init();

        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);

        _playersManager.Deinit();

        RemoveListener<Listeners.OnMapStart>(OnMapStartHandler);
    }

    public void OnConfigParsed(ConfigModel config)
    {
        Config = config;

        _playersManager.SetConfig(config);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var controller = @event.Userid;
        if (controller != null)
        {
            _playersManager.AddPlayer(controller);
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var controller = @event.Userid;
        if (controller != null)
        {
            _playersManager.RemovePlayer(controller);
        }
        return HookResult.Continue;
    }

    private void OnMapStartHandler(string mapName)
    {
        AddTimer(0.5f, OnUpdate, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void OnUpdate()
    {
        _playersManager.Update();
    }
}

public class ClanTagModel
{
    [JsonPropertyName("ID")] public string? ID { get; set; }
    [JsonPropertyName("Visual")] public string? Visual {  get; set; }
}

public class PlayerModel
{
    [JsonPropertyName("SteamID")] public string? SteamID { get; set; }
    [JsonPropertyName("ClanTagID")] public string? ClanTagID { get; set; }
}

public class ConfigModel : BasePluginConfig
{
    [JsonPropertyName("ClanTags")] public List<ClanTagModel> ClanTags { get; set; } = [ new ClanTagModel { ID = "Test", Visual = "[ClanTagAnimator]" } ];
    [JsonPropertyName("Players")] public List<PlayerModel> Players { get; set; } = [ new PlayerModel { SteamID = "STEAM_0:0:81128502", ClanTagID = "Test" } ];
}
