using Godot;
using System;
using System.Text.Json;

public partial class SaveSlotButton : Button
{
    /* The NodePaths should be dragged in the inspector */
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

    public override void _Ready()
    {
        _name = GetNode<Label>(PlayerNamePath);
        _scene = GetNode<Label>(PlayerScenePath);
        _time = GetNode<Label>(PlayerTimePath);
        _level = GetNode<Label>(PlayerLevelPath);
        _portrait = GetNode<TextureRect>(PortraitPath);
    }

    public void SetSlotInfo(string json)
    {
        _save = JsonSerializer.Deserialize<PlayerSaveData>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _name.Text = "Selene"; // eventually allow the player to input their own name
        _scene.Text = _save.SceneID;
        _time.Text = _save.SavedAt.ToLocalTime().ToString("yyyy‑MM‑dd HH:mm");
        _level.Text = $"Lv {_save.CurrentStats["Experience"]}";
        // optional portrait: load a Texture2D path you stored in save
        GetNode<TextureRect>(PortraitPath).Texture = GD.Load<Texture2D>("res://ArtAssets/Characters/NPC/Portraits/Icon11.png");
    }

    public override void _Pressed()
    {
        if (_save == null) return;              // shouldn’t happen
        GameManager.Instance.StartLoadGame(_save.SaveSlot);
    }
}