using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class AbilityEffectResource : Resource
    {
        [Export] public string EffectId { get; set; } = string.Empty;
        [Export] public AbilityEffectType EffectType { get; set; }
        [Export] public AbilityTargetStat TargetStat { get; set; } = AbilityTargetStat.None;
        [Export] public int Amount { get; set; }
        [Export] public string AbilityId { get; set; } = string.Empty;
        [Export] public string PassiveId { get; set; } = string.Empty;
        [Export] public string Payload { get; set; } = string.Empty;

        public AbilityEffectDefinition ToDefinition()
        {
            return new AbilityEffectDefinition
            {
                EffectId = EffectId,
                EffectType = EffectType,
                TargetStat = TargetStat,
                Amount = Amount,
                AbilityId = AbilityId,
                PassiveId = PassiveId,
                Payload = Payload
            };
        }
    }
}
