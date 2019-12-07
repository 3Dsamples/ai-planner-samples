using System;
#if PLANNER_DOMAIN_GENERATED
using AI.Planner.Domains;
using AI.Planner.Domains.Enums;
using Unity.AI.Planner.DomainLanguage.TraitBased;
#endif

namespace AI.Planner.Actions.Match3Plan 
{
#if PLANNER_DOMAIN_GENERATED

	public struct CustomSwapReward : ICustomReward<StateData>
	{
		public const int GoalReward = 8;
		public const int BasicReward = 1;

		public float RewardModifier(StateData originalState, ActionKey action, StateData newState)
		{
			var gameId = newState.GetTraitBasedObjectId(action[Match3Utility.GameIndex]);
			var game = newState.GetTraitBasedObject(gameId);
			var gameTrait = newState.GetTraitOnObject<Game>(game);

			if (gameTrait.Score > 0)
				return gameTrait.Score;
			
			return -1;
		}
	}
#endif
}