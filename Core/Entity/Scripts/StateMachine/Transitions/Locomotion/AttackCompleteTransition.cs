namespace ethra.V1.Transitions
{
    /// <summary>
    /// Leaves Attack once its animation-duration timer is complete.
    /// Selects locomotion target based on current move/run input.
    /// ONLY WORKS FOR PLAYER.
    /// </summary>
    public sealed class AttackCompleteTransition : IStateTransition
    {
        private readonly BaseState _idleTarget;
        private readonly BaseState _walkTarget;
        private readonly BaseState _runTarget;
        private readonly float _deadZone;

        private BaseState _chosenTarget;

        public BaseState Target => _chosenTarget ?? _idleTarget;

        public AttackCompleteTransition(BaseState idleTarget, BaseState walkTarget, BaseState runTarget, float deadZone = 0.05f)
        {
            _idleTarget = idleTarget;
            _walkTarget = walkTarget;
            _runTarget = runTarget;
            _deadZone = deadZone;
            _chosenTarget = _idleTarget;
        }

        public bool ShouldTransition(Entity owner)
        {
            if (owner is not Player player)
            {
                return false;
            }

            if (player.AttackTimerRemaining > 0f)
            {
                return false;
            }

            if (!player.ComboCanExitAttack)
            {
                return false;
            }

            bool isMoving = player.MoveInput.LengthSquared() > (_deadZone * _deadZone);
            if (!isMoving)
            {
                _chosenTarget = _idleTarget;
                return true;
            }

            _chosenTarget = player.RunPressed ? _runTarget : _walkTarget;
            return true;
        }
    }
}
