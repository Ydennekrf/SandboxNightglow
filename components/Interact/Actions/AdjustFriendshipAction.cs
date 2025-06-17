public class AdjustFriendshipAction : ChoiceAction
{
    public string TargetNpcId { get; init; }
    public int    Delta       { get; init; }
}