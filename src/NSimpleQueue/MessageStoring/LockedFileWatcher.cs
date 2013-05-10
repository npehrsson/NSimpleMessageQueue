using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace NSimpleQueue.MessageStoring {
  public class LockedFileWatcher : IDisposable {
    private readonly ConcurrentDictionary<FileInfo, FileInfo> _files;
    private readonly TimeSpan _fileCheckIntervall;
    private readonly Timer _timer;

    public event EventHandler<FileInfo> FileUnlocked;

    public LockedFileWatcher(TimeSpan checkIntervall) {
      _fileCheckIntervall = checkIntervall;
      _files = new ConcurrentDictionary<FileInfo, FileInfo>();
      _timer = new Timer(x => checkLockedFiles(), null, TimeSpan.Zero, TimeSpan.Zero);
    }

    public void Watch(FileInfo fileInfo) {
      if (fileInfo == null)
        throw new ArgumentNullException("fileInfo");

      _files.AddOrUpdate(fileInfo, fileInfo, (x, y) => x);
      scheduleNewRun();
    }

    public static bool IsFileLocked(FileInfo file) {
      try {
        using (file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        return false;
      }
      catch (IOException) {
        return true;
      }
    }

    public void Dispose() {
      if (_timer != null)
        _timer.Dispose();
    }

    private void checkLockedFiles() {
      foreach (var fileInfo in _files.Values) {
        if (IsFileLocked(fileInfo))
          continue;

        onFileUnlocked(fileInfo);

        FileInfo f;
        _files.TryRemove(fileInfo, out f);
      }

      if (_files.Any())
        scheduleNewRun();
    }

    private void scheduleNewRun() {
      _timer.Change(_fileCheckIntervall, TimeSpan.Zero);
    }

    private void onFileUnlocked(FileInfo fileInfo) {
      var eventHandler = FileUnlocked;

      if (FileUnlocked != null)
        eventHandler(this, fileInfo);
    }
  }
}