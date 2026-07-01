namespace Com.Scm.Nas
{
    /// <summary>
    /// 扫描处理状态
    /// </summary>
    public enum NasScanEnum
    {
        None = 0,
        /// <summary>
        /// 等处理
        /// </summary>
        Todo = 1,
        /// <summary>
        /// 新增
        /// </summary>
        Create = 2,
        /// <summary>
        /// 更新
        /// </summary>
        Change = 3,
        /// <summary>
        /// 删除
        /// </summary>
        Delete = 4,
        /// <summary>
        /// 上传 
        /// </summary>
        Upload = 5,
        /// <summary>
        /// 下载
        /// </summary>
        Download = 6,
    }
}
