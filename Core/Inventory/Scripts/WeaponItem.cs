using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public class WeaponItem : InventoryItem
    {
        private const bool DebugComboProfiles = true;
        private bool _isEquipped;
        private Texture2D _weaponUpDraw;
        private Texture2D _weaponDownDraw;
        private Texture2D _weaponUpStow;
        private Texture2D _weaponDownStow;
        private string _comboProfilePath;
        private WeaponComboResource _comboProfile;

        public string WeaponSlot => "MainHand";
        public bool IsEquipped => _isEquipped;
        public Texture2D WeaponUpDraw => _weaponUpDraw;
        public Texture2D WeaponDownDraw => _weaponDownDraw;
        public Texture2D WeaponUpStow => _weaponUpStow;
        public Texture2D WeaponDownStow => _weaponDownStow;
        public string ComboProfilePath => _comboProfilePath;
        public WeaponComboResource ComboProfile => _comboProfile ??= LoadComboProfile(_comboProfilePath);

        public WeaponItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            int maxStack,
            List<ItemEffects> effects = null,
            string iconPath = "",
            string weaponUpDrawPath = "",
            string weaponDownDrawPath = "",
            string weaponUpStowPath = "",
            string weaponDownStowPath = "",
            string comboProfilePath = "")
            : base(id, name, value, description, rarity, effects, category: "Weapon", subtype: "MainHand", maxStack: maxStack, iconPath: iconPath)
        {
            _weaponUpDraw = LoadTexture(weaponUpDrawPath);
            _weaponDownDraw = LoadTexture(weaponDownDrawPath);
            _weaponUpStow = LoadTexture(weaponUpStowPath);
            _weaponDownStow = LoadTexture(weaponDownStowPath);
            _comboProfilePath = comboProfilePath;
        }

        public void Equip()
        {
            if (_isEquipped)
            {
                return;
            }

            foreach (ItemEffects effect in Effects)
            {
                if (effect is IEffect itemEffect)
                {
                    itemEffect.ResolveItemEffect();
                }
            }

            _isEquipped = true;
            GD.Print($"Equipped weapon '{Name}' in slot '{WeaponSlot}'.");
        }

        public void Unequip()
        {
            if (!_isEquipped)
            {
                return;
            }

            foreach (ItemEffects effect in Effects)
            {
                if (effect is IEffect itemEffect)
                {
                    itemEffect.RemoveItemEffect();
                }
            }

            _isEquipped = false;
            GD.Print($"Unequipped weapon '{Name}' from slot '{WeaponSlot}'.");
        }

        public override void Use()
        {
            if (IsEquipped)
            {
                Unequip();
            }
            else
            {
                Equip();
            }
        }

        private static WeaponComboResource LoadComboProfile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            WeaponComboResource profile = ResourceLoader.Load<WeaponComboResource>(path);
            if (profile == null)
            {
                GD.PushWarning($"WeaponItem: failed to load combo profile at '{path}'.");
            }
            else if (DebugComboProfiles)
            {
                GD.Print($"WeaponItem: loaded combo profile '{path}' with phaseCount={profile.Phases?.Count ?? 0}.");
            }

            ComboProfileValidator.EmitToGodotLog(profile, $"Weapon '{path}'");

            return profile;
        }
    }
}
