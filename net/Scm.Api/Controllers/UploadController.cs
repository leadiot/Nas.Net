using Com.Scm.Config;
using Com.Scm.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.Scm.Api.Controllers
{
    [ApiExplorerSettings(GroupName = "Nas")]
    [AllowAnonymous]
    public class UploadController : ApiController
    {
        private EnvConfig _EnvConfig;

        public UploadController(EnvConfig envConfig)
        {
            _EnvConfig = envConfig;
        }

        [HttpPost("file")]
        public async Task<ScmUploadResponse> UploadFileAsync(ScmUploadRequest request)
        {
            var response = new ScmUploadResponse();

            var file = request.file;
            if (file == null)
            {
                response.SetFailure("上传文件为空！");
                return response;
            }

            var name = file.Name;

            //var exts = Path.GetExtension(file.FileName).ToLower();
            //if (!IsAcceptExts(exts))
            //{
            //    response.SetFailure("不支持的文件类型！");
            //    return response;
            //}

            var dstFile = _EnvConfig.GetTempPath(name);
            using (var stream = System.IO.File.OpenWrite(dstFile))
            {
                await file.CopyToAsync(stream);
            }

            response.SetSuccess($"文件上传成功！");
            return response;
        }
    }
}
