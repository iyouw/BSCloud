using System.IO;
using System.Threading.Tasks;

namespace BSCloud.Services
{
  public interface IBSService
  {
    Task<string> DiffAsync(Stream zipFile, string zipFileName, string baseFileName = "weex.js",  string filter = "js", string patchInfo = "patch_info.txt");

    Task<string> PatchAsync(Stream zipFile, string zipFileName, string baseFileName = "weex.js", string patchInfo = "patch_info.txt");
  }
}