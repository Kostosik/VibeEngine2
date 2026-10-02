using Engine.Audio;
using Engine.Audio.Data;
using Engine.Core.Assets;
using System.Buffers.Binary;

namespace Engine.Tests.Audio;

public sealed class AudioManagerTests
{
    [Fact]
    public void Load_CachesBufferForSamePath()
    {
        var path =
            new AssetPath("test.wav");

        var device =
            new TestAudioDevice();

        using var manager =
            new AudioManager(
                new InMemoryAssetSource(
                    path,
                    CreateTestWav()),
                device);

        var first =
            manager.Load(path);

        var second =
            manager.Load(path);

        Assert.Same(
            first,
            second);

        Assert.Equal(
            1,
            device.CreatedBufferCount);
    }

    [Fact]
    public void Play_CreatesAndStartsSource()
    {
        var path =
            new AssetPath("test.wav");

        var device =
            new TestAudioDevice();

        using var manager =
            new AudioManager(
                new InMemoryAssetSource(
                    path,
                    CreateTestWav()),
                device);

        var buffer =
            manager.Load(path);

        using var source =
            manager.Play(buffer);

        var testSource =
            Assert.IsType<TestAudioSource>(
                source);

        Assert.True(
            testSource.Played);

        Assert.Equal(
            1,
            device.CreatedSourceCount);
    }

    [Fact]
    public void Dispose_DisposesLoadedBuffersBeforeDevice()
    {
        var path =
            new AssetPath("test.wav");

        var device =
            new TestAudioDevice();

        var manager =
            new AudioManager(
                new InMemoryAssetSource(
                    path,
                    CreateTestWav()),
                device);

        var buffer =
            manager.Load(path);

        var testBuffer =
            Assert.IsType<TestAudioBuffer>(
                buffer);

        manager.Dispose();

        Assert.True(
            testBuffer.Disposed);

        Assert.True(
            device.Disposed);

        Assert.Equal(
            1,
            device.BufferDisposalsBeforeDeviceDispose);
    }

    private static byte[] CreateTestWav()
    {
        const int sampleRate = 44100;
        const ushort channels = 1;
        const ushort bitsPerSample = 16;
        const ushort blockAlign = 2;
        const uint byteRate = sampleRate * blockAlign;

        var samples =
            new byte[]
            {
                0,
                0,
                0,
                0
            };

        var fileSize =
            44 +
            samples.Length;

        var wav =
            new byte[fileSize];

        wav[0] = (byte)'R';
        wav[1] = (byte)'I';
        wav[2] = (byte)'F';
        wav[3] = (byte)'F';

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(4),
            (uint)(fileSize - 8));

        wav[8] = (byte)'W';
        wav[9] = (byte)'A';
        wav[10] = (byte)'V';
        wav[11] = (byte)'E';

        wav[12] = (byte)'f';
        wav[13] = (byte)'m';
        wav[14] = (byte)'t';
        wav[15] = (byte)' ';

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(16),
            16);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(20),
            1);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(22),
            channels);

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(24),
            sampleRate);

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(28),
            byteRate);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(32),
            blockAlign);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(34),
            bitsPerSample);

        wav[36] = (byte)'d';
        wav[37] = (byte)'a';
        wav[38] = (byte)'t';
        wav[39] = (byte)'a';

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(40),
            (uint)samples.Length);

        samples.CopyTo(
            wav.AsSpan(44));

        return wav;
    }

    private sealed class InMemoryAssetSource :
        IAssetSource
    {
        private readonly AssetPath _path;
        private readonly byte[] _data;

        public InMemoryAssetSource(
            AssetPath path,
            byte[] data)
        {
            _path = path;
            _data = data;
        }

        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            if (path != _path)
            {
                throw new FileNotFoundException(
                    $"Asset '{path}' was not found.");
            }

            return _data;
        }
    }

    private sealed class TestAudioDevice :
        IAudioDevice
    {
        private readonly List<TestAudioBuffer> _buffers = [];

        public int CreatedBufferCount { get; private set; }

        public int CreatedSourceCount { get; private set; }

        public bool Disposed { get; private set; }

        public int BufferDisposalsBeforeDeviceDispose { get; private set; }

        public IAudioBuffer CreateBuffer(
            AudioData data)
        {
            var buffer =
                new TestAudioBuffer(
                    this);

            _buffers.Add(buffer);

            CreatedBufferCount++;

            return buffer;
        }

        public IAudioSource CreateSource(
            IAudioBuffer buffer)
        {
            CreatedSourceCount++;

            return new TestAudioSource();
        }

        public IAudioListener CreateListener()
        {
            return new TestAudioListener();
        }

        internal void OnBufferDisposed()
        {
            if (!Disposed)
            {
                BufferDisposalsBeforeDeviceDispose++;
            }
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class TestAudioBuffer :
        IAudioBuffer
    {
        private readonly TestAudioDevice _device;

        public bool Disposed { get; private set; }

        public TestAudioBuffer(
            TestAudioDevice device)
        {
            _device = device;
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;

            _device.OnBufferDisposed();
        }
    }

    private sealed class TestAudioSource :
        IAudioSource
    {
        public bool Played { get; private set; }

        public void Play()
        {
            Played = true;
        }

        public void Pause()
        {
        }

        public void Stop()
        {
        }

        public void Rewind()
        {
        }

        public void SetGain(
            float gain)
        {
        }

        public void SetPitch(
            float pitch)
        {
        }

        public void SetLooping(
            bool looping)
        {
        }

        public void SetPosition(
            Engine.Core.Math.Vector3 position)
        {
        }

        public void SetVelocity(
            Engine.Core.Math.Vector3 velocity)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TestAudioListener :
        IAudioListener
    {
        public void SetPosition(
            Engine.Core.Math.Vector3 position)
        {
        }

        public void SetVelocity(
            Engine.Core.Math.Vector3 velocity)
        {
        }

        public void SetGain(
            float gain)
        {
        }

        public void SetOrientation(
            Engine.Core.Math.Vector3 forward,
            Engine.Core.Math.Vector3 up)
        {
        }

        public void Dispose()
        {
        }
    }
}