# Attainment Architecture

## Context

Attainment is a local, single-user Windows application for organizing study material and creating derived learning products. It combines a WPF presentation layer, local SQLite persistence, PDF processing and an OpenAI integration.

## Runtime Components

```text
WPF Views and Controls
        |
        v
ViewModels and Commands
        |
        v
Application Services
   |        |        |
   v        v        v
EF Core   OpenAI   PDF services
   |                 | \
SQLite            PdfPig QuestPDF
```

The target architecture keeps framework-specific concerns at the edges:

- **Presentation** owns WPF layout, binding, navigation state and user feedback.
- **Application services** coordinate use cases and transactions.
- **Domain models** define invariants independent of WPF, HTTP and EF Core.
- **Infrastructure** implements persistence, OpenAI and PDF ports.

## Composition Root

`App.xaml.cs` is the only composition root. It creates the Generic Host and registers services, ViewModels and Views. Application code should receive dependencies through constructors instead of reading `App.Services`.

Recommended lifetimes:

- Singleton: stateless, thread-safe clients and navigation/configuration services.
- Transient: Pages and ViewModels created for a navigation operation.
- Factory-created: `ApplicationDbContext`, with one short-lived context per operation.

## Persistence

The SQLite database lives at `%USERPROFILE%\.attainment\attainment.db`. EF Core migrations are the source of truth for the schema.

Persistence guidelines:

- Use `IDbContextFactory<ApplicationDbContext>` in desktop services.
- Use `AsNoTracking` and DTO projections for read-only lists.
- Back critical uniqueness and relationship rules with database constraints.
- Do not expose tracked EF entities as long-lived UI state.
- Apply migrations once during startup and surface a clear fatal startup error if migration fails.

## OpenAI Boundary

The OpenAI integration is an external boundary. It must:

- Use typed request and response models.
- Be asynchronous and cancellable.
- Configure explicit timeouts and file-size limits.
- Translate provider failures into application-level errors.
- Validate generated exam data before rendering or exporting it.
- Avoid logging API keys, source documents or complete model responses by default.

Tests replace the network client and never call the live API.

## PDF Boundary

PdfPig extracts source text and QuestPDF generates exams. PDF work is isolated behind `IPdf` so that ViewModels do not depend on either library.

PDF inputs are untrusted. Validate existence, extension and size, and handle malformed or encrypted files without crashing the UI.

## Security and Privacy

Study files and generated content may be sensitive. Distribution builds should document that selected material can be sent to OpenAI when the user starts exam generation.

Secrets should use a Windows-protected secret store rather than plain SQLite values. Logs must contain correlation and diagnostic metadata, not keys or full document content.

## Error Handling

Infrastructure exceptions should not be displayed directly. Services log technical details through `ILogger`; ViewModels expose stable, actionable messages suitable for users.

Long-running operations use a state model such as `Idle`, `Loading`, `Success`, `Empty` and `Error`, and offer cancellation where it adds value.

## Testing Strategy

1. Domain and parser unit tests.
2. Application service tests with fakes.
3. SQLite integration and migration tests.
4. HTTP contract tests with a fake handler.
5. ViewModel command and state tests.
6. A small number of WPF navigation/composition smoke tests.

## Architectural Decisions

### WPF remains the desktop framework

The application is Windows-only and already uses WPF successfully. Replacing it would add risk without addressing current design issues. Improvements should focus on consistent MVVM, service boundaries and testability.

### Vertical slices over technical mega-layers

As the application grows, group Page, ViewModel, service contract and DTOs by feature (`Subjects`, `Resources`, `Products`, `Exams`, `Settings`). Keep shared infrastructure and controls separate.

### Migrations over `EnsureCreated`

Migrations support schema evolution and existing installations. `EnsureCreated` is appropriate only for disposable databases and must not be combined with migrations for the application database.
