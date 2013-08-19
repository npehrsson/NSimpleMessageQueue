using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace NSimpleQueue.MessageStoring {
  internal class FileMessageStoreTransaction : IInternalSimpleMessageQueueTransaction {
    private readonly IMessageStore _messageStore;
    private readonly DirectoryInfo _rootDirectory;
    private readonly DirectoryInfo _dataDirectory;
    private readonly DirectoryInfo _metaDirectory;
    private readonly IList<SimpleQueueMessage> _enlistedForRemoval;
    private bool _isCommitedOrRolledBack;
    private bool _isDisposing;

    public FileMessageStoreTransaction(IMessageStore messageStore, DirectoryInfo rootDirectory, DirectoryInfo dataDirectory, DirectoryInfo metaDirectory) {
      _messageStore = messageStore;
      _rootDirectory = rootDirectory;
      _dataDirectory = dataDirectory;
      _metaDirectory = metaDirectory;
      TransactionId = Guid.NewGuid();
      _enlistedForRemoval = new List<SimpleQueueMessage>();
    }

    public BlockingCollection<SimpleQueueMessage> Queue { get; set; }
    public DirectoryInfo TransactionDataDirectory { get; private set; }
    public DirectoryInfo TransactionMetaDirectory { get; private set; }

    public Guid TransactionId { get; private set; }

    public void Begin() {
      TransactionDirectory = _rootDirectory.CreateSubdirectory(Path.Combine("transactions", TransactionId.ToString()));
      TransactionDirectory.Refresh();
      TransactionMetaDirectory = _rootDirectory.CreateSubdirectory(SimpleMessageQueueConstants.MetaDirectoryName);
      TransactionDataDirectory = _rootDirectory.CreateSubdirectory(SimpleMessageQueueConstants.DataDirectoryName);
    }

    public DirectoryInfo TransactionDirectory { get; set; }

    public void Dispose()
    {
      _isDisposing = true;
      if (!_isCommitedOrRolledBack) Rollback();
      TransactionDirectory.Delete(true);
    }

    public void Commit() {
      foreach (var file in TransactionMetaDirectory.GetFiles()) {
        file.MoveTo(Path.Combine(_metaDirectory.FullName, file.Name));
      }

      foreach (var file in TransactionDataDirectory.GetFiles()) {
        file.MoveTo(Path.Combine(_dataDirectory.FullName, file.Name));
      }

      foreach (var simpleQueueMessage in _enlistedForRemoval) {
        _messageStore.RemoveMessage(simpleQueueMessage);
      }
      _isCommitedOrRolledBack = true;
      Dispose();
    }

    public void Rollback() {
      
      _isCommitedOrRolledBack = true;
      foreach (var simpleQueueMessage in _enlistedForRemoval) {
        Queue.Add(simpleQueueMessage);
      }

      if (_isDisposing) return;
      Dispose();
    }

    public void EnlistForRemoval(SimpleQueueMessage message) {
      if (message == null) throw new ArgumentNullException("message");
      _enlistedForRemoval.Add(message);
    }
  }
}