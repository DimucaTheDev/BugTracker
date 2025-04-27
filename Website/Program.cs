using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System.Reflection;
using System.Resources;
using Website.Data;
using Website.Model;
using Website.Util;

namespace Website
{
    public class Program
    {
        public const string Issuer = "BugTracker";
        public const string Audience = "AuthClient";
        public static SymmetricSecurityKey Key = new(File.ReadAllBytes("private.key"));
        public static WebApplication Application;
        public static bool IsDev;

        private static readonly string ResourceName = "Website.Resources.SharedResources";
        private static ResourceManager Localizer = new(ResourceName, Assembly.GetExecutingAssembly());
        private static Logger<Program> Logger => new(new SerilogLoggerFactory(Log.Logger));

        public static async Task Main(string[] args)
        {
            await SetupServer(args);
            new Thread(CheckAttachments).Start();
            await Application.RunAsync();
        }

        private static void GeneratePrivateKey()
        {
            var keyContent = new string(Enumerable.Range(0, 2048)
                .Select(_ => (char)Random.Shared.Next(0x21, 0x7E)) // random characters
                .ToArray());
            File.WriteAllText("private.key", keyContent);
            Log.Information(Localizer.GetString("main_key")!);
        }
        public static void CheckAttachments()
        {
            using var scope = Application.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            List<AttachedFileModel> notExist = databaseContext.AttachedFiles.
                ToList()
                .Where(file => !File.Exists(Path.Combine("attached-files", file.Guid)))
                .ToList();
            var ids = string.Join(" ", notExist.Select(s => $"{s.Id}"));
            Logger.LogError(Localizer.GetString("main_not_exist"), ids);
        }

        public static async Task SetupServer(string[] args)
        {
            CreateDirectories();
            SetupLogger();
            Logger.LogInformation(Localizer.GetString("main_starting"));
            GenerateKeyIfNeeded();
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder);
            Application = builder.Build();
            ConfigureMiddleware(Application, builder);
            await InitializeDatabase(Application);
        }

        private static void CreateDirectories()
        {
            foreach (var dir in new[] { "logs", "static/projects", "static/useravatars", "static/wideprojects", "static/template", "attached-files" })
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static void GenerateKeyIfNeeded()
        {
            if (!File.Exists("private.key")) GeneratePrivateKey();
        }

        private static void SetupLogger()
        {
            var format = "[ {Level:u3} {Timestamp:yyyy-MM-dd HH:mm:ss} [{SourceContext}]] {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: format)
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
                outputTemplate: format) // Логи в файл
            .Enrich.FromLogContext()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .MinimumLevel.Information()
            .CreateLogger();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog();
            builder.Services.AddLocalization((s) => s.ResourcesPath = "Resources");
            builder.Services.AddScoped<ILanguageService, LanguageService>();
            builder.Services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlite($"Data Source={(IsDev ? "debug_database" : "database")}.db"));
            builder.Services.AddScoped<DatabaseContext>();
            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddMvc(options => options.EnableEndpointRouting = false);
            ConfigureAuthentication(builder);
        }

        private static void ConfigureAuthentication(WebApplicationBuilder builder)
        {

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });
            builder.Services.AddDataProtection().UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            });
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Issuer,
                        ValidAudience = Audience,
                        IssuerSigningKey = Key,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        private static void ConfigureMiddleware(WebApplication app, WebApplicationBuilder builder)
        {
            if (!(IsDev = app.Environment.IsDevelopment()))
            {
                app.UseHttpsRedirection();
                app.UseHsts();
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "static")),
                RequestPath = "/static"
            });
            string[] supportedLangs = ["en-US", "ru-RU"];
            app.UseRequestLocalization(new RequestLocalizationOptions()
                .AddSupportedCultures(supportedLangs)
                .AddSupportedUICultures(supportedLangs));

            app.Use(AuthenticationMiddleware);
            app.UseRouting();
            app.UseAuthentication();
            app.Use(AuthorizationMiddleware);
            app.Use(UserRedirectMiddleware);
            app.UseAntiforgery();
            app.MapGet("/project", context =>
            {
                context.Response.Redirect("/projects");
                return Task.CompletedTask;
            });
            app.MapRazorComponents<Blazor.App>().AddInteractiveServerRenderMode();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private static async Task InitializeDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            if (!databaseContext.Ranks.Any())
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

        private static async Task AuthenticationMiddleware(HttpContext context, Func<Task> next)
        {
            var token = context.Request.Cookies["auth_do_not_share"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers.Append("Authorization", "Bearer " + token);
            }
            await next();
        }

        private static async Task AuthorizationMiddleware(HttpContext ctx, Func<Task> next)
        {
            var endpoint = ctx.GetEndpoint();
            if (endpoint?.RequiresAuthorization() == true)
            {
                ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                ctx.Response.Headers["Pragma"] = "no-cache";
                ctx.Response.Headers["Expires"] = "-1";
                if (!ctx.Request.TryAuthenticate(out _))
                {
                    ctx.Response.Redirect($"/login?redir={ctx.Request.Path}");
                    return;
                }
            }
            await next();
        }

        private static async Task UserRedirectMiddleware(HttpContext ctx, Func<Task> next)
        {
            if (ctx.Request.Path.ToString().TrimEnd('/') == "/user")
            {
                var auth = ctx.Request.TryAuthenticate(out var user);
                ctx.Response.Redirect(auth ? $"/user/{user.Username}" : "/login?redir=/user");
                return;
            }
            await next();
        }
    }
}
