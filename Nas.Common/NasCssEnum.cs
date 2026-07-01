using System.ComponentModel;

namespace Com.Scm.Nas
{
    /// <summary>
    /// 冲突策略
    /// </summary>
    public enum NasCssEnum
    {
        None = 0,

        /// <summary>
        /// 以最后更新为准
        /// </summary>
        [Description("时间优先")]
        ChangeFirst = 1,

        /// <summary>
        /// 以本地文件为准
        /// </summary>
        [Description("保留本地")]
        NativeFirst = 2,

        /// <summary>
        /// 以远端文件为准
        /// </summary>
        [Description("保留远端")]
        RemoteFirst = 3,

        /// <summary>
        /// 保留所有，重命名旧文件为冲突文件
        /// </summary>
        [Description("保留所有")]
        KeepBoth = 4
    }
}
