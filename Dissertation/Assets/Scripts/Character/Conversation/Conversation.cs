﻿using System.Collections;
using UnityEngine;
using Dissertation.Character.AI;
using Dissertation.UI;
using Dissertation.Util.Localisation;
using System;
using Dissertation.Util;
using System.Collections.Generic;
using Dissertation.Character.Player;
using Dissertation.Input;

namespace Dissertation.Character
{
	public class Conversation : MonoBehaviour
	{
		[SerializeField] private AgentController _owner;
		[SerializeField] private BoxCollider2D _conversationTrigger;

		public bool IsInConversation { get; private set; } = false;

		private ConversationPrompt _prompt;

		private ConversationFragment _currentFragment;
		private List<string> _availableConversations = new List<string>();

		private bool _dialogueClosed = false;
		private int _optionSelected = 0;

		private List<BaseCharacterController> _potentialParticipants = new List<BaseCharacterController>();
		private BaseCharacterController _talkingTo = null;

		public static Action<ConversationFragment, AgentController> ConversationStarted;
		public static Action<AgentController> ConversationEnded;

		private Coroutine _conversation = null;
		private DialogueBox _currentDialogue = null;
		private SpeechBubble _currentBubble = null;

		private void Start()
		{
			SetupConversations(_owner._agentConfig.AvailableConversations);

			_owner.Health.OnDied += OnOwnerDie;

			_prompt = HUD.Instance.CreateMenu<ConversationPrompt>();
			_prompt.Setup(_owner);
			_prompt.SetVisible(false);
		}

		private void OnDestroy()
		{
			if(_prompt != null)
			{
				HUD.Instance.DestroyMenu(_prompt);
			}
		}

		private void Update()
		{
			if (IsConversationAvailable() != -1)
			{
				foreach (BaseCharacterController character in _potentialParticipants)
				{
					if (character.CharacterYoke.GetButtonDown(InputAction.Interact))
					{
						TryStartConversation(character);
					}
				}
			}
		}

		//Sets up stack of conversations
		private void SetupConversations( TextAsset[] conversationReferences )
		{
			foreach(TextAsset reference in conversationReferences)
			{
				if(reference == null)
				{
					Debug.LogError(string.Format("Conversation reference was null on {0}", gameObject.name), gameObject);
					continue;
				}

				ConversationFragment conversation = App.AIBlackboard.GetOrLoadConversation(reference);
				if (conversation != null)
				{
					_availableConversations.Add(reference.name);
				}
				else
				{
					Debug.LogError("Couldn't find conversation with reference " + reference);
				}
			}
		}

		//Starts specific conversation
		private void StartConversation( string conversationReference, BaseCharacterController listener )
		{
			_currentFragment = App.AIBlackboard.GetConversation(conversationReference);
			_talkingTo = listener;

			_conversation = StartCoroutine(RunConversation(conversationReference));
		}

		//Starts next conversation off stack
		private void TryStartConversation(BaseCharacterController other)
		{
			int availableConversation = IsConversationAvailable();
			if ( availableConversation == -1 || IsInConversation || !_conversationTrigger.bounds.Contains(other.transform.position) )
			{
				return;
			}

			StartConversation(_availableConversations[availableConversation], other);
			_availableConversations.RemoveAt(availableConversation);
		}

		private IEnumerator RunConversation(string conversationReference)
		{
			SetPromptVisible( false );
			IsInConversation = true;
			ConversationStarted.InvokeSafe(_currentFragment, _owner);

			while(_currentFragment != null)
			{
				if(_currentFragment.IsPlayer)
				{
					yield return ShowPlayerSpeech(_currentFragment.ToSay[0], _currentFragment.ToSay[1]);
				}
				else
				{
					yield return ShowSpeech(_currentFragment.ToSay[0], false);
				}
			}

			_talkingTo = null;
			IsInConversation = false;

			if (ConversationFunctionLibrary.ShouldRerun(App.AIBlackboard.GetConversation(conversationReference).ShouldRerun, _owner))
			{
				_availableConversations.Insert(0, conversationReference);
			}

			ConversationEnded.InvokeSafe(_owner);

			if (_conversationTrigger.bounds.Contains(App.AIBlackboard.Player.transform.position))
			{
				SetPromptVisible(true);
			}

			_conversation = null;
		}

