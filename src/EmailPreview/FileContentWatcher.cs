using Nito.AsyncEx;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailPreview;

public class FileContentWatcher : IDisposable
{
    private readonly FileInfo file;
    private readonly FileSystemWatcher watcher;
    private readonly AsyncAutoResetEvent @event;
    private bool disposed;

    public FileContentWatcher(FileInfo file)
    {
        this.file = file;

        string directory = file.DirectoryName 
            ?? throw new ArgumentException("DirectoryName must not be null", nameof(file));
        
        watcher = new(directory, file.Name);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        watcher.Created += Watcher_Created;
        watcher.Renamed += Watcher_Renamed;
        watcher.Changed += Watcher_Changed;

        @event = new AsyncAutoResetEvent(set: false);
    }

    public Task WaitForContentChange(CancellationToken cancellationToken)
    {
        if (disposed) throw new ObjectDisposedException(nameof(FileContentWatcher));

        watcher.EnableRaisingEvents = true;

        return @event.WaitAsync(cancellationToken);
    }

    private void Watcher_Created(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Equals(file.FullName, StringComparison.OrdinalIgnoreCase))
            @event.Set();
    }

    private void Watcher_Renamed(object sender, RenamedEventArgs e)
    {
        if (e.FullPath.Equals(file.FullName, StringComparison.OrdinalIgnoreCase))
            @event.Set();
    }

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Equals(file.FullName, StringComparison.OrdinalIgnoreCase))
            @event.Set();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            watcher.Dispose();
            disposed = true;
        }
    }
}
