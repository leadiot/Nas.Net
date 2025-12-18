using Com.Scm.Dao.User;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace Com.Scm.Nas.Log
{
    /// <summary>
    /// 
    /// </summary>
    [SugarTable("nas_log_file")]
    public class NasLogSyncDao : ScmUserDao
    {
        /// <summary>
        /// 操作
        /// </summary>
        [Required]
        public NasOptEnums opt { get; set; }

        /// <summary>
        /// 方向
        /// </summary>
        [Required]
        public NasDirEnums dir { get; set; }

        /// <summary>
        /// 文件
        /// </summary>
        [Required]
        [StringLength(1024)]
        public string file { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        [Required]
        public long time { get; set; }

        /// <summary>
        /// 终端ID
        /// </summary>
        [Required]
        public long terminal_id { get; set; }
    }
}