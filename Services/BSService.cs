using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using BSCloud.Infra;

namespace BSCloud.Services
{
  public class BSService : IBSService
  {
    public async Task<string> DiffAsync(Stream zip, string zipFileName, string baseFileName="weex.js", string filter="js", string patchInfo = "patch_info.txt")
    {
      var dirPath = await ZipHelper.UnZipAsync(zip);

      var patches = await DiffCoreAsync(dirPath, baseFileName, filter);

      await WritePatcheAsync(patches.Where(x=>!string.IsNullOrEmpty(x)), dirPath, patchInfo);

      return await ZipHelper.ZipAsync(dirPath, zipFileName);
    }

    private async Task<string[]> DiffCoreAsync(string dirPath,string baseFileName, string filter)
    {
      var baseData = await File.ReadAllBytesAsync(Path.Combine(dirPath, baseFileName));
      var dirInfo = new DirectoryInfo(dirPath);
      var tasks = dirInfo.EnumerateFiles(filter, SearchOption.AllDirectories)
                  .Select(file => DoDiffAsync(file, baseData, dirPath, baseFileName, filter));
      return await Task.WhenAll(tasks);
    }

    private async Task<string> DoDiffAsync(FileInfo file, byte[] baseData, string dirPath, string baseFileName, string filter)
    {
      var res = string.Empty;
      if(file.Name != baseFileName)
        {
          var newData =  await File.ReadAllBytesAsync(file.FullName);
          var diffFile = new FileInfo(Path.GetTempFileName());
          using(var fs = diffFile.OpenWrite())
          {
            await BSAlgorithm.DiffAsync(baseData, newData, fs);
          }
          if(diffFile.Length < file.Length)
          {
            diffFile.Replace(file.FullName, null);
            res = Path.GetRelativePath(dirPath, file.FullName);
          }
        }
        return res;
    }

    private async Task WritePatcheAsync(IEnumerable<string> patches, string dirPath, string patchInfo)
    {
      using(var sm = new StreamWriter(Path.Combine(dirPath, patchInfo)))
      {
        foreach (var patch in patches)
        {
          if(patch != patches.First())
          {
            await sm.WriteAsync("|");
          }
          await sm.WriteAsync(patch);
        }
      }
    }

    public async Task<string> PatchAsync(Stream zip, string zipFileName, string baseFileName="weex.js", string patchInfo = "patch_info.txt")
    {
      var dirPath = await ZipHelper.UnZipAsync(zip);
      await PatchCoreAsync(dirPath, baseFileName, patchInfo);
      return await ZipHelper.ZipAsync(dirPath, zipFileName);
    }

    private async Task PatchCoreAsync(string dirPath, string baseFileName, string patchInfo = "patch_info.txt")
    {
      var patches = await ParsePatchAsync(dirPath, patchInfo);
      var tasks = patches.Select(async x=>  await DoPatchAsync(dirPath, baseFileName, x));
      await Task.WhenAll(tasks);
    }

    private async Task<string[]> ParsePatchAsync(string dirPath, string patchInfo)
    {
      var text = await File.ReadAllTextAsync(Path.Combine(dirPath, patchInfo));
      return text.Split("|");
    }

    private async Task DoPatchAsync(string dirPath, string baseFileName, string patchFile)
    {
      using(var bs = File.OpenRead(Path.Combine(dirPath, baseFileName)))
      {
        var pi  = new FileInfo(Path.Combine(dirPath, patchFile));
        var oi = new FileInfo(Path.GetTempFileName());
        using(var os = oi.OpenWrite())
        {
          await BSAlgorithm.PatchAsync(bs, ()=> pi.OpenRead(), os);
        }
        oi.Replace(pi.FullName, null);
      }
    }
  }
}