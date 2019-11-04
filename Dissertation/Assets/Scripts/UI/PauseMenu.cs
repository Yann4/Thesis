﻿using Dissertation.Character.Player;
using Dissertation.Input;
using Dissertation.Util.Localisation;
using UnityEngine;

namespace Dissertation.UI
{
	public class PauseMenu : MenuBase, IPauser
	{
		public override void Initialise()
		{
			base.Initialise();
			SetVisible(false);
		}

		protected override void Update()
		{
			if(InputManager.GetButtonDown(InputAction.TogglePause))
			{
				if (!IsOpen)
				{
					OpenMenu();
				}
				else
				{
					CloseMenu();
				}
			}
		}

		public void ToggleAgentDebugUI()
		{
			foreach(AgentDebugUI menu in HUD.Instance.FindMenus<AgentDebugUI>())
			{
				if(menu.IsVisible())
				{
					menu.CloseMenu();
				}
				else
				{
					menu.OpenMenu();
				}
			}
		}

		public void FullyHealPlayer()
		{
			PlayerController player = FindObjectOfType<PlayerController>();
			player.Health.Heal((uint)player.Config.MaxHealth);
		}

		public void TryQuit()
		{
			DialogueBox dialogue = HUD.Instance.FindMenu<DialogueBox>();
			dialogue.Show(LocManager.GetTranslation("/Menu/Quit_Header"), LocManager.GetTranslation("/Menu/Quit_Body"), LocManager.GetTranslation("/Menu/Ok"), LocManager.GetTranslation("/Menu/Cancel"),
				() => App.Quit());
		}

		public override void SetVisible(bool visible)
		{
			_canvas.enabled = visible;
		}

		public override bool IsVisible()
		{
			return _canvas.enabled;
		}
	}
}