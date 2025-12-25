using Com.Scm.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.Scm.Api.Controllers
{
    [ApiExplorerSettings(GroupName = "Scm")]
    public class TestController : ApiController
    {
        public TestController()
        {
        }


        [HttpPost("Demo"), AllowAnonymous]
        public object Demo(ScmUploadRequest request)
        {
            //var id = UidUtils.NextId();
            //var code = UidUtils.NextCodes("samples_demo");
            //return new { id, code };
            return "Ok";
        }
    }
}
