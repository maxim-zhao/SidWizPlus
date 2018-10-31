using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms.Design;
using LibSidWiz.Triggers;
using NAudio.Dsp;
using NAudio.Wave;

namespace LibSidWiz
{
    /// <summary>
    /// Wraps a single "voice", and also deals with loading the data into memory
    /// </summary>
    public class Channel: INotifyPropertyChanged
    {
        private IList<float> _samples;
        private string _filename;
        private ITriggerAlgorithm _algorithm;
        private int _triggerLookaheadFrames;
        private Color _color = Color.White;
        private string _name = "";
        private float _lineWidth = 3;
        private float _highPassFilterFrequency = -1;
        private float _scale = 1.0f;
        private float _max;
        private TimeSpan _length;
        private int _sampleRate;

        public void LoadData()
        {
            Console.WriteLine($"- Reading {Filename}");
            float[] buffer;
            using (var reader = new WaveFileReader(Filename))
            {
                SampleRate = reader.WaveFormat.SampleRate;
                Length = TimeSpan.FromSeconds((double) reader.SampleCount / reader.WaveFormat.SampleRate);

                // We read the file and convert to mono
                buffer = new float[reader.SampleCount];
                reader.ToSampleProvider().ToMono().Read(buffer, 0, (int) reader.SampleCount);
            }

            // We don't care about ones where the samples are all equal
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (buffer.Length == 0 || buffer.All(s => s == buffer[0]))
            {
                Console.WriteLine($"- {Filename} is silent");
                // So we skip steps here
                _samples = null;
                return;
            }

            if (HighPassFilterFrequency > 0)
            {
                Console.WriteLine($"- High-pass filtering {Filename}");
                // Apply the high pass filter
                var filter = BiQuadFilter.HighPassFilter(SampleRate, HighPassFilterFrequency, 1);
                for (int i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = filter.Transform(buffer[i]);
                }
            }

            Max = buffer.Select(Math.Abs).Max();
            Console.WriteLine($"- Peak sample amplitude for {Filename} is {Max}");

            _samples = buffer;
        }

        [Category("Source")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [Description("The filename to be rendered")]
        public string Filename
        {
            get => _filename;
            set
            {
                _filename = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filename)));
                if (_filename != "" && string.IsNullOrEmpty(_name))
                {
                    Name = GuessNameFromMultidumperFilename(_filename);
                }
            }
        }

        [Category("Triggering")]
        [Description("The algorithm to use for rendering")]
        [TypeConverter(typeof(TriggerAlgorithmTypeConverter))]
        public ITriggerAlgorithm Algorithm
        {
            get => _algorithm;
            set
            {
                _algorithm = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Algorithm)));
            }
        }

        [Category("Triggering")]
        [Description("How many frames to allow the triggering algorithm to look ahead. Zero means only look within the current frame. Set to larger numbers to support sync to low frequencies, but too large numbers can cause erroneous matches.")]
        public int TriggerLookaheadFrames
        {
            get => _triggerLookaheadFrames;
            set
            {
                _triggerLookaheadFrames = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filename)));
            }
        }

        [Category("Display")]
        [Description("The line colour")]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }

        [Category("Display")]
        [Description("The label for the channel")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        [Category("Display")]
        [Description("The line width, in pixels. Fractional values are supported.")]
        public float LineWidth
        {
            get => _lineWidth;
            set
            {
                _lineWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filename)));
            }
        }

        [Category("Adjustment")]
        [Description("High pass frequency adjustment. -1 means disabled. Use a value like 10 to remove DC offsets.")]
        public float HighPassFilterFrequency
        {
            get => _highPassFilterFrequency;
            set
            {
                _highPassFilterFrequency = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighPassFilterFrequency)));
            }
        }

        [Category("Adjustment")]
        [Description("Vertical scaling. This may be set by the auto-scaler.")]
        public float Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Scale)));
            }
        }

        [Category("Data information")]
        [Description("Peak amplitude for the channel")]
        public float Max
        {
            get => _max;
            private set
            {
                _max = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Max)));
            }
        }

        [Category("Data information")]
        [Description("Number of samples in the channel")]
        public int SampleCount => _samples?.Count ?? 0;

        [Category("Data information")]
        [Description("Duration of the channel")]
        public TimeSpan Length
        {
            get => _length;
            private set
            {
                _length = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
            }
        }

        [Category("Data information")]
        [Description("Sampling rate of the channel")]
        public int SampleRate
        {
            get => _sampleRate;
            private set
            {
                _sampleRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SampleRate)));
            }
        }

        public float GetSample(int sampleIndex)
        {
            return sampleIndex < 0 || sampleIndex >= _samples.Count ? 0 : _samples[sampleIndex] * Scale;
        }

        public int GetTriggerPoint(int frameIndexSamples, int frameSamples)
        {
            return Algorithm.GetTriggerPoint(this, frameIndexSamples, frameIndexSamples + frameSamples * (TriggerLookaheadFrames + 1));
        }

        public static string GuessNameFromMultidumperFilename(string filename)
        {
            var namePart = Path.GetFileNameWithoutExtension(filename);
            try
            {
                if (namePart == null)
                {
                    return filename;
                }

                var index = namePart.IndexOf(" - YM2413 #", StringComparison.Ordinal);
                if (index > -1)
                {
                    index = int.Parse(namePart.Substring(index + 11));
                    if (index < 9)
                    {
                        return $"YM2413 tone {index + 1}";
                    }

                    switch (index)
                    {
                        case 9: return "YM2413 Bass Drum";
                        case 10: return "YM2413 Snare Drum";
                        case 11: return "YM2413 Tom-Tom";
                        case 12: return "YM2413 Cymbal";
                        case 13: return "YM2413 Hi-Hat";
                    }
                }

                index = namePart.IndexOf(" - SEGA PSG #", StringComparison.Ordinal);
                if (index > -1)
                {
                    if (int.TryParse(namePart.Substring(index + 13), out index))
                    {
                        switch (index)
                        {
                            case 0:
                            case 1:
                            case 2:
                                return $"Sega PSG Square {index + 1}";
                            case 3:
                                return "Sega PSG Noise";
                        }
                    }
                }

                // Guess it's the bit after the last " - "
                index = namePart.LastIndexOf(" - ", StringComparison.Ordinal);
                if (index > -1)
                {
                    return namePart.Substring(index + 3);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error guessing channel name for {filename}: {ex}");
            }

            // Default to just the filename
            return namePart;
        }

        /// <summary>
        /// This allows us to use a property grid to select a trigger algorithm
        /// </summary>
        public class TriggerAlgorithmTypeConverter: StringConverter
        {
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(
                    Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(t => typeof(ITriggerAlgorithm).IsAssignableFrom(t) && t != typeof(ITriggerAlgorithm))
                        .Select(t => t.Name)
                        .ToList());
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    var type = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .FirstOrDefault(t => typeof(ITriggerAlgorithm).IsAssignableFrom(t) && t.Name.ToLowerInvariant().Equals(value.ToString().ToLowerInvariant()));
                    if (type != null)
                    {
                        return Activator.CreateInstance(type) as ITriggerAlgorithm;
                    }
                }

                return base.ConvertFrom(context, culture, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}