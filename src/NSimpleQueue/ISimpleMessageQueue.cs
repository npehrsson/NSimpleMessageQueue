using System;
using System.Runtime.Serialization;
using System.Threading;

namespace NSimpleQueue {
  public interface ISimpleMessageQueue : IDisposable {
    IFormatter Formatter { get; set; }
    long Count { get; }
    SimpleQueueMessage Receive(CancellationToken cancellationToken);
    void Enqueue(object item);
  }
}
