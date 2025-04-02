using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website.Model
{
    /// <summary>
    /// This model is used in DatabaseContext. For project CONFIG see <see cref="ProjectConfigModel"/>
    /// </summary>
    [Table("Projects")]
    [PrimaryKey(nameof(Id))]
    public class ProjectModel
    {
        //[Obsolete($"Used in database. Use {nameof(ProjectCode)} for short project code")]
        public uint Id { get; set; }

        public string ProjectName { get; set; }

        /// <summary>
        /// Short Code (like ABC, ABCD...)
        /// </summary>
        public string ProjectCode { get; set; }

        /// <summary>
        /// Team-lead(proj manager) user id.
        /// </summary>
        public uint ManagerUserId { get; set; }

        /// <summary>
        /// Navigation property for Manager User
        /// </summary>
        [ForeignKey(nameof(ManagerUserId))]
        public UserModel? ManagerUser { get; set; }

        /// <summary>
        /// May be null. Project main page url
        /// </summary>
        public string? ProjectUrl { get; set; }

        public uint ProjectCategoryId { get; set; }

        /// <summary>
        /// Navigation property for Category
        /// </summary>
        [ForeignKey(nameof(ProjectCategoryId))]
        public ProjectCategoryModel? Category { get; set; }

        public string? Description { get; set; }

        public override string ToString() => ProjectCode;
    }
}