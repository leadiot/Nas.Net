using Microsoft.AspNetCore.Mvc;

namespace Com.Scm.Nas.Sync.Dvo
{
    public class GetDirRequest : ScmSearchPageRequest
    {
        [FromHeader(Name = "TerminalId")]
        public long terminal_id { get; set; }

        [FromHeader(Name = "Token")]
        public string token { get; set; }

        public long dir_id { get; set; }
    }
}
