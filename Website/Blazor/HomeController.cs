using Microsoft.AspNetCore.Mvc;
using Website.Util;

namespace Website.Blazor
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet("/hi")]
        public IActionResult Index()
        {
            var valueTuple = PasswordHelper.GetHash("Test123");
            return Content($"{valueTuple}");
        }

        [HttpGet("/hi/{pass}/{hash}")]
        public IActionResult Test(string pass, string hash)
        {
            return Content($"{PasswordHelper.AreEqual(pass, hash)}");
        }
    }
}
