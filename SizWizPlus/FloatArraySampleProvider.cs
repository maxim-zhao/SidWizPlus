using System;
using NAudio.Wave;

namespace SidWizPlus
{
    internal class FloatArraySampleProvider : ISampleProvider
    {
        private readonly float[] _data;
        private int _index;

        public FloatArraySampleProvider(float[] data, int samplingRate)
        {
            _data = data;
            _index = 0;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(samplingRate, 2);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            offset += _index;
            count = Math.Min(_data.Length - offset, count);
            if (count > 0)
            {
                // Array.Copy(_data, offset, buffer, 0, count);
                // Can't use Array.Copy here because NAudio is cheating under the covers by having arrays of different types "pointing" at the same memory
                for (int i = 0; i < count; ++i)
                {
                    buffer[i] = _data[offset + i];
                }
            }
            else
            {
                count = 0;
            }

            _index += count;
            return count;
        }

        public WaveFormat WaveFormat { get; }
    }
}