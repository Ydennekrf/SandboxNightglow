using Godot;
using System;
using ethra.V1;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class CombatManager :  ISaveable, ICombat, IResolveable
{
        private string _saveKey = "Combat";
        private int _resolveOrder = 20;
        public string SaveKey => _saveKey;

        public int ResolveOrder => _resolveOrder;

        //=====ICOmbat=====//

        public void ApplyStatus(Entity target, string statusId, int stacks = 1, float? durationSeconds = null, Entity source = null)
        {
            throw new NotImplementedException();
        }

        public bool CanHit(Entity attacker, Entity target, string abilityId)
        {
            throw new NotImplementedException();
        }
         public bool TryResolveAttack(Entity attacker, Entity target, string abilityId, out float finalDamage, out bool isCritical)
        {
            throw new NotImplementedException();
        }
        

        public void DealAreaDamage(IEnumerable<Entity> targets, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null, IEnumerable<string> statusIds = null)
        {
            throw new NotImplementedException();
        }

        public void DealDamage(Entity target, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null)
        {
            throw new NotImplementedException();
        }

        public void Heal(Entity target, float amount, Entity source = null, IEnumerable<string> tags = null)
        {
            throw new NotImplementedException();
        }

        public float PreviewDamage(Entity attacker, Entity target, string abilityId)
        {
            throw new NotImplementedException();
        }

        public void RemoveStatus(Entity target, string statusId, int stacks = int.MaxValue)
        {
            throw new NotImplementedException();
        }
        //======IResolvable======//
        public void Resolve()
        {
            
        }

        public void Resolve(object obj)
        {
            
        }
        //=======ISavable========//
        public void RestoreSnapshot(object snapshot)
        {
            throw new NotImplementedException();
        }
        public object CaptureSnapshot()
        {
            throw new NotImplementedException();
        }
       

    }
}

