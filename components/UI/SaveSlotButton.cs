using Godot;
using System;
using System.Text.Json;
using ethra.V1;

public partial class SaveSlotButton : Button
{
    [Export] private NodePath PortraitPath;
    [Export] private NodePath PlayerNamePath;
    [Export] private NodePath PlayerScenePath;
    [Export] private NodePath PlayerTimePath;
    [Export] private NodePath PlayerLevelPath;

    private Label _name;
    private Label _scene;
    private Label _time;
    private Label _level;
    private TextureRect _portrait;

    private PlayerSaveData _save;
    private SaveSlotInfo _pendingSlotInfo;
    private bool _pendingNewGameMode;

    public override void _Ready()
    {
        _name = GetNode<Label>(PlayerNamePath);
        _scene = GetNode<Label>(PlayerScenePath);
        _time = GetNode<Label>(PlayerTimePath);
        _level = GetNode<Label>(PlayerLevelPath);
        _portrait = GetNode<TextureRect>(PortraitPath);

        if (_pendingSlotInfo != null)
        {
            ApplySlotInfo(_pendingSlotInfo, _pendingNewGameMode);
        }
    }

    public void SetSlotInfo(SaveSlotInfo slot, bool newGameMode)
    {
        _pendingSlotInfo = slot;
        _pendingNewGameMode = newGameMode;

        if (_name == null || _scene == null || _time == null || _level == null)
        {
            return;
        }

        ApplySlotInfo(slot, newGameMode);
    }

    private void ApplySlotInfo(SaveSlotInfo slot, bool newGameMode)
    {
        if (slot == null)
        {
            SetEmptySlot(0, newGameMode);
            return;
        }

        if (!slot.Occupied)
        {
            SetEmptySlot(slot.SlotNumber, newGameMode);
            return;
        }

        _name.Text = newGameMode
            ? $"Overwrite: {slot.PlayerName}"
            : slot.PlayerName;
        _level.Text = $"Slot {slot.SlotNumber}";
        _scene.Text = string.IsNullOrWhiteSpace(slot.SceneName) ? "Unknown location" : slot.SceneName;
        _time.Text = string.IsNullOrWhiteSpace(slot.GameTimePlayed) ? "00:00:00" : slot.GameTimePlayed;
    }

    public void SetSlotInfo(string json)
    {
        _save = JsonSerializer.Deserialize<PlayerSaveData>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        WorldStateDto worldState = _save.WorldState ?? new WorldStateDto();
        _name.Text = string.IsNullOrWhiteSpace(worldState.PlayerName) ? "Player" : worldState.PlayerName;
        _scene.Text = ResolveSceneText(worldState);
        _time.Text = FormatPlayedTime(worldState.GameTimePlayedSeconds);
        _level.Text = $"Lv {ResolveLevel(_save)}";
        _portrait.Texture = GD.Load<Texture2D>("res://ArtAssets/Characters/NPC/Portraits/Icon11.png");
    }

    public override void _Pressed()
    {
        if (_save == null)
        {
            return;
        }

        GameManager.Instance.StartLoadGame(_save.SaveSlot);
    }

    private static string ResolveSceneText(WorldStateDto worldState)
    {
        if (!string.IsNullOrWhiteSpace(worldState.CurrentSceneDisplayName))
        {
            return worldState.CurrentSceneDisplayName;
        }

        return string.IsNullOrWhiteSpace(worldState.CurrentLocation?.SceneId)
            ? "Unknown location"
            : worldState.CurrentLocation.SceneId;
    }

    private static string FormatPlayedTime(double seconds)
    {
        TimeSpan played = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        return played.TotalHours >= 1d
            ? $"{(int)played.TotalHours:D2}:{played.Minutes:D2}:{played.Seconds:D2}"
            : $"{played.Minutes:D2}:{played.Seconds:D2}";
    }

    private static int ResolveLevel(PlayerSaveData save)
    {
        if (save?.CurrentStats != null && save.CurrentStats.TryGetValue("Level", out int level))
        {
            return level;
        }

        return 1;
    }

    private void SetEmptySlot(int slotNumber, bool newGameMode)
    {
        _name.Text = newGameMode ? "Start New Game" : "Empty Slot";
        _level.Text = $"Slot {slotNumber}";
        _scene.Text = "Empty Slot";
        _time.Text = string.Empty;
    }
}
