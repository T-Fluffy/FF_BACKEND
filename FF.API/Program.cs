using FF.Core.Interfaces;
using FF.Core.Options;
using FF.Infrastructure.Parsers;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuration
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

// 2. Services (No more DB Context!)
builder.Services.AddScoped<IBookParser, FFBookParser>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Static Files (for Game Art)
var storageRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "Storage"));
var imagePath = Path.Combine(storageRoot, "GameArt");
var uploadPath = Path.Combine(storageRoot, "Uploads");

// Ensure directories exist
Directory.CreateDirectory(imagePath);
Directory.CreateDirectory(uploadPath);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagePath),
    RequestPath = "/assets/game-art"
});

app.MapControllers();

app.Run();