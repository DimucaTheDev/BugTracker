using Microsoft.EntityFrameworkCore;
using Website.Data;

namespace Website.Model
{
    [PrimaryKey(nameof(Id))]
    public class UserActionModel
    {
        public uint Id { get; set; }
        public ActionType ActionType { get; set; }
        public uint? UserId { get; set; }
        public uint? ProjectId { get; set; }
        public uint? IssueId { get; set; }
        public object? Arg1 { get; set; }
        public object? Arg2 { get; set; }
        public object? Arg3 { get; set; }
    }
}
