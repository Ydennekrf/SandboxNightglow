using System;
using System.Collections.Generic;
using Godot;

public partial class Player : Entity
{

	public int PlayerId { get; private set; }
	public int SaveSlotId { get; set; }



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
		_wepUpDraw = GetNode<Sprite2D>(WepUpDraw);
		_wepDownDraw = GetNode<Sprite2D>(WepDownDraw);
		_wepUpStow = GetNode<Sprite2D>(WepUpStow);
		_wepDownStow = GetNode<Sprite2D>(WepDownStow);
		_hair = GetNode<Sprite2D>(Hair);
		_clothes = GetNode<Sprite2D>(Clothes);
		_body = GetNode<Sprite2D>(Body);

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

	public void SetWeaponSprites(WeaponBase weapon)
	{
		_wepUpDraw.Texture = weapon.WepUpDraw;
		_wepDownDraw.Texture = weapon.WepDownDraw;
		_wepUpStow.Texture = weapon.WepUpStow;
		_wepDownStow.Texture = weapon.WepDownStow;
	}

	public override void Die()
	{
		base.Die();
		// bring up the game over scene using the scene manager
		// maybe check if any items or equipped gear can save from the death?
    }




} 
