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

        #region 小文件上传
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

            var name = file.FileName;

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
        #endregion

        #region 大文件上传
        /// <summary>
        /// 分块上传
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("chunk")]
        public async Task<ScmUploadResponse> UploadChunkAsync(ScmUploadRequest request)
        {
            return null;
        }

        /// <summary>
        /// 上传校验
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("check")]
        public async Task<ScmUploadResponse> UploadCheckAsync(ScmUploadRequest request)
        {
            return null;
        }

        /// <summary>
        /// 文件合并
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("merge")]
        public async Task<ScmUploadResponse> UploadMergeAsync(ScmUploadRequest request)
        {
            return null;
        }
        #endregion
    }
}
