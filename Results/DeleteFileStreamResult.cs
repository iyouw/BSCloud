using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace BSCloud.Results
{
  public class DeleteFileStreamResult : FileStreamResult
  {
    public DeleteFileStreamResult(Stream fileStream, string contentType)
      :base(fileStream, contentType)
    {

    }

    public DeleteFileStreamResult(Stream fileStream, MediaTypeHeaderValue contentType)
      :base(fileStream, contentType)
    {

    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
      await base.ExecuteResultAsync(context);
      var file = (this.FileStream as FileStream)?.Name;
      File.Delete(file);
    }
  }
}