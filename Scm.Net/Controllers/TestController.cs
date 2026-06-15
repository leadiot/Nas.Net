using Com.Scm.Request;
using Com.Scm.Response;
using Com.Scm.Token;
using Microsoft.AspNetCore.Mvc;

namespace Com.Scm.Controllers
{
    [ApiExplorerSettings(GroupName = "Scm")]
    public class TestController : ApiController
    {
        private IJwtTokenHolder _JwtHolder;

        public TestController(IJwtTokenHolder scmHolder)
        {
            _JwtHolder = scmHolder;
        }

        [HttpPost("Echo")]
        public ScmApiResponse PostEcho(ScmRequest request)
        {
            var token = _JwtHolder.GetToken();
            var response = new ScmApiDataResponse<long>();
            response.Data = token.terminal_id;
            response.SetSuccess();

            return response;
        }

        [HttpPost("Mime")]
        public ScmApiResponse MimeAsync()
        {
            var token = _JwtHolder.GetToken();
            var response = new ScmApiDataResponse<long>();
            response.Data = token.terminal_id;
            response.SetSuccess();

            return response;
        }
    }
}
