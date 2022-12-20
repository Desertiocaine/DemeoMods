namespace HouseRules.Essentials.Rules
{
    using System.Collections.Generic;
    using Boardgame;
    using Boardgame.BoardEntities;
    using Boardgame.BoardEntities.Abilities;
    using DataKeys;
    using HarmonyLib;
    using HouseRules.Types;
    using UnityEngine;

    public sealed class FreeActionPointsOnCritRule : Rule, IConfigWritable<List<BoardPieceId>>, IPatchable, IMultiplayerSafe
    {
        public override string Description => "Critical Hit restores action points.";

        private static List<BoardPieceId> _globalAdjustments;
        private static bool _isActivated;

        private readonly List<BoardPieceId> _adjustments;

        public FreeActionPointsOnCritRule(List<BoardPieceId> adjustments)
        {
            _adjustments = adjustments;
        }

        public List<BoardPieceId> GetConfigObject() => _adjustments;

        protected override void OnActivate(GameContext gameContext)
        {
            _globalAdjustments = _adjustments;
            _isActivated = true;
        }

        protected override void OnDeactivate(GameContext gameContext) => _isActivated = false;

        private static void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Ability), "GenerateAttackDamage"),
                postfix: new HarmonyMethod(
                    typeof(FreeActionPointsOnCritRule),
                    nameof(Ability_GenerateAttackDamage_Postfix)));
        }

        private static void Ability_GenerateAttackDamage_Postfix(Piece source, Dice.Outcome diceResult)
        {
            if (!_isActivated)
            {
                return;
            }

            if (!source.IsPlayer())
            {
                return;
            }

            if (diceResult != Dice.Outcome.Crit)
            {
                return;
            }

            if (!_globalAdjustments.Contains(source.boardPieceId))
            {
                return;
            }

            source.effectSink.TryGetStat(Stats.Type.ActionPoints, out int currentAP);
            if (source.boardPieceId == BoardPieceId.HeroGuardian)
            {
                if (currentAP < 1)
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.ActionPoints, currentAP + 2);
                }
                else
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.ActionPoints, currentAP + 1);
                }

                source.EnableEffectState(EffectStateType.PlayerBerserk);
                source.effectSink.SetStatusEffectDuration(EffectStateType.PlayerBerserk, 1);
            }
            else if (source.boardPieceId == BoardPieceId.HeroRogue)
            {
                if (currentAP < 1)
                {
                    source.EnableEffectState(EffectStateType.Invisibility);
                    source.effectSink.SetStatusEffectDuration(EffectStateType.Invisibility, 2);
                }
                else
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.ActionPoints, currentAP + 1);
                }
            }
            else if (source.boardPieceId == BoardPieceId.HeroBarbarian && !source.HasEffectState(EffectStateType.Enraged))
            {
                if (currentAP < 1)
                {
                    source.EnableEffectState(EffectStateType.Enraged);
                    source.effectSink.SetStatusEffectDuration(EffectStateType.Enraged, 1);
                }
                else
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.ActionPoints, currentAP + 1);
                }
            }
        }
    }
}
