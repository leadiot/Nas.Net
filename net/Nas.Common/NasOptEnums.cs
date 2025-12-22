namespace Com.Scm.Nas
{
    /// <summary>
    /// 文件类型
    /// </summary>
    public enum NasTypeEnums
    {
        None,
        /// <summary>
        /// 目录
        /// </summary>
        Dir,
        /// <summary>
        /// 文档
        /// </summary>
        Doc
    }

    /// <summary>
    /// 同步方向
    /// </summary>
    public enum NasDirEnums
    {
        None,
        /// <summary>
        /// 全量上传
        /// </summary>
        AllUpload,
        /// <summary>
        /// 增量上传
        /// </summary>
        IncUpload,
        /// <summary>
        /// 双向同步
        /// </summary>
        Sync,
        /// <summary>
        /// 全量下载
        /// </summary>
        AllDownload,
        /// <summary>
        /// 增量下载
        /// </summary>
        IncDownload
    }

    /// <summary>
    /// 操作类型
    /// </summary>
    public enum NasOptEnums
    {
        None,
        /// <summary>
        /// 删除
        /// </summary>
        Delete,
        /// <summary>
        /// 创建
        /// </summary>
        Create,
        /// <summary>
        /// 更名
        /// </summary>
        Rename,
        /// <summary>
        /// 修改
        /// </summary>
        Change,
        /// <summary>
        /// 移动
        /// </summary>
        Move,
        /// <summary>
        /// 复制
        /// </summary>
        Copy,
        /// <summary>
        /// 压缩
        /// </summary>
        Compress,
        /// <summary>
        /// 解压
        /// </summary>
        Decompress,
        /// <summary>
        /// 分享
        /// </summary>
        Share,
        /// <summary>
        /// 解除分享
        /// </summary>
        Unshare,
        /// <summary>
        /// 隐私
        /// </summary>
        Lock,
        /// <summary>
        /// 解除隐私
        /// </summary>
        Unlock,
        /// <summary>
        /// 恢复
        /// </summary>
        Restore,
    }

    /// <summary>
    /// 监控状态
    /// </summary>
    public enum NasWatchEnums
    {
        None,
        /// <summary>
        /// 进行中
        /// </summary>
        Running,
        /// <summary>
        /// 暂停中
        /// </summary>
        Suspend,
        /// <summary>
        /// 已中止
        /// </summary>
        Stoped
    }
}
