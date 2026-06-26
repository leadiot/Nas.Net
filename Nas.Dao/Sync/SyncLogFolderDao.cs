using Com.Scm.Dao;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace Com.Scm.Nas.Sync
{
    [SugarTable("nas_log_folder")]
    public class SyncLogFolderDao : ScmDataDao
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public long user_id { get; set; }

        /// <summary>
        /// 终端ID
        /// </summary>
        public long terminal_id { get; set; }

        /// <summary>
        /// 目录ID
        /// </summary>
        [Required]
        public long folder_id { get; set; }

        /// <summary>
        /// 日志ID
        /// </summary>
        [Required]
        public long log_id { get; set; }
    }
}
