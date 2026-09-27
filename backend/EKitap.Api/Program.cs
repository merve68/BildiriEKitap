using EKitap.Api.Data;
using EKitap.Api.Services;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrEmpty(builder.Environment.WebRootPath))
{
    var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(webRoot);
    builder.Environment.WebRootPath = webRoot;
}

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IWordReader, WordReader>();
builder.Services.AddSingleton<IEbookPdfService, EbookPdfService>();
builder.Services.AddScoped<IKitapService, KitapService>();
builder.Services.AddSingleton<IKitapGenerationQueue, KitapGenerationQueue>();
builder.Services.AddHostedService<KitapGenerationWorker>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads"));
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "ebooks"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.MapControllers();
app.Run();
