using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

using ICSharpCode.SharpZipLib.Zip;

namespace BSCloud.Services
{
  public class BSService : IBSService
  {
    public async Task<(Stream,string)> DiffAsync(Stream src, Stream target, string srcDirName, string filter="js")
    {
      var rootDir = CreateTempDir(srcDirName);
      var oldData = new byte[target.Length];
      await target.ReadAsync(oldData, 0, oldData.Length);
      var tasks = TraversalZipStream(src, async zipEntry =>
      {
        if(zipEntry.IsDirectory)
        {
          Directory.CreateDirectory(Path.Combine(rootDir, zipEntry.Name));
        }
        if(zipEntry.IsFile && zipEntry.Name.EndsWith(filter))
        {
          using(var fs = File.Create(Path.Combine(rootDir, zipEntry.Name)))
          {
            BSAlgorithm.Diff(oldData, await Task.FromResult(new byte[2]), fs);
          }
        }
      });

      var ms = new MemoryStream();
      var zip = new FastZip();
      zip.CreateZip(ms, rootDir, true, "", "", true);
      ms.Position = 0;
      return (ms, rootDir);
    }

    public async Task<Stream> PatchAsync(Stream src, Stream target, string srcDirName, string filter = "js")
    {
      var temp = Path.Combine(Directory.GetCurrentDirectory(), "Temp");

      var rootDir = Path.Combine(temp, $"{srcDirName}_patch");
      if(Directory.Exists(rootDir))
      {
        Directory.Delete(rootDir,true);
      }
      Directory.CreateDirectory(rootDir);

      var zip = new FastZip();
      zip.ExtractZip(src, rootDir, FastZip.Overwrite.Always, null, "", "", false, false);

      var patchDir = Path.Combine(rootDir,"patch");
      Directory.CreateDirectory(patchDir);

      var srcDir = Path.Combine(rootDir, srcDirName);

      await TraversalDir(srcDir, patchDir, (entry, patchEntry)=>
      {
        if(entry.EndsWith(filter))
        {
          using(var fs = File.Create(patchEntry))
          {
            BSAlgorithm.Patch(target, () => new FileStream(entry, FileMode.Open, FileAccess.Read, FileShare.Read), fs);
          }
        }
      });
      var ms = new MemoryStream();

      zip.CreateZip(ms, rootDir, true, "", "", true);

      ms.Position = 0;
      return ms;
    }
    

    private string CreateTempDir(string name)
    {
      var path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", name);
      if(Directory.Exists(path))
      {
        Directory.Delete(path, true);
      }
      Directory.CreateDirectory(path);
      return path;
    }

    private IEnumerable<Task> TraversalZipStream(Stream zipStream, Func<ZipEntry, Task> ProccessZip)
    {
      ZipEntry entry;
      var zs = new ZipInputStream(zipStream);
      while((entry = zs.GetNextEntry()) != null)
      {
        yield return ProccessZip(entry);
      }
    }


    private async Task TraversalDir(string srcDir, string destDir, Action<string, string> ProcessFile)
    {
      var entries = Directory.EnumerateFileSystemEntries(srcDir);
      foreach(var entry in entries)
      {
        var destEntry = entry.Replace(srcDir, destDir);
        if(IsDirectory(entry))
        {
          Directory.CreateDirectory(destEntry);
          await TraversalDir(entry, destEntry, ProcessFile);
        }
        else
        {
          ProcessFile(entry, destEntry);
        }
      }
    }

    private bool IsDirectory(string path)
    {
      return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
    }
  }
}