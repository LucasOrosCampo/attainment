namespace attainment.Application;

public sealed class DuplicateNameException(string entityName, string value)
    : InvalidOperationException($"A {entityName} named '{value}' already exists.");
