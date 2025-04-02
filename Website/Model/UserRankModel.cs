using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website.Model
{
    [PrimaryKey(nameof(Id))]
    public class UserRankModel
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public UserRights Rights { get; set; }
        //[NotMapped] public Config.RankConfig? RankConfig { get => Config.Instance.RankConfigs[(int)Id]; set => Config.Instance.RankConfigs[(int)Id] = value!; }
        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }
        [Column("ShowRankNameByDefault")]
        public bool ShowRankName { get; set; } = true;
    }
}
