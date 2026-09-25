namespace Engine.Core.Diagnostics;

public readonly record struct Diagnostic(
    DiagnosticLevel Level,
    string Code,
    string Message);

