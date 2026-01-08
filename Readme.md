# ⚔️ Fighting Fantasy Engine API

A high-fidelity PDF-to-Digital engine designed specifically to parse, structure, and digitize Fighting Fantasy gamebooks. This project transforms static PDFs into dynamic JSON datasets, complete with automated image extraction, choice mapping, and structural patches.

---
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![JSON](https://img.shields.io/badge/json-5E5E5E?style=for-the-badge&logo=json&logoColor=white)
![PDF Pig](https://img.shields.io/badge/PDF_Pig-FF6F00?style=for-the-badge&logo=pdf&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-Passing-brightgreen?style=for-the-badge&logo=github-actions)
![Coverage](https://img.shields.io/badge/Coverage-85%25-yellow?style=for-the-badge)

## 🏗️ System Architecture

The project is structured following Clean Architecture principles to separate the parsing logic from the data storage and web presentation layers.

```
FF.Project/
├── FF.Core/
│   ├── Entities/
│   │   ├── Book.cs           # Root object (Title, Intro, Sections)
│   │   ├── Section.cs        # Individual game beats (Content, Image, Choices)
│   │   └── Choice.cs         # Navigation links (Target Section, Dice Roll flags)
│   └── Interfaces/
│       └── IBookParser.cs    # Contract for PDF parsing
├── FF.Infrastructure/
│   ├── Parsers/
│   │   └── FFBookParser.cs   # The "Brain": Handles logic, state, and FF patches
│   └── Options/
│       └── FileStorageOptions.cs # Path configurations for PDF/Images
├── FF.Web/                   # Frontend / API layer
└── Assets/
    ├── PDF_Uploads/          # Source PDF files
    └── Game_Art/             # Extracted section illustrations 
```
## 🚀 Key Features

* State-Machine Parsing: Differentiates between "Front Matter" (Introduction/Rules) and actual "Game Sections" using keyword triggers like TURN OVER.

* Geometric Header Detection: Identifies section numbers based on centered X/Y coordinates, effectively ignoring page-top navigation numbers.

* The "Section 50" Patch: Specialized sequential logic to detect Section 50, which is historically missing a header number in many printings of Seas of Blood.

* Automatic Image Slicing: Extracts high-quality illustrations from the PDF and maps them to their respective sections.

* Victory Terminator: Intelligent buffer flushing that stops ingestion once the "You have won" signature is detected, preventing publisher advertisements from polluting the data.

## 🛠️ The Core Logic (```FFBookParser.cs```)

This engine uses PdfPig for geometric analysis and a custom buffer system to ensure exactly 400 sections are captured.

Specialized Patches
The parser includes hard-coded logic to fix common OCR and layout errors found in gamebook PDFs:

```
// 1. The "Abdul" Patch (Detecting Section 50 without a number)
if (currentSectionNum == 49 && foundNumber == -1)
{
    if (cleanText.Contains("'Come now,'") || cleanText.Contains("lost the wager"))
    {
        foundNumber = 50; 
    }
}

// 2. The Victory Terminator (Killing the buffer at the end of the story)
if (currentSectionNum == 400 && cleanText.Contains("You have won."))
{
    isBookFinished = true; // Prevents ingesting back-matter advertisements
}
```
## 📄 Data Structure (JSON)
The resulting JSON is structured for immediate use in game engines or web applications.
```
{
  "Title": "Seas of Blood",
  "Introduction": "The wager between you and Abdul the Butcher...",
  "Sections": [
    {
      "SectionNumber": 400,
      "Content": "There, you say to Abdul, flinging your coffer open... You have won.",
      "ImagePath": "/assets/game-art/seas_of_blood/p250_i0.png",
      "Choices": [],
      "HasCombat": false
    }
  ]
}
```
## 🛠️ Setup & Usage

1. Place PDF: Drop your Seas of Blood.pdf into the configured PDF_Uploads folder.

2. Configure Paths: Update appsettings.json with your local ImageOutputPath.

3. Run Ingestion:

```
// Example call in your Controller or Service
var parser = new FFBookParser(options);
var book = await parser.ParseAsync("Seas of Blood");
```
4. Verify: Check ProcessedBooks/Seas of Blood.json and your Game_Art folder for the extracted assets.

## 🧪 Testing Strategy

The project utilizes **xUnit** and **FluentAssertions** to maintain data integrity across book versions.

* **Unit Tests**: Validates Regex patterns for choice extraction.
* **Regression Tests**: Ensures the "Section 50" and "Victory" patches remain functional after logic updates.
* **Validation**: Every ingestion is validated against a schema to ensure `SectionNumber` ranges (1-400) are strictly followed.

To run tests:
`dotnet test`
