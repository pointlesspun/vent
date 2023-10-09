/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text;

using Vent.Registry;
using Vent.Util;

namespace Vent.History
{
    public static class EntityHistoryExtensions
    {
        /// <summary>
        /// Move's the store mutation position to the tail (-1)
        /// </summary>
        /// <param name="store"></param>
        public static void ToTail(this HistorySystem store)
        {
            while (store.Undo()) { }
        }

        /// <summary>
        /// Move's the store mutation position to the head (store.MutationCount)
        /// </summary>
        /// <param name="store"></param>
        public static void ToHead(this HistorySystem store)
        {
            while (store.Redo()) { }
        }

        /// <summary>
        /// Repeat undo count times
        /// </summary>
        /// <param name="store"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool Undo(this HistorySystem store, int count)
        {
            for (int i = 0; count < 0 || i < count; i++)
            {
                if (!store.Undo())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Repeat redo count times
        /// </summary>
        /// <param name="store"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool Redo(this HistorySystem store, int count)
        {
            for (int i = 0; count < 0 || i < count; i++)
            {
                if (!store.Redo())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a string from the current state of the store
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        public static string ToStateString(this HistorySystem history)
        {
            var builder = new StringBuilder();


            builder.AppendLine($"Mutations: {history.CurrentMutation} / {history.MutationCount}");

            var mutations = history.Registry.GetEntitiesOf<CommitEntity>()
                                    .OrderBy(m => m.TimeStamp)
                                    .ToList();

            foreach (var mutation in mutations)
            {
                builder.AppendLine(mutation.ToString());
            }

            builder.AppendLine($"Entity count: {history.Registry.EntitiesInScope}");

            var versionedEntities = history.Registry.Where(kvp => history.HasVersionInfo(kvp.Value))
                                        .Select(kvp => kvp.Value)
                                        .OrderBy(e => e.Id);

            if (versionedEntities.Count() > 0)
            {
                foreach (var e in versionedEntities)
                {
                    var versionInfo = history.GetVersionInfo(e);
                    builder.AppendLine($"id: {e.Id}, type:{e.GetType().Name}) = {e}, v:{versionInfo.CurrentVersion}/{versionInfo.Versions.Count}");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Select a random entity from the store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="store"></param>
        /// <param name="rng"></param>
        /// <param name="needsVersioning"></param>
        /// <returns></returns>
        public static T SelectRandomEntity<T>(this HistorySystem store, Random rng, bool needsVersioning = true) where T : IEntity
        {
            Contract.NotNull(rng);

            var candidateEntities = store.Registry.Where(e => e.Value is T && (!needsVersioning || store.GetVersionInfo(e.Value) != null))
                                            .Select(kvp => kvp.Value)
                                            .Cast<T>();

            var count = candidateEntities.Count();

            if (count > 0)
            {
                var skip = rng.Next(0, count);
                return candidateEntities.Skip(skip).First();
            }

            return default;
        }

        /// <summary>
        /// Utility used primarily by tests. The entity may not be part of the registry but an entity
        /// with its Id is contained in History's registry.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="entity"></param>
        public static void DeregisterById(this HistorySystem store, int id)
        {
            store.Deregister(store.Registry[id]);
        }
    }
}
