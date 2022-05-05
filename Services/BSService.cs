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
    public async Task<string> DiffAsync(Stream src, Stream target, string srcDirName, string filter="js")
    {
      var (rootDir, srcDir, diffDir) = CreateTempDir(srcDirName,"src","diff");
      var zip = new FastZip();
      zip.ExtractZip(src, srcDir, FastZip.Overwrite.Always, null, "", "", false, true);

      var oldData = new byte[target.Length];
      await target.ReadAsync(oldData, 0, oldData.Length);

      var tasks = ProcessDirEntries(srcDir, async (entry, isDir) =>
      {
        var rel = Path.GetRelativePath(srcDir, entry);
        var entryPath = Path.Combine(diffDir, rel);
        if(isDir)
        {
          Directory.CreateDirectory(entryPath);
        } 
        else if(entryPath.EndsWith(filter))
        {
          using(var fs = File.Create(entryPath))
          {
            BSAlgorithm.Diff(oldData, await File.ReadAllBytesAsync(entry), fs);
          }
        }
      });

      await Task.WhenAll(tasks);

      var zipFile = Path.Combine(rootDir,"diff.zip");
      zip.CreateZip(File.Create(zipFile), diffDir, true, "", "", false);
      
      return zipFile;
    }

    public async Task<string> PatchAsync(Stream src, Stream target, string srcDirName, string filter = "js")
    {
      var (rootDir, srcDir, patchDir) = CreateTempDir(srcDirName,"src","patch");
      var temp = Path.Combine(Directory.GetCurrentDirectory(), "Temp");

      var zip = new FastZip();
      zip.ExtractZip(src, srcDir, FastZip.Overwrite.Always, null, "", "", false, true);

      var tasks = ProcessDirEntries(srcDir,  (entry, isDir) =>
      {
        var rel = Path.GetRelativePath(srcDir, entry);
        var entryPath = Path.Combine(patchDir, rel);
        if(isDir)
        {
          Directory.CreateDirectory(entryPath);
        } 
        else if(entryPath.EndsWith(filter))
        {
          using(var fs = File.Create(entryPath))
          {
            BSAlgorithm.Patch(target, () => new FileStream(entry, FileMode.Open, FileAccess.Read, FileShare.Read), fs);
          }
        }

        return Task.FromResult(0);
      });

      await Task.WhenAll(tasks);

      var zipFile = Path.Combine(rootDir,"patch.zip");
      zip.CreateZip(File.Create(zipFile), patchDir, true, "", "", false);

      return zipFile;
    }
    

    private (string,string,string) CreateTempDir(string dir, string src, string dest)
    {
      var root = Path.Combine(Directory.GetCurrentDirectory(), "Temp", dir);
      if(Directory.Exists(root))
      {
        Directory.Delete(root, true);
      }
      Directory.CreateDirectory(root);
      var srcDir = Path.Combine(root, src);
      Directory.CreateDirectory(srcDir);
      var destDir = Path.Combine(root, dest);
      Directory.CreateDirectory(destDir);
      return (root, srcDir, destDir);
    }

    private IEnumerable<Task> ProcessDirEntries(string dir, Func<string,bool, Task> processEntry)
    {
      foreach (var (entry, isDir) in TraversalDir(dir))
      {
        yield return processEntry(entry, isDir);        
      }
    }

    private IEnumerable<(string, bool)> TraversalDir(string dir)
    {
      var entries = new List<(string, bool)>();
      foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
      {
        var isDir = IsDirectory(entry);
        entries.Add((entry, isDir));
        if(isDir)
        {
          entries.AddRange(TraversalDir(entry));
        }
      }
      return entries;
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