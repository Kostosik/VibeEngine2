# VibeEngine2

VibeEngine2 is a modular C# game-engine project built on .NET 8. Its architecture aims to keep the engine domain-agnostic: the engine provides mechanisms, while a game defines their meaning.

The repository includes runtime and simulation infrastructure, ECS, world/chunk and tile systems, graphics, input, UI, physics, navigation, audio, content and serialization, networking, animation, particles, diagnostics, and editor/sandbox applications.

> The project is under active development. The list of systems in the repository does not imply that every feature is production-ready.

## Requirements

- .NET 8 SDK
- Git
- Windows is the primary development environment for the current sandbox and graphics setup.

## Build and test

From the repository root:

```powershell
dotnet restore .\VibeEngine2.sln
dotnet build .\VibeEngine2.sln -c Release
dotnet test .\VibeEngine2.sln -c Release
```

A GitHub Actions workflow runs restore, Release build, and tests on pushes and pull requests targeting `master`. You can also start it manually from the **Actions** tab.

## Projects

The solution is organized into separate projects for engine subsystems and runnable applications. Key entry points include:

- **Game.Sandbox** — integration and experimentation app for exercising engine systems together.
- **Editor.Sandbox** — runnable editor application.
- **Engine.Runtime** — runtime composition and coordination.
- **Engine.Core / Engine.ECS** — shared primitives and entity-component-system infrastructure.
- **Engine.Graphics / Engine.Graphics.OpenGL** — graphics abstractions and the current OpenGL backend.
- **Engine.Tests** — automated engine tests.

Other projects contain focused subsystems such as physics, worlds/chunks, navigation, networking, audio, content, animation, and particles.

## Development workflow

The repository is developed using Git and Visual Studio. Before pushing a change, run the relevant tests locally. The CI workflow provides an additional automated check for the solution.

## Design principle

**The engine provides mechanisms; the game defines meaning.**

Avoid introducing game-specific concepts into engine-level APIs unless they are genuinely generic engine mechanisms.
