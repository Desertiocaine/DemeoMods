﻿namespace HouseRules.Essentials.Rules
{
    using System;
    using System.Collections.Generic;
    using Boardgame;
    using Boardgame.Board;
    using Boardgame.BoardEntities.Abilities;
    using DataKeys;
    using HarmonyLib;
    using HouseRules.Core;
    using HouseRules.Core.Types;

    public sealed class AbilityAoeAdjustedRule : Rule, IConfigWritable<Dictionary<AbilityKey, int>>, IMultiplayerSafe
    {
        public override string Description => "Some ability AOE ranges are adjusted";

        protected override SyncableTrigger ModifiedSyncables => SyncableTrigger.StatusEffectDataModified;

        private readonly Dictionary<AbilityKey, int> _adjustments;
        private Dictionary<AbilityKey, int> _originals;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityAoeAdjustedRule"/> class.
        /// </summary>
        /// <param name="adjustments">Key-value pairs mapping the name of an ability and the AOE range
        /// added to their base.</param>
        /// <remarks>Adding '1' to a 3x3 spell will make a 5x5. Negative values will reduce AOE.</remarks>
        public AbilityAoeAdjustedRule(Dictionary<AbilityKey, int> adjustments)
        {
            _adjustments = adjustments;
            _originals = new Dictionary<AbilityKey, int>();
        }

        public Dictionary<AbilityKey, int> GetConfigObject() => _adjustments;

        protected override void OnPreGameCreated(GameContext gameContext)
        {
            _originals = ReplaceAbilities(_adjustments);
        }

        protected override void OnDeactivate(GameContext gameContext)
        {
            ReplaceAbilities(_originals);
        }

        private static Dictionary<AbilityKey, int> ReplaceAbilities(Dictionary<AbilityKey, int> replacements)
        {
            var originals = new Dictionary<AbilityKey, int>();

            foreach (var replacement in replacements)
            {
                if (!AbilityFactory.TryGetAbility(replacement.Key, out var ability))
                {
                    throw new InvalidOperationException(
                        $"AbilityKey [{replacement.Key}] does not have a corresponding ability.");
                }

                originals[replacement.Key] = -replacement.Value;
                var aoe = Traverse.Create(ability).Field<AreaOfEffect>("areaOfEffect").Value;
                Traverse.Create(aoe).Field<int>("range").Value += replacement.Value; // Adjust the AOE outline when casting.
                ability.areaOfEffectRange += replacement.Value; // Adjust value displayed on the card.

                if (HR.SelectedRuleset.Name.Contains("Revolutions"))
                {
                    if (replacement.Key == AbilityKey.Net)
                    {
                        ability.addTileEffectToTile = TileEffect.Web;
                        ability.areaOfEffectCritRange = 0;
                    }
                }
            }

            return originals;
        }
    }
}
