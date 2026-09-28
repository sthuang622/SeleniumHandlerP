using Selenium_Handler.Handler.Abstraction;
using Selenium_Handler.Handler.Implementation;
using Selenium_Handler.Config;
using Selenium_Handler.Models;
using Selenium_Handler.Repo.Abstraction;
using Selenium_Handler.Repo.Implementation;
using Selenium_Handler.Service.Abstraction;
using Selenium_Handler.Service.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<FolderPaths>(builder.Configuration.GetSection("Scripts"));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SeleniumHandlerDocker"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));
builder.Services.AddScoped<ISeleniumFileHandler, SeleniumFileHandler>();
builder.Services.AddScoped<ILoggingHandler, LoggingHandler>();
builder.Services.AddScoped<ISeleniumHandler, SeleniumHandler>();
builder.Services.AddScoped<ISeleniumService, SeleniumService>();
builder.Services.AddScoped<ITestResultRepo, TestResultRepo>();
builder.Services.AddScoped<ITestResultsService, TestResultsService>();
builder.Services.AddSingleton<FolderPathResolver>();

var app = builder.Build();
FolderInitializer.EnsureFoldersExist(app.Services);
EnsureDatabaseSchema(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
var folderPathResolver = app.Services.GetRequiredService<FolderPathResolver>();
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(folderPathResolver.ScreenshotFolder),
    RequestPath = "/screenshots"
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();


static void EnsureDatabaseSchema(IServiceProvider services) {
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.ExecuteSqlRaw("""
        IF COL_LENGTH(N'dbo.StepResult', N'ErrorMessage') IS NULL
        BEGIN
            ALTER TABLE dbo.StepResult
            ADD ErrorMessage NVARCHAR(2048) NULL;
        END;

        IF COL_LENGTH(N'dbo.TestResult', N'UpdateTime') IS NULL
        BEGIN
            ALTER TABLE dbo.TestResult
            ADD UpdateTime DATETIME2(0) NOT NULL CONSTRAINT DF_TestResult_UpdateTime DEFAULT SYSUTCDATETIME();
        END;

        IF COL_LENGTH(N'dbo.StepResult', N'UpdateTime') IS NULL
        BEGIN
            ALTER TABLE dbo.StepResult
            ADD UpdateTime DATETIME2(0) NOT NULL CONSTRAINT DF_StepResult_UpdateTime DEFAULT SYSUTCDATETIME();
        END;

        IF COL_LENGTH(N'dbo.FileConfigResult', N'RunId') IS NULL
        BEGIN
            ALTER TABLE dbo.FileConfigResult
            ADD RunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_FileConfigResult_RunId DEFAULT NEWID();
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_FileConfigResult_RunId'
              AND object_id = OBJECT_ID(N'dbo.FileConfigResult')
        )
        BEGIN
            CREATE INDEX IX_FileConfigResult_RunId
            ON dbo.FileConfigResult (RunId);
        END;
        """);
}
