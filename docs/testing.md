# Testing and Continuous Integration

## Local validation

Run the same core checks used by CI from the repository root:

```powershell
dotnet tool restore
dotnet restore attainment.sln
dotnet list attainment.sln package --vulnerable --include-transitive --no-restore
dotnet build attainment.sln --configuration Release --no-restore -warnaserror
dotnet test attainment.sln --configuration Release --no-build --no-restore
git diff --check
```

The test suite must not call OpenAI or require an API key. HTTP tests use a fake handler, and persistence tests use isolated temporary SQLite databases.

## Test boundaries

- Unit tests cover exam parsing and validation.
- HTTP contract tests cover request construction, response parsing and provider errors.
- SQLite integration tests cover application services, constraints and migration upgrades.
- ViewModel tests should cover long-running state transitions and cancellation.
- WPF UI automation is reserved for a small set of composition and navigation smoke tests.

## Continuous integration

`.github/workflows/ci.yml` runs on Windows for every pull request and every push to `main`. It restores the repository-local tools, audits dependencies, treats build warnings as errors and runs tests with coverage collection.

Keep the workflow deterministic: pin the SDK through `global.json`, avoid live external services and commit migrations with every schema change.

## Migration changes

For a schema change:

1. Restore `dotnet-ef` with `dotnet tool restore`.
2. Add the migration from the repository root.
3. Test an empty database.
4. Test an upgrade from the previous schema with representative data.
5. Review generated SQL for destructive operations.
