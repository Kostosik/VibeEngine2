using Engine.Core.Diagnostics;

namespace Engine.Tooling.Console;

public readonly record struct ConsoleMessage(
    DateTime Timestamp,
    DiagnosticLevel Level,
    string Message,
    string? Code);