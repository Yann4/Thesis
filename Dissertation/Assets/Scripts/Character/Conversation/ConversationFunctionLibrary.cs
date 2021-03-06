﻿using Dissertation.Character.Player;
using System;
using UnityEngine;
using Data = Dissertation.Character.ConversationFragment.ConversationData;

namespace Dissertation.Character
{
	public static class ConversationFunctionLibrary
	{
		public static void RunFunction( ConversationOutput toRun, Data parameters, BaseCharacterController speaker, BaseCharacterController listener )
		{
			switch (toRun)
			{
				case ConversationOutput.None:
					break;
				case ConversationOutput.TransferMoney:
					TransferMoney(speaker, listener, parameters.iVal);
					break;
				case ConversationOutput.Heal:
					HealListener(listener, parameters.iVal);
					break;
				case ConversationOutput.GiveAbility:
					GiveAbility(listener, parameters.sVal);
					break;
				case ConversationOutput.BuyAbility:
					GiveAbility(listener, parameters.sVal);
					TransferMoney(speaker, listener, parameters.iVal);
					break;
			}
		}

		private static void GiveAbility(BaseCharacterController listener, string ability)
		{
			switch(ability)
			{
				case "DoubleJump":
					{
						if (listener.MaxJumps < 2)
						{
							listener.MaxJumps = 2;
						}

						break;
					}
				case "Melee":
					{
						PlayerController player = listener as PlayerController;
						if(player != null)
						{
							player.UnlockAbility(Inventory.Ability.Square);
						}

						break;
					}
				case "Dash":
					{
						PlayerController player = listener as PlayerController;
						if (player != null)
						{
							player.UnlockAbility(Inventory.Ability.Circle);
						}

						break;
					}
				case "Shoot":
					{
						PlayerController player = listener as PlayerController;
						if (player != null)
						{
							player.UnlockAbility(Inventory.Ability.Triangle);
						}

						break;
					}
				default:
					UnityEngine.Debug.LogErrorFormat("Can't unlock ability '{0}'", ability);
					break;
			}
		}

		public static bool IsAvailable( ConversationPredicate predicate, BaseCharacterController speaker )
		{
			switch (predicate)
			{
				case ConversationPredicate.PlayerIsHurt:
					return IsPlayerHurt();
				case ConversationPredicate.NoDoubleJump:
					return App.AIBlackboard.Player.MaxJumps < 2;
				case ConversationPredicate.PlayerIsSquare:
					return App.AIBlackboard.Player.CurrentShape == CharacterFaction.Square;
				case ConversationPredicate.PlayerIsCircle:
					return App.AIBlackboard.Player.CurrentShape == CharacterFaction.Circle;
				case ConversationPredicate.PlayerIsTriangle:
					return App.AIBlackboard.Player.CurrentShape == CharacterFaction.Triangle;
				case ConversationPredicate.PlayerHasCashAndIsSquare:
					return App.AIBlackboard.Player.Inventory.Contents.Currency >= 1000 && App.AIBlackboard.Player.CurrentShape == CharacterFaction.Square;
				case ConversationPredicate.PlayerHasCashAndIsTriangle:
					return App.AIBlackboard.Player.Inventory.Contents.Currency >= 1000 && App.AIBlackboard.Player.CurrentShape == CharacterFaction.Triangle;
				case ConversationPredicate.NOTPlayerHasCashAndIsSquare:
					return !(App.AIBlackboard.Player.Inventory.Contents.Currency >= 1000 && App.AIBlackboard.Player.CurrentShape == CharacterFaction.Square);
				case ConversationPredicate.NOTPlayerHasCashAndIsTriangle:
					return !(App.AIBlackboard.Player.Inventory.Contents.Currency >= 1000 && App.AIBlackboard.Player.CurrentShape == CharacterFaction.Triangle);
				case ConversationPredicate.None:
				default:
					return true;
			}
		}

		public static bool ShouldRerun(RerunPredicate predicate, BaseCharacterController speaker)
		{
			switch (predicate)
			{
				case RerunPredicate.Never:
					return false;
				case RerunPredicate.Always:
					return true;
				case RerunPredicate.IfPlayerCantDoubleJump:
					return App.AIBlackboard.Player.MaxJumps == 1;
				case RerunPredicate.IfPlayerDoesntHaveTriangleAbility:
					return !App.AIBlackboard.Player.FactionUnlocked(CharacterFaction.Triangle);
				case RerunPredicate.IfPlayerDoesntHaveCircleAbility:
					return !App.AIBlackboard.Player.FactionUnlocked(CharacterFaction.Circle);
				default:
					Debug.LogErrorFormat("RerunPredicate {0} not implemented", predicate);
					return false;
			}
		}

		private static void TransferMoney(BaseCharacterController speaker, BaseCharacterController listener, int amount)
		{
			if (amount > 0)
			{
				speaker.Inventory.TransferCurrencyTo(listener.Inventory, amount);
			}
			else
			{
				listener.Inventory.TransferCurrencyTo(speaker.Inventory, -amount);
			}
		}

		private static void HealListener(BaseCharacterController listener, int healBy)
		{
			if (healBy == 0)
			{
				listener.Health.FullHeal();
			}
			else
			{
				listener.Health.Heal((uint)healBy);
			}
		}

		private static bool IsPlayerHurt()
		{
			return App.AIBlackboard.Player.Health.HealthPercentage < 1.0f;
		}
	}
}