namespace Lertaro.Linux.Core;

public sealed class LinuxIndexWatcher : IDisposable
{
    private readonly object _stateSync = new();
    private readonly LinuxMutableIndex _index;
    private readonly LinuxIndexStore _store;
    private readonly string _indexPath;
    private readonly TimeSpan _saveDelay;
    private readonly FileSystemWatcher _watcher;
    private readonly Timer _saveTimer;
    private bool _dirty;
    private bool _disposed;
    private Exception? _lastError;

    public LinuxIndexWatcher(
        LinuxMutableIndex index,
        string indexPath,
        LinuxIndexStore? store = null,
        TimeSpan? saveDelay = null)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexPath);

        _index = index;
        _indexPath = Path.GetFullPath(indexPath);
        _store = store ?? new LinuxIndexStore();
        _saveDelay = saveDelay ?? TimeSpan.FromSeconds(1);
        if (_saveDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(saveDelay));

        _watcher = new FileSystemWatcher(index.Root)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.DirectoryName |
                           NotifyFilters.Size |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Attributes,
            EnableRaisingEvents = false
        };
        _watcher.Created += OnCreated;
        _watcher.Changed += OnChanged;
        _watcher.Deleted += OnDeleted;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
        _saveTimer = new Timer(_ => PersistIfDirty(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public Exception? LastError
    {
        get
        {
            lock (_stateSync)
                return _lastError;
        }
    }

    public void Start()
    {
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _watcher.EnableRaisingEvents = true;
        }
    }

    public void Stop()
    {
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _watcher.EnableRaisingEvents = false;
        }
        Flush();
    }

    public void Flush()
    {
        var snapshot = _index.Snapshot();
        _store.Save(snapshot, _indexPath);
        lock (_stateSync)
        {
            _dirty = false;
            _lastError = null;
            if (!_disposed)
                _saveTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    public void Reconcile()
    {
        var rebuilt = LinuxIndexSnapshot.Build(_index.Root);
        _index.ReplaceAll(rebuilt);
        ScheduleSave();
    }

    public void Dispose()
    {
        bool shouldFlush;
        lock (_stateSync)
        {
            if (_disposed)
                return;
            _watcher.EnableRaisingEvents = false;
            _disposed = true;
            shouldFlush = _dirty;
        }

        _watcher.Dispose();
        _saveTimer.Dispose();
        if (!shouldFlush)
            return;

        try
        {
            _store.Save(_index.Snapshot(), _indexPath);
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            RecordError(ex);
        }
    }

    private void OnCreated(object sender, FileSystemEventArgs args) =>
        Apply(() => _index.ReplaceSubtree(args.FullPath));

    private void OnChanged(object sender, FileSystemEventArgs args) =>
        Apply(() => _index.RefreshSingle(args.FullPath));

    private void OnDeleted(object sender, FileSystemEventArgs args) =>
        Apply(() => _index.RemovePathAndDescendants(args.FullPath) > 0);

    private void OnRenamed(object sender, RenamedEventArgs args) =>
        Apply(() =>
        {
            var removed = _index.RemovePathAndDescendants(args.OldFullPath) > 0;
            return _index.ReplaceSubtree(args.FullPath) || removed;
        });

    private void OnError(object sender, ErrorEventArgs args)
    {
        RecordError(args.GetException());
        try
        {
            Reconcile();
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            RecordError(ex);
        }
    }

    private void Apply(Func<bool> update)
    {
        try
        {
            if (update())
                ScheduleSave();
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            RecordError(ex);
        }
    }

    private void ScheduleSave()
    {
        lock (_stateSync)
        {
            if (_disposed)
                return;
            _dirty = true;
            _saveTimer.Change(_saveDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private void PersistIfDirty()
    {
        lock (_stateSync)
        {
            if (_disposed || !_dirty)
                return;
            _dirty = false;
        }

        try
        {
            _store.Save(_index.Snapshot(), _indexPath);
            lock (_stateSync)
                _lastError = null;
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            RecordError(ex);
            lock (_stateSync)
            {
                if (_disposed)
                    return;
                _dirty = true;
                _saveTimer.Change(_saveDelay, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private void RecordError(Exception error)
    {
        lock (_stateSync)
            _lastError = error;
    }

    private static bool IsRecoverable(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or System.Security.SecurityException;
}
