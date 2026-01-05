using FF.Core.Entities;

namespace FF.Core.Interfaces;

public interface IBookParser
{
    // Takes a file name/path and returns a populated Book object (which is saved as JSON internally)
    Task<Book> ParseAsync(string filePath);
}