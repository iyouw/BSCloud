using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BSCloud.Services;
using BSCloud.Results;


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
    public async Task<FileStreamResult> Diff(IFormFile zipFile, string baseFileName="weex.js", string filter="*.js", string patchInfo = "patch_info.txt")
    {
      using(var zip = zipFile.OpenReadStream())
      {
        var diff = await _service.DiffAsync(zip, Path.GetFileNameWithoutExtension(zipFile.FileName),baseFileName, filter, patchInfo);
        return new DeleteFileStreamResult(System.IO.File.OpenRead(diff),"application/octet-stream")
        {
          FileDownloadName = Path.GetFileName(diff)
        };
      }
    }

    [HttpPost]
    public async Task<FileStreamResult> Patch(IFormFile zipFile, string baseFileName="weex.js", string filter="*.js")
    {
      using(var zip = zipFile.OpenReadStream())
      {
        var patch = await _service.PatchAsync(zip, Path.GetFileNameWithoutExtension(zipFile.FileName),baseFileName, filter);
        return new DeleteFileStreamResult(System.IO.File.OpenRead(patch), "application/octet-stream")
        {
          FileDownloadName = Path.GetFileName(patch)
        };
      }
    }
  }
}