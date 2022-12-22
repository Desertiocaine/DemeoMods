﻿namespace HouseRules.Essentials.Rules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Boardgame;
    using Boardgame.BoardEntities.AI;
    using Boardgame.Data;
    using Boardgame.SerializableEvents;
    using DataKeys;
    using HarmonyLib;
    using HouseRules.Types;
    using Random = UnityEngine.Random;

    public sealed class CardAdditionOverriddenRule : Rule, IConfigWritable<Dictionary<BoardPieceId, List<AbilityKey>>>,
        IPatchable, IMultiplayerSafe
    {
        public override string Description => "Card additions are overridden";

        private static Dictionary<BoardPieceId, List<AbilityKey>> _globalHeroCards;
        private static bool _isActivated;
        private static bool _isPotionStand;
        private static bool _isWaterBottleChest;
        private static bool _isVortexDustChest;
        private static int _numPlayers;
        private static int _numVigor;
        private static int _numAlags;
        private static int _numEnergy;

        private readonly Dictionary<BoardPieceId, List<AbilityKey>> _heroCards;

        public CardAdditionOverriddenRule(Dictionary<BoardPieceId, List<AbilityKey>> heroCards)
        {
            _heroCards = heroCards;
        }

        public Dictionary<BoardPieceId, List<AbilityKey>> GetConfigObject() => _heroCards;

        protected override void OnActivate(GameContext gameContext)
        {
            _globalHeroCards = _heroCards;
            _isActivated = true;
        }

        protected override void OnDeactivate(GameContext gameContext)
        {
            _numAlags = 0;
            _numEnergy = 0;
            _numVigor = 0;
            _isActivated = false;
        }

        private static void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Interactable), "OnInteraction", new Type[] { typeof(int), typeof(IntPoint2D), typeof(GameContext), typeof(int) }),
                prefix: new HarmonyMethod(
                    typeof(CardAdditionOverriddenRule),
                    nameof(Interactable_OnInteraction_Prefix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(SerializableEventQueue), "RespondToRequest"),
                prefix: new HarmonyMethod(
                    typeof(CardAdditionOverriddenRule),
                    nameof(SerializableEventQueue_RespondToRequest_Prefix)));
        }

        private static void Interactable_OnInteraction_Prefix(
            GameContext gameContext,
            IntPoint2D targetTile)
        {
            if (!_isActivated)
            {
                return;
            }

            if (!gameContext.pieceAndTurnController.GetInteractableAtPosition(targetTile))
            {
                return;
            }

            _isPotionStand = false;
            Interactable whatIsit = gameContext.pieceAndTurnController.GetInteractableAtPosition(targetTile);
            if (whatIsit.type == Interactable.Type.PotionStand)
            {
                _numPlayers = gameContext.pieceAndTurnController.GetNumberOfPlayerPieces();
                _isPotionStand = true;
            }
            else if (whatIsit.type == Interactable.Type.WaterBottleChest)
            {
                _numPlayers = gameContext.pieceAndTurnController.GetNumberOfPlayerPieces();
                _isWaterBottleChest = true;
            }
            else if (whatIsit.type == Interactable.Type.VortexDustChest)
            {
                _numPlayers = gameContext.pieceAndTurnController.GetNumberOfPlayerPieces();
                _isVortexDustChest = true;
            }
        }

        private static void SerializableEventQueue_RespondToRequest_Prefix(
            SerializableEventQueue __instance,
            ref SerializableEvent request)
        {
            if (!_isActivated)
            {
                return;
            }

            if (request.type != SerializableEvent.Type.AddCardToPiece)
            {
                return;
            }

            if (_isPotionStand)
            {
                // TODO: Add method to allow custom card loot for Potion Stands here
                if (_numPlayers > 1)
                {
                    _numPlayers--;
                }
                else
                {
                    _isPotionStand = false;
                }

                return;
            }
            else if (_isWaterBottleChest)
            {
                // TODO: Add method to allow custom card loot for Water Bottle Chests here
                if (_numPlayers > 1)
                {
                    _numPlayers--;
                }
                else
                {
                    _isWaterBottleChest = false;
                }

                return;
            }
            else if (_isVortexDustChest)
            {
                // TODO: Add method to allow custom card loot for Vortex Dust Chests here
                if (_numPlayers > 1)
                {
                    _numPlayers--;
                }
                else
                {
                    _isVortexDustChest = false;
                }

                return;
            }

            var addCardToPieceEvent = (SerializableEventAddCardToPiece)request;
            var gameContext = Traverse.Create(__instance).Property<GameContext>("gameContext").Value;
            var pieceId = Traverse.Create(addCardToPieceEvent).Field<int>("pieceId").Value;
            var cardSource = Traverse.Create(addCardToPieceEvent).Field<int>("cardSource").Value;

            if (cardSource != (int)MotherTracker.Context.Energy && cardSource != (int)MotherTracker.Context.Chest)
            {
                return;
            }

            if (!gameContext.pieceAndTurnController.TryGetPiece(pieceId, out var piece))
            {
                return;
            }

            if (!piece.IsPlayer())
            {
                return;
            }

            if (!_globalHeroCards.TryGetValue(piece.boardPieceId, out var replacementAbilityKeys))
            {
                return;
            }

            int rand;
            AbilityKey replacementAbilityKey;
            if (HR.SelectedRuleset.Name.Contains("Demeo Revolutions"))
            {
                int randNum = Random.Range(1, 101);
                if (randNum < 51)
                {
                    // Class cards
                    rand = Random.Range(replacementAbilityKeys.Count - 15, replacementAbilityKeys.Count);
                }
                else if (randNum > 97)
                {
                    if (_numVigor < 5)
                    {
                        // Invisibility, Vigor Potion, and Reveal Path
                        _numVigor++;
                        rand = Random.Range(replacementAbilityKeys.Count - 24, replacementAbilityKeys.Count - 21);
                    }
                    else if (_numAlags < 4)
                    {
                        // Rejuv and Damage Resist Potions
                        _numAlags++;
                        rand = Random.Range(replacementAbilityKeys.Count - 26, replacementAbilityKeys.Count - 24);
                    }
                    else if (_numEnergy < 2)
                    {
                        // Energy
                        _numEnergy++;
                        rand = replacementAbilityKeys.Count - 27;
                        EssentialsMod.Logger.Msg($"Energy [{rand}]");
                    }
                    else
                    {
                        if (piece.boardPieceId == BoardPieceId.HeroWarlock || piece.boardPieceId == BoardPieceId.HeroSorcerer)
                        {
                            // Casters can get Magic Potions but not Strength
                            rand = Random.Range(replacementAbilityKeys.Count - 20, replacementAbilityKeys.Count - 15);
                        }
                        else
                        {
                            // Melee can get Strength Potions but not Magic
                            rand = Random.Range(replacementAbilityKeys.Count - 21, replacementAbilityKeys.Count - 16);
                        }
                    }
                }
                else if (randNum > 92)
                {
                    if (_numAlags < 4)
                    {
                        // Rejuv and Damage Resist Potions
                        _numAlags++;
                        rand = Random.Range(replacementAbilityKeys.Count - 26, replacementAbilityKeys.Count - 24);
                    }
                    else if (_numEnergy < 2)
                    {
                        // Energy
                        _numEnergy++;
                        rand = replacementAbilityKeys.Count - 27;
                    }
                    else
                    {
                        if (piece.boardPieceId == BoardPieceId.HeroWarlock || piece.boardPieceId == BoardPieceId.HeroSorcerer)
                        {
                            // Casters can get Magic Potions but not Strength
                            rand = Random.Range(replacementAbilityKeys.Count - 20, replacementAbilityKeys.Count - 15);
                        }
                        else
                        {
                            // Melee can get Strength Potions but not Magic
                            rand = Random.Range(replacementAbilityKeys.Count - 21, replacementAbilityKeys.Count - 16);
                        }
                    }
                }
                else if (randNum > 80)
                {
                    if (_numEnergy < 2)
                    {
                        // Energy
                        _numEnergy++;
                        rand = replacementAbilityKeys.Count - 27;
                    }
                    else
                    {
                        if (piece.boardPieceId == BoardPieceId.HeroWarlock || piece.boardPieceId == BoardPieceId.HeroSorcerer)
                        {
                            // Casters can get Magic Potions but not Strength
                            rand = Random.Range(replacementAbilityKeys.Count - 20, replacementAbilityKeys.Count - 15);
                        }
                        else
                        {
                            // Melee can get Strength Potions but not Magic
                            rand = Random.Range(replacementAbilityKeys.Count - 21, replacementAbilityKeys.Count - 16);
                        }
                    }
                }
                else if (randNum > 75)
                {
                    // Very good Potions/Cards
                    if (piece.boardPieceId == BoardPieceId.HeroWarlock || piece.boardPieceId == BoardPieceId.HeroSorcerer)
                    {
                        // Casters can get Magic Potions but not Strength
                        rand = Random.Range(replacementAbilityKeys.Count - 20, replacementAbilityKeys.Count - 15);
                    }
                    else
                    {
                        // Melee can get Strength Potions but not Magic
                        rand = Random.Range(replacementAbilityKeys.Count - 21, replacementAbilityKeys.Count - 16);
                    }
                }
                else
                {
                    // Standard cards
                    rand = Random.Range(0, replacementAbilityKeys.Count - 27);
                }

                replacementAbilityKey = replacementAbilityKeys[rand];
            }
            else
            {
                rand = Random.Range(0, replacementAbilityKeys.Count);
                replacementAbilityKey = replacementAbilityKeys[rand];
            }

            Traverse.Create(addCardToPieceEvent).Field<AbilityKey>("card").Value = replacementAbilityKey;
        }
    }
}
