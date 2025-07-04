using System;
using System.Collections.Generic;
using Godot;

public partial class Player : Entity
{

	public int PlayerId { get; private set; }
	public int SaveSlotId { get; set; }

	private CollisionShape2D _hitShape;

	#region Sprites

	[Export] public NodePath WepUpDraw;
	[Export] public NodePath WepDownDraw;
	[Export] public NodePath WepUpStow;
	[Export] public NodePath WepDownStow;
	[Export] public NodePath Hair;
	[Export] public NodePath Clothes;
	[Export] public NodePath Body;
	[Export] public NodePath Notify;
	[Export] public NodePath PlayerOverlay;
	[Export] public Texture2D OneHandBody;
	[Export] public Texture2D TwoHandBody;
	[Export] public Texture2D BowBody;

	public Sprite2D _wepUpDraw;
	public Sprite2D _wepDownDraw;
	public Sprite2D _wepUpStow;
	public Sprite2D _wepDownStow;
	public Sprite2D _hair;
	public Sprite2D _clothes;
	public Sprite2D _body;
	public Sprite2D _notify;
	public Sprite2D _overlay;

	#endregion


	public override void _Ready()
	{

		var saveButton = GetNode<Button>("UI/MarginContainer/HBoxContainer/VBoxContainer/Save");
		saveButton.Pressed += OnSavePressed;

		_hitShape = GetNode<CollisionShape2D>("HitBoxArea/CollisionShape2D");
		_hitShape.Disabled = true;
		EventManager.I.Subscribe<EquipmentChange>(GameEvent.EquipmentChanged, SetCurrentSprites);


	}

	public override void _ExitTree()
	{
		EventManager.I.Unsubscribe<EquipmentChange>(GameEvent.EquipmentChanged, SetCurrentSprites);
	}

	private void OnSavePressed()
	{
		// when we add the interact system this will have to somehow grab the
		// current scene ID and the Spawn ID and put it into the data store.
		Data.EntityStats[StatType.MaxHealth].changeStatValue(25);
		GD.Print("Save Pressed");
		currentSpawnId = "Second";
		PlayerManager.Instance.StorePlayerData(this);
	}

	public void SetPlayerId(int id)
	{
		PlayerId = id;
	}

	private void SetCurrentSprites(EquipmentChange request)
	{
		// fires any time there is a change of equipment, by default is null unless data is passed
		// this may happen if a trinket is equipped that changes nothing visually
		InventoryItem Item = InventoryManager.I.Get(request.New.ItemId);

		if (Item == null)
			return;

		switch (Item)
		{
			case WeaponItem w:

				// _wepDownDraw.Texture = w.WepDownDraw;
				// _wepDownStow.Texture = w.WepDownStow;
				// _wepUpDraw.Texture = w.WepUpDraw;
				// _wepUpStow.Texture = w.WepUpStow;


				if (w.type == BodyType.OneHanded)
				{
					_body.Texture = OneHandBody;
				}
				else if (w.type == BodyType.TwoHanded)
				{
					_body.Texture = TwoHandBody;
				}
				else
				{
					_body.Texture = BowBody;
				}

				break;
			case ArmorItem a:

				_clothes.Texture = a.Clothes;



				if (a.type == BodyType.OneHanded)
				{
					_body.Texture = OneHandBody;
				}
				else if (a.type == BodyType.TwoHanded)
				{
					_body.Texture = TwoHandBody;
				}
				else
				{
					_body.Texture = BowBody;
				}
				break;
			default:
				break;

				// Add more cases if you later have BowItem, StaffItem, etc.
		}
	}


public void ActivateHitBox(
    float width,      // rectangle size X
    float height,     // rectangle size Y
    float forward,    // distance straight ahead
    float activeSecs = 0.1f) // lifetime before auto-disable
{
    // resize or create the rectangle
    var rect = _hitShape.Shape as RectangleShape2D ?? new RectangleShape2D();
    rect.Size = new Vector2(width, height);
    _hitShape.Shape = rect;

    // calculate offset purely by facing
    Vector2 offset = FacingDirection switch        // ← you already track Facing
    {
        Facing.Down  => new Vector2(0,  forward),
        Facing.Up    => new Vector2(0, -forward),
        Facing.Left  => new Vector2(-forward, 0),
        Facing.Right => new Vector2( forward, 0),
        _            => Vector2.Zero
    };
    _hitShape.Position = offset;

    // enable & schedule auto-disable
    _hitShape.Disabled = false;
    var t = CreateTween();
    t.TweenCallback(Callable.From(() => _hitShape.Disabled = true))
     .SetDelay(activeSecs);
}
} 
