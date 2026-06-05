namespace Nas.Alipan
{
    /// <summary>
    /// 阿里云盘开放平台配置
    /// </summary>
    public class AlipanConfig
    {
        /// <summary>
        /// 应用客户端ID（AppID），从阿里云盘开放平台获取
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// 应用客户端密钥（AppSecret），从阿里云盘开放平台获取
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// OAuth2 回调地址
        /// </summary>
        public string RedirectUri { get; set; } = "https://www.alipan.com/web/callback";

        /// <summary>
        /// OAuth2 授权范围
        /// </summary>
        public string Scope { get; set; } = "base:file:read base:file:write";

        /// <summary>
        /// 访问令牌
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// 刷新令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 令牌过期时间（Unix时间戳，秒）
        /// </summary>
        public long ExpireTime { get; set; }

        /// <summary>
        /// 默认空间ID（登录后获取）
        /// </summary>
        public string DefaultDriveId { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// OAuth2 授权URL
        /// </summary>
        public string AuthorizeUrl => "https://openapi.alipan.com/oauth/authorize";

        /// <summary>
        /// OAuth2 令牌URL
        /// </summary>
        public string TokenUrl => "https://openapi.alipan.com/oauth/token";

        /// <summary>
        /// API基础URL
        /// </summary>
        public string ApiBaseUrl => "https://openapi.alipan.com";
    }
}