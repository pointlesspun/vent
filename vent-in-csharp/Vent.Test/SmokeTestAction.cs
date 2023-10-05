/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent.Test
{
    public class SmokeTestAction
    {
        public string Name { get; set; }

        public int UpperBound { get; set; } = 1;
            
        public Action<EntityHistory> Mutation { get; set; }

        public Func<EntityHistory, bool> MeetsRequirements { get; set; }

        public Func<EntityHistory, int> DynamicWeight { get; set; }

        public SmokeTestAction()
        {
            DynamicWeight = DefaultWeightFunction;
        }


        public SmokeTestAction(
            String name,
            int upperBound, 
            Action<EntityHistory> mutation, 
            Func<EntityHistory, bool> meetsRequirements,
            Func<EntityHistory, int> weightFunction = null)
        {
            Name = name;    
            UpperBound = upperBound;
            Mutation = mutation;
            MeetsRequirements = meetsRequirements;  
            DynamicWeight = weightFunction ?? DefaultWeightFunction;
        }


        public int DefaultWeightFunction(EntityHistory entityStore)  => UpperBound;

        public override string ToString() => Name;  
        


        public static SmokeTestAction SelectAction(List<SmokeTestAction> actions, EntityHistory entityStore, Random randomSelection)
        {
            var eligableActions = actions.Where(a => a.MeetsRequirements == null || a.MeetsRequirements(entityStore)).ToList();
            var actionWeights = eligableActions.Select(a => a.DynamicWeight(entityStore)).ToList();
            var totalWeight = actionWeights.Sum(); 
            var roll = randomSelection.Next(totalWeight);
            var weight = 0;

            if (eligableActions.Count == 1)
            {
                return eligableActions[0];
            }

            for (var i = 0; i < eligableActions.Count; i++)
            {
                var action = eligableActions[i];    
                var actionWeight = actionWeights[i];

                weight += actionWeights[i];
                
                if (weight > roll)
                {
                    return action;
                }              
            }

            return null;
        }

        private static string GetCurrentMutationText(EntityHistory entityStore)
        {
            if (entityStore.CurrentMutation < 0)
            {
                return "@Tail";
            }
            else if (entityStore.CurrentMutation >=  entityStore.MutationCount)
            {
                return "@Head";
            }

            return entityStore.GetMutation(entityStore.CurrentMutation).ToString();
        }

        public static readonly string UndoActionName = "undo";
        public static readonly string RedoActionName = "redo";
        public static readonly string UndoManyActionName = "undo many";
        public static readonly string RedoManyActionName = "redo many";

        public static List<SmokeTestAction> CreateDefaultActionSet(Random randomSelection, Action<string> logFunction = null)
        {
            // xxx to do add multiple undo / redo triggers
            return new List<SmokeTestAction>
            {
                new SmokeTestAction(
                    "create and commit",
                    40,
                    (store) =>
                    {
                        var guid = Guid.NewGuid().ToString();
                        var entityName = $"{store.Registry.NextEntityId}.{guid}";
                        logFunction?.Invoke($"#{store.CurrentMutation}: create & commit id:{store.Registry.NextEntityId} = {entityName}");
                        store.Commit(new PropertyEntity<string>($"{entityName}"));
                    },
                    (store) => store.Registry.SlotCount < store.Registry.MaxEntitySlots - 3
                ),
                new SmokeTestAction(
                    UndoActionName,
                    20,
                    (store) =>
                    {
                        var mutationText = GetCurrentMutationText(store);
                        logFunction?.Invoke($"{UndoActionName} -> {store.CurrentMutation}/{store.MutationCount}, mutation: {mutationText}");
                        store.Undo();
                    },
                    (store) => store.OpenGroupCount == 0 && store.CurrentMutation > 0
                ),
                new SmokeTestAction(
                    RedoActionName,
                    20,
                    (store) =>
                    {
                        var mutationText = GetCurrentMutationText(store);
                        logFunction?.Invoke($"{RedoActionName} -> {store.CurrentMutation}/{store.MutationCount}, mutation: {mutationText}");
                        store.Redo();
                    },
                    (store) => store.CurrentMutation < store.MutationCount
                ),
                new SmokeTestAction(
                    "update (commit)",
                    20,
                    (store) => {
                        var ent = store.SelectRandomEntity<PropertyEntity<string>>(randomSelection);

                        if (ent != null)
                        {
                            ent.Value += " +";
                            logFunction?.Invoke($"#{store.CurrentMutation}: update (commit) entity with id: {ent.Id} to new value: '{ent.Value}'");
                            store.Commit(ent);
                        }
                        else
                        {
                            logFunction?.Invoke($"No random entity found, skipping update (commit)");
                        }
                    },
                    (store) => store.Registry.EntitiesInScope > 0 && store.Registry.SlotCount < store.Registry.MaxEntitySlots - 1
                ),
                new SmokeTestAction(
                    "deregister",
                    25,
                    (store) => {
                        var ent = store.SelectRandomEntity<PropertyEntity<string>>(randomSelection);

                        if (ent != null)
                        {
                            logFunction?.Invoke($"#{store.CurrentMutation}: deregister entity with id:{ent.Id} and valuie '{ent.Value}'");
                            store.Deregister(ent);
                        }
                        else
                        {
                            logFunction?.Invoke($"No random entity found, skipping deregister");
                        }
                    },
                    (store) => store.Registry.EntitiesInScope > 0 && store.Registry.SlotCount < store.Registry.MaxEntitySlots - 1
                ),
                new SmokeTestAction(
                    "remove oldest",
                    30,
                    (store) =>
                    {
                        var count = randomSelection.Next(1, 2);

                        for (var i = 0; i < count && store.MutationCount > 0; i++)
                        {
                            logFunction?.Invoke($"Remove oldest {i}/{count} mutation(s)");
                            store.DeleteMutation(0);
                        }
                    },
                    (store) => store.MutationCount > 0 && 
                                    (!(store.GetMutation(0) is BeginMutationGroup) || !store.IsGroupOpen(0)),
                    (store) => store.MutationCount + (int)(100.0 * (store.Registry.SlotCount / Math.Min(5000, store.Registry.MaxEntitySlots)))
                ),
                new SmokeTestAction(
                    UndoManyActionName,
                    7,
                    (store) =>
                    {
                        var count = randomSelection.Next(1, store.MutationCount);
                        logFunction?.Invoke($"{UndoManyActionName} ({count}) -> new mutation index: {Math.Max(-1, store.CurrentMutation - count)}");
                        store.Undo(count);
                        
                    },
                    (store) => store.OpenGroupCount == 0 && store.CurrentMutation > 3
                ),
                new SmokeTestAction(
                    RedoManyActionName,
                    7,
                    (store) =>
                    {
                        var count = randomSelection.Next(1, store.MutationCount);
                        logFunction?.Invoke($"{RedoManyActionName} ({count}) -> new mutation index: {Math.Min(store.MutationCount, store.CurrentMutation + count)}");
                        store.Redo(count);
                    },
                    (store) => store.MutationCount > 3 && store.CurrentMutation < store.MutationCount - 3
                ),

            };
        }
    }

    public static class SmokeTestActionExtensions
    {
        public static List<SmokeTestAction> AddGroupActions(
            this List<SmokeTestAction> list, 
            Random randomSelection, 
            Action<string> logFunction = null,
            int maxGroups = 5,
            int beginGroupWeight = 10,
            int endGroupWeight = 10)
        {
            list.Add(new SmokeTestAction(
                "begin group",
                beginGroupWeight,
                (store) => {
                    logFunction?.Invoke($"#{store.CurrentMutation}: begin group {store.OpenGroupCount}/{maxGroups}");
                    store.BeginMutationGroup();
                },
                (store) => store.Registry.SlotCount < store.Registry.MaxEntitySlots - 1 && store.OpenGroupCount < maxGroups
            ));

            list.Add(new SmokeTestAction(
                "end group",
                endGroupWeight,
                (store) => {
                    logFunction?.Invoke($"#{store.CurrentMutation}: end group {store.OpenGroupCount}/{maxGroups}");
                    store.EndMutationGroup();
                },
                (store) => store.Registry.SlotCount < store.Registry.MaxEntitySlots - 1 && store.OpenGroupCount > 0,
                (store) => endGroupWeight + (int) (100 * ((double)store.OpenGroupCount+1) / ((double)maxGroups))
            ));

            return list;
        }
    }
}
