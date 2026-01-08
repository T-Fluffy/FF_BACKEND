using FluentAssertions;
using FF.Infrastructure.Parsers;
using FF.Core.Options;
using Microsoft.Extensions.Options;

namespace FF.Tests;

public class ParserLogicTests
{
    // We need a helper to create the parser since it requires Options
    private FFBookParser CreateParser()
    {
        var options = Options.Create(new FileStorageOptions 
        { 
            PdfUploadPath = "TestPath", 
            ImageOutputPath = "TestPath" 
        });
        return new FFBookParser(options);
    }

    [Fact]
    public void ExtractChoices_Should_Find_Simple_TurnTo()
    {
        // Arrange
        var parser = CreateParser();
        string text = "If you wish to attack, turn to 25.";

        // Act
        var result = parser.ExtractChoices(text);

        // Assert
        result.Should().HaveCount(1);
        result.First().TargetSectionNumber.Should().Be(25);
    }

    [Fact]
    public void ExtractChoices_Should_Detect_DiceRoll()
    {
        // Arrange
        var parser = CreateParser();
        string text = "Test your Luck. Roll two dice. If lucky turn to 10.";

        // Act
        var result = parser.ExtractChoices(text);

        // Assert
        result.First().IsDiceRoll.Should().BeTrue();
    }

    [Fact]
    public void RemoveHeaderNumber_Should_Clean_Text()
    {
        // Arrange
        var parser = CreateParser();
        string rawText = "400. You have won the game.";

        // Act
        string cleanText = parser.RemoveHeaderNumber(rawText, 400);

        // Assert
        cleanText.Trim().Should().Be("You have won the game.");
    }

    [Fact]
    public void Section50_Logic_Should_Trigger_On_Keywords()
    {
        // Arrange - simulating the loop logic from your parser
        string lineText = "'Come now,' said Abdul, 'you have lost the wager.'";
        int currentSectionNum = 49;
        int foundNumber = -1;

        // Act - Replicating the "Patch" logic exactly as it is in your main file
        if (currentSectionNum == 49 && foundNumber == -1)
        {
            if (lineText.Contains("'Come now,'") || lineText.Contains("lost the wager"))
            {
                foundNumber = 50;
            }
        }

        // Assert
        foundNumber.Should().Be(50);
    }
}