using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NSimpleQueue.MessageStoring {
  public interface IMessageStore : IDisposable {
    event EventHandler<NewMessageEventArgs> NewMessage;
    IFormatter Formatter { get; set; }
    void Store(SimpleQueueMessage message);
    void RemoveMessage(SimpleQueueMessage message);
    IEnumerator<SimpleQueueMessage> GetMetas();
    object GetData(SimpleQueueMessage message);
  }
}