using System.Collections.Concurrent;

namespace NSimpleQueue
{
  internal interface IInternalSimpleMessageQueueTransaction : ISimpleMessageQueueTransaction
  {
    BlockingCollection<SimpleQueueMessage> Queue { get; set; }
  }
}