using Com.Scm.Config;
using Com.Scm.Controllers;
using Com.Scm.Filters;
using Com.Scm.Image.SkiaSharp;
using Com.Scm.Nas.Log;
using Com.Scm.Nas.Res;
using Com.Scm.Nas.Sync.Dvo;
using Com.Scm.Sys.SysSafety;
using Com.Scm.Terminal;
using Com.Scm.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace Com.Scm.Api.Controllers
{
    [ApiExplorerSettings(GroupName = "Nas")]
    public class SyncController : ApiController
    {
        private ISqlSugarClient _SqlClient;
        private EnvConfig _EnvConfig;
        private ScmSysSafetyService _SafetyService;

        public SyncController(ISqlSugarClient sqlClient,
            EnvConfig envConfig,
            ScmSysSafetyService safetyService,
            ITerminalHolder terminalHolder)
        {
            _SqlClient = sqlClient;
            _EnvConfig = envConfig;
            _SafetyService = safetyService;
        }


        /// <summary>
        /// 增量更新
        /// 根据日志更新
        /// </summary>
        [HttpGet]
        public async Task<ScmSearchPageResponse<NasLogFileDto>> GetByLogAsync(GetLogRequest request, long terminalId, string accessToken)
        {
            return await _SqlClient.Queryable<NasLogFileDao>()
                .Where(a => a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasLogFileDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 全量更新
        /// 按目录更新
        /// </summary>
        [HttpGet]
        public async Task<ScmSearchPageResponse<NasFileDirDto>> GetDirByDirAsync(GetDirRequest request)
        {
            return await _SqlClient.Queryable<NasFileDirDao>()
                .Where(a => a.dir_id == request.id && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasFileDirDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 全量更新
        /// 按目录更新
        /// </summary>
        [HttpGet]
        public async Task<ScmSearchPageResponse<NasFileDocDto>> GetDocByDirAsync(GetDocRequest request)
        {
            return await _SqlClient.Queryable<NasFileDocDao>()
                .Where(a => a.dir_id == request.id && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasFileDocDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 上传操作日志
        /// </summary>
        [HttpPost("Log")]
        [AllowAnonymous]
        public async Task<PostLogResult> PostLogAsync(NasLogFileDto dto, [FromHeader] long terminalId, [FromHeader] string accessToken)
        {
            if (dto == null)
            {
                return PostLogResult.Failure("上传对象为空！");
            }

            var tmpFile = _EnvConfig.GetTempPath(dto.hash + ".tmp");
            if (!System.IO.File.Exists(tmpFile))
            {
                return PostLogResult.Failure("上传文档不存在！");
            }

            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            if (!FileUtils.Moveto(tmpFile, dstFile))
            {
                return PostLogResult.Failure("上传文档移动异常！");
            }

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return PostLogResult.Success();
        }

        #region 文件下载
        /// <summary>
        /// 单文件下载
        /// </summary>
        [AllowAnonymous]
        public void Download()
        {
        }
        #endregion

        #region 文件上传
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("upload")]
        public async Task<ScmUploadResponse> UploadAsync(ScmUploadRequest request)
        {
            if (request.type == UploadTypeEnum.ByFile)
            {
                return await ByFileAsync(request);
            }

            if (request.type == UploadTypeEnum.ByPart)
            {
                return await ByPartAsync(request);
            }

            if (request.type == UploadTypeEnum.ByHash)
            {
                return await ByHashAsync(request);
            }

            var response = new ScmUploadResponse();
            response.SetFailure("未知的上传类型！");
            return response;
        }

        /// <summary>
        /// 文件上传
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("file")]
        public async Task<ScmUploadResponse> ByFileAsync(ScmUploadRequest request)
        {
            var response = new ScmUploadResponse();

            var file = request.file;
            if (file == null)
            {
                response.SetFailure("上传文件为空！");
                return response;
            }

            var name = file.Name;

            var exts = Path.GetExtension(file.FileName).ToLower();
            if (!IsAcceptExts(exts))
            {
                response.SetFailure("不支持的文件类型！");
                return response;
            }

            var dstFile = _EnvConfig.GetTempPath(name);
            using (var stream = System.IO.File.OpenWrite(dstFile))
            {
                await file.CopyToAsync(stream);
            }

            response.SetSuccess($"文件上传成功！");
            return response;
        }

        /// <summary>
        /// 分段上传
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("part")]
        public async Task<ScmUploadResponse> ByPartAsync(ScmUploadRequest request)
        {
            var response = new ScmUploadResponse();

            var files = Request.Form.Files;
            if (files.Count == 0)
            {
                response.SetFailure("请选择文件！");
                return response;
            }

            var qty = 0;
            foreach (var file in files)
            {
                var name = file.Name;

                var exts = Path.GetExtension(file.FileName).ToLower();
                if (!IsAcceptExts(exts))
                {
                    response.SetFailure("不支持的文件类型！");
                    return response;
                }

                var dstFile = "";
                using (var stream = System.IO.File.OpenWrite(dstFile))
                {
                    await file.CopyToAsync(stream);
                }
                qty += 1;
            }

            response.SetSuccess(qty, $"成功上传 {qty} 个文件！");
            return response;
        }

        /// <summary>
        /// 检查上传清单
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmUploadResponse> CheckAsync(ScmUploadRequest request)
        {
            return null;
        }

        /// <summary>
        /// 摘要上传
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("hash")]
        public async Task<ScmUploadResponse> ByHashAsync(ScmUploadRequest request)
        {
            var response = new ScmUploadResponse();

            var files = Request.Form.Files;
            if (files.Count == 0)
            {
                response.SetFailure("请选择文件！");
                return response;
            }

            var qty = 0;
            foreach (var file in files)
            {
                var name = file.Name;

                var exts = Path.GetExtension(file.FileName).ToLower();
                if (!IsAcceptExts(exts))
                {
                    response.SetFailure("不支持的文件类型！");
                    return response;
                }

                var dstFile = "";
                using (var stream = System.IO.File.OpenWrite(dstFile))
                {
                    await file.CopyToAsync(stream);
                }
                qty += 1;
            }

            response.SetSuccess(qty, $"成功上传 {qty} 个文件！");
            return response;
        }
        #endregion

        /// <summary>
        /// 是否可接受文件
        /// </summary>
        /// <param name="exts"></param>
        /// <returns></returns>
        private bool IsAcceptExts(string exts)
        {
            var safety = _SafetyService.Get();
            if (string.IsNullOrWhiteSpace(safety.UploadWhite))
            {
                return true;
            }

            var arr = safety.UploadWhite.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            return arr.Contains(exts);
        }

        [HttpGet("avatar/{file}"), AllowAnonymous, NoJsonResult, NoAuditLog]
        public async Task<IActionResult> AvatarAsync(string file)
        {
            var path = _EnvConfig.GetAvatarPath(file);
            if (!System.IO.File.Exists(path))
            {
                var result = new ImageEngine().GenAvatar();
                return File(result.Image, "image/png");
            }

            using (var stream = System.IO.File.OpenRead(path))
            {
                var bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                return File(bytes, "image/png");
            }
        }
    }
}
