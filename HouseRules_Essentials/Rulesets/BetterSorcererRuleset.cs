﻿namespace HouseRules.Essentials.Rulesets
{
    using System.Collections.Generic;
    using DataKeys;
    using HouseRules.Essentials.Rules;
    using HouseRules.Types;

    internal static class BetterSorcererRuleset
    {
        internal static Ruleset Create()
        {
            const string name = "Better Sorcerer";
            const string description = "0 Action Cost for Sorcerer's Zap - No other changes. #STS";

            var abilityActionCostRule = new AbilityActionCostAdjustedRule(new Dictionary<AbilityKey, bool>
            {
                { AbilityKey.Zap, false },
            });

            var levelPropertiesRule = new LevelPropertiesModifiedRule(new Dictionary<string, int>
            {
                { "FloorOneElvenSummoners", 0 },
                { "FloorTwoElvenSummoners", 0 },
                { "FloorThreeElvenSummoners", 0 },
            });

            return Ruleset.NewInstance(
                name,
                description,
                levelPropertiesRule,
                abilityActionCostRule);
        }
    }
}
