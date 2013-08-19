using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NSimpleQueue.MessageStoring;

namespace NSimpleQueue {
  public class InnerSimpleQueue {
    private readonly DirectoryInfo _directory;
    private readonly IMessageStore _messageStore;

    public InnerSimpleQueue(BlockingCollection<SimpleQueueMessage> queue, string directoryPath) {
      if (queue == null) throw new ArgumentNullException("queue");
      Queue = queue;
      DirectoryPath = directoryPath;

      _messageStore = new FileMessageStore(DirectoryPath);
      _directory = new DirectoryInfo(DirectoryPath);

      MakeSureQueueExsists();
    }

    public BlockingCollection<SimpleQueueMessage> Queue { get; private set; }
    public string DirectoryPath { get; private set; }
    public int Subscribers { get; set; }
    public bool AnySubscribers { get { return Subscribers > 0; } }
    public IFormatter Formatter { get { return _messageStore.Formatter; } set { _messageStore.Formatter = value; } }
    public long Count { get { return Queue.Count; } }

    public void Initialize() {
      Formatter = new BinaryFormatter();
      _messageStore.NewMessage += NewMessage;
      GetExistingMessages();
    }

    private void NewMessage(object sender, NewMessageEventArgs e) {
      Queue.Add(e.Message);
    }

    public ISimpleMessageQueueTransaction BeginTransaction() {
      var transaction = _messageStore.BeginTransaction();
      transaction.Queue = Queue;

      return transaction;
    }

    public SimpleQueueMessage Receive(CancellationToken cancellationToken) {
      var message = Queue.Take(cancellationToken);
      message.MessageStore = _messageStore;
      //just trigger it now we do not support lazy loading yet
      var value = message.Payload;
      _messageStore.RemoveMessage(message);

      return message;
    }

    public SimpleQueueMessage Receive(CancellationToken cancellationToken, ISimpleMessageQueueTransaction transaction) {
      var message = Queue.Take(cancellationToken);
      
      message.MessageStore = _messageStore;
      //just trigger it now we do not support lazy loading yet
      var value = message.Payload;
      transaction.EnlistForRemoval(message);
      
      return message;
    }

    public void Enqueue(object item) {
      var message = item as SimpleQueueMessage ?? new SimpleQueueMessage(item);

      message.MessageId = Comb.NewComb();
      _messageStore.Store(message);
      Queue.Add(message);
    }

    private void GetExistingMessages() {
      var enumerator = _messageStore.GetMetas();
      while (enumerator.MoveNext()) {
        Queue.Add(enumerator.Current);
      }
    }

    private void MakeSureQueueExsists() {
      if (!_directory.Exists)
        throw new InvalidOperationException("queue does not exsist");
    }

    public void Dispose() {
      Queue.Dispose();
      _messageStore.Dispose();
    }
  }
}