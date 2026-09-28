using Microsoft.EntityFrameworkCore;

namespace Selenium_Handler.Models {
    public class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        public DbSet<FileConfigResult> FileConfigResults => Set<FileConfigResult>();
        public DbSet<TestResult> TestResults => Set<TestResult>();
        public DbSet<StepResult> StepResults => Set<StepResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<FileConfigResult>(entity => {
                entity.ToTable("FileConfigResult");

                entity.HasKey(fileConfigResult => fileConfigResult.Id);

                entity.Property(fileConfigResult => fileConfigResult.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(fileConfigResult => fileConfigResult.RunId)
                    .IsRequired();

                entity.Property(fileConfigResult => fileConfigResult.Name)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(fileConfigResult => fileConfigResult.Description)
                    .HasMaxLength(1024);

                entity.Property(fileConfigResult => fileConfigResult.FilePath)
                    .HasMaxLength(1024)
                    .IsRequired();

                entity.Property(fileConfigResult => fileConfigResult.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();

                entity.HasIndex(fileConfigResult => fileConfigResult.RunId)
                    .HasDatabaseName("IX_FileConfigResult_RunId");
            });

            modelBuilder.Entity<TestResult>(entity => {
                entity.ToTable("TestResult");

                entity.HasKey(testResult => testResult.Id);

                entity.Property(testResult => testResult.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(testResult => testResult.Step)
                    .IsRequired();

                entity.Property(testResult => testResult.Name)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(testResult => testResult.Description)
                    .HasMaxLength(1024);

                entity.Property(testResult => testResult.FilePath)
                    .HasMaxLength(1024)
                    .IsRequired();

                entity.Property(testResult => testResult.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();

                entity.Property(testResult => testResult.UpdateTime)
                    .HasPrecision(0)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();

                entity.HasOne(testResult => testResult.FileConfigResult)
                    .WithMany(fileConfigResult => fileConfigResult.TestResults)
                    .HasForeignKey(testResult => testResult.FileConfigResultId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(testResult => testResult.FileConfigResultId)
                    .HasDatabaseName("IX_TestResult_FileConfigResultId");

                entity.HasIndex(testResult => new { testResult.FileConfigResultId, testResult.Step, testResult.Id })
                    .HasDatabaseName("IX_TestResult_FileConfigResultId_Step_Id");
            });

            modelBuilder.Entity<StepResult>(entity => {
                entity.ToTable("StepResult");

                entity.HasKey(stepResult => stepResult.Id);

                entity.Property(stepResult => stepResult.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(stepResult => stepResult.Step)
                    .IsRequired();

                entity.Property(stepResult => stepResult.Name)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(stepResult => stepResult.Description)
                    .HasMaxLength(1024);

                entity.Property(stepResult => stepResult.Screenshot)
                    .HasMaxLength(1024);

                entity.Property(stepResult => stepResult.Result)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(stepResult => stepResult.ErrorMessage)
                    .HasMaxLength(2048);

                entity.Property(stepResult => stepResult.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();

                entity.Property(stepResult => stepResult.UpdateTime)
                    .HasPrecision(0)
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();

                entity.HasOne(stepResult => stepResult.TestResult)
                    .WithMany(testResult => testResult.StepResults)
                    .HasForeignKey(stepResult => stepResult.TestResultId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(stepResult => stepResult.TestResultId)
                    .HasDatabaseName("IX_StepResult_TestResultId");

                entity.HasIndex(stepResult => new { stepResult.TestResultId, stepResult.Step, stepResult.Id })
                    .HasDatabaseName("IX_StepResult_TestResultId_Step_Id");
            });
        }
    }
}
