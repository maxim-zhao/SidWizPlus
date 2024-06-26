using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace LibSidWiz
{
    internal class SampleBuffer: IDisposable
    {
        private readonly WaveStream _reader;
        private readonly ISampleProvider _sampleProvider;

        private class Chunk
        {
            public long Offset;
            public long End;
            public float[] Buffer;

            public bool TryGet(long index, out float value)
            {
                if (index >= Offset && index < End)
                {
                    value = Buffer[index - Offset];
                    return true;
                }

                value = 0;
                return false;
            }
        }

        private readonly Chunk _chunk1;
        private readonly Chunk _chunk2;

        // 4 bytes per sample so this is 1MB
        // If we are rendering ~16 frames at once, we need (typically) 1323 samples per frame,
        // which is a window of 84KB. This is far from causing us trouble here.
        private const int ChunkSize = 256 * 1024 * 1;

        public long Count { get; }

        public int SampleRate { get; }

        public TimeSpan Length { get; }

        public float Max { get; private set; }

        public float Min { get; private set; }

        public SampleBuffer(string filename, Channel.Sides side, bool filter)
        {
            _reader = new AudioFileReader(filename);
            Count = _reader.Length * 8 / _reader.WaveFormat.BitsPerSample / _reader.WaveFormat.Channels;
            SampleRate = _reader.WaveFormat.SampleRate;
            Length = _reader.TotalTime;
            _sampleProvider = side switch
            {
                Channel.Sides.Left => _reader.ToSampleProvider().ToMono(1.0f, 0.0f),
                Channel.Sides.Right => _reader.ToSampleProvider().ToMono(0.0f, 1.0f),
                Channel.Sides.Mix => _reader.ToSampleProvider().ToMono(),
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
            };

            if (filter)
            {
                _sampleProvider = new HighPassSampleProvider(_sampleProvider);
            }

            _chunk1 = new Chunk
            {
                Buffer = new float[ChunkSize],
                Offset = -1,
                End = -1
            };
            _chunk2 = new Chunk
            {
                Buffer = new float[ChunkSize],
                Offset = -1,
                End = -1
            };
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public float this[long index]
        {
            get
            {
                // We may be accessed from multiple threads; we therefore need to lock access to avoid concurrent access.
                lock (this)
                {
                    if (_chunk1.TryGet(index, out var value) || _chunk2.TryGet(index, out value))
                    {
                        return value;
                    }

                    // If we are asked for sample 0, reset the buffers
                    if (index == 0)
                    {
                        _chunk1.Offset = _chunk1.End = _chunk2.Offset = _chunk2.End = -1;
                    }

                    // Else pick the lower index chunk to read into
                    var chunk = _chunk1.Offset < _chunk2.Offset ? _chunk1 : _chunk2;
                    // Pick the rounded offset
                    chunk.Offset = (index / ChunkSize) * ChunkSize;
                    chunk.End = chunk.Offset + ChunkSize;
                    _reader.Position = chunk.Offset * _reader.WaveFormat.BitsPerSample / 8 * _reader.WaveFormat.Channels;
                    _sampleProvider.Read(chunk.Buffer, 0, ChunkSize);
                    return chunk.Buffer[index - chunk.Offset];
                }
            }
        }

        public void Analyze()
        {
            Min = float.MaxValue;
            Max = float.MinValue;
            for (int i = 0; i < Count; ++i)
            {
                var sample = this[i];
                if (sample < Min)
                {
                    Min = sample;
                }
                if (sample > Max)
                {
                    Max = sample;
                }
            }
        }
    }

    internal class HighPassSampleProvider(ISampleProvider sampleProvider) : ISampleProvider
    {
        private readonly BiQuadFilter _filter = BiQuadFilter.HighPassFilter(sampleProvider.WaveFormat.SampleRate, 20, 1);

        public int Read(float[] buffer, int offset, int count)
        {
            int result = sampleProvider.Read(buffer, offset, count);

            // Apply the filter
            for (int i = 0; i < result; ++i)
            {
                buffer[i] = _filter.Transform(buffer[i]);
            }

            return result;
        }

        public WaveFormat WaveFormat => sampleProvider.WaveFormat;
    }
}
