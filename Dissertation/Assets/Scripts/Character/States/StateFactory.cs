﻿using System;
using UnityEngine;

namespace Dissertation.Character.AI
{
	public static class StateFactory
	{
		public static int NumSpecialistStates = Enum.GetNames(typeof(SpecialistStates)).Length;

		private static SpecialistState[] _referenceSpecialistStates = new SpecialistState[NumSpecialistStates - 1];

		static StateFactory()
		{
			for(int idx = 0; idx < NumSpecialistStates - 1; idx++)
			{
				SpecialistStates state = (SpecialistStates)(1 << idx);
				if (state != SpecialistStates.INVALID)
				{
					_referenceSpecialistStates[idx] = GetReferenceState(state);
				}
			}
		}

		public static State GetState(StateConfig config)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Construct state");

			State state = null;
			switch (config.StateType)
			{
				case States.MoveTo:
					Debug.Assert(config is MoveToState.MoveToConfig);
					state = new MoveToState(config as MoveToState.MoveToConfig);
					break;
				case States.Idle:
					Debug.Assert(config is IdleState.IdleConfig);
					state = new IdleState(config as IdleState.IdleConfig);
					break;
				case States.Traverse:
					Debug.Assert(config is TraverseState.TraverseStateConfig);
					state = new TraverseState(config as TraverseState.TraverseStateConfig);
					break;
				case States.PathTo:
					Debug.Assert(config is PathToState.PathToConfig);
					state = new PathToState(config as PathToState.PathToConfig);
					break;
				case States.Attack:
					{
						Debug.Assert(config is AttackState.AttackConfig);
						switch (config.Owner.Config.Faction)
						{
							case CharacterFaction.Circle:
								state = new DashAttackState(config as AttackState.AttackConfig);
								break;
							case CharacterFaction.Square:
								state = new MeleeAttackState(config as AttackState.AttackConfig);
								break;
							case CharacterFaction.Triangle:
								state = new RangedAttackState(config as AttackState.AttackConfig);
								break;
							default:
								Debug.Assert(false, "Shouldn't have got here");
								break;
						}
						break;
					}

				case States.Steal:
					Debug.Assert(config is StealState.StealConfig);
					state = new StealState(config as StealState.StealConfig);
					break;
				case States.Justice:
					Debug.Assert(config is ServeJusticeState.JusticeConfig);
					state = new ServeJusticeState(config as ServeJusticeState.JusticeConfig);
					break;
				case States.Flee:
					Debug.Assert(config is FleeState.FleeConfig);
					state = new FleeState(config as FleeState.FleeConfig);
					break;
				case States.Mine:
					Debug.Assert(config is MineCurrencyState.MineCurrencyConfig);
					state = new MineCurrencyState(config as MineCurrencyState.MineCurrencyConfig);
					break;
				case States.Defend:
				case States.INVALID:
				default:
					Debug.LogError("Factory not set up for state type " + config.StateType);
					break;
			}

			UnityEngine.Profiling.Profiler.EndSample();
			return state;
		}

		public static StateConfig GetDefaultState(States type, AgentController owner)
		{
			switch (type)
			{
				case States.Idle:
					return new IdleState.IdleConfig(owner, 3.0f);
				default:
					Debug.LogError("State type " + type + " is not currently a valid default state");
					return null;
			}
		}

		private static SpecialistState GetReferenceState(SpecialistStates state)
		{
			switch (state)
			{
				case SpecialistStates.Steal:
					return new StealState();
				case SpecialistStates.Justice:
					return new ServeJusticeState();
				case SpecialistStates.Flee:
					return new FleeState();
				case SpecialistStates.Defend:
					return new DefendState();
				case SpecialistStates.Mine:
					return new MineCurrencyState();
				case SpecialistStates.INVALID:
				default:
					Debug.Assert(false, "Can't get reference state for " + state);
					return null;
			}
		}

		public static bool ShouldEnterState(AgentController owner, SpecialistStates state, out StateConfig config)
		{
			Debug.Assert(state != SpecialistStates.INVALID);
			int idx = Array.FindIndex((SpecialistStates[])Enum.GetValues(typeof(SpecialistStates)), x => x == state) - 1; //-1 to account for SpecialistStates.INVALID

			return _referenceSpecialistStates[idx].ShouldRunState(owner, out config);
		}
	}
}