using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Website.Data;
using Website.Util;

namespace Website.Controllers
{
    //#if DEBUG
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public DebugController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        // Reload config
        [HttpPost("/debug/reloadConfig")]
        public IActionResult ReloadConfig()
        {
            Config.Reload();
            return Redirect("/Overview");
        }

        [HttpPost("/debug/setc")]
        public IActionResult SetConfig([FromForm] string n, [FromForm] string v)
        {
            Response.SetData(n, v);
            return Redirect("/");
        }

        // Test login with a specific username
        [HttpPost("/test/login/{name}")]
        public IActionResult Login(string name)
        {
            var user = _dbContext.Users.FirstOrDefault(s => s.Username == name);
            if (user == null) return BadRequest("User not exists!");

            var token = GenerateJwtToken(name);
            return Ok(new { token });
        }

        // Test login for admin
        [HttpPost("/test/admin")]
        public IActionResult LoginAsAdmin()
        {
            var token = GenerateJwtToken("DimucaTheDev", "admin");
            return Ok(new { token });
        }

        // Auth test route - requires valid token
        [HttpGet("/test/auth")]
        [HttpPost("/test/auth")]
        [Util.Auth]
        public IActionResult AuthTest()
        {
            return Ok("Only TRUE sigmas can read this!🗣️🔥🔥💯");
        }

        // Test JWT - shows expiration date of token
        [HttpGet("/test/jwt")]
        [HttpPost("/test/jwt")]
        public IActionResult Jwt()
        {
            if (Request.TryAuthenticate(model => model.Username == User.FindFirst(ClaimTypes.Name)?.Value, out var user))
            {
                var expiry = DateTime.UnixEpoch.AddSeconds(int.Parse(User.Claims.First(s => s.Type == "exp").Value)).ToLocalTime();
                return Ok($"Hi, {user.Username}! Your token expires on {expiry:F}");
            }

            return Unauthorized("You are not authorized!");
        }

        // JWT scope test - requires admin role
        [HttpGet("/test/jwtscope")]
        [HttpPost("/test/jwtscope")]
        [Util.Auth(Roles = "admin")]
        public IActionResult JwtScope()
        {
            Request.TryAuthenticate(model => model.Username == User.FindFirst(ClaimTypes.Name)!.Value, out var user);
            var expiry = DateTime.UnixEpoch.AddSeconds(int.Parse(User.Claims.First(s => s.Type == "exp").Value)).ToLocalTime();
            return Ok($"Hi admin aka {user.Username}! Your token expires on {expiry:F}");
        }

        // Set cookie for testing
        [HttpPost("/test/setcookie")]
        public IActionResult SetCookie()
        {
            var r = Response.SetData("text", "Hello, World!");
            return Content(r);
        }

        // Get cookie value for testing
        [HttpPost("/test/getcookie")]
        public IActionResult GetCookie()
        {
            if (Request.TryGetData<string>("text", out var r))
            {
                return Content(r);
            }
            return BadRequest("Cookie not found.");
        }

        // Logout - remove authentication cookie
        [HttpPost("/test/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("auth_do_not_share");
            return Redirect("/");
        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(string username, string role = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, role));
            }

            var jwt = new JwtSecurityToken(
                claims: claims,
                issuer: Program.Issuer,
                audience: Program.Audience,
                expires: DateTime.Now.AddSeconds(20),
                signingCredentials: new SigningCredentials(Program.Key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
    //#endif
}
