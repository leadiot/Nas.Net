using Com.Scm.Config;
using Com.Scm.Nas.Log;
using Com.Scm.Nas.Res;
using Com.Scm.Nas.Sync.Dvo;
using Com.Scm.Utils;
using SqlSugar;

namespace Com.Scm.Nas.Sync
{
    public class NasSyncService
    {
        private EnvConfig _EnvConfig;
        private ISqlSugarClient _SqlClient;

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<bool> CreateFile(NasLogFileDto dto, PostLogResult result)
        {
            if (dto == null)
            {
                return false;
            }

            if (dto.type == NasTypeEnums.Doc)
            {
                return await CreateDoc(dto, result);
            }

            if (dto.type == NasTypeEnums.Dir)
            {
                return await CreateDir(dto, result);
            }

            return false;
        }

        /// <summary>
        /// 创建文档
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> CreateDoc(NasLogFileDto dto, PostLogResult result)
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

            await CreateDocDao(dto);

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
        private async Task<bool> CreateDir(NasLogFileDto dto, PostLogResult result)
        {
            var tmpFile = _EnvConfig.GetUploadPath(dto.file);
            if (!Directory.Exists(tmpFile))
            {
                Directory.CreateDirectory(tmpFile);
            }

            await CreateDirDao(dto);
            var dao = dto.Adapt<NasLogFileDao>();
            await _SqlClient.Insertable(dao).ExecuteCommandAsync();

            return true;
        }

        private async Task CreateDocDao(NasLogFileDto dto)
        {
            var docDao = new NasFileDocDao();
            docDao.terminal_id = 0;
            docDao.drive_id = dto.drive_id;
            docDao.dir_id = dto.dir_id;
            docDao.name = dto.name;
            docDao.path = dto.file;
            docDao.hash = dto.hash;
            docDao.size = dto.size;
            docDao.PrepareCreate(0);

            await _SqlClient.Insertable(docDao).ExecuteCommandAsync();
        }

        private async Task CreateDirDao(NasLogFileDto dto)
        {
            var dirDao = new NasFileDirDao();
            dirDao.terminal_id = 0;
            dirDao.drive_id = dto.drive_id;
            dirDao.dir_id = dto.dir_id;
            dirDao.name = dto.name;
            dirDao.path = dto.file;
            dirDao.PrepareCreate(0);

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
    }
}
