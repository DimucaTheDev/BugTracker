using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Website.Model;

namespace Website.Data
{
    public sealed class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<UserRankModel> Ranks => Set<UserRankModel>();
        public DbSet<UserModel> Users => Set<UserModel>();
        public DbSet<ProjectCategoryModel> ProjectCategories => Set<ProjectCategoryModel>();
        public DbSet<ProjectModel> Projects => Set<ProjectModel>();
        public DbSet<IssueModel> Issues => Set<IssueModel>();
        public DbSet<AttachedFileModel> AttachedFiles => Set<AttachedFileModel>();
        public DbSet<VersionModel> Versions => Set<VersionModel>();
        // public DbSet<UserActionModel> UserActions => Set<UserActionModel>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>()
                .HasOne(u => u.UserRank)
                .WithMany() // Если у UserRank нет коллекции для Users
                .HasForeignKey(u => u.RankId)
                .OnDelete(DeleteBehavior.SetNull); // Или другой тип поведения при удалении

            var valueComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());
            modelBuilder.Entity<IssueModel>()
                .Property(e => e.AttachedFiles)
                .HasConversion(
                    v => string.Join(',', v), // Сериализация (в строку)
                    v => string.IsNullOrWhiteSpace(v) ? new() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() // Десериализация
                , valueComparer);
            modelBuilder.Entity<IssueModel>()
                .Property(e => e.AffectedVersionIds)
                .HasConversion(
                    v => string.Join(',', v), // Сериализация (в строку)
                    v => string.IsNullOrWhiteSpace(v) ? new() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList() // Десериализация
                    );
            //modelBuilder.Entity<VersionModel>().Property(e=>e.)
        }
    }
}
