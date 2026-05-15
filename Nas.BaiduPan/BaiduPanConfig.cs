namespace Nas.BaiduPan
{
    public class BaiduPanConfig
    {
        public string AppId { get; set; } = string.Empty;
        
        public string AppKey { get; set; } = string.Empty;
        
        public string AppSecret { get; set; } = string.Empty;
        
        public string RedirectUri { get; set; } = "oob";
        
        public string AccessToken { get; set; } = string.Empty;
        
        public string RefreshToken { get; set; } = string.Empty;
        
        public long ExpireTime { get; set; }
        
        public string Scope { get; set; } = "basic,netdisk";
        
        public string TokenUrl => "https://openapi.baidu.com/oauth/2.0/token";
        
        public string AuthorizeUrl => "https://openapi.baidu.com/oauth/2.0/authorize";
        
        public string ApiBaseUrl => "https://pan.baidu.com/rest/2.0";
    }
}