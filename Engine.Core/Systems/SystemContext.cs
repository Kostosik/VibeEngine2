using Engine.Core.Time;

namespace Engine.Core.Systems;

public readonly record struct SystemContext(
    TimeSnapshot Time);