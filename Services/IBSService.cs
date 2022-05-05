using System.IO;
using System.Threading.Tasks;

namespace BSCloud.Services
{
  public interface IBSService
  {
    Task<(Stream,string)> DiffAsync(Stream src, Stream target, string srcDirName, string filter = "js");

    Task<Stream> PatchAsync(Stream src, Stream target, string srcDirName, string filter = "js");
  }
}