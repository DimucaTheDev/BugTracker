using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Website.Data;
using Website.Model;
using Website.Util;

namespace Website.Controllers
{
    [Route("api/issues")]
    public class IssuesController(DatabaseContext database) : Controller
    {
        private readonly DatabaseContext _database = database;

        [Auth]
        [HttpPost("create")]
        public IActionResult CreateIssue([FromForm] string projectCode, [FromForm] IssueType issueType, [FromForm] string title, [FromForm] string description, [FromForm] List<string> selectedItems, IFormFileCollection files)
        {
            ProjectModel? project = _database.Projects.FirstOrDefault(s => s.ProjectCode == projectCode);
            if (project == null) return BadRequest(new { description = "Project not exists" });
            List<int> versions = new();
            foreach (var item in selectedItems)
            {
                int versionId = _database.Versions.FirstOrDefault(s => s.Project == project && s.Name == item)?.Id ?? -1;
                if (versionId == -1) return BadRequest(new { description = $"Version '{item}' not exists" });
                versions.Add(versionId);
            }
            if (!Request.TryAuthenticate(out var user)) return Unauthorized(new { description = "Unable to authorize" });
            List<string> filesId = new();
            foreach (var file in files)
            {
                var id = Guid.NewGuid();
                using var fileStream = System.IO.File.Create("attached-files/" + id);
                file.CopyTo(fileStream);
                filesId.Add(id.ToString("D"));
                _database.AttachedFiles.Add(new AttachedFileModel
                {
                    FileName = file.FileName,
                    Guid = id.ToString("D"),
                    Size = file.Length,
                    UploadedAt = DateTime.Now,
                    UserUploadedId = user.Id,
                });
            }
            uint latestIssueId = database.Issues
                .Where(s => s.ProjectId == project.Id!)
                .OrderByDescending(issue => issue.IssueId)
                .Select(s => s.IssueId)
                .FirstOrDefault();
            var issue = new IssueModel
            {
                Project = project,
                IssueType = issueType,
                IssueId = latestIssueId + 1,
                Title = title,
                Description = description,
                AffectedVersionIds = versions,
                UserCreated = user,
                CreatedAt = DateTime.Now,
                Status = IssueStatus.Open,
                Uuid = Guid.NewGuid(),
                AttachedFiles = filesId
            };
            _database.Issues.Add(issue);
            _database.SaveChanges();
            return Ok(new { redir = $"/issue/{projectCode}-{issue.IssueId}" });
        }

        [Auth]
        [HttpPost("edit")]
        public IActionResult EditIssue(
            [FromForm] string issueGuid, [FromForm] IssueType issueType, [FromForm] string title,
            [FromForm] string description, [FromForm] List<string> selectedItems, [FromForm] string fixedIn,
            [FromForm] string issueConfirmation, [FromForm] string issueSolution, [FromForm] string issueStatus, IFormFileCollection files)
        {
            var keepFiles = Request.Form.Where(s => s.Key.StartsWith("keepfile-")).Select(s => s.Key.Replace("keepfile-", "")).ToList();

            var issue = _database.Issues
                .Include(s => s.Project)
                .FirstOrDefault(s => s.Uuid.Equals(new Guid(issueGuid)));

            if (issue == null) return BadRequest(new { description = "Указана несуществующая задача" });

            if (!Request.TryAuthenticate(out var user)) return BadRequest(new { description = "Пользователь не авторизован" });

            foreach (var file in files)
            {
                var id = Guid.NewGuid();
                using var fileStream = System.IO.File.Create("attached-files/" + id);
                file.CopyTo(fileStream);
                keepFiles.Add(id.ToString("D"));
                _database.AttachedFiles.Add(new AttachedFileModel
                {
                    FileName = file.FileName,
                    Guid = id.ToString("D"),
                    Size = file.Length,
                    UploadedAt = DateTime.Now,
                    UserUploadedId = user.Id,
                });
            }

            issue.Status = Enum.Parse<IssueStatus>(issueStatus);
            issue.ConfirmationStatus = Enum.Parse<ConfirmationStatus>(issueConfirmation);
            issue.Solution = Enum.Parse<IssueSolution>(issueSolution);
            issue.IssueType = issueType;
            issue.Title = title;
            issue.Description = description;
            issue.AffectedVersionIds =
                selectedItems.Select(s =>
                _database.Versions.Where(v => v.Project == issue.Project)
                        .First(v => v.Name == s))
                    .Select(s => s.Id).ToList();
            issue.FixedInVersionId = _database.Versions
                .Where(v => v.Project == issue.Project)
                .FirstOrDefault(v => v.Name == fixedIn)?.Id;
            issue.UpdatedAt = DateTime.Now;
            issue.AttachedFiles = keepFiles;
            if (!Request.Form.ContainsKey("keep-assignee"))
                issue.UserAssignedId = null;

            _database.SaveChanges();
            return Ok(new { redir = $"/issue/{issue.Project!.ProjectCode}-{issue.IssueId}" });
        }
    }
}