namespace Engine.Tooling.Inspection;

public readonly record struct ComponentInspection(
    Type Type,
    object Value);