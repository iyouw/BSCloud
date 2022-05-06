using System.Diagnostics;
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
    private readonly ILogger<BSService> _logger;

    public BSService(ILogger<BSService> logger)
    {
      _logger = logger;      
    }
    public async Task<string> DiffAsync(Stream zip, string zipFileName, string baseFileName="weex.js", string filter="js", string patchInfo = "patch_info.txt")
    {
      var dirPath = ZipHelper.UnZip(zip);

      var d = Stopwatch.StartNew();
      var patches = await DiffCoreAsync(dirPath, baseFileName, filter);
      d.Stop();
      _logger.LogInformation($"diff ellapsed: {d.ElapsedMilliseconds}ms");

      await WritePatches(patches, dirPath, patchInfo);

      return ZipHelper.Zip(dirPath, zipFileName);
    }

    private async Task<IList<string>> DiffCoreAsync(string dirPath,string baseFileName, string filter)
    {
      var res = new List<string>();
      var baseData = await File.ReadAllBytesAsync(Path.Combine(dirPath, baseFileName));
      var dirInfo = new DirectoryInfo(dirPath);
      var tasks = dirInfo.EnumerateFiles(filter, SearchOption.AllDirectories).Select(async file =>
      {
        if(file.Name != baseFileName)
        {
          var newData = await File.ReadAllBytesAsync(file.FullName);
          var diffFile = new FileInfo($"{file.FullName}.diff");
          using(var fs = diffFile.Create())
          {
            var da = Stopwatch.StartNew();
            BSAlgorithm.Diff(baseData, newData, fs);
            da.Stop();
            _logger.LogInformation($"{file.FullName} diff algorithm ellapsed: {da.ElapsedMilliseconds}");
          }
          if(diffFile.Length < file.Length)
          {
            lock(this)
            {
              res.Add(Path.GetRelativePath(dirPath, file.FullName));
            }
            diffFile.Replace(file.FullName, null);
          }
        }
      });
      await Task.WhenAll(tasks);
      return res;
    }

    private async Task WritePatches(IList<string> patches, string dirPath, string patchInfo)
    {
      var isFirst = true;
      using(var sm = new StreamWriter(Path.Combine(dirPath, patchInfo)))
      {
        foreach (var patch in patches)
        {
          if(isFirst)
          {
            isFirst = false;
            await sm.WriteAsync(patch);
          }
          else
          {
            await sm.WriteAsync($"|{patch}");
          }
        }
      }
    }

    public Task<string> PatchAsync(Stream zip, string zipFileName, string baseFileName="weex.js", string filter = "js")
    {
      var dirPath = ZipHelper.UnZip(zip);

      var zipFile = ZipHelper.Zip(dirPath, zipFileName);
      return Task.FromResult(zipFile);
    }
  }
}