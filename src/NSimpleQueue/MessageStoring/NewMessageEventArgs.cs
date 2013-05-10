using System;

namespace NSimpleQueue.MessageStoring {
  public class NewMessageEventArgs : EventArgs {
    public SimpleQueueMessage Message { get; set; }
  }
}