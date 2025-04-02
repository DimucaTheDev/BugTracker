using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website.Model
{
    [Table("Versions")]
    [PrimaryKey(nameof(Id))]
    public class VersionModel
    {
        public int Id { get; set; }
        public uint ProjectId { get; set; }
        public int? Priority { get; set; } // Больше приоритет - выше в списке версий
        [ForeignKey(nameof(ProjectId))] public ProjectModel? Project { get; set; }
        public string Name { get; set; }
    }
}
