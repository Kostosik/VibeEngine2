namespace Engine.Editor.Projects;

/// <summary>
/// Describes the user's request to create a new project.
/// The editor UI collects these values; a project creation service owns filesystem changes.
/// </summary>
public sealed record ProjectCreationRequest(
    string Name,
    string ParentDirectory,
    string ProjectDirectory);
