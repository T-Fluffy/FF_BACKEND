namespace FF.Core.Entities;

public class Section
{
    public int SectionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public List<Choice> Choices { get; set; } = new List<Choice>();
}