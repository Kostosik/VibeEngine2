using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class PingPongRenderTargetsTests
{
    [Fact]
    public void SwapAndResize_KeepTwoDistinctTargets()
    {
        var manager = new TestRenderTargetManager();

        var buffers = new PingPongRenderTargets(
            manager,
            new RenderTargetDescription(1280, 720));

        var initialRead = buffers.ReadTarget;
        var initialWrite = buffers.WriteTarget;

        Assert.NotEqual(initialRead, initialWrite);

        buffers.Swap();

        Assert.Equal(initialWrite, buffers.ReadTarget);
        Assert.Equal(initialRead, buffers.WriteTarget);

        buffers.Resize(640, 360);

        Assert.Equal(640, buffers.Description.Width);
        Assert.Equal(360, buffers.Description.Height);

        Assert.NotEqual(initialRead, buffers.ReadTarget);
        Assert.NotEqual(buffers.ReadTarget, buffers.WriteTarget);

        var finalRead = buffers.ReadTarget;
        var finalWrite = buffers.WriteTarget;

        buffers.Dispose();

        Assert.False(manager.Exists(finalRead));
        Assert.False(manager.Exists(finalWrite));
    }

    private sealed class TestRenderTargetManager : IRenderTargetManager
    {
        private readonly Dictionary<
            RenderTargetHandle,
            (RenderTargetDescription Description, TextureHandle Texture)> _targets = new();

        private uint _nextTarget = 1;
        private uint _nextTexture = 100;

        public RenderTargetHandle Create(
            RenderTargetDescription description)
        {
            description.Validate();

            var target = new RenderTargetHandle(_nextTarget++);
            var texture = new TextureHandle(_nextTexture++);

            _targets.Add(
                target,
                (description, texture));

            return target;
        }

        public bool Exists(RenderTargetHandle target) =>
            _targets.ContainsKey(target);

        public RenderTargetDescription GetDescription(
            RenderTargetHandle target) =>
            GetEntry(target).Description;

        public TextureHandle GetColorTexture(
            RenderTargetHandle target) =>
            GetEntry(target).Texture;

        public void Destroy(RenderTargetHandle target)
        {
            _targets.Remove(target);
        }

        public void Resize(
            RenderTargetHandle target,
            int width,
            int height)
        {
            var entry = GetEntry(target);

            _targets[target] = (
                new RenderTargetDescription(
                    width,
                    height,
                    entry.Description.Format),
                entry.Texture);
        }

        private (
            RenderTargetDescription Description,
            TextureHandle Texture) GetEntry(
                RenderTargetHandle target)
        {
            if (!_targets.TryGetValue(target, out var entry))
            {
                throw new KeyNotFoundException();
            }

            return entry;
        }
    }
}