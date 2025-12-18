using Com.Scm.Dto;
using System.ComponentModel.DataAnnotations;

namespace Com.Scm.Nas.Res
{
    /// <summary>
    /// 目录
    /// </summary>
    public class NasFileDirDto : ScmDataDto
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

        /// <summary>
        /// 版本
        /// </summary>
        [Required]
        public long ver { get; set; }
    }
}