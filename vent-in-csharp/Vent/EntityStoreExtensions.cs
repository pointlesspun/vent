﻿/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using System.Text;

namespace Vent
{
    public static class EntityStoreExtensions
    {
        /// <summary>
        /// Move's the store mutation position to the tail (-1)
        /// </summary>
        /// <param name="store"></param>
        public static void ToTail(this EntityStore store)
        {
            while (store.Undo()) { }
        }

        /// <summary>
        /// Move's the store mutation position to the head (store.MutationCount)
        /// </summary>
        /// <param name="store"></param>
        public static void ToHead(this EntityStore store)
        {
            while (store.Redo()) { }
        }

        /// <summary>
        /// Repeat undo count times
        /// </summary>
        /// <param name="store"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool Undo(this EntityStore store, int count)
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
        public static bool Redo(this EntityStore store, int count)
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
        /// <param name="store"></param>
        /// <returns></returns>
        public static string ToStateString(this EntityStore store)
        {
            var builder = new StringBuilder();

            
            builder.AppendLine($"Mutations: {store.CurrentMutation} / {store.MutationCount}");

            var mutations = store.Where( e => e is CommitEntity)
                                    .Cast<CommitEntity>()
                                    .OrderBy( m => m.TimeStamp)
                                    .ToList();

            foreach (var mutation in mutations )
            {
                builder.AppendLine(mutation.ToString());
            }

            builder.AppendLine($"Entity count: {store.EntitiesInScope}");

            var versionedEntities = store.Where(e => store.HasVersionInfo(e)).OrderBy(e => e.Id);

            if (versionedEntities.Count() > 0)
            {
                foreach (var e in versionedEntities)
                {
                    var versionInfo = store.GetVersionInfo(e);
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
        public static T SelectRandomEntity<T>(this EntityStore store, Random rng, bool needsVersioning = true ) where T : IEntity
        {
            Contract.NotNull(rng);
            
            var candidateEntities = store.Where(e => e is T
                                            && (!needsVersioning || store.GetVersionInfo(e) != null)).Cast<T>();
            var count = candidateEntities.Count();

            if (count > 0)
            {
                var skip = rng.Next(0, count);
                return candidateEntities.Skip(skip).First();
            }

            return default;
        }
    }
}
