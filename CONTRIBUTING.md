# Contributing to Attainment

## Workflow

1. Start from an up-to-date `main` branch.
2. Create a focused branch such as `feature/resource-notes` or `fix/settings-reload`.
3. Keep each pull request limited to one coherent concern.
4. Add or update tests for behavior changes.
5. Update documentation when configuration, architecture or user workflows change.
6. Run the validation commands before opening a pull request.

## Validation

```powershell
dotnet restore attainment.sln
dotnet list attainment.sln package --vulnerable --include-transitive --no-restore
dotnet build attainment.sln --configuration Release --no-restore -warnaserror
dotnet test attainment.sln --configuration Release --no-build --no-restore
git diff --check
```

See [Testing and Continuous Integration](docs/testing.md) for test boundaries and migration validation.

When a change affects migrations, also start the app against a new database and update an existing development database.

## Architecture Rules

- Views contain layout and strictly visual behavior.
- ViewModels expose observable state and commands, without referencing concrete WPF controls.
- Application and infrastructure services own database, file system, network and PDF operations.
- Resolve dependencies through constructor injection. Avoid service locator calls from views and ViewModels.
- Create EF Core contexts through `IDbContextFactory<ApplicationDbContext>` and keep them short-lived.
- Use asynchronous APIs for database, network and file I/O. Propagate `CancellationToken` where practical.
- Validate external data, including AI output, before it enters the domain or persistence layers.
- Never store secrets in source control, logs, test data or exception messages.

## C# and XAML

- Follow `.editorconfig`.
- Enable nullable reference types in new projects and files.
- Prefer file-scoped namespaces in C#.
- Use descriptive names; avoid one-letter variables outside very small projections.
- Reuse application resources instead of duplicating colors and control templates.
- Include keyboard behavior and `AutomationProperties` for new interactive controls.
- Localize user-visible strings rather than scattering literals through code-behind.

## Tests

Prioritize tests around risk:

- Domain validation and parsing.
- EF Core queries, constraints and migrations using temporary SQLite databases.
- HTTP request/response handling with a fake `HttpMessageHandler`.
- ViewModel state transitions and commands.
- PDF smoke tests for representative Unicode and long-text inputs.

Tests must not call live external services or use a real API key.

## Pull Requests

Every pull request should explain:

- What changed.
- Why it changed and the root cause for fixes.
- User or developer impact.
- Validation performed.
- Migration, security or rollout considerations.
