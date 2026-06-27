using Com.Scm.Dsa;
using Com.Scm.Dto;
using Com.Scm.Dvo;
using Com.Scm.Enums;
using Com.Scm.Exceptions;
using Com.Scm.Mqtt;
using Com.Scm.Nas.Cfg;
using Com.Scm.Nas.Log;
using Com.Scm.Nas.Res.Dvo;
using Com.Scm.Service;
using Com.Scm.Token;
using Com.Scm.Utils;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 文档服务接口
    /// </summary>
    [ApiExplorerSettings(GroupName = "Nas")]
    public class NasResFileService : ApiService
    {
        protected readonly SugarRepository<NasResFileDao> _thisRepository;
        protected readonly IJwtTokenHolder _jwtHolder;
        protected readonly IMqttPublisher _Publisher;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thisRepository"></param>
        public NasResFileService(SugarRepository<NasResFileDao> thisRepository,
            ISqlSugarClient sqlClient,
            IJwtTokenHolder jwtHolder,
            IResHolder resHolder,
            IMqttPublisher publisher)
        {
            _thisRepository = thisRepository;
            _SqlClient = sqlClient;
            _jwtHolder = jwtHolder;
            _ResHolder = resHolder;
            _Publisher = publisher;
        }

        /// <summary>
        /// 查询分页
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ScmPageResultDto<NasResFileDvo>> GetPagesAsync(SearchRequest request)
        {
            if (!IsNormalId(request.dir_id))
            {
                request.dir_id = GetRootDirId();
            }

            var result = await _thisRepository.AsQueryable()
                .Where(a => a.dir_id == request.dir_id)
                .WhereIF(!request.IsAllStatus(), a => a.row_status == request.row_status)
                //.WhereIF(IsValidId(request.option_id), a => a.option_id == request.option_id)
                .WhereIF(!string.IsNullOrEmpty(request.key), a => a.name.Contains(request.key))
                .OrderBy(a => a.type, OrderByType.Asc)
                .OrderBy(a => a.name, OrderByType.Asc)
                .Select<NasResFileDvo>()
                .ToPageAsyncV2(request.page, request.limit);

            Prepare(result.Items);
            return result;
        }

        protected virtual long GetRootDirId()
        {
            return NasEnv.DEF_DIR_ID;
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<List<NasResFileDvo>> GetListAsync(SearchRequest request)
        {
            if (!IsNormalId(request.dir_id))
            {
                request.dir_id = GetRootDirId();
            }

            var result = await _thisRepository.AsQueryable()
                .Where(a => a.row_status == ScmRowStatusEnum.Enabled)
                .WhereIF(request.opt == Dvo.SearchOption.ByDir, a => a.dir_id == request.dir_id)
                .WhereIF(request.opt == Dvo.SearchOption.ByKind, a => a.kind == request.kind)
                //.WhereIF(IsNormalId(request.folder_id), a => a.folder_id == request.folder_id)
                .WhereIF(!string.IsNullOrEmpty(request.key), a => a.name.Contains(request.key))
                .OrderBy(a => a.type, OrderByType.Asc)
                .OrderBy(a => a.name, OrderByType.Asc)
                .Select<NasResFileDvo>()
                .ToListAsync();

            Prepare(result);
            return result;
        }

        /// <summary>
        /// 根据主键查询
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<NasResFileDto> GetAsync(long id)
        {
            return await _thisRepository
                .AsQueryable()
                .Where(a => a.id == id)
                .Select<NasResFileDto>()
                .FirstAsync();
        }

        /// <summary>
        /// 查看读取
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<NasResFileDvo> GetViewAsync(long id)
        {
            return await _thisRepository
                .AsQueryable()
                .Where(a => a.id == id)
                .Select<NasResFileDvo>()
                .FirstAsync();
        }

        /// <summary>
        /// 下拉列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<List<ResOptionDvo>> GetOptionAsync(ScmSearchRequest request)
        {
            var result = await _thisRepository.AsQueryable()
                .Where(a => a.row_status == ScmRowStatusEnum.Enabled)
                .OrderBy(a => a.id)
                .Select(a => new ResOptionDvo { id = a.id, label = a.name, value = a.id })
                .ToListAsync();

            return result;
        }

        /// <summary>
        /// 编辑读取
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<NasResFileDto> GetEditAsync(long id)
        {
            return await _thisRepository
                .AsQueryable()
                .Where(a => a.id == id)
                .Select<NasResFileDto>()
                .FirstAsync();
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> AddAsync(NasResFileDto model)
        {
            var dao = await _thisRepository.GetFirstAsync(a => a.name == model.name && a.dir_id == model.dir_id);
            if (dao != null)
            {
                throw new BusinessException("已存在相同名称的文档！");
            }

            if (!IsNormalId(model.dir_id))
            {
                model.dir_id = GetRootDirId();
            }

            var parentDao = await _thisRepository.GetByIdAsync(model.dir_id);
            if (parentDao == null || parentDao.type != ScmFileTypeEnum.Dir)
            {
                throw new BusinessException("上级目录不存在！");
            }

            var parentPath = parentDao.path;

            dao = model.Adapt<NasResFileDao>();
            dao.type = ScmFileTypeEnum.Dir;
            dao.modify_time = TimeUtils.GetUnixTime(true);
            dao.path = NasUtils.CombinePath(parentPath, model.name);

            var result = await _thisRepository.InsertAsync(dao);

            await AddCreateLog(dao, parentDao.user_id);

            return result;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(NasResFileDto model)
        {
            var dao = await _thisRepository.GetFirstAsync(a => a.name == model.name && a.dir_id == model.dir_id && a.id != model.id);
            if (dao != null)
            {
                throw new BusinessException("已存在相同名称的文档！");
            }

            dao = await _thisRepository.GetByIdAsync(model.id);
            if (dao == null)
            {
                throw new BusinessException("无效的文档！");
            }

            var parentDao = await _thisRepository.GetByIdAsync(model.dir_id);
            if (parentDao == null || parentDao.type != ScmFileTypeEnum.Dir)
            {
                throw new BusinessException("上级目录不存在！");
            }

            var src = dao.path;
            var parentPath = NasUtils.GetParentPath(src);
            dao = model.Adapt(dao);
            dao.modify_time = TimeUtils.GetUnixTime(true);
            dao.path = NasUtils.CombinePath(parentPath, model.name);
            var result = await _thisRepository.UpdateAsync(dao);

            await AddRenameLog(dao, parentDao.user_id, src);

            return result;
        }

        /// <summary>
        /// 批量更新状态
        /// </summary>
        /// <param name="param">逗号分隔</param>
        /// <returns></returns>
        public async Task<int> StatusAsync(ScmChangeStatusRequest param)
        {
            return await UpdateStatus(_thisRepository, param.ids, param.status);
        }

        /// <summary>
        /// 批量删除记录
        /// </summary>
        /// <param name="ids">逗号分隔</param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<int> DeleteAsync(string ids)
        {
            var token = _jwtHolder.GetToken();

            var idList = ids.ToListLong();
            var daoList = await _thisRepository.GetListAsync(a => idList.Contains(a.id));
            await AddDeleteLog(daoList, token.user_id);

            return await DeleteRecord(_thisRepository, ids.ToListLong());
        }

        #region 文件操作
        /// <summary>
        /// 创建事件
        /// </summary>
        /// <param name="dao"></param>
        public async Task AddCreateLog(NasResFileDao dao, long userId)
        {
            var manager = new NasManager(_SqlClient);
            var logDao = manager.AddLogFileDao(dao, ScmEnv.DEFAULT_ID, ScmEnv.DEFAULT_ID, NasOptEnums.Create);
            var folderList = manager.ListFolderDao(userId);
            await AddFolderLog(manager, folderList, logDao, dao);
        }

        /// <summary>
        /// 更名事件
        /// </summary>
        /// <param name="dao"></param>
        /// <param name="userId"></param>
        /// <param name="src"></param>
        public async Task AddRenameLog(NasResFileDao dao, long userId, string src)
        {
            var manager = new NasManager(_SqlClient);
            var logDao = manager.AddLogFileDao(dao, ScmEnv.DEFAULT_ID, ScmEnv.DEFAULT_ID, NasOptEnums.Rename, src);
            var folderList = manager.ListFolderDao(userId);
            await AddFolderLog(manager, folderList, logDao, dao);
        }

        /// <summary>
        /// 删除事件
        /// </summary>
        /// <param name="dao"></param>
        /// <param name="userId"></param>
        public async Task AddDeleteLog(NasResFileDao dao, long userId)
        {
            var manager = new NasManager(_SqlClient);
            var logDao = manager.AddLogFileDao(dao, ScmEnv.DEFAULT_ID, ScmEnv.DEFAULT_ID, NasOptEnums.Delete);
            var folderList = manager.ListFolderDao(userId);
            await AddFolderLog(manager, folderList, logDao, dao);
        }

        /// <summary>
        /// 删除事件
        /// </summary>
        /// <param name="daoList"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task AddDeleteLog(List<NasResFileDao> daoList, long userId)
        {
            var manager = new NasManager(_SqlClient);
            var folderList = manager.ListFolderDao(userId);
            foreach (var dao in daoList)
            {
                var logDao = manager.AddLogFileDao(dao, ScmEnv.DEFAULT_ID, ScmEnv.DEFAULT_ID, NasOptEnums.Delete);
                await AddFolderLog(manager, folderList, logDao, dao);
            }
        }

        /// <summary>
        /// 以文件夹为单位，添加日志
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="folderList"></param>
        /// <param name="logDao"></param>
        /// <param name="resDao"></param>
        /// <returns></returns>
        private async Task AddFolderLog(NasManager manager, List<NasCfgFolderDao> folderList, NasLogFileDao logDao, NasResFileDao resDao)
        {
            var parentList = manager.ListParentDao(resDao);

            foreach (var folderDao in folderList)
            {
                // 匹配不到父级目录
                var parent = parentList.Find(a => a.id == folderDao.res_id);
                if (parent == null)
                {
                    continue;
                }

                var logFolderDao = manager.AddLogFolderDao(logDao, folderDao.terminal_id, folderDao.id);
                await PulishToMqttAsync(logFolderDao, logDao);
            }
        }

        /// <summary>
        /// 发布到Mqtt
        /// </summary>
        /// <param name="token"></param>
        /// <param name="folderDao"></param>
        /// <param name="fileDao"></param>
        /// <returns></returns>
        private async Task PulishToMqttAsync(NasLogFolderDao folderDao, NasLogFileDao fileDao)
        {
            var dto = new NasLogFileDto
            {
                id = folderDao.id,
                terminal_id = fileDao.terminal_id,
                folder_id = fileDao.folder_id,
                res_id = fileDao.res_id,
                dir_id = fileDao.dir_id,
                type = fileDao.type,
                name = fileDao.name,
                path = fileDao.path,
                hash = fileDao.hash,
                size = fileDao.size,
                modify_time = fileDao.modify_time,
                opt = fileDao.opt,
                dir = fileDao.dir,
                src = fileDao.src
            };
            var json = dto.ToJsonString();
            var topic = $"nas/{folderDao.terminal_id}/folder";

            if (!_Publisher.IsConnected)
            {
                await _Publisher.ConnectAsync();
            }
            await _Publisher.PublishAsync(topic, json, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
        }

        #endregion
    }
}