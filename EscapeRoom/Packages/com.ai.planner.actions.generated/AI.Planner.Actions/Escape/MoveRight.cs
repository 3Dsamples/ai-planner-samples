using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Burst;
using AI.Planner.Domains;
using AI.Planner.Domains.Enums;

namespace AI.Planner.Actions.Escape
{
    [BurstCompile]
    struct MoveRight : IJobParallelForDefer
    {
        public Guid ActionGuid;
        
        const int k_CharacterIndex = 0;
        const int k_ToIndex = 1;
        const int k_FromIndex = 2;
        const int k_MaxArguments = 3;

        [ReadOnly] NativeArray<StateEntityKey> m_StatesToExpand;
        StateDataContext m_StateDataContext;

        internal MoveRight(Guid guid, NativeList<StateEntityKey> statesToExpand, StateDataContext stateDataContext)
        {
            ActionGuid = guid;
            m_StatesToExpand = statesToExpand.AsDeferredJobArray();
            m_StateDataContext = stateDataContext;
        }

        public static int GetIndexForParameterName(string parameterName)
        {
            
            if (string.Equals(parameterName, "Character", StringComparison.OrdinalIgnoreCase))
                 return k_CharacterIndex;
            if (string.Equals(parameterName, "To", StringComparison.OrdinalIgnoreCase))
                 return k_ToIndex;
            if (string.Equals(parameterName, "From", StringComparison.OrdinalIgnoreCase))
                 return k_FromIndex;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            var CharacterFilter = new NativeArray<ComponentType>(1, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Character>(),  };
            var ToFilter = new NativeArray<ComponentType>(3, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Waypoint>(), [1] = ComponentType.Exclude<AI.Planner.Domains.ActivationLock>(), [2] = ComponentType.Exclude<AI.Planner.Domains.KeyLock>(),  };
            var FromFilter = new NativeArray<ComponentType>(1, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Waypoint>(),  };
            var CharacterObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(CharacterObjectIndices, CharacterFilter);
            var ToObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(ToObjectIndices, ToFilter);
            var FromObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(FromObjectIndices, FromFilter);
            var WaypointBuffer = stateData.WaypointBuffer;
            var CharacterBuffer = stateData.CharacterBuffer;
            
            for (int i0 = 0; i0 < CharacterObjectIndices.Length; i0++)
            {
                var CharacterIndex = CharacterObjectIndices[i0];
                var CharacterObject = stateData.TraitBasedObjects[CharacterIndex];
                
                
                
            
            for (int i1 = 0; i1 < ToObjectIndices.Length; i1++)
            {
                var ToIndex = ToObjectIndices[i1];
                var ToObject = stateData.TraitBasedObjects[ToIndex];
                
                if (!(WaypointBuffer[ToObject.WaypointIndex].Occupied == false))
                    continue;
                
                if (!(WaypointBuffer[ToObject.WaypointIndex].Left == CharacterBuffer[CharacterObject.CharacterIndex].Waypoint))
                    continue;
                
            
            for (int i2 = 0; i2 < FromObjectIndices.Length; i2++)
            {
                var FromIndex = FromObjectIndices[i2];
                var FromObject = stateData.TraitBasedObjects[FromIndex];
                
                
                
                if (!(CharacterBuffer[CharacterObject.CharacterIndex].Waypoint == stateData.GetTraitBasedObjectId(FromIndex)))
                    continue;

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_CharacterIndex] = CharacterIndex,
                                                       [k_ToIndex] = ToIndex,
                                                       [k_FromIndex] = FromIndex,
                                                    };
                argumentPermutations.Add(actionKey);
            }
            }
            }
            CharacterObjectIndices.Dispose();
            ToObjectIndices.Dispose();
            FromObjectIndices.Dispose();
            CharacterFilter.Dispose();
            ToFilter.Dispose();
            FromFilter.Dispose();
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalCharacterObject = originalStateObjectBuffer[action[k_CharacterIndex]];
            var originalToObject = originalStateObjectBuffer[action[k_ToIndex]];
            var originalFromObject = originalStateObjectBuffer[action[k_FromIndex]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newCharacterBuffer = newState.CharacterBuffer;
            var newWaypointBuffer = newState.WaypointBuffer;
            {
                    var @Character = newCharacterBuffer[originalCharacterObject.CharacterIndex];
                    @Character.@Waypoint = originalState.GetTraitBasedObjectId(originalToObject);
                    newCharacterBuffer[originalCharacterObject.CharacterIndex] = @Character;
            }
            {
                    var @Waypoint = newWaypointBuffer[originalToObject.WaypointIndex];
                    @Waypoint.@Occupied = true;
                    newWaypointBuffer[originalToObject.WaypointIndex] = @Waypoint;
            }
            {
                    var @Waypoint = newWaypointBuffer[originalFromObject.WaypointIndex];
                    @Waypoint.@Occupied = false;
                    newWaypointBuffer[originalFromObject.WaypointIndex] = @Waypoint;
            }
            {
                    var @Waypoint = newWaypointBuffer[originalToObject.WaypointIndex];
                    @Waypoint.@Visited += 1;
                    newWaypointBuffer[originalToObject.WaypointIndex] = @Waypoint;
            }

            

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = -0.1f;

            return reward;
        }

        public void Execute(int jobIndex)
        {
            m_StateDataContext.JobIndex = jobIndex; //todo check that all actions set the job index

            var stateEntityKey = m_StatesToExpand[jobIndex];
            var stateData = m_StateDataContext.GetStateData(stateEntityKey);

            var argumentPermutations = new NativeList<ActionKey>(4, Allocator.Temp);
            GenerateArgumentPermutations(stateData, argumentPermutations);

            var transitionInfo = new NativeArray<MoveRightFixupReference>(argumentPermutations.Length, Allocator.Temp);
            for (var i = 0; i < argumentPermutations.Length; i++)
            {
                transitionInfo[i] = new MoveRightFixupReference { TransitionInfo = ApplyEffects(argumentPermutations[i], stateEntityKey) };
            }

            // fixups
            var stateEntity = stateEntityKey.Entity;
            var fixupBuffer = m_StateDataContext.EntityCommandBuffer.AddBuffer<MoveRightFixupReference>(jobIndex, stateEntity);
            fixupBuffer.CopyFrom(transitionInfo);

            transitionInfo.Dispose();
            argumentPermutations.Dispose();
        }

        
        public static T GetCharacterTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_CharacterIndex]);
        }
        
        public static T GetToTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_ToIndex]);
        }
        
        public static T GetFromTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_FromIndex]);
        }
        
    }

    public struct MoveRightFixupReference : IBufferElementData
    {
        internal StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> TransitionInfo;
    }
}


