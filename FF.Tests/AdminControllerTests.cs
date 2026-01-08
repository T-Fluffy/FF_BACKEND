using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using FluentAssertions;
using FF.API; 
using Microsoft.Extensions.Configuration;
namespace FF.Tests;

public class AdminControllerTests : IClassFixture<WebApplicationFactory<Program>> 
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(AppContext.BaseDirectory);

            // This OVERRIDES the appsettings.json specifically for this test
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Remove the ".." so it looks in the current folder
                    ["FileStorage:PdfUploadPath"] = "Storage/Uploads",
                    ["FileStorage:ImageOutputPath"] = "Storage/GameArt"
                });
            });
        });
    }

    [Fact]
    public async Task IngestBook_Should_Return_Ok_When_FileName_Is_Valid()
    {
        // Arrange
        var client = _factory.CreateClient();
        string fileName = "Seas of Blood.pdf";
        string url = $"/api/admin/ingest?fileName={Uri.EscapeDataString(fileName)}";

        // Act
        var response = await client.PostAsync(url, null);

        // Assert: If it fails, we extract the error message for easier debugging
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorDetail = await response.Content.ReadAsStringAsync();
            throw new Exception($"Test failed with status {response.StatusCode}. Details: {errorDetail}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Ingestion Successful");
        result.Sections.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IngestBook_Should_Return_BadRequest_When_FileName_Is_Empty()
    {
        // Arrange
        var client = _factory.CreateClient();
        string url = "/api/admin/ingest?fileName="; 

        // Act
        var response = await client.PostAsync(url, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cleanup_Should_Return_Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        string bookTitle = "Seas of Blood";
        string url = $"/api/admin/cleanup/{bookTitle}";

        // Act
        var response = await client.DeleteAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Cleared data and images for {bookTitle}");
    }
}

// Map the JSON response to match AdminController.cs
public record IngestionResponse(string Message, int Sections);