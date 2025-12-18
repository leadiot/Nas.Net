using Com.Scm.Dvo;

namespace Com.Scm.Nas.Log
{
    /// <summary>
    /// 
    /// </summary>
    public class NasLogSyncDvo : ScmDataDvo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public long user_id { get; set; }

        /// <summary>
        /// 操作
        /// </summary>
        public NasOptEnums opt { get; set; }

        /// <summary>
        /// 方向
        /// </summary>
        public NasDirEnums dir { get; set; }

        /// <summary>
        /// 文件
        /// </summary>
        public string file { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public long time { get; set; }

        /// <summary>
        /// 终端ID
        /// </summary>
        public long terminal_id { get; set; }
    }
}