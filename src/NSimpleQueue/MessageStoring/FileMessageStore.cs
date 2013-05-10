using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace NSimpleQueue.MessageStoring {
  public class FileMessageStore : IMessageStore {
    private readonly DirectoryInfo _metaDirectory;
    private readonly DirectoryInfo _dataDirectory;
    private readonly DirectoryInfo _directory;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly LockedFileWatcher _lockedFileWatcher;
    private readonly HashSet<string> _filesToIgnoreList;
    private readonly object _lockObject;

    public FileMessageStore(string directoryPath) {
      _directory = new DirectoryInfo(directoryPath);
      _metaDirectory = new DirectoryInfo(Path.Combine(directoryPath, SimpleMessageQueueConstants.MetaDirectoryName));
      _dataDirectory = new DirectoryInfo(Path.Combine(directoryPath, SimpleMessageQueueConstants.DataDirectoryName));
      _fileWatcher = new FileSystemWatcher(_dataDirectory.FullName) {
        EnableRaisingEvents = true,
        NotifyFilter = NotifyFilters.CreationTime
      };

      _lockObject = new Object();
      _fileWatcher.Created += NewFileCreated;

      _filesToIgnoreList = new HashSet<string>();
      _lockedFileWatcher = new LockedFileWatcher(new TimeSpan(0, 0, 0, 0, 100));
      _lockedFileWatcher.FileUnlocked += (sender, info) => OnNewFile(info);
    }

    public event EventHandler<NewMessageEventArgs> NewMessage;
    public IFormatter Formatter { get; set; }

    public void Store(SimpleQueueMessage message) {
      lock (_lockObject) {
        _filesToIgnoreList.Add(message.MessageId.ToString());
      }

      using (var stream = File.Create(Path.Combine(_metaDirectory.FullName, message.MessageId.ToString()))) {
        Formatter.Serialize(stream, message);
      }
      using (var stream = File.Create(Path.Combine(_dataDirectory.FullName, message.MessageId.ToString()))) {
        Formatter.Serialize(stream, message.Payload);
      }
    }

    public void RemoveMessage(SimpleQueueMessage message) {
      File.Delete((Path.Combine(_metaDirectory.FullName, message.MessageId.ToString())));
      File.Delete((Path.Combine(_dataDirectory.FullName, message.MessageId.ToString())));
    }

    public IEnumerator<SimpleQueueMessage> GetMetas() {
      foreach (var file in _metaDirectory.GetFiles()) {
        yield return GetMessage(file);
      }
    }

    public object GetData(SimpleQueueMessage message) {
      using (var stream = File.OpenRead(Path.Combine(_dataDirectory.FullName, message.MessageId.ToString()))) {
        return Formatter.Deserialize(stream);
      }
    }

    private SimpleQueueMessage GetMessage(FileInfo file) {
      using (var stream = file.OpenRead()) {
        return Formatter.Deserialize(stream) as SimpleQueueMessage;
      }
    }

    private void NewFileCreated(object sender, FileSystemEventArgs e) {
      var fileInfo = new FileInfo(e.FullPath);

      if (!LockedFileWatcher.IsFileLocked(new FileInfo(e.FullPath)))
        OnNewFile(fileInfo);

      _lockedFileWatcher.Watch(fileInfo);
    }

    private void OnNewFile(FileInfo file) {
      lock (_lockObject) {

        if (_filesToIgnoreList.Contains(file.Name)) {
          _filesToIgnoreList.Remove(file.Name);
          return;
        }
      }

      var message = GetMessage(file);
      OnNewMessage(message);
    }

    private void OnNewMessage(SimpleQueueMessage message) {
      var eventHandler = NewMessage;

      if (eventHandler == null)
        return;

      eventHandler(this, new NewMessageEventArgs() { Message = message });
    }

    public void Dispose() {
      _fileWatcher.Dispose();
      _lockedFileWatcher.Dispose();
    }
  }
}