using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class TextureHandleTests
{
    [Fact]
    public void DefaultHandle_IsInvalid()
    {
        var handle =
            TextureHandle.Invalid;

        Assert.False(handle.IsValid);
        Assert.Equal(
            0u,
            handle.Value);
    }

    [Fact]
    public void NonZeroHandle_IsValid()
    {
        var handle =
            new TextureHandle(42);

        Assert.True(handle.IsValid);
        Assert.Equal(
            42u,
            handle.Value);
    }
}