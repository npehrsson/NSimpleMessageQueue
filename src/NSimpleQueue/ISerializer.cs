using System.IO;

namespace NSimpleQueue
{
  public interface ISerializer {
    void Serialize(Stream stream, object graph);
    object Deserialize(Stream stream);
  }
}