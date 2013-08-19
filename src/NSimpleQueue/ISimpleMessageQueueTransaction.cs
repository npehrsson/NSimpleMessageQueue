using System;

namespace NSimpleQueue {
  public interface ISimpleMessageQueueTransaction : IDisposable {
    void Commit();
    void Rollback();
    void EnlistForRemoval(SimpleQueueMessage message);
  }
}