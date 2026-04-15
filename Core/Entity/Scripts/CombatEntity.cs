

using System.Collections.Generic;

namespace ethra.V1
{
    public partial class CombatEntity : Entity, ICombatEntity, ICombat , IStats
    {
         internal ICombat _combat;

          private int _lvl;
        private int _exp;
        private int _maxHP;
        private int _curHP;
        private int _maxMana;
        private int _curMana;
        private int _str;
        private int _dex;
        private int _int;
        private int _spi;
        private int _vit;
        private int _luk;

        public int MaxHP {get{return _maxHP;} set{_maxHP = value;}}
        public int CurHP {get{return _curHP;} set{if(_curHP + value > _maxHP){_curHP = _maxHP;} else {_curHP += value;}}}
        public int MaxMana {get{return _maxMana;} set{_maxMana = value;}}
        public int CurMana {get{return _curMana;} set{if(_curMana + value > _maxMana){_curMana = _maxMana;} else {_curMana += value;}}}
        public int Strength {get{return _str;} set {if(_str + value > 99){_str = 99;} else{_str = value;}}}
        public int Dexterity {get{return _dex;} set {if(_dex + value > 99){_dex = 99;} else{_dex = value;}}}
        public int Intelligence {get{return _int;} set {if(_int + value > 99){_int = 99;} else{_int = value;}}}
        public int Spirit {get{return _spi;} set {if(_spi + value > 99){_spi = 99;} else{_spi = value;}}}
        public int Vitality {get{return _vit;} set {if(_vit + value > 99){_vit = 99;} else{_vit = value;}}}
        public int Luck {get{return _luk;} set {if(_luk + value > 99){_luk = 99;} else{_luk = value;}}}

         public CombatEntity(IEntityManager entity, ICombat combat, IStateMachine fsm) : base (entity, fsm)
        {
            _combat = combat;
        }
        public void ApplyStatus(Entity target, string statusId, int stacks = 1, float? durationSeconds = null, Entity source = null)
        {
            throw new System.NotImplementedException();
        }

        public void Attack()
        {
            throw new System.NotImplementedException();
        }

        public bool CanHit(Entity attacker, Entity target, string abilityId)
        {
            throw new System.NotImplementedException();
        }

        public void DealAreaDamage(IEnumerable<Entity> targets, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null, IEnumerable<string> statusIds = null)
        {
            throw new System.NotImplementedException();
        }

        public void DealDamage(Entity target, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null)
        {
            throw new System.NotImplementedException();
        }

        public void Die()
        {
            throw new System.NotImplementedException();
        }

        public void Heal(Entity target, float amount, Entity source = null, IEnumerable<string> tags = null)
        {
            throw new System.NotImplementedException();
        }

        public void Hurt()
        {
            throw new System.NotImplementedException();
        }

        public override void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public float PreviewDamage(Entity attacker, Entity target, string abilityId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveStatus(Entity target, string statusId, int stacks = int.MaxValue)
        {
            throw new System.NotImplementedException();
        }

        public bool TryResolveAttack(Entity attacker, Entity target, string abilityId, out float finalDamage, out bool isCritical)
        {
           return _combat.TryResolveAttack(attacker,target,abilityId,out finalDamage, out isCritical);
        }
    }
}