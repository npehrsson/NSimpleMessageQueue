using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NSimpleQueue {
  public class MemoryQueueLookup {
    private readonly Dictionary<string, InnerSimpleQueue> _lookupTable;

    static MemoryQueueLookup() {
      Current = new MemoryQueueLookup();
    }

    public MemoryQueueLookup() {
      _lookupTable = new Dictionary<string, InnerSimpleQueue>();
    }

    public static MemoryQueueLookup Current { get; private set; }

    public InnerSimpleQueue CreateOrGetQueue(string directoryPath) {
      if (string.IsNullOrEmpty(directoryPath))
        throw new ArgumentNullException("directoryPath");

      lock (typeof(MemoryQueueLookup)) {
        InnerSimpleQueue innerQueue;
        if (_lookupTable.TryGetValue(directoryPath, out innerQueue)) {
          innerQueue.Subscribers++;
          return innerQueue;
        }

        innerQueue = new InnerSimpleQueue(new BlockingCollection<SimpleQueueMessage>(), directoryPath);
        innerQueue.Subscribers++;
        innerQueue.Initialize();
        _lookupTable.Add(directoryPath, innerQueue);

        return innerQueue;
      }
    }

    public void UnSubscribe(InnerSimpleQueue queue) {
      lock (typeof(MemoryQueueLookup)) {
        InnerSimpleQueue innerQueue;
        if (!_lookupTable.TryGetValue(queue.DirectoryPath, out innerQueue))
          return;

        innerQueue.Subscribers--;

        if (innerQueue.AnySubscribers)
          return;

        TearDownQueue(innerQueue);
      }
    }

    private void TearDownQueue(InnerSimpleQueue queue) {
      _lookupTable.Remove(queue.DirectoryPath);
      queue.Dispose();
    }
  }
}