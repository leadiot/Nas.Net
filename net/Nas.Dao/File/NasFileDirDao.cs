using Com.Scm.Dao.User;
using Com.Scm.Enums;
using Com.Scm.Utils;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 目录
    /// </summary>
    [SugarTable("nas_file_dir")]
    public class NasFileDirDao : ScmUserDataDao
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
        /// 名称
        /// </summary>
        [Required]
        [StringLength(256)]
        public string name { get; set; }

        /// <summary>
        /// 路径
        /// </summary>
        [StringLength(2048)]
        public string path { get; set; }

        /// <summary>
        /// 目录ID
        /// </summary>
        public long dir_id { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int qty { get; set; }

        public ScmBoolEnum p_delete { get; set; }
        public ScmBoolEnum s_delete { get; set; }
        public ScmBoolEnum is_delete { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        [Required]
        public long ver { get; set; }

        public override void PrepareCreate(long userId)
        {
            base.PrepareCreate(userId);

            p_delete = ScmBoolEnum.False;
            s_delete = ScmBoolEnum.False;
            is_delete = ScmBoolEnum.False;

            ver = 1;
            row_status = ScmRowStatusEnum.Enabled;

            update_time = TimeUtils.GetUnixTime();
            create_time = update_time;
        }

        public override void PrepareUpdate(long userId)
        {
            base.PrepareUpdate(userId);

            ver += 1;
            update_time = TimeUtils.GetUnixTime();
        }
    }
}