
using Vent.Registry;
/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun
namespace Vent.History
{
    public class EntityHistory : EntityBase
    {
        /// <summary>
        /// Associates an entity by id with its versionInfo
        /// </summary>
        public Dictionary<int, VersionInfo> EntityVersionInfo
        {
            get;
            set;
        } = new();

        /// <summary>
        /// Ordered list of all mutations in this history
        /// </summary>
        public List<IMutation> Mutations
        {
            get;
            set;
        } = new();

        public int CurrentMutation
        {
            get;
            set;
        }

        /// <summary>
        /// Number of begin calls that have not yet got a corresponding end call
        /// </summary>
        public int OpenGroupCount
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum number of mutations, new mutations will cause the oldest
        /// mutation to be removed.
        /// </summary>
        public int MaxMutations
        {
            get;
            set;
        } = 10;

        /// <summary>
        /// If an entity version goes out of scope because the max mutations
        /// have been reached and the deleted mutation deletes the version information,
        /// its versioninfo may be preserved or deleted (depending on intended future use).
        /// If DeleteOutOfScopeVersions is true the versionInfo will be removed, if
        /// not it will remain in the store.
        /// </summary>
        public bool DeleteOutOfScopeVersions
        {
            get;
            set;
        } = true;

        public EntityHistory()
        {
        }

        public EntityHistory(int maxMutations, bool deleteOutOfScopeVersions = true)
        {
            MaxMutations = maxMutations;
            DeleteOutOfScopeVersions = deleteOutOfScopeVersions;
        }
    }
}
