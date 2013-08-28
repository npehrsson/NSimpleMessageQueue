using System;
using System.Runtime.Serialization;
using System.Threading;

namespace NSimpleQueue {
  public interface ISimpleMessageQueue : IDisposable {
    IFormatter Formatter { get; set; }
    long Count { get; }
    ISimpleMessageQueueTransaction BeginTransaction();
    SimpleQueueMessage Receive(CancellationToken cancellationToken);
    SimpleQueueMessage Receive(CancellationToken cancellationToken, ISimpleMessageQueueTransaction transaction);
    void Enqueue(object item);
  }
}
