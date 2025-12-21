using Com.Scm.Config;
using Com.Scm.Nas.Log;
using Com.Scm.Nas.Res;
using Com.Scm.Nas.Sync.Dvo;
using Com.Scm.Service;
using Com.Scm.Terminal;
using Com.Scm.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace Com.Scm.Nas.Sync
{
    [ApiExplorerSettings(GroupName = "Scm")]
    [AllowAnonymous]
    public class NasSyncService : ApiService
    {
        private ITerminalHolder _TerminalHolder;

        public NasSyncService(ISqlSugarClient sqlClient, EnvConfig envConfig, ITerminalHolder terminalHolder)
        {
            _SqlClient = sqlClient;
            _EnvConfig = envConfig;
            _TerminalHolder = terminalHolder;
        }

        public async Task<ScmSearchPageResponse<NasLogFileDto>> GetLogAsync(GetLogRequest request)
        {
            var terminal = _TerminalHolder.GetTerminal(request.terminal_id);
            if (terminal == null || terminal.IsExpired())
            {
                return null;
            }

            return await _SqlClient.Queryable<NasLogFileDao>()
                .Where(a => a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasLogFileDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmSearchPageResponse<NasFileDirDto>> GetDirAsync(GetDirRequest request)
        {
            var terminal = _TerminalHolder.GetTerminal(request.terminal_id);
            if (terminal == null || terminal.IsExpired())
            {
                return null;
            }

            return await _SqlClient.Queryable<NasFileDirDao>()
                .Where(a => a.dir_id == request.id && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasFileDirDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmSearchPageResponse<NasFileDocDto>> GetDocAsync(GetDocRequest request)
        {
            var terminal = _TerminalHolder.GetTerminal(request.terminal_id);
            if (terminal == null || terminal.IsExpired())
            {
                return null;
            }

            return await _SqlClient.Queryable<NasFileDocDao>()
                .Where(a => a.dir_id == request.id && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasFileDocDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="terminalId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<PostLogResult> PostLogAsync(NasLogFileDto dto,
            [FromHeader] long terminalId,
            [FromHeader] string token)
        {
            if (dto == null)
            {
                return PostLogResult.Failure("上传对象为空！");
            }

            var terminalToken = _TerminalHolder.GetTerminal(terminalId);
            if (terminalToken == null || terminalToken.IsExpired())
            {
                return null;
            }

            var result = new PostLogResult();
            if (dto.opt == NasOptEnums.Delete)
            {
                await DeleteFile(dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Create)
            {
                await CreateFile(terminalToken, dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Rename)
            {
                await RenameFile(dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Move)
            {
                await MoveFile(dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Copy)
            {
                await CopyFile(dto, result);
                return result;
            }

            result.SetFailure("不支持的操作：" + dto.opt);
            return result;
            //var tmpFile = _EnvConfig.GetTempPath(dto.hash + ".tmp");
            //if (!System.IO.File.Exists(tmpFile))
            //{
            //    return PostLogResult.Failure("上传文档不存在！");
            //}

            //var dstFile = _EnvConfig.GetUploadPath(dto.file);
            //if (!FileUtils.Moveto(tmpFile, dstFile))
            //{
            //    return PostLogResult.Failure("上传文档移动异常！");
            //}

            //var dao = dto.Adapt<NasLogFileDao>();
            //await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            //return PostLogResult.Success();
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<bool> CreateFile(ScmTerminalToken token, NasLogFileDto dto, PostLogResult result)
        {
            if (dto.type == NasTypeEnums.Doc)
            {
                return await CreateDoc(token, dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await CreateDir(token, dto, result);
            }

            return false;
        }

        /// <summary>
        /// 创建文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CreateDoc(ScmTerminalToken token, NasLogFileDto dto, PostLogResult result)
        {
            var tmpFile = _EnvConfig.GetTempPath(dto.hash + ".tmp");
            if (!File.Exists(tmpFile))
            {
                return false;
            }

            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            if (!FileUtils.Moveto(tmpFile, dstFile))
            {
                PostLogResult.Failure("上传文档移动异常！");
                return false;
            }

            await CreateDocDao(token, dto);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CreateDir(ScmTerminalToken token, NasLogFileDto dto, PostLogResult result)
        {
            var tmpFile = GetUploadPath(token, dto.file);
            if (!Directory.Exists(tmpFile))
            {
                Directory.CreateDirectory(tmpFile);
            }

            await CreateDirDao(token, dto);
            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        private async Task CreateDocDao(ScmTerminalToken token, NasLogFileDto dto)
        {
            var docDao = new NasFileDocDao();
            docDao.terminal_id = token.id;
            docDao.drive_id = dto.drive_id;
            docDao.dir_id = dto.dir_id;
            docDao.name = dto.name;
            docDao.path = dto.file;
            docDao.hash = dto.hash;
            docDao.size = dto.size;
            docDao.PrepareCreate(token.user_id);

            await _SqlClient.Insertable(docDao).ExecuteCommandAsync();
        }

        private async Task CreateDirDao(ScmTerminalToken token, NasLogFileDto dto)
        {
            var dirDao = new NasFileDirDao();
            dirDao.terminal_id = token.id;
            dirDao.drive_id = dto.drive_id;
            dirDao.dir_id = dto.dir_id;
            dirDao.name = dto.name;
            dirDao.path = dto.file;
            dirDao.PrepareCreate(token.user_id);

            await _SqlClient.Insertable(dirDao).ExecuteCommandAsync();
        }

        private async Task DeleteDocDao(NasLogFileDto dto)
        {
            await _SqlClient.Deleteable<NasFileDocDao>()
                .Where(a => a.dir_id == dto.dir_id && a.name == dto.name)
                .ExecuteCommandAsync();
        }

        private async Task DeleteDirDao(NasLogFileDto dto)
        {
            await _SqlClient.Deleteable<NasFileDirDao>()
                .Where(a => a.dir_id == dto.dir_id)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<bool> DeleteFile(NasLogFileDto dto, PostLogResult result)
        {
            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await DeleteDoc(dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await DeleteDir(dto, result);
            }

            return false;
        }

        /// <summary>
        /// 删除文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> DeleteDoc(NasLogFileDto dto, PostLogResult result)
        {
            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            FileUtils.DeleteFile(dstFile);

            await DeleteDocDao(dto);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> DeleteDir(NasLogFileDto dto, PostLogResult result)
        {
            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            FileUtils.DeleteFolder(dstFile);

            await DeleteDirDao(dto);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<bool> MoveFile(NasLogFileDto dto, PostLogResult result)
        {
            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await MoveDoc(dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await MoveDir(dto, result);
            }

            return false;
        }

        /// <summary>
        /// 移动文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> MoveDoc(NasLogFileDto dto, PostLogResult result)
        {
            var srcFile = _EnvConfig.GetUploadPath(dto.src);
            if (!File.Exists(srcFile))
            {
                return false;
            }

            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            FileUtils.Moveto(srcFile, dstFile);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> MoveDir(NasLogFileDto dto, PostLogResult result)
        {
            var srcFile = _EnvConfig.GetUploadPath(dto.src);
            if (!Directory.Exists(srcFile))
            {
                return false;
            }

            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            FileUtils.Moveto(srcFile, dstFile);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<bool> CopyFile(NasLogFileDto dto, PostLogResult result)
        {
            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await CopyDoc(dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await CopyDir(dto, result);
            }

            return false;
        }

        /// <summary>
        /// 移动文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CopyDoc(NasLogFileDto dto, PostLogResult result)
        {
            var srcFile = _EnvConfig.GetUploadPath(dto.src);
            if (!File.Exists(srcFile))
            {
                return false;
            }

            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            FileUtils.Copyto(srcFile, dstFile);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CopyDir(NasLogFileDto dto, PostLogResult result)
        {
            var srcFile = _EnvConfig.GetUploadPath(dto.src);
            if (!Directory.Exists(srcFile))
            {
                return false;
            }

            var dstFile = _EnvConfig.GetUploadPath(dto.file);
            FileUtils.Copyto(srcFile, dstFile);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">虚拟绝对路径</param>
        /// <returns></returns>
        private async Task<NasFileDirDao> GetDirDaoByPath(string path)
        {
            return await _SqlClient.Queryable<NasFileDirDao>()
                .Where(a => a.user_id == 0 && a.path == path)
                .FirstAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">虚拟绝对路径</param>
        /// <returns></returns>
        private async Task<NasFileDocDao> GetDocDaoByPath(string path)
        {
            return await _SqlClient.Queryable<NasFileDocDao>()
                .Where(a => a.user_id == 0 && a.path == path)
                .FirstAsync();
        }

        public async Task<bool> RenameFile(NasLogFileDto dto, PostLogResult result)
        {
            return true;
        }

        private string GetUploadPath(ScmTerminalToken token, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var tmp = path.ToLower();
            if (tmp.StartsWith(NasEnv.VirtualTag))
            {
                path = path.Substring(NasEnv.VirtualTag.Length);
            }

            return _EnvConfig.GetUploadPath(path);
        }
    }
}
