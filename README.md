# Attainment

Attainment is a Windows desktop study application built with WPF. It organizes subjects, learning resources and generated products, and can create exams from PDF material with an OpenAI-backed workflow.

## Features

- Manage subjects, resources and derived products.
- Attach PDF or PowerPoint files to learning resources.
- Extract text from PDFs with PdfPig.
- Generate and preview multiple-choice exams.
- Export exams and answer keys with QuestPDF.
- Persist local data with EF Core and SQLite.

## Technology

| Area | Technology |
| --- | --- |
| Desktop UI | WPF and XAML on .NET 10 |
| Application host | `Microsoft.Extensions.Hosting` and dependency injection |
| Persistence | Entity Framework Core 10 and SQLite |
| PDF input | PdfPig |
| PDF output | QuestPDF |
| Tests | xUnit |

WPF is the desktop framework used by this project. There is no third-party UI toolkit. See the [architecture review and WPF guide](docs/attainment-architecture-and-wpf-guide.html) for a project-specific introduction to XAML, binding, MVVM and the supporting libraries.

## Prerequisites

- Windows 10 or later.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), version `10.0.100` or a compatible later feature band.
- Visual Studio 2022 with the .NET desktop development workload, JetBrains Rider, or the `dotnet` CLI.

Verify the effective SDK before restoring the solution:

```powershell
dotnet --info
dotnet --version
```

## Getting Started

```powershell
git clone https://github.com/LucasOrosCampo/attainment.git
cd attainment
dotnet restore attainment.sln
dotnet build attainment.sln
dotnet test attainment.sln --no-build
dotnet run --project attainment/attainment.csproj
```

The SQLite database is created under `%USERPROFILE%\.attainment\attainment.db`. Debug builds seed sample data only when all domain tables are empty.

## OpenAI Configuration

1. Start the application.
2. Open the **Settings** tab.
3. Set `openai.key` to a valid API key.
4. Optionally change `openai.model`; the default is `gpt-4o-mini`.
5. Save the settings.

The API key is protected for the current Windows user with DPAPI before it is stored in SQLite. Do not commit API keys or include them in logs, screenshots, issues or test fixtures. The application should be treated as a local single-user tool; review the [OpenAI integration guide](docs/openai-integration.md) and the security notes in [the architecture documentation](docs/architecture.md) before distributing it.

## Database Migrations

Restore the repository-local EF Core tool:

```powershell
dotnet tool restore
```

Create and apply migrations from the repository root:

```powershell
dotnet ef migrations add MigrationName --project attainment
dotnet ef database update --project attainment
```

Never edit an applied migration. Add a new migration for every schema change.

## Repository Layout

```text
attainment/              WPF application
  Controls/              Reusable WPF controls and converters
  Infrastructure/        OpenAI, PDF and persistence-facing services
  Migrations/            EF Core schema history
  Models/                Persistence and exam models
  ViewModels/             Presentation state and commands
  Views/                  WPF pages and code-behind
attainment.test/         Automated tests
docs/                    Architecture and developer guides
```

## Documentation

- [Architecture](docs/architecture.md)
- [OpenAI integration](docs/openai-integration.md)
- [Testing and continuous integration](docs/testing.md)
- [Architecture review and WPF guide](docs/attainment-architecture-and-wpf-guide.html)
- [Contributing](CONTRIBUTING.md)

## Build and Publish

```powershell
dotnet build attainment.sln --configuration Release
dotnet test attainment.sln --configuration Release --no-build
dotnet publish attainment/attainment.csproj --configuration Release --runtime win-x64 --self-contained true
```

The publish output is placed below `attainment/bin/Release/net10.0-windows/win-x64/publish`.
