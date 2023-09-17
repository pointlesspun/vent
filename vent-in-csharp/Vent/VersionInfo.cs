/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

namespace Vent
{
    public class VersionInfo : EntityBase
    {
        public List<IEntity> Versions { get; set; } = new();

        public int HeadId { get; set; }

        /// <summary>
        /// -1, current version is head
        /// 0, Head is pointing to version 0
        /// 1, Head is pointing to version 1
        /// ...
        /// </summary>
        public int CurrentVersion { get; set; } = 0;

        public void CommitVersion(IEntity newVersion)
        {
            Contract.NotNull(newVersion);

            Versions.Add(newVersion);
            CurrentVersion++;
        }

        public void Undo(IEntity target)
        {
            Contract.NotNull(target);

            CurrentVersion--;

            // check if version is not at the tail
            if (CurrentVersion >= 0)
            {
                var version = Versions[CurrentVersion];

                target.CopyPropertiesFrom(version);
                target.Id = HeadId;
            }
        }

        public void Redo(IEntity target)
        {
            Contract.NotNull(target);

            if (CurrentVersion < 0)
            {
                CurrentVersion = 0;
            }

            // check if current version is not at the head 
            if (CurrentVersion < Versions.Count)
            {
                target.CopyPropertiesFrom(Versions[CurrentVersion]);
                target.Id = HeadId;
            }

            CurrentVersion++;
        }

        public void Revert(IEntity target)
        {
            Contract.NotNull(target);

            if (CurrentVersion < 0)
            {
                CurrentVersion = 0;
            }
            else if (CurrentVersion >= Versions.Count)
            {
                CurrentVersion = Versions.Count - 1;
            }

            target.CopyPropertiesFrom(Versions[CurrentVersion]);
            target.Id = HeadId;
        }

        public void RemoveVersion(IEntity version)
        {
            RemoveVersion(Versions.IndexOf(version));
        }

        public void RemoveVersion(int index)
        {
            Contract.Requires(index >= 0 && index <  Versions.Count);

            Versions.RemoveAt(index);

            if (CurrentVersion > index)
            {
                CurrentVersion--;
            }
        }
    }
}
