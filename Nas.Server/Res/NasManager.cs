using Com.Scm.Nas.Cfg;
using Com.Scm.Nas.Log;
using SqlSugar;

namespace Com.Scm.Nas.Res
{
    public class NasManager
    {
        private ISqlSugarClient _SqlClient;

        public NasManager(ISqlSugarClient sqlClient)
        {
            _SqlClient = sqlClient;
        }

        public List<NasCfgFolderDao> ListFolderDao(long userId)
        {
            return _SqlClient.Queryable<NasCfgFolderDao>()
                .Where(a => a.user_id == userId && a.row_status == Enums.ScmRowStatusEnum.Enabled)
                .ToList();
        }

        public List<NasResFileDao> ListParentDao(NasResFileDao dao)
        {
            var list = new List<NasResFileDao>();
            while (dao.dir_id != NasEnv.DEF_DIR_ID)
            {
                dao = GetDaoById(dao.dir_id);
                list.Add(dao);
            }
            return list;
        }

        private NasResFileDao GetDaoById(long id)
        {
            return _SqlClient.Queryable<NasResFileDao>()
                .Where(a => a.id == id)
                .First();
        }

        public NasLogFileDao AddLogFileDao(NasResFileDao resDao, long terminalId, long folderId, NasOptEnums opt, string src = null)
        {
            var logDao = new NasLogFileDao();
            logDao.terminal_id = terminalId;
            logDao.folder_id = folderId;
            logDao.res_id = resDao.id;
            logDao.dir_id = resDao.dir_id;
            logDao.type = resDao.type;
            logDao.name = resDao.name;
            logDao.path = resDao.path;
            logDao.hash = resDao.hash;
            logDao.size = resDao.size;
            logDao.opt = opt;
            logDao.dir = NasDirEnums.Download;
            logDao.ver = resDao.ver;
            logDao.src = src;
            logDao.modify_time = resDao.modify_time;
            _SqlClient.Insertable(logDao).ExecuteCommand();

            return logDao;
        }

        public NasLogFolderDao AddLogFolderDao(NasLogFileDao logDao, long terminalId, long folderId)
        {
            var folderDao = new NasLogFolderDao();
            folderDao.user_id = logDao.user_id;
            folderDao.terminal_id = terminalId;
            folderDao.folder_id = folderId;
            folderDao.log_id = logDao.id;
            _SqlClient.Insertable(folderDao).ExecuteCommand();

            return folderDao;
        }
    }
}
