﻿using Dissertation.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dissertation.Character
{
	public class Inventory : MonoBehaviour
	{
		public enum Ability
		{
			DoubleJump,
			Triangle,
			Circle,
			Square
		}

		[Serializable]
		public class InventoryContents
		{
			public uint Currency = 0;
			public List<Ability> Abilities = new List<Ability>();

			public InventoryContents() { }

			public InventoryContents(InventoryContents other)
			{
				Currency = other.Currency;
				Abilities = new List<Ability>(other.Abilities);
			}

			public void Add(InventoryContents additionalContents)
			{
				if(additionalContents != null)
				{
					Currency += additionalContents.Currency;
					Abilities.AddRange(additionalContents.Abilities);

					additionalContents.Clear();
				}
			}

			public void Clear()
			{
				Currency = 0;
				Abilities.Clear();
			}

			public bool IsEmpty()
			{
				return Currency == 0 && Abilities.Count == 0;
			}

			public InventoryContents Copy()
			{
				return new InventoryContents(this);
			}
		}

		[SerializeField] private GameObject _prompt;
		[SerializeField] private UnityEngine.UI.Image _progressbar;

		[SerializeField] private float _pickDuration = 4.0f;
		[SerializeField] private InventoryContents _baseContents;
		[SerializeField] private bool _placedContainer = false;
		[SerializeField] private Spawner _linkedSpawner;
		[SerializeField, Tooltip("Time for placed container to refill it's contents (with baseContents). 0.0 means it doesn't refill")] private float _refillTime = 0.0f;

		public InventoryContents Contents { get; private set; } = new InventoryContents();

		public BaseCharacterController Owner { get; private set; }
		public bool OnGround { get; private set; } = false;

		private Vector3 _deathLocation;

		private List<BaseCharacterController> _pickers = new List<BaseCharacterController>();
		private float _pickerEnterTime;
		private Coroutine _pickUp;

		public Action<int> OnGetCurrency;
		public Action<int> OnLoseCurrency;

		private void Start()
		{
			OnGround |= _placedContainer;
			Contents.Add(_baseContents.Copy());

			if (!OnGround && Owner != null)
			{
				App.WorldState.SetState(new Narrative.WorldProperty(Owner.ID, Narrative.EProperty.MoneyEqual, Contents.Currency));
			}

			if (_linkedSpawner != null)
			{
				_linkedSpawner.OnSpawnNonStatic += OnSpawn;
			}
		}

		private void OnSpawn(BaseCharacterController spawned)
		{
			Owner = spawned;
		}

		public void Initialise(BaseCharacterController owner, InventoryContents initialContents, bool dropped = false)
		{
			Contents.Add(initialContents);
			Owner = owner;
			OnGround = dropped;

			Debug.Assert(Contents != null);

			if (!OnGround)
			{
				Owner.Health.OnDied += OnDie;
			}
		}

		public void Add(InventoryContents additionalContents)
		{
			int currencyChange = (int)additionalContents.Currency;
			Contents.Add(additionalContents);

			OnGetCurrency.InvokeSafe(currencyChange);

			if(Owner != null)
			{
				App.WorldState.SetState(new Narrative.WorldProperty(Owner.ID, Narrative.EProperty.MoneyEqual, Contents.Currency));
				foreach(Ability ability in Contents.Abilities)
				{
					Owner.UnlockAbility(ability);
				}
			}
		}

		public void TransferCurrencyTo(Inventory other, int amount)
		{
			if (Contents.Currency >= amount)
			{
				other.Contents.Currency += (uint)amount;
				Contents.Currency -= (uint)amount;

				OnLoseCurrency.InvokeSafe(amount);

				if (Owner != null)
				{
					App.WorldState.SetState(new Narrative.WorldProperty(Owner.ID, Narrative.EProperty.MoneyEqual, Contents.Currency));
				}
			}
		}

		private void OnDestroy()
		{
			if (!OnGround && Owner != null)
			{
				Owner.Health.OnDied -= OnDie;
			}
		}

		private void OnDie(BaseCharacterController died)
		{
			_deathLocation = Owner.transform.position;
			DropInventory();
		}

		private void DropInventory()
		{
			if(!Contents.IsEmpty())
			{
				Inventory droppedInventory = Instantiate(Owner.Config.DropInventoryPrefab, _deathLocation, Quaternion.identity).GetComponent<Inventory>();
				droppedInventory.Initialise(Owner, Contents, true);
			}
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (OnGround && !Contents.IsEmpty())
			{
				BaseCharacterController controller = collision.gameObject.GetComponent<BaseCharacterController>();
				if ( controller != null && !_pickers.Contains(controller) )
				{
					_pickers.Add(controller);
					_pickerEnterTime = Time.time;

					if (controller is Player.PlayerController)
					{
						_prompt.SetActive(true);
					}

					if (_pickDuration != 0)
					{
						_pickUp = StartCoroutine(TryPickUp(controller));
					}
				}
			}
		}

		private void Update()
		{
			if( _pickDuration == 0 && !Contents.IsEmpty() )
			{
				foreach (BaseCharacterController picker in _pickers)
				{
					if(picker.CharacterYoke.GetButton(Input.InputAction.Interact))
					{
						PickUp(picker);
					}
				}
			}
		}

		private IEnumerator TryPickUp(BaseCharacterController picker)
		{
			if (OnGround)
			{
				float stayDuration = 0.0f;
				while (stayDuration < _pickDuration)
				{
					stayDuration = (Time.time - _pickerEnterTime);
					_progressbar.fillAmount = (stayDuration / _pickDuration);
					yield return null;
				}

				PickUp(picker);
				Destroy(gameObject);
			}
		}

		private void PickUp(BaseCharacterController picker)
		{
			picker.Inventory.Add(Contents);
			_prompt.SetActive(false);

			if( Owner != null && picker != Owner )
			{
				App.AIBlackboard.AddCriminal(picker);
			}

			if(_refillTime != 0.0f)
			{
				StartCoroutine(RefillContainer());
			}
		}

		private IEnumerator RefillContainer()
		{
			yield return new WaitForSeconds(_refillTime);

			Contents.Add( _baseContents );

			if(_pickers.Any(picker => picker is Player.PlayerController))
			{
				_prompt.SetActive(true);
			}
		}

		private void OnTriggerExit2D(Collider2D collision)
		{
			if (OnGround)
			{
				BaseCharacterController picker = _pickers.Find(p => p.gameObject == collision.gameObject);
				if ( picker != null )
				{
					_pickers.Remove(picker);

					if (picker is Player.PlayerController)
					{
						_prompt.SetActive(false);
					}

					if (_pickUp != null)
					{
						StopCoroutine(_pickUp);
						_pickUp = null;
					}
				}
			}
		}
	}
}