using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace BSCloud.Infra
{
  public static class ZipHelper
  {
    public static string TempDir = Path.Combine(Directory.GetCurrentDirectory(),"Temp");
    public static readonly FastZip Ziper = new FastZip();

    public static string Zip(string dirPath, string fileName)
    {
      var file = Path.Combine(TempDir, $"{fileName}.zip");
      Ziper.CreateZip(file, dirPath, true, "", "");
      Directory.Delete(dirPath, true);
      return file;
    }

    public static string UnZip(Stream zip)
    {
      var dirName = Guid.NewGuid().ToString();
      var dirPath = Path.Combine(TempDir, dirName);
      Ziper.ExtractZip(zip, dirPath, FastZip.Overwrite.Always, null, "", "", true, false);
      return dirPath;
    }
  }
}