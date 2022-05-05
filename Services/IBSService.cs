using System.IO;
using System.Threading.Tasks;

namespace BSCloud.Services
{
  public interface IBSService
  {
    Task<string> DiffAsync(Stream src, Stream target, string srcDirName, string filter = "js");

    Task<string> PatchAsync(Stream src, Stream target, string srcDirName, string filter = "js");
  }
}