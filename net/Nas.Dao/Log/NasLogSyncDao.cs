using Com.Scm.Dao.User;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace Com.Scm.Nas.Log
{
    /// <summary>
    /// 同步日志
    /// </summary>
    [SugarTable("nas_log_sync")]
    public class NasLogSyncDao : ScmUserDataDao
    {
        /// <summary>
        /// 终端ID
        /// </summary>
        [Required]
        public long terminal_id { get; set; }

        /// <summary>
        /// 驱动ID
        /// </summary>
        [Required]
        public long drive_id { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        [Required]
        public NasTypeEnums type { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [Required]
        public NasOptEnums opt { get; set; }

        /// <summary>
        /// 同步方向
        /// </summary>
        [Required]
        public NasDirEnums dir { get; set; }

        /// <summary>
        /// 同步文件
        /// </summary>
        [Required]
        [StringLength(2048)]
        public string file { get; set; }

        /// <summary>
        /// 文件摘要
        /// </summary>
        [StringLength(64)]
        public string hash { get; set; }

        /// <summary>
        /// 来源文件
        /// </summary>
        [StringLength(2048)]
        public string src { get; set; }
    }
}