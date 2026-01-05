namespace FF.Core.Entities;

public class Book
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = "Fighting Fantasy";
    public List<Section> Sections { get; set; } = new List<Section>();
}