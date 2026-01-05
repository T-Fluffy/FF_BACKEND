using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FF.Core.Entities;
using FF.Core.Interfaces;
using FF.Core.Options;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FF.Infrastructure.Parsers;

public class FFBookParser : IBookParser
{
    private readonly FileStorageOptions _storageOptions;

    public FFBookParser(IOptions<FileStorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
    }

    public async Task<Book> ParseAsync(string fileName)
    {
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            fileName += ".pdf";

        string fullPdfPath = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), fileName);
        string bookTitle = Path.GetFileNameWithoutExtension(fileName);
        string bookFolderId = bookTitle.Replace(" ", "_").ToLower();
        string imageFolder = Path.Combine(Path.GetFullPath(_storageOptions.ImageOutputPath), bookFolderId);
        
        Directory.CreateDirectory(imageFolder);

        var book = new Book { Title = bookTitle };
        var allLines = new List<TextLine>();

        using var document = PdfDocument.Open(fullPdfPath);
        
        // 1. Extract Images and Group Text into Lines
        foreach (var page in document.GetPages())
        {
            // Save Images from this page
            var images = page.GetImages().ToList();
            for (int i = 0; i < images.Count; i++)
            {
                string imgName = $"p{page.Number}_i{i}.png";
                File.WriteAllBytes(Path.Combine(imageFolder, imgName), images[i].RawBytes.ToArray());
            }

            // Group words into lines (tolerance of 3 points for Y-coordinate)
            var linesOnPage = page.GetWords()
                .Where(w => w.BoundingBox.Top < page.Height * 0.95 && w.BoundingBox.Bottom > 45)
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom / 3) * 3) 
                .OrderByDescending(g => g.Key);

            foreach (var group in linesOnPage)
            {
                var sortedWords = group.OrderBy(w => w.BoundingBox.Left).ToList();
                var lineText = string.Join(" ", sortedWords.Select(w => w.Text));
                
                allLines.Add(new TextLine 
                { 
                    Text = lineText, 
                    Page = page.Number, 
                    Y = group.Key 
                });
            }
        }

        // 2. Locate Start of Story (Skip rules/intro)
        int startIdx = allLines.FindIndex(l => l.Text.Contains("TURN OVER") || l.Text.Contains("Your journey is about to begin")) + 1;
        if (startIdx <= 0) startIdx = 0;

        // 3. Find Section Anchors (Strict Line Match)
        var anchors = new List<(int Num, int LineIdx)>();
        int target = 1;

        for (int i = startIdx; i < allLines.Count; i++)
        {
            string trimmed = allLines[i].Text.Trim().TrimEnd('.');
            // If the entire line is just the number we are looking for
            if (int.TryParse(trimmed, out int val) && val == target)
            {
                anchors.Add((target, i));
                target++;
                if (target > 400) break;
            }
        }

        // 4. Slice and Map Images
        for (int i = 0; i < anchors.Count; i++)
        {
            var curr = anchors[i];
            int endLineIdx = (i < anchors.Count - 1) ? anchors[i + 1].LineIdx : allLines.Count;
            
            var sb = new StringBuilder();
            for (int j = curr.LineIdx + 1; j < endLineIdx; j++)
            {
                sb.AppendLine(allLines[j].Text);
            }

            var section = new Section
            {
                SectionNumber = curr.Num,
                Content = sb.ToString().Trim(),
                ImagePath = $"/assets/game-art/{bookFolderId}/p{allLines[curr.LineIdx].Page}_i0.png"
            };

            // Check if the page actually had an image
            if (!File.Exists(Path.Combine(imageFolder, $"p{allLines[curr.LineIdx].Page}_i0.png")))
                section.ImagePath = null;

            section.Choices = ExtractChoices(section.Content);
            book.Sections.Add(section);
        }

        // 5. Save JSON
        string jsonDir = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), "ProcessedBooks");
        Directory.CreateDirectory(jsonDir);
        string jsonPath = Path.Combine(jsonDir, $"{bookTitle}.json");
        
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(book, new JsonSerializerOptions { WriteIndented = true }));

        return book;
    }

    private List<Choice> ExtractChoices(string text)
    {
        return Regex.Matches(text, @"(?i)turn\s+to\s+(\d+)")
            .Select(m => new Choice { 
                TargetSectionNumber = int.Parse(m.Groups[1].Value), 
                Description = $"Turn to {m.Groups[1].Value}" 
            }).ToList();
    }

    private class TextLine { 
        public string Text { get; set; } = ""; 
        public int Page { get; set; } 
        public double Y { get; set; }
    }
}