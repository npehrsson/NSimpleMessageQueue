using System;
using NSimpleQueue.MessageStoring;

namespace NSimpleQueue {
  [Serializable]
  public class SimpleQueueMessage {
    [NonSerialized]
    private object _payload;

    public SimpleQueueMessage(object payload) {
      _payload = payload;
    }

    internal SimpleQueueMessage(Guid messageId) {
      MessageId = messageId;
    }

    public Guid MessageId { get; internal set; }
    internal IMessageStore MessageStore { get; set; }

    public object Payload {
      get {
        if (_payload == null)
          DeserializePayLoad();
        return _payload;
      }
    }

    private void DeserializePayLoad() {
      if (MessageStore == null)
        throw new InvalidOperationException("MessageStore is not set");

      _payload = MessageStore.GetData(this);
    }
  }
}