﻿namespace HouseRules.Essentials.Rules
{
    using System.Collections.Generic;
    using Boardgame;
    using Boardgame.BoardEntities;
    using Boardgame.BoardEntities.Abilities;
    using Data;
    using DataKeys;
    using HarmonyLib;
    using HouseRules.Types;
    using UnityEngine;

    public sealed class FreeHealOnHitRule : Rule, IConfigWritable<List<BoardPieceId>>, IPatchable, IMultiplayerSafe
    {
        public override string Description => "Hit restores health.";

        private static List<BoardPieceId> _globalAdjustments;
        private static bool _isActivated;
        private static int phase = 0;

        private readonly List<BoardPieceId> _adjustments;

        public FreeHealOnHitRule(List<BoardPieceId> adjustments)
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
                    typeof(FreeHealOnHitRule),
                    nameof(Ability_GenerateAttackDamage_Postfix)));
        }

        private static void Ability_GenerateAttackDamage_Postfix(Piece source, Piece mainTarget, Dice.Outcome diceResult)
        {
            if (!_isActivated)
            {
                return;
            }

            if ((source.IsPlayer() || source.IsBot() || source.IsWarlockMinion()) && mainTarget != null && mainTarget.boardPieceId == BoardPieceId.WizardBoss)
            {
                // Serpent Lord boss gets ONE damage resist and is immune to damage while invisible
                mainTarget.effectSink.TryGetStat(Stats.Type.DamageResist, out var damageResist);
                if (damageResist < 1)
                {
                    mainTarget.effectSink.TrySetStatBaseValue(Stats.Type.DamageResist, 1);
                }

                if (mainTarget.HasEffectState(EffectStateType.Invisible))
                {
                    mainTarget.EnableEffectState(EffectStateType.Invulnerable1);
                }

                return;
            }
            else if (source.IsPlayer())
            {
                if (diceResult != Dice.Outcome.Hit)
                {
                    return;
                }

                int chance = Random.Range(1, 101);
                if (_globalAdjustments.Contains(source.boardPieceId))
                {
                    int chance2 = Random.Range(1, 101);
                    if (source.boardPieceId == BoardPieceId.HeroRogue)
                    {
                        if (chance > 98 && chance2 > 50)
                        {
                            source.effectSink.Heal(2);
                            source.AnimateWobble();
                        }
                        else if (chance2 > 50)
                        {
                            source.effectSink.Heal(1);
                            source.AnimateWobble();
                        }
                    }
                    else if (chance > 98)
                    {
                        source.effectSink.Heal(2);
                        source.AnimateWobble();
                    }
                    else
                    {
                        source.effectSink.Heal(1);
                    }
                }
                else if (chance > 98)
                {
                    source.effectSink.Heal(1);
                    source.AnimateWobble();
                }

                return;
            }
            else if (source.boardPieceId == BoardPieceId.ElvenQueen)
            {
                // Elven Queen gets ONE damage resist after her first attack and now has 'phases' to make her more challenging
                source.effectSink.TryGetStat(Stats.Type.DamageResist, out var damageResist);
                if (damageResist < 1)
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.DamageResist, 1);
                }

                int nextPhase;
                int low = 1;
                int high = 6;
                if (source.GetHealth() >= (source.GetMaxHealth() / 2))
                {
                    nextPhase = Random.Range(1, 6);
                }
                else if (source.GetHealth() < (source.GetMaxHealth() / 3))
                {
                    source.EnableEffectState(EffectStateType.MagicShield);
                    source.effectSink.SetStatusEffectDuration(EffectStateType.MagicShield, 69);
                    source.EnableEffectState(EffectStateType.Courageous);
                    source.effectSink.SetStatusEffectDuration(EffectStateType.Courageous, 69);
                    return;
                }
                else if (source.GetHealth() < (source.GetMaxHealth() / 2))
                {
                    low = 2;
                    high = 4;
                    nextPhase = Random.Range(2, 4);
                }
                else
                {
                    low = 3;
                    high = 6;
                    nextPhase = Random.Range(3, 6);
                }

                while (nextPhase == phase)
                {
                    nextPhase = Random.Range(low, high);
                }

                phase = nextPhase;
                switch (nextPhase)
                {
                    case 1:
                        source.EnableEffectState(EffectStateType.Deflect);
                        source.effectSink.SetStatusEffectDuration(EffectStateType.Deflect, 1);
                        break;
                    case 2:
                        source.EnableEffectState(EffectStateType.MagicShield);
                        source.effectSink.SetStatusEffectDuration(EffectStateType.MagicShield, 1);
                        break;
                    case 3:
                        source.EnableEffectState(EffectStateType.FireImmunity);
                        source.effectSink.SetStatusEffectDuration(EffectStateType.FireImmunity, 1);
                        break;
                    case 4:
                        source.EnableEffectState(EffectStateType.Recovery);
                        source.effectSink.SetStatusEffectDuration(EffectStateType.Recovery, 2);
                        break;
                    case 5:
                        source.EnableEffectState(EffectStateType.Courageous);
                        source.effectSink.SetStatusEffectDuration(EffectStateType.Courageous, 1);
                        break;
                }

                return;
            }
            else if (source.boardPieceId == BoardPieceId.MotherCy || source.boardPieceId == BoardPieceId.RootLord || source.boardPieceId == BoardPieceId.BossTown || source.boardPieceId == BoardPieceId.RatKing)
            {
                // All other bosses also get ONE damage resist after their first attack!
                source.effectSink.TryGetStat(Stats.Type.DamageResist, out var damageResist);
                if (damageResist < 1)
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.DamageResist, 1);
                }

                return;
            }
            else if (source.IsWarlockMinion())
            {
                // Cana gets ONE damage resist after her first attack and Frenzy if below half health
                source.effectSink.TryGetStat(Stats.Type.DamageResist, out var damageResist);
                if (damageResist < 1)
                {
                    source.effectSink.TrySetStatBaseValue(Stats.Type.DamageResist, 1);
                }

                if (source.GetHealth() < source.GetMaxHealth() / 2)
                {
                    source.EnableEffectState(EffectStateType.Frenzy);
                    source.effectSink.SetStatusEffectDuration(EffectStateType.Frenzy, 1);
                }
                else
                {
                    source.DisableEffectState(EffectStateType.Frenzy);
                }

                return;
            }

            return;
        }
    }
}
