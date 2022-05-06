using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace BSCloud.Infra
{
  public static class ZipHelper
  {
    public static string TempDir = Path.Combine(Path.GetTempPath(),"BSCloud");

    public static string Zip(string dirPath, string fileName)
    {
      var file = Path.Combine(TempDir, $"{fileName}.zip");
      var ziper = new FastZip();
      ziper.CreateZip(file, dirPath, true, "", "");
      Directory.Delete(dirPath, true);
      return file;
    }

    public static string UnZip(Stream zip)
    {
      var dirName = Guid.NewGuid().ToString();
      var dirPath = Path.Combine(TempDir, dirName);
      var ziper = new FastZip();
      ziper.ExtractZip(zip, dirPath, FastZip.Overwrite.Always, null, "", "", true, false);
      return dirPath;
    }
  }
}