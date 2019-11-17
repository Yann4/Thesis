﻿using Data = Dissertation.Character.ConversationFragment.ConversationData;

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
			}
		}

		public static bool IsAvailable( ConversationPredicate predicate, BaseCharacterController speaker )
		{
			switch(predicate)
			{
				case ConversationPredicate.PlayerIsHurt:
					return IsPlayerHurt();
				default:
					return true;
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