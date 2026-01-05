using FF.Core.Interfaces;
using FF.Core.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IBookParser _parser;
    private readonly FileStorageOptions _storageOptions;

    public AdminController(IBookParser parser, IOptions<FileStorageOptions> storageOptions)
    {
        _parser = parser;
        _storageOptions = storageOptions.Value;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestBook([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("FileName is required.");

        try
        {
            var book = await _parser.ParseAsync(fileName);
            return Ok(new { 
                Message = "Ingestion Successful", 
                Sections = book.Sections.Count 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("cleanup/{bookTitle}")]
    public IActionResult Cleanup(string bookTitle)
    {
        string rootPath = Path.GetFullPath(_storageOptions.PdfUploadPath);
        string jsonPath = Path.Combine(rootPath, "ProcessedBooks", $"{bookTitle}.json");
        
        string bookFolderId = bookTitle.Replace(" ", "_").ToLower();
        string imagePath = Path.Combine(Path.GetFullPath(_storageOptions.ImageOutputPath), bookFolderId);

        if (System.IO.File.Exists(jsonPath)) 
            System.IO.File.Delete(jsonPath);

        if (System.IO.Directory.Exists(imagePath)) 
            System.IO.Directory.Delete(imagePath, true);

        return Ok($"Cleared data and images for {bookTitle}");
    }
}