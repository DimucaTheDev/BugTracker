using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Website.Data
{
    /// <summary>
    /// Represents website configuration.
    /// </summary>
    public class Config
    {
        public static Config Instance = null!;

        private static readonly Config DefaultConfig = new() { MainRedirectTo = "/projects", ResizeMDImages = true };
        private const string ConfigFileName = "config.json";
        private const string OverviewFilePath = "static/overview.md";

        static Config() => Reload();
        /// <summary>
        /// Reloads website config from a json file <see cref="ConfigFileName"/>
        /// </summary>
        public static void Reload()
        {
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    Instance = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFileName), new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip
                    })!;
                }
                else
                {
                    File.WriteAllText(ConfigFileName, JsonSerializer.Serialize(Instance = DefaultConfig));
                }
                Instance.OverviewMarkdown = File.Exists(OverviewFilePath) ? File.ReadAllText(OverviewFilePath) : "";
                Instance.Templates = new();
                foreach (var template in Directory.GetFiles("static/template"))
                {
                    Instance.Templates.Add(Path.GetFileNameWithoutExtension(template).ToUpper(), File.ReadAllText(template));
                }
            }
            catch (JsonException e)
            {
                Log.Information($"Configuration file config.json is corrupted and can not be deserialized.\n{e}");
                Instance = DefaultConfig;
            }
            catch (Exception e) // when (e is IOException or UnauthorizedAccessException)
            {
                Log.Information($"Unable to read config.json:\n{e}");
                Instance = DefaultConfig;
            }
            Log.Information("Config reloaded");
        }

        /// <summary>
        /// User will be redirected to this url when accessing Main(/) page.
        /// </summary>
        public string MainRedirectTo { get; set; } = null!;

        /// <summary>
        /// Project default description template.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Templates { get; set; }

        /// <summary>
        /// A Markdown string content on <see cref="MainRedirectTo"/> page
        /// </summary>
        [JsonIgnore]
        public string OverviewMarkdown { get; set; }


        /// <summary>
        /// If true, Markdown will apply "max-width=100%" css property to all images
        /// </summary>
        public bool ResizeMDImages { get; set; }

        //todo: remove this shitty shit
        [Obsolete("Use database instead.", error: true)]
        public class RankConfig
        {
            [JsonPropertyName("background")]
            public string? HexColorBackground { get; set; }
            [JsonPropertyName("foreground")]
            public string? HexColorForeground { get; set; }
            ///// <summary>
            ///// Full Prefix+Name unformatted string.
            ///// </summary> 
            //public string FullPrefixString { get; set; }
            //public override string ToString() => FullPrefixString;
            //public string ToString(UserModel user) => Format(user);

            //// [ %% ]
            //public string Format(UserModel user) => FullPrefixString
            //    .Replace("%username%", user.Username)
            //    .Replace("%shownusername%", user.ShownName)
            //    .Replace("%useruuid%", user.UUID.ToString("D"))
            //    .Replace("%rankname%", this.);
        }
    }
}
