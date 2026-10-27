using Engine.Animation;
using Engine.Editor.Documents;

namespace Engine.Tests.Editor;

public sealed class AnimationClipDocumentTests
{
    [Fact]
    public void Open_LoadsClipAndStartsClean()
    {
        var directory = Directory.CreateTempSubdirectory();

        var path = Path.Combine(
            directory.FullName,
            "Idle.animation.json");

        File.WriteAllText(
            path,
            new AnimationClipJsonSerializer().Serialize(
                CreateClip(10.0f)));

        var document = AnimationClipDocument.Open(path);

        Assert.Equal(
            AnimationClipDocument.TypeId,
            document.DocumentTypeId);

        Assert.Equal(
            Path.GetFullPath(path),
            document.FilePath);

        Assert.Equal(
            "Idle.animation.json",
            document.DisplayName);

        Assert.Equal(1, document.Clip.TrackCount);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void UpdateClip_MarksDirtyAndSavePersistsChanges()
    {
        var directory = Directory.CreateTempSubdirectory();

        var path = Path.Combine(
            directory.FullName,
            "Idle.animation.json");

        File.WriteAllText(
            path,
            new AnimationClipJsonSerializer().Serialize(
                CreateClip(10.0f)));

        var document = AnimationClipDocument.Open(path);
        var changedCount = 0;

        document.Changed += () => changedCount++;

        document.UpdateClip(CreateClip(20.0f));

        Assert.True(document.IsDirty);
        Assert.Equal(1, changedCount);

        document.Save();

        Assert.False(document.IsDirty);
        Assert.Equal(2, changedCount);

        var reloaded = AnimationClipDocument.Open(path);

        Assert.Equal(
            20.0f,
            reloaded.Clip.Sample<float>(
                new AnimationTrackId("opacity"),
                2.0));
    }

    [Fact]
    public void Open_RejectsUnsupportedFileExtension()
    {
        var directory = Directory.CreateTempSubdirectory();

        var path = Path.Combine(
            directory.FullName,
            "Idle.json");

        File.WriteAllText(path, "{}");

        Assert.Throws<ArgumentException>(
            () => AnimationClipDocument.Open(path));
    }

    private static AnimationClip CreateClip(float endValue)
    {
        var clip = new AnimationClip(2.0);

        clip.AddTrack(
            new AnimationTrackId("opacity"),
            new AnimationTrack<float>(
                new[]
                {
                    new AnimationKeyframe<float>(0.0, 0.0f),
                    new AnimationKeyframe<float>(2.0, endValue)
                },
                new FloatLinearAnimationInterpolator()));

        return clip;
    }
}