using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website.Model
{
    [Table("Users")]
    [PrimaryKey(nameof(Id))]
    public class UserModel
    {
        public uint Id { get; set; }

        public string Username { get; set; }

        public string? ShownName { get; set; }

        public string? Email { get; set; }

        public bool IsBot { get; set; }

        /// <summary>
        /// User Unique IDentifier, must be unique(🧐)
        /// </summary>
        public Guid Uuid { get; set; }

        public string PasswordHash { get; set; }

        public uint RankId { get; set; }

        /// <summary>
        /// Navigation property for User Rank
        /// </summary>
        [ForeignKey(nameof(RankId))]
        public UserRankModel? UserRank { get; set; }

        public override string ToString() => ShownName ?? Username; //$"[ {Rank.Name} ] {Username}";
    }
}