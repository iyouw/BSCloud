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
      var rootPath = FindRootPath(dirPath, baseFileName);

      var pathes = await DiffCoreAsync(rootPath, baseFileName, filter);
      var patches = pathes.Where(x=>!string.IsNullOrEmpty(x)).Prepend(baseFileName);

      await WritePatcheAsync(patches, rootPath, patchInfo);

      return await ZipHelper.ZipAsync(dirPath, zipFileName);
    }

    private async Task<string[]> DiffCoreAsync(string dirPath,string baseFileName, string filter)
    {
      var basePath = Path.Combine(dirPath, baseFileName);
      var baseData = await File.ReadAllBytesAsync(basePath);
      var dirInfo = new DirectoryInfo(dirPath);
      var tasks = dirInfo.EnumerateFiles(filter, SearchOption.AllDirectories)
                  .Select(file => DoDiffAsync(file, baseData, dirPath, basePath, filter));
      return await Task.WhenAll(tasks);
    }

    private async Task<string> DoDiffAsync(FileInfo file, byte[] baseData, string dirPath, string basePath, string filter)
    {
      var res = string.Empty;
      if(file.FullName != basePath)
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
      var rootPath = FindRootPath(dirPath, baseFileName);
      await PatchCoreAsync(rootPath, baseFileName, patchInfo);
      return await ZipHelper.ZipAsync(dirPath, zipFileName);
    }

    private async Task PatchCoreAsync(string dirPath, string baseFileName, string patchInfo = "patch_info.txt")
    {
      var patches = await ParsePatchAsync(dirPath, patchInfo);
      var tasks = patches.Skip(1).Select(async x=>  await DoPatchAsync(dirPath, baseFileName, x));
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

    private string FindRootPath(string dirPath, string baseFileName){
      if(File.Exists(Path.Combine(dirPath, baseFileName))){
        return dirPath;
      }
      foreach (var subPath in Directory.EnumerateDirectories(dirPath))
      {
        if(File.Exists(Path.Combine(subPath, baseFileName)))
        {
          return subPath;
        }
      }
      throw new FileNotFoundException(baseFileName);
    }
  }
}