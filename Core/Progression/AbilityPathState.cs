using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    /// <summary>
    /// Serializable snapshot of a player's ability path progress.
    /// </summary>
    public class AbilityPathSave
    {
        public string PathId { get; set; } = string.Empty;
        public string CurrentNodeId { get; set; } = string.Empty;
        public List<string> UnlockedNodeIds { get; set; } = new();
        public List<string> UnlockedActiveAbilityIds { get; set; } = new();
        public List<string> UnlockedPassiveAbilityIds { get; set; } = new();
    }

    /// <summary>
    /// Runtime unlock state for one player ability path.
    /// </summary>
    /// <remarks>
    /// AbilityPathState combines static AbilityPathDefinition data with per-player unlocked node/ability IDs.
    /// It is owned by PlayerProgression/Player and should be saved through AbilityPathSave.
    /// </remarks>
    public sealed class AbilityPathState
    {
        private readonly HashSet<string> _unlockedNodeIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedActiveAbilityIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedPassiveAbilityIds = new(System.StringComparer.OrdinalIgnoreCase);
        private AbilityPathDefinition _definition;

        public string PathId => _definition?.PathId ?? string.Empty;
        public string CurrentNodeId { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> UnlockedNodeIds => _unlockedNodeIds;
        public IReadOnlyCollection<string> UnlockedActiveAbilityIds => _unlockedActiveAbilityIds;
        public IReadOnlyCollection<string> UnlockedPassiveAbilityIds => _unlockedPassiveAbilityIds;

        /// <summary>
        /// Initializes path progress from a static definition and unlocks the starting node.
        /// </summary>
        public void Initialize(AbilityPathDefinition definition)
        {
            _definition = definition;
            _unlockedNodeIds.Clear();
            _unlockedActiveAbilityIds.Clear();
            _unlockedPassiveAbilityIds.Clear();
            CurrentNodeId = string.Empty;

            if (_definition == null || string.IsNullOrWhiteSpace(_definition.StartingNodeId))
            {
                return;
            }

            CurrentNodeId = _definition.StartingNodeId;
            _unlockedNodeIds.Add(_definition.StartingNodeId);
        }

        /// <summary>
        /// Restores path progress from save data while retaining the supplied static definition.
        /// </summary>
        public void Restore(AbilityPathDefinition definition, AbilityPathSave save)
        {
            Initialize(definition);
            if (save == null)
            {
                return;
            }

            _unlockedNodeIds.Clear();
            foreach (string nodeId in save.UnlockedNodeIds ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    _unlockedNodeIds.Add(nodeId);
                }
            }

            if (_unlockedNodeIds.Count == 0 && !string.IsNullOrWhiteSpace(_definition?.StartingNodeId))
            {
                _unlockedNodeIds.Add(_definition.StartingNodeId);
            }

            CurrentNodeId = !string.IsNullOrWhiteSpace(save.CurrentNodeId)
                ? save.CurrentNodeId
                : _definition?.StartingNodeId ?? string.Empty;

            _unlockedActiveAbilityIds.Clear();
            foreach (string abilityId in save.UnlockedActiveAbilityIds ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(abilityId))
                {
                    _unlockedActiveAbilityIds.Add(abilityId);
                }
            }

            _unlockedPassiveAbilityIds.Clear();
            foreach (string passiveId in save.UnlockedPassiveAbilityIds ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(passiveId))
                {
                    _unlockedPassiveAbilityIds.Add(passiveId);
                }
            }

            RebuildAbilityUnlockIdsFromNodes();
        }

        /// <summary>
        /// Captures current unlock state for save serialization.
        /// </summary>
        public AbilityPathSave CaptureSnapshot()
        {
            return new AbilityPathSave
            {
                PathId = PathId,
                CurrentNodeId = CurrentNodeId,
                UnlockedNodeIds = _unlockedNodeIds.OrderBy(id => id).ToList(),
                UnlockedActiveAbilityIds = _unlockedActiveAbilityIds.OrderBy(id => id).ToList(),
                UnlockedPassiveAbilityIds = _unlockedPassiveAbilityIds.OrderBy(id => id).ToList()
            };
        }

        /// <summary>
        /// Rebinds static definition data without intentionally clearing unlocked state.
        /// </summary>
        public void SetDefinitionPreservingState(AbilityPathDefinition definition)
        {
            AbilityPathSave snapshot = CaptureSnapshot();
            if (_unlockedNodeIds.Count == 0)
            {
                Initialize(definition);
                return;
            }

            Restore(definition, snapshot);
        }

        public IReadOnlyList<AbilityPathNodeDefinition> GetNodes()
        {
            if (_definition?.Nodes == null)
            {
                return System.Array.Empty<AbilityPathNodeDefinition>();
            }

            return _definition.Nodes;
        }

        public IReadOnlyList<AbilityPathNodeDefinition> GetUnlockableNodes()
        {
            if (_definition?.Nodes == null)
            {
                return System.Array.Empty<AbilityPathNodeDefinition>();
            }

            return _definition.Nodes
                .Where(node => node != null && !_unlockedNodeIds.Contains(node.NodeId) && IsConnectedToUnlockedNode(node))
                .OrderBy(node => node.DisplayName)
                .ToList();
        }

        public bool TryGetNode(string nodeId, out AbilityPathNodeDefinition node)
        {
            node = _definition?.Nodes?.FirstOrDefault(n => n.NodeId == nodeId);
            return node != null;
        }

        public bool IsUnlocked(string nodeId) => !string.IsNullOrWhiteSpace(nodeId) && _unlockedNodeIds.Contains(nodeId);
        public bool IsUnlockable(string nodeId)
        {
            return !string.IsNullOrWhiteSpace(nodeId)
                && TryGetNode(nodeId, out AbilityPathNodeDefinition node)
                && !_unlockedNodeIds.Contains(nodeId)
                && IsConnectedToUnlockedNode(node);
        }
        public bool HasActiveAbility(string abilityId) => !string.IsNullOrWhiteSpace(abilityId) && _unlockedActiveAbilityIds.Contains(abilityId);
        public bool HasPassiveAbility(string passiveId) => !string.IsNullOrWhiteSpace(passiveId) && _unlockedPassiveAbilityIds.Contains(passiveId);

        /// <summary>
        /// Attempts to spend points and unlock an adjacent ability node for the supplied player.
        /// </summary>
        public bool TryUnlockNode(string nodeId, Player player, out string message)
        {
            message = string.Empty;
            if (player == null)
            {
                message = "Player is unavailable.";
                return false;
            }

            if (!TryGetNode(nodeId, out AbilityPathNodeDefinition node))
            {
                message = "Ability node was not found.";
                return false;
            }

            if (_unlockedNodeIds.Contains(node.NodeId))
            {
                message = "Node is already unlocked.";
                return false;
            }

            if (!IsConnectedToUnlockedNode(node))
            {
                message = "Unlock a connected node first.";
                return false;
            }

            int cost = Godot.Mathf.Max(0, node.Cost);
            if (!player.Progression.TrySpendAbilityPoints(cost))
            {
                message = "Not enough ability points.";
                return false;
            }

            _unlockedNodeIds.Add(node.NodeId);
            CurrentNodeId = node.NodeId;
            AbilityEffectApplier.ApplyEffects(player, node.Effects, _unlockedActiveAbilityIds, _unlockedPassiveAbilityIds);
            message = $"Unlocked {node.DisplayName}.";
            return true;
        }

        private bool IsConnectedToUnlockedNode(AbilityPathNodeDefinition node)
        {
            if (node?.ConnectedNodeIds == null)
            {
                return false;
            }

            return node.ConnectedNodeIds.Any(id => _unlockedNodeIds.Contains(id));
        }

        private void RebuildAbilityUnlockIdsFromNodes()
        {
            if (_definition?.Nodes == null)
            {
                return;
            }

            foreach (AbilityPathNodeDefinition node in _definition.Nodes)
            {
                if (node == null || !_unlockedNodeIds.Contains(node.NodeId) || node.Effects == null)
                {
                    continue;
                }

                foreach (AbilityEffectDefinition effect in node.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    if (effect.EffectType == AbilityEffectType.UnlockActiveAbility && !string.IsNullOrWhiteSpace(effect.AbilityId))
                    {
                        _unlockedActiveAbilityIds.Add(effect.AbilityId);
                    }
                    else if (effect.EffectType == AbilityEffectType.UnlockPassiveAbility && !string.IsNullOrWhiteSpace(effect.PassiveId))
                    {
                        _unlockedPassiveAbilityIds.Add(effect.PassiveId);
                    }
                }
            }
        }
    }
}
