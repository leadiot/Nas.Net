using Com.Scm.Dao.User;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 驱动
    /// </summary>
    [SugarTable("nas_res_drive")]
    public class NasResDriveDao : ScmUserDataDao
    {
        /// <summary>
        /// 终端ID
        /// </summary>
        [Required]
        public long terminal_id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string name { get; set; }

        /// <summary>
        /// 路径
        /// </summary>
        [StringLength(256)]
        public string path { get; set; }
    }
}