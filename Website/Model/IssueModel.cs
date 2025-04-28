using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using Website.Data;

namespace Website.Model
{
    [Table("Issues")]
    [PrimaryKey(nameof(Id))]
    public class IssueModel
    {
        public int Id { get; set; }
        public uint ProjectId { get; set; }
        public uint IssueId { get; set; }
        [ForeignKey(nameof(ProjectId))] public ProjectModel? Project { get; set; }
        public uint UserCreatedId { get; set; }
        [ForeignKey(nameof(UserCreatedId))] public UserModel? UserCreated { get; set; }
        public uint? UserAssignedId { get; set; }
        [ForeignKey(nameof(UserAssignedId))] public UserModel? UserAssigned { get; set; }
        public Guid Uuid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public IssueStatus Status { get; set; }
        public IssuePriority Priority { get; set; }
        public IssueType IssueType { get; set; }
        public List<string>? AttachedFiles { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<int>? AffectedVersionIds { get; set; }
        public int? FixedInVersionId { get; set; }
        [ForeignKey(nameof(FixedInVersionId))] public VersionModel? FixedInVersion { get; set; }
        public IssueSolution Solution { get; set; }
        public ConfirmationStatus ConfirmationStatus { get; set; }
    }
}