using Com.Scm.Api;
using Com.Scm.Config;
using Com.Scm.Nas.Cfg;
using Com.Scm.Nas.Log;
using Com.Scm.Nas.Res;
using Com.Scm.Nas.Sync.Dvo;
using Com.Scm.Service;
using Com.Scm.Terminal;
using Com.Scm.Token;
using Com.Scm.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Text;

namespace Com.Scm.Nas.Sync
{
    /// <summary>
    /// 终端文件同步服务
    /// </summary>
    [ApiExplorerSettings(GroupName = "Nas")]
    [AllowAnonymous]
    public class NasSyncService : AppService
    {
        private ScmContextHolder _ScmHolder;
        private ITerminalHolder _TerminalHolder;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sqlClient"></param>
        /// <param name="envConfig"></param>
        /// <param name="terminalHolder"></param>
        public NasSyncService(ISqlSugarClient sqlClient,
            EnvConfig envConfig,
            ScmContextHolder scmHolder,
            ITerminalHolder terminalHolder)
        {
            _SqlClient = sqlClient;
            _EnvConfig = envConfig;
            _ScmHolder = scmHolder;
            _TerminalHolder = terminalHolder;
        }

        /// <summary>
        /// 消息回馈
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmApiResponse> GetEchoAsync(GetLogRequest request)
        {
            var response = new ScmApiResponse();
            response.SetSuccess();

            return response;
        }

        /// <summary>
        /// 获取驱动列表
        /// </summary>
        /// <returns></returns>
        public async Task<ScmApiListResponse<NasCfgDriveDto>> GetDriveAsync()
        {
            var list = await _SqlClient.Queryable<NasCfgDriveDao>()
                .Where(a => a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .Select<NasCfgDriveDto>()
                .ToListAsync();

            var response = new ScmApiListResponse<NasCfgDriveDto>();
            response.SetSuccess(list);

            return response;
        }

        /// <summary>
        /// 更新驱动
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<NasCfgDriveDto> PostDriveAsync(NasCfgDriveDto model, [FromHeader] string appToken)
        {
            var token = GetToken(appToken);
            LogUtils.Debug("SaveDrive:" + token.ToJsonString());

            if (!ScmUtils.IsValidId(model.folder_id))
            {
                model.folder_id = CreateDirDao(model.path, ScmEnv.DEFAULT_ID);
            }

            var dao = await _SqlClient.Queryable<NasCfgDriveDao>()
                .Where(a => a.terminal_id == token.terminal_id && a.folder_id == model.folder_id)
                .FirstAsync();

            if (dao == null)
            {
                dao = model.Adapt<NasCfgDriveDao>();
                dao.terminal_id = token.terminal_id;
                dao.PrepareCreate(ScmEnv.DEFAULT_ID);
                await _SqlClient.Insertable(dao).ExecuteCommandAsync();
            }
            else
            {
                dao.row_status = Enums.ScmRowStatusEnum.Enabled;
                dao.PrepareUpdate(ScmEnv.DEFAULT_ID);
                await _SqlClient.Updateable(dao).ExecuteCommandAsync();
            }

            model.id = dao.id;

            return model;
        }

        /// <summary>
        /// 检查指定HASH是否存在
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        [HttpGet("{hash}")]
        public async Task<bool> GetQueryAsync(string hash)
        {
            var exists = await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.hash == hash)
                .AnyAsync();

            return true;
        }

