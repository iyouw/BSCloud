using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BSCloud.Services;

namespace BSCloud.Controllers
{
  [ApiController]
  [Route("api/[controller]/[action]")]
  public class BSController: ControllerBase
  {
    private readonly IBSService _service;

    public BSController(IBSService service)
    {
      this._service = service;
    }

    [HttpPost]
    public async Task<FileStreamResult> Diff(IFormFile src, IFormFile target)
    {
      var dir = string.Empty;
      try
      {
        using(var srcFile = src.OpenReadStream())
        using(var targetFile = target.OpenReadStream())
        {
          var (destFile, rootDir) = await _service.DiffAsync(srcFile, targetFile, Path.GetFileNameWithoutExtension(src.FileName));
          dir = rootDir;
          return File(destFile,"application/octet-stream","diff.zip");
        }
      }
      finally
      {
        Directory.Delete(dir, true);
      }
      
    }

    [HttpPost]
    public async Task<FileStreamResult> Patch(IFormFile src, IFormFile target)
    {
      using(var srcFile = src.OpenReadStream())
      using(var targetFile = target.OpenReadStream())
      {
        using(var destFile = await _service.PatchAsync(srcFile, targetFile, Path.GetFileNameWithoutExtension(src.FileName)))
        {
          return File(srcFile,"application/octet-stream","patch.zip");
        }
      }
    }
  }
}