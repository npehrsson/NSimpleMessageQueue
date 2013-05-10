using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NSimpleQueue.Tests {
  [TestFixture]
  public class SimpleQueueTests {
    private const string QueuePath = @"c:\queue\";

    [SetUp]
    public void Setup() {
      SimpleMessageMessageQueue.Delete(QueuePath);
      SimpleMessageMessageQueue.Create(QueuePath, new BinaryFormatter());
    }

    [TearDown]
    public void TearDown() {

    }

    [Test]
    public void Enqueue_WhenCreatingTwoInstancesAndAddingOneToEachOne_QueueCountIsTwo() {
      using (var queue1 = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {
        using (var queue2 = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {

          queue1.Enqueue(1);
          queue2.Enqueue(2);

          Assert.AreEqual(2, queue1.Count);
          Assert.AreEqual(2, queue2.Count);
        }
      }
    }

    [Test]
    public void Receive_WhenAddingTwoMessagesAndThenReadOne_QueueCountIsOne() {
      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {

        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.Receive(new CancellationTokenSource().Token);

        Assert.AreEqual(1, queue.Count);
      }
    }

    [Test]
    public void Recover_WhenAddingTwoMessagesAndDisposesQueue_NewQueueHasTwoMessages() {
      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {

        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.Dispose();
      }

      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {
        Assert.AreEqual(2, queue.Count);
      }
    }

    [Test]
    public void Enqueue_Adding10000MessagesSequencial_WorksFine() {
      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {

        for (var i = 0; i < 10000; i++)
          queue.Enqueue(1);

        Assert.AreEqual(10000, queue.Count);
      }
    }

    [Test]
    public void Enqueue_Adding10000MessagesAsyncroniusly_WorksFine() {
      Parallel.For(0, 10, x => {
          using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {

            for (var i = 0; i < 1000; i++)
              queue.Enqueue(1);
          }
        });

      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {
        Assert.AreEqual(10000, queue.Count);
      }
    }

    [Test]
    public void Dequeue_WhenAddingStringToQueue_WhenDequingSameStringIsReturned() {
      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {

        queue.Enqueue("Niclas");

        queue.Dispose();
      }

      using (var queue = new SimpleMessageMessageQueue(new DirectoryInfo(QueuePath))) {
        Assert.AreEqual("Niclas", queue.Receive(new CancellationTokenSource().Token).Payload);
      }
    }
  }
}
