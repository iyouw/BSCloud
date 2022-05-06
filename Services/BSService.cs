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
      var uz = Stopwatch.StartNew();
      var dirPath = ZipHelper.UnZip(zip);
      uz.Stop();
      _logger.LogInformation($"unzip ellapsed: {uz.ElapsedMilliseconds}ms");

      var d = Stopwatch.StartNew();
      var patches = await DiffCoreAsync(dirPath, baseFileName, filter);
      d.Stop();
      _logger.LogInformation($"diff ellapsed: {d.ElapsedMilliseconds}ms");

      var p = Stopwatch.StartNew();
      await WritePatches(patches, dirPath, patchInfo);
      p.Stop();
      _logger.LogInformation($"write patches ellapsed: {p.ElapsedMilliseconds}ms");

      var z = Stopwatch.StartNew();
      var res = ZipHelper.Zip(dirPath, zipFileName);
      z.Stop();
      _logger.LogInformation($"zip ellapsed: {z.ElapsedMilliseconds}ms");
      return res;
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
              res.Add(Path.Combine(dirPath, file.FullName));
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