using Com.Scm.Dvo;

namespace Com.Scm.Nas.Res.Dir.Dvo
{
    /// <summary>
    /// 目录
    /// </summary>
    public class NasFileDirDvo : ScmDataDvo
    {
        /// <summary>
        /// 终端ID
        /// </summary>
        public long terminal_id { get; set; }

        /// <summary>
        /// 驱动ID
        /// </summary>
        public long drive_id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 路径
        /// </summary>
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
        public long ver { get; set; }
    }
}