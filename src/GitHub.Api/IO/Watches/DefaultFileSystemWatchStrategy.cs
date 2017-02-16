using System;
using System.Collections.Generic;
using GitHub.Unity;

namespace GitHub.Api
{
    class DefaultFileSystemWatchStrategy : FileSystemWatchStrategyBase
    {
        private readonly object watchesLock = new object();

        private readonly IFileSystemWatchFactory watchFactory;

        private Dictionary<PathAndFilter, IFileSystemWatch> watches = new Dictionary<PathAndFilter, IFileSystemWatch>();

        public DefaultFileSystemWatchStrategy(IFileSystemWatchFactory watchFactory)
        {
            this.watchFactory = watchFactory;
        }

        public override void Watch(string path, string filter = null)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            var key = new PathAndFilter { Path = path, Filter = filter };

            IFileSystemWatch watch;
            lock(watchesLock)
            {
                if (watches.ContainsKey(key))
                {
                    throw new Exception("path and filter combination already watched");
                }

                Logger.Debug("Watching Path:{0} Filter:{1}", path, filter == null ? "[NONE]" : filter);

                if (filter != null)
                {
                    watch = watchFactory.CreateWatch(path, filter);
                }
                else
                {
                    watch = watchFactory.CreateWatch(path);
                }

                watches.Add(key, watch);
            }

            watch.AddListener(this);
        }

        public override void ClearWatch(string path, string filter = null)
        {
            var key = new PathAndFilter { Path = path, Filter = filter };

            lock(watchesLock)
            {
                IFileSystemWatch value;
                if (!watches.TryGetValue(key, out value))
                {
                    throw new Exception("path and filter combination not watched");
                }

                watches.Remove(key);

                value.Enable = false;
                value.RemoveListener(this);
                value.Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (watches == null)
            {
                return;
            }

            var watchList = watches;
            watches = null;

            foreach (var watcher in watchList.Values)
            {
                watcher.Dispose();
            }
        }
    }
}