        /// <summary>
        /// 获取同步日志（按时间升序排列）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmSearchPageResponse<NasLogFileDto>> GetLogAsync(GetLogRequest request)
        {
            //var terminalId = _ScmHolder.GetToken().terminal_id;

            //var terminal = _TerminalHolder.GetTerminal(terminalId);
            //if (terminal == null || terminal.IsExpired())
            //{
            //    return null;
            //}
            var driveId = request.drive_id;
            var driveDao = GetDriveDao(driveId);

            return await _SqlClient.Queryable<NasLogFileDao>()
                .Where(a => a.drive_id != driveId &&
                    a.row_status == Enums.ScmRowStatusEnum.Enabled &&
                    a.path.StartsWith(driveDao.path) &&
                    a.id > request.id)
                .OrderBy(a => a.id, OrderByType.Asc)
                .Select<NasLogFileDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 获取目录列表（根据上级目录）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmSearchPageResponse<NasResFileDto>> GetDirAsync(GetDirRequest request)
        {
            //var terminalId = _ScmHolder.GetToken().terminal_id;

            //var terminal = _TerminalHolder.GetTerminal(terminalId);
            //if (terminal == null || terminal.IsExpired())
            //{
            //    return null;
            //}

            var byPath = request.by_path;// !string.IsNullOrEmpty(request.path);

            return await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.type == NasTypeEnums.Dir && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .WhereIF(byPath, a => a.path == request.path)
                .WhereIF(!byPath, a => a.dir_id == request.dir_id)
                .OrderBy(a => a.name, OrderByType.Asc)
                .Select<NasResFileDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 获取文档列表（根据上级目录）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmSearchPageResponse<NasResFileDto>> GetDocAsync(GetDocRequest request)
        {
            //var terminalId = _ScmHolder.GetToken().terminal_id;

            //var terminal = _TerminalHolder.GetTerminal(terminalId);
            //if (terminal == null || terminal.IsExpired())
            //{
            //    return null;
            //}

            var byPath = request.by_path;// !string.IsNullOrEmpty(request.path);

            return await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.type == NasTypeEnums.Doc && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .WhereIF(byPath, a => a.path == request.path)
                .WhereIF(!byPath, a => a.dir_id == request.dir_id)
                .OrderBy(a => a.name, OrderByType.Asc)
                .Select<NasResFileDto>()
                .ToPageAsync(request.page, request.limit);
        }

        /// <summary>
        /// 获取文件列表（根据上级目录）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmSearchPageResponse<NasResFileDto>> GetFileAsync(GetDocRequest request)
        {
            //var terminalId = _ScmHolder.GetToken().terminal_id;

            //var terminal = _TerminalHolder.GetTerminal(terminalId);
            //if (terminal == null || terminal.IsExpired())
            //{
            //    return null;
            //}

            var byPath = request.by_path;// !string.IsNullOrEmpty(request.path);

            var items = await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .WhereIF(byPath, a => a.path == request.path)
                .WhereIF(!byPath, a => a.dir_id == request.dir_id)
                .OrderBy(a => a.type, OrderByType.Asc)
                .OrderBy(a => a.name, OrderByType.Asc)
                .Select<NasResFileDto>()
                .ToPageAsync(request.page, request.limit);

            return items;
        }

        /// <summary>
        /// 上传同步日志
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="terminalId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<SyncResult> PostSyncAsync(NasLogFileDto dto, [FromHeader] string appToken)
        {
            if (dto == null)
            {
                LogUtils.Debug("上传对象为空！");
                return SyncResult.Failure("上传对象为空！");
            }

            var token = GetToken(appToken);

            var terminalToken = _TerminalHolder.GetTerminal(token.terminal_id);
            if (terminalToken == null || terminalToken.IsExpired())
            {
                LogUtils.Debug("无效的终端信息！");
                return SyncResult.Failure("无效的终端信息！");
            }

            var result = new SyncResult();
            if (dto.opt == NasOptEnums.Delete)
            {
                await DeleteFile(terminalToken, dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Create)
            {
                await CreateFile(terminalToken, dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Rename)
            {
                await RenameFile(terminalToken, dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Move)
            {
                await MoveFile(terminalToken, dto, result);
                return result;
            }

            if (dto.opt == NasOptEnums.Copy)
            {
                await CopyFile(terminalToken, dto, result);
                return result;
            }

            LogUtils.Debug("不支持的操作：" + dto.opt);
            result.SetFailure("不支持的操作：" + dto.opt);
            return result;
        }

        #region 删除文件
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> DeleteFile(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("删除文件：" + dto.path);

            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await DeleteDir(token, dto, result);
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await DeleteDoc(token, dto, result);
            }

            LogUtils.Debug("未知的文件类型：" + dto.type);
            result.SetFailure("未知的文件类型：" + dto.type);
            return false;
        }

        /// <summary>
        /// 删除文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> DeleteDoc(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("删除文档：" + dto.path);

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.DeleteDoc(dstFile);

            var docDao = await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.type == NasTypeEnums.Doc && a.path == dto.path && a.hash == dto.hash)
                .FirstAsync();
            if (docDao == null)
            {
                await DeleteDocDao(docDao);
            }

            AddLogFileByDto(token, dto);

            result.SetSuccess();
            return true;
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> DeleteDir(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("删除目录：" + dto.path);

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.DeleteDir(dstFile);

            var dirDao = await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.type == NasTypeEnums.Dir && a.path == dto.path)
                .FirstAsync();
            if (dirDao != null)
            {
                await DeleteDirDao(dirDao);

                await _SqlClient.Deleteable(dirDao).ExecuteCommandAsync();
            }

            AddLogFileByDto(token, dto);

            result.SetSuccess();
            return true;
        }

        private async Task DeleteDocDao(NasResFileDao dao)
        {
            await _SqlClient.Deleteable(dao).ExecuteCommandAsync();
        }

        private async Task DeleteDirDao(NasResFileDao dao)
        {
            var dirList = await _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.type == NasTypeEnums.Dir && a.dir_id == dao.dir_id)
                .ToListAsync();
            foreach (var dir in dirList)
            {
                await DeleteDirDao(dir);
            }

            await _SqlClient.Deleteable<NasResFileDao>()
                .Where(a => a.dir_id == dao.dir_id)
                .ExecuteCommandAsync();
        }
        #endregion

        #region 创建文件
        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CreateFile(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("创建文件：" + dto.path);

            if (dto.type == NasTypeEnums.Doc)
            {
                return await CreateDoc(token, dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await CreateDir(token, dto, result);
            }

            result.SetFailure("未知的文件类型：" + dto.type);
            return false;
        }

        /// <summary>
        /// 创建文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CreateDoc(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("创建文档：" + dto.path);

            if (string.IsNullOrEmpty(dto.src))
            {
                return false;
            }

            var tmpFile = _EnvConfig.GetTempPath(dto.src);
            if (!FileUtils.ExistsDoc(tmpFile))
            {
                result.SetFailure($"上传文档不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            if (!FileUtils.Moveto(tmpFile, dstFile))
            {
                SyncResult.Failure("上传文档移动异常！");
                return false;
            }

            var dirId = CreateDirDao(dto.path, token.user_id);

            AddLogFileByDto(token, dto);

            result.SetSuccess(dirId);
            return true;
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CreateDir(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("创建目录：" + dto.path);

            var tmpFile = GetPhysicalPath(dto.path);
            if (!Directory.Exists(tmpFile))
            {
                Directory.CreateDirectory(tmpFile);
            }

            var dirId = CreateDirDao(GetParentDir(dto.path), token.user_id);

            var docDao = AddCreateFile(token, dto, dirId);

            AddLogFileByDto(token, dto);

            result.SetSuccess(docDao.id);
            return true;
        }

        private NasResFileDao AddCreateFile(ScmTerminalInfo token, NasLogFileDto dto, long dirId)
        {
            var docDao = GetFileDaoByPath(dto.path, dto.type);
            if (docDao == null)
            {
                docDao = new NasResFileDao();
                docDao.type = dto.type;
                docDao.name = dto.name;
                docDao.path = dto.path;
                docDao.dir_id = dirId;
                docDao.PrepareCreate(token.user_id);

                _SqlClient.Insertable(docDao).ExecuteCommand();
            }
            return docDao;
        }
        #endregion

        #region 移动文件
        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> MoveFile(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("移动文件：" + dto.path);

            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await MoveDoc(token, dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await MoveDir(token, dto, result);
            }

            result.SetFailure("未知的文件类型：" + dto.type);
            return false;
        }

        /// <summary>
        /// 移动文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> MoveDoc(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("移动文档：" + dto.path);

            var srcFile = GetPhysicalPath(dto.src);
            if (!FileUtils.ExistsDoc(srcFile))
            {
                result.SetFailure($"来源文档 {dto.src} 不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.Moveto(srcFile, dstFile);

            var dirId = CreateDirDao(GetParentDir(dto.path), token.user_id);
            var docDao = AddCreateFile(token, dto, dirId);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            result.SetSuccess(docDao.id);
            return true;
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> MoveDir(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("移动目录：" + dto.path);

            var srcFile = GetPhysicalPath(dto.src);
            if (!FileUtils.ExistsDoc(srcFile))
            {
                result.SetFailure($"来源目录 {dto.src} 不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.Moveto(srcFile, dstFile);

            var dirId = CreateDirDao(dto.path, token.user_id);

            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            result.SetSuccess(dirId);
            return true;
        }
        #endregion

        #region 复件文件
        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CopyFile(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("复制文件：" + dto.path);

            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await CopyDoc(token, dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await CopyDir(token, dto, result);
            }

            result.SetFailure("未知的文件类型：" + dto.type);
            return false;
        }

        /// <summary>
        /// 移动文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CopyDoc(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("复制文档：" + dto.path);

            var srcFile = GetPhysicalPath(dto.src);
            if (!FileUtils.ExistsDoc(srcFile))
            {
                LogUtils.Debug($"来源文档 {dto.src} 不存在！");
                result.SetFailure($"来源文档 {dto.src} 不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.Copyto(srcFile, dstFile);

            var dirId = CreateDirDao(GetParentDir(dto.path), token.user_id);
            var docDao = AddCreateFile(token, dto, dirId);

            AddLogFileByDto(token, dto);

            result.SetSuccess(docDao.id);
            return true;
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CopyDir(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("复制目录：" + dto.path);

            var srcFile = GetPhysicalPath(dto.src);
            if (!FileUtils.ExistsDoc(srcFile))
            {
                LogUtils.Debug($"来源目录 {dto.src} 不存在！");
                result.SetFailure($"来源目录 {dto.src} 不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.Copyto(srcFile, dstFile);

            var dirId = CreateDirDao(dto.path, token.user_id);

            AddLogFileByDto(token, dto);

            result.SetSuccess(dirId);
            return true;
        }
        #endregion

        #region 更名文件
        /// <summary>
        /// 更名文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> RenameFile(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("更名文件：" + dto.path);

            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await RenameDir(token, dto, result);
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await RenameDoc(token, dto, result);
            }

            result.SetFailure("未知的文件类型：" + dto.type);
            return false;
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> RenameDir(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("更名目录：" + dto.path);

            var srcFile = GetPhysicalPath(dto.src);
            if (!FileUtils.ExistsDir(srcFile))
            {
                result.SetFailure($"来源目录 {dto.src} 不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.RenameTo(srcFile, dstFile);

            var dirDao = GetDirDaoByPath(dto.src);
            if (dirDao != null)
            {
                dirDao.name = dto.name;
                dirDao.path = dto.path;
                UpdateResFileDao(token, dirDao);

                RenameDirDao(token, dirDao);
            }
            else
            {
                dto.dir_id = GetParentIdByPath(dto.path);
                // 追加文档记录
                AddResFileByDto(token, dto);
            }

            AddLogFileByDto(token, dto);

            result.SetSuccess();
            return true;
        }

        private void RenameDirDao(ScmTerminalInfo token, NasResFileDao dao)
        {
            // 子级目录路径更新
            var dirListDao = ListDirDaoByParent(dao.id);
            foreach (var dir in dirListDao)
            {
                dir.path = dao.path + NasEnv.WebSeparator + dir.name;
                RenameDirDao(token, dir);
            }
            _SqlClient.Updateable(dirListDao).ExecuteCommand();

            // 子级文档路径更新
            var docListDao = ListDocDaoByParent(dao.id);
            foreach (var doc in docListDao)
            {
                doc.path = dao.path + NasEnv.WebSeparator + doc.name;
            }
            _SqlClient.Updateable(docListDao).ExecuteCommand();
        }

        /// <summary>
        /// 移动文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> RenameDoc(ScmTerminalInfo token, NasLogFileDto dto, SyncResult result)
        {
            LogUtils.Debug("更名文档：" + dto.path);

            var srcFile = GetPhysicalPath(dto.src);
            if (!FileUtils.ExistsDoc(srcFile))
            {
                result.SetFailure($"来源文档 {dto.src} 不存在！");
                return false;
            }

            var dstFile = GetPhysicalPath(dto.path);
            FileUtils.RenameTo(srcFile, dstFile);

            var docDao = GetDocDaoByPath(dto.src);
            if (docDao != null)
            {
                docDao.name = dto.name;
                docDao.path = dto.path;
                UpdateResFileDao(token, docDao);
            }
            else
            {
                dto.dir_id = GetParentIdByPath(dto.path);
                // 追加文档记录
                AddResFileByDto(token, dto);
            }

            AddLogFileByDto(token, dto);

            result.SetSuccess();
            return true;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 根据路径获取文件对象
        /// </summary>
        /// <param name="path">虚拟绝对路径</param>
        /// <returns></returns>
        private NasResFileDao GetFileDaoByPath(string path, NasTypeEnums type)
        {
            return _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.path == path && a.type == type)
                .First();
        }

        private NasResFileDao GetDirDaoByPath(string path)
        {
            return GetFileDaoByPath(path, NasTypeEnums.Dir);
        }

        private NasResFileDao GetDocDaoByPath(string path)
        {
            return GetFileDaoByPath(path, NasTypeEnums.Doc);
        }

        private List<NasResFileDao> ListResFileDaoByParent(long dirId)
        {
            return _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.dir_id == dirId)
                .ToList();
        }

        private List<NasResFileDao> ListDirDaoByParent(long dirId)
        {
            return _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.dir_id == dirId && a.type == NasTypeEnums.Dir)
                .ToList();
        }

        private List<NasResFileDao> ListDocDaoByParent(long dirId)
        {
            return _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.dir_id == dirId && a.type == NasTypeEnums.Doc)
                .ToList();
        }

        public long GetParentIdByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NasEnv.DEF_DIR_ID;
            }

            path = path.TrimEnd(NasEnv.WebSeparator);
            if (string.IsNullOrEmpty(path))
            {
                return NasEnv.DEF_DIR_ID;
            }

            var index = path.LastIndexOf(NasEnv.WebSeparator);
            if (index > 0)
            {
                path = path.Substring(0, index);
            }

            var dirDao = GetDirDaoByPath(path);
            return dirDao != null ? dirDao.id : NasEnv.DEF_DIR_ID;
        }

        /// <summary>
        /// 获取物理路径
        /// （服务端路径，不可与客户端方法相同）
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetPhysicalPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (path.StartsWith(NasEnv.VirtualTag, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(NasEnv.VirtualTag.Length);
            }

            return _EnvConfig.GetUploadPath(path);
        }

        /// <summary>
        /// 获取上级目录
        /// </summary>
        /// <param name="file">虚拟绝对路径</param>
        /// <returns></returns>
        private static string GetParentDir(string file)
        {
            file = file.TrimEnd(NasEnv.WebSeparator);
            var idx = file.LastIndexOf(NasEnv.WebSeparator);
            if (idx > 0)
            {
                return file.Substring(0, idx);
            }
            return "" + NasEnv.WebSeparator;
        }

        /// <summary>
        /// 根据路径级联创建目录
        /// </summary>
        /// <param name="path"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private long CreateDirDao(string path, long userId)
        {
            var tmp = "";
            var dirId = NasEnv.DEF_DIR_ID;
            path = path.Trim(NasEnv.WebSeparator);
            foreach (var arr in path.Split(NasEnv.WebSeparator))
            {
                if (string.IsNullOrEmpty(arr))
                {
                    continue;
                }

                tmp += NasEnv.WebSeparator + arr;
                var dao = _SqlClient.Queryable<NasResFileDao>().Where(a => a.path == tmp).First();
                if (dao == null)
                {
                    dao = new NasResFileDao
                    {
                        type = NasTypeEnums.Dir,
                        name = arr,
                        path = tmp,
                        dir_id = dirId, // 根目录
                    };
                    dao.PrepareCreate(userId);
                    _SqlClient.Insertable(dao).ExecuteCommand();
                }
                dirId = dao.id;
            }

            return dirId;
        }

        private void UpdateResFileDao(ScmTerminalInfo token, NasResFileDao dao)
        {
            dao.PrepareUpdate(token.user_id);
            _SqlClient.Updateable(dao).ExecuteCommand();
        }

        private void AddResFileByDto(ScmTerminalInfo token, NasLogFileDto dto)
        {
            var docDao = dto.Adapt<NasResFileDao>();
            docDao.PrepareCreate(token.user_id);
            _SqlClient.Insertable(docDao).ExecuteCommand();
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="token"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        private void AddLogFileByDto(ScmTerminalInfo token, NasLogFileDto dto)
        {
            var dao = dto.Adapt<NasLogFileDao>();
            dao.PrepareCreate(token.user_id);
            _SqlClient.Insertable(dao).ExecuteCommand();
        }

        /// <summary>
        /// 适用于应用，使用绑定登录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="holder"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private NasToken GetToken(string token)
        {
            if (token.StartsWith(ScmToken.PRE_APP))
            {
                token = token.Substring(ScmToken.PRE_APP.Length);
            }

            var bytes = Convert.FromBase64String(token);
            token = Encoding.UTF8.GetString(bytes);

            var arr = token.Split(":");
            var nasToken = new NasToken();
            if (arr.Length == 3)
            {
                var tmp = arr[0];
                if (TextUtils.IsLong(tmp))
                {
                    nasToken.terminal_id = long.Parse(tmp);
                }

                tmp = arr[1];
                if (TextUtils.IsLong(tmp))
                {
                    nasToken.time = long.Parse(tmp);
                }

                nasToken.digest = arr[2];
            }

            return nasToken;
        }

        private NasCfgDriveDao GetDriveDao(long driveId)
        {
            return _SqlClient.Queryable<NasCfgDriveDao>().First(a => a.id == driveId);
        }
        #endregion
    }

    public class NasToken
    {
        public long terminal_id { get; set; }
        public long time { get; set; }
        public string digest { get; set; }
    }
}