		private IEnumerator ShowSpeech(string locstring, bool isPlayer)
		{
			_optionSelected = 0;

			Transform toTrack = isPlayer ? App.AIBlackboard.Player.transform : _owner.transform;

			_dialogueClosed = false;

			_currentBubble = HUD.Instance.CreateMenu<SpeechBubble>();
			_currentBubble.OnClose += OnDialogueClosed;
			_currentBubble.Show(toTrack, LocManager.GetTranslation(locstring), PlayerPressedSkip);

			yield return new WaitUntil(() => _dialogueClosed);

			if (_currentBubble != null)
			{
				_currentBubble.OnClose -= OnDialogueClosed;
			}
		}

		private IEnumerator ShowPlayerSpeech(string option1, string option2)
		{
			if (string.IsNullOrEmpty(option2))
			{
				yield return ShowSpeech(option1, true);
			}
			else
			{
				_dialogueClosed = false;

				_currentDialogue = HUD.Instance.FindMenu<DialogueBox>();
				_currentDialogue.Show("", "", LocManager.GetTranslation(option1), LocManager.GetTranslation(option2), Option1Selected, Option2Selected);

				yield return new WaitUntil(() => _dialogueClosed);
			}
		}

		private void OnDialogueClosed()
		{
			_dialogueClosed = true;

			ConversationFunctionLibrary.RunFunction(_currentFragment.Output, _currentFragment.OptionOutputData[_optionSelected], _owner, _talkingTo);

			if (_currentFragment.NextFragments.Length > 0)
			{
				_currentFragment = _currentFragment.NextFragments[_optionSelected];
			}
			else
			{
				_currentFragment = null;
			}

			_currentDialogue = null;
			_currentBubble = null;
		}

		private void Option1Selected()
		{
			_optionSelected = 0;
			OnDialogueClosed();
		}

		private void Option2Selected()
		{
			_optionSelected = 1;
			OnDialogueClosed();
		}

		private bool PlayerPressedSkip()
		{
			return false;
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (IsConversationAvailable() != -1)
			{
				BaseCharacterController character = collision.gameObject.GetComponent<BaseCharacterController>();

				if (character != null)
				{
					if (character.GetType() == typeof(PlayerController))
					{
						SetPromptVisible(true);
					}

					_potentialParticipants.Add(character);
				}
			}
		}

		private void OnTriggerExit2D(Collider2D collision)
		{
			if (IsConversationAvailable() != -1)
			{
				BaseCharacterController character = collision.gameObject.GetComponent<BaseCharacterController>();

				if (character != null)
				{
					if (character.GetType() == typeof(PlayerController))
					{
						SetPromptVisible(false);
					}

					_potentialParticipants.Remove(character);
				}
			}
		}

		private void SetPromptVisible(bool visible)
		{
			_prompt.SetVisible( visible && IsConversationAvailable() != -1 );
		}

		private int IsConversationAvailable()
		{
			if(_availableConversations.Count > 0 && WantsToTalk())
			{
				for(int idx = 0; idx < _availableConversations.Count; idx++)
				{
					if (ConversationFunctionLibrary.IsAvailable(App.AIBlackboard.GetConversation(_availableConversations[idx]).IsAvailable, _owner))
					{
						return idx;
					}
				}
			}

			return -1;
		}

		private bool WantsToTalk()
		{
			return !_owner.Health.IsDead && !App.AIBlackboard.IsHostileToPlayer(_owner);
		}

		private void OnOwnerDie(BaseCharacterController obj)
		{
			_prompt.SetVisible(false);

			if (_conversation != null)
			{
				StopCoroutine(_conversation);
				_conversation = null;

				if (_currentDialogue != null)
				{
					HUD.Instance.DestroyMenu(_currentDialogue);
				}

				if (_currentBubble != null)
				{
					HUD.Instance.DestroyMenu(_currentBubble);
				}
			}
		}
	}
}