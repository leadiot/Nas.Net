namespace Com.Scm.Nas
{
    /// <summary>
    /// 扫描处理状态
    /// </summary>
    public enum NasScanEnum
    {
        None,
        /// <summary>
        /// 等处理
        /// </summary>
        Todo,
        /// <summary>
        /// 删除
        /// </summary>
        Delete,
        /// <summary>
        /// 正常
        /// </summary>
        Normal,
        /// <summary>
        /// 新增
        /// </summary>
        Create,
        /// <summary>
        /// 更新
        /// </summary>
        Change,
        /// <summary>
        /// 上传 
        /// </summary>
        Upload,
        /// <summary>
        /// 下载
        /// </summary>
        Download,
    }
}
