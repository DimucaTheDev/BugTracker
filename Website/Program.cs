using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Website.Data;
using Website.Util;

namespace Website
{
    public class Program
    {
        //TODO: CHANGE ON RELEASE!!!
        public const string Issuer = "http://localhost:5230/test/jwt";
        public const string Audience = "AuthClient";
        //This key is used to (de)encrypt cookie data and jwt token
        public static SymmetricSecurityKey Key = new(File.ReadAllBytes("private.key"));
        public static WebApplication Application;
        private static void GeneratePrivateKey() => File.WriteAllText("private.key",
            Enumerable.Range(0, 2048).Select(s => ((char)Random.Shared.Next(0x21/* ! */, 0x7e/* ~ */)).ToString())
                .Aggregate((a, b) => a + b));

        public static bool IsDev;
        /// <summary>
        /// Used to prevent HTTP caching :)
        /// resets every server startup
        /// </summary>
        //TODO: maybe 0 on release? 
        public static int RandomInteger = Random.Shared.Next();

        public static async Task Main(string[] args)
        {
            Console.WriteLine(AppContext.TargetFrameworkName);
            var task = SetupServer(args);
            await task;
            new Thread(CheckAttachments).Start();
            await Application.RunAsync();
        }
        public static void CheckAttachments()
        {
            var logger = Application.Logger;
            var databaseContext = Application.Services.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
            foreach (var file in databaseContext.AttachedFiles)
            {
                if (!File.Exists(Path.Combine("attached-files", file.Guid)))
                {
                    logger.LogWarning($"File({file.Id}) {file.Guid} ({file.FileName}) is in database but not exists on server!");
                }
            }
        }
        public static async Task SetupServer(string[] args)
        {
            // Create directories for static files
            new[]
            {
                "static/projects",
                "static/useravatars",
                "static/wideprojects",
                "static/template",
                "attached-files"
            }.ToList().ForEach(s => Directory.CreateDirectory(s));

            // Generate RSA keys for private key if not exists
            using RSACryptoServiceProvider rsa = new();
            if (!File.Exists("private.key")) GeneratePrivateKey();

            var builder = WebApplication.CreateBuilder(args);

            // Database setup
            builder.Services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlite($"Data Source={(IsDev ? "debug_database" : "database")}.db")
                //    .LogTo((e, l) => true, e =>
                //{
                //    File.AppendAllText("entity_framework.log", e.ToString());
                //})
                );
            builder.Services.AddScoped<DatabaseContext>();
            // MVC and Razor Components setup
            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddMvc(s => s.EnableEndpointRouting = false);

            // Authentication and Authorization setup
            builder.Services.AddAuthorization(bca =>
            {
                bca.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });
            builder.Services.AddDataProtection().UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            });
            builder.Services.AddAuthentication(s =>
            {
                s.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = Key,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero // To fully deny expired tokens
                };
            });

            builder.Services.AddRouting();

            // Build the application
            var app = Application = builder.Build();

            // Middleware to handle 404 errors and custom redirects
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
                {
                    await next();
                }
            });

            // Error handling for non-development environments
            if (!(IsDev = app.Environment.IsDevelopment()))
            {
                app.UseHttpsRedirection();
                app.UseHsts();
                app.UseExceptionHandler("/Error");
            }

            // Static files middleware
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "static")),
                RequestPath = "/static"
            });

            // Вытаскивает из кук жвт токен и сует в заголовок
            app.Use(async (context, next) =>
            {
                var token = context.Request.Cookies["auth_do_not_share"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers.Append("Authorization", "Bearer " + token);
                }
                await next();
            });
            app.UseRouting();
            app.UseAuthentication();

            app.Use((ctx, next) =>
            {
                var endpoint = ctx.GetEndpoint();
                if (endpoint?.Authorized() == true)
                {
                    ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                    ctx.Response.Headers["Pragma"] = "no-cache";
                    ctx.Response.Headers["Expires"] = "-1";
                    if (!ctx.Request.TryAuthenticate(out _))
                    {
                        ctx.Response.Redirect($"/login?redir={ctx.Request.Path}");
                        return Task.CompletedTask;
                    }
                }
                return next(ctx);
            });
            app.Use((ctx, next) =>
            {
                // /user -> [ /user/MY_NAME ] ИЛИ [ /login?redir/user -> /user -> /user/MY_NAME ]
                if (ctx.Request.Path.ToString().TrimEnd('/') == "/user")
                {
                    var auth = ctx.Request.TryAuthenticate(out var user);
                    ctx.Response.Redirect(auth
                        ? $"/user/{user.Username}"
                        : "/login?redir=/user");
                    return Task.CompletedTask;
                }
                return next(ctx);
            });

            app.UseAntiforgery();

            // /project -> /projects
            app.MapGet("/project", (s) =>
            {
                s.Response.Redirect("/projects");
                return Task.CompletedTask;
            });
            app.MapRazorComponents<Blazor.App>().AddInteractiveServerRenderMode();
            app.UseEndpoints(s => s.MapControllers());

#if RELEASE
            if (!app.Urls.Any(s => s.Contains("*")) && false)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("*** WARNING: No wildcardURL specified. This means that the server will be accessible only from localhost.");
                Console.ResetColor();
            }
#endif

            var databaseContext = app.Services.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();

            if (!databaseContext.Ranks.Any()) // If there are no ranks in the database, add the default rank
            {
                databaseContext.Ranks.Add(new() { Name = "User", Id = 0, ShowRankName = false });
                await databaseContext.SaveChangesAsync();
            }

            var logger = app.Logger;
            logger.LogDebug($"Total projects count:\t{databaseContext.Projects.Count()}");
            logger.LogDebug($"Total users count:\t{databaseContext.Users.Count()}");
            logger.LogDebug($"Total ranks count:\t{databaseContext.Ranks.Count()}");
            logger.LogDebug($"Total issues count:\t{databaseContext.Issues.Count()}");
            logger.LogDebug($"Total versions count:\t{databaseContext.Versions.Count()}");
            logger.LogDebug($"Total attachments count:\t{databaseContext.AttachedFiles.Count()}");
        }
    }
}
