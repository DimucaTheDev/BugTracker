using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website.Model
{
    [PrimaryKey(nameof(Id))]
    [Table("AttachedFiles")]
    public class AttachedFileModel
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public uint UserUploadedId { get; set; }
        [ForeignKey(nameof(UserUploadedId))]
        public UserModel? UserUploaded { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; }
        public int RequestedTimes { get; set; }
        public long? Size { get; set; }
    }
}
