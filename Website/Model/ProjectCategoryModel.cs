using Microsoft.EntityFrameworkCore;

namespace Website.Model
{
    [PrimaryKey(nameof(Id))]
    public class ProjectCategoryModel
    {
        public uint Id { get; set; } = 0;
        public string Name { get; set; } = "No Category";
    }
}
