using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using Website.Data;
using Website.Model;
using Website.Util;

namespace Website.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(DatabaseContext dbContext) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm(Name = "remember")] string? rememberFlag)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null || !PasswordHelper.AreEqual(password, user.PasswordHash))
            {
                //TODO: cooldown/too much attempts
                return BadRequest(new { error = true, code = 400, description = "Некорректные данные для входа!" });
            }

            var remember = rememberFlag is "on" or "true";
            var expirationTime = DateTime.UtcNow.Add(remember ? TimeSpan.FromDays(15) : TimeSpan.FromHours(3));
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var jwt = new JwtSecurityToken(
                claims: claims,
                issuer: Program.Issuer,
                audience: Program.Audience,
                expires: expirationTime,
                signingCredentials: new SigningCredentials(Program.Key, SecurityAlgorithms.HmacSha256)
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            Response.Cookies.Append("auth_do_not_share", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = expirationTime
            });

            return Ok(new
            {
                ok = true,
                token,
                validUntil = expirationTime.ToString("O")
            });
        }
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromForm] string username, [FromForm] string password, [FromForm] string repeat_password, [FromForm] string? shown_name, [FromForm] string email)
        {
            if (password != repeat_password)
            {
                return BadRequest(new { error = true, code = 400, description = "Пароли не совпадают." });
            }
            if (!IsEmailValid(email))
            {
                return BadRequest(new { error = true, code = 400, description = "Некорректный формат почты." });
            }
            if (await dbContext.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            {
                return BadRequest(new { error = true, code = 400, description = "Пользователь с таким именем уже существует." });
            }
            if (await dbContext.Users.AnyAsync(u => (u.Email ?? "").ToLower() == email.ToLower()))
            {
                return BadRequest(new { error = true, code = 400, description = "Указанная почта уже используется." });
            }
            var hash = PasswordHelper.GetHash(password);
            var user = new UserModel
            {
                Username = username,
                PasswordHash = hash,
                Email = email,
                ShownName = shown_name ?? username,
                Uuid = Guid.NewGuid(),
                RankId = 0
            };

            var expirationTime = DateTime.UtcNow.Add(TimeSpan.FromDays(15));
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email)
            };
            var jwt = new JwtSecurityToken(
                claims: claims,
                issuer: Program.Issuer,
                audience: Program.Audience,
                expires: expirationTime,
                signingCredentials: new SigningCredentials(Program.Key, SecurityAlgorithms.HmacSha256)
            );
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            Response.Cookies.Append("auth_do_not_share", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = expirationTime
            });

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"User {user.Username} is created.");

            return Ok(new { ok = true });
        }
        private bool IsEmailValid(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
