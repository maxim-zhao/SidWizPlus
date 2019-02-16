using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using LibSidWiz.Triggers;
using Newtonsoft.Json;

namespace LibSidWiz
{
    /// <summary>
    /// Wraps a single "voice", and also deals with loading the data into memory
    /// </summary>
    public class Channel: IDisposable
    {
        private SampleBuffer _samples;
        private string _filename;
        private ITriggerAlgorithm _algorithm;
        private int _triggerLookaheadFrames;
        private Color _lineColor = Color.White;
        private string _name = "";
        private float _lineWidth = 3;
        //private float _highPassFilterFrequency = -1;
        private float _scale = 1.0f;
        private int _viewWidthInSamples = 1500;
        private Color _fillColor = Color.Transparent;
        private float _zeroLineWidth;
        private Color _zeroLineColor = Color.Transparent;
        private Font _labelFont;
        private Color _labelColor = Color.Transparent;
        private Color _borderColor = Color.Transparent;
        private float _borderWidth;

        public event Action<Channel, bool> Changed;

        public Task<bool> LoadDataAsync(CancellationToken token = new CancellationToken())
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(Filename))
                    {
                        _samples = null;
                        SampleCount = 0;
                        Max = 0;
                        Length = TimeSpan.Zero;
                        Loading = false;
                        IsEmpty = true;
                        return false;
                    }

                    IsEmpty = false;

                    Console.WriteLine($"- Reading {Filename}");
                    _samples = new SampleBuffer(Filename);
                    SampleRate = _samples.SampleRate;
                    Length = _samples.Length;

                    token.ThrowIfCancellationRequested();

                    _samples.Analyze();

                    // We don't care about ones where the samples are all equal
                    if (Math.Abs(_samples.Min - _samples.Max) < 0.0001)
                    {
                        Console.WriteLine($"- {Filename} is silent");
                        // So we skip steps here
                        _samples.Dispose();
                        _samples = null;
                        SampleCount = 0;
                        Max = 0;
                        Loading = false;
                        return false;
                    }

                    SampleCount = _samples.Count;

                    /*
                    if (HighPassFilterFrequency > 0)
                    {
                        // TODO: this only happens on load...
                        // TODO: this won't work with random access...
                        Console.WriteLine($"- High-pass filtering {Filename}");
                        // Apply the high pass filter
                        var filter = BiQuadFilter.HighPassFilter(SampleRate, HighPassFilterFrequency, 1);
                        for (int i = 0; i < buffer.Length; ++i)
                        {
                            buffer[i] = filter.Transform(buffer[i]);
                        }
                    }
                    */

                    token.ThrowIfCancellationRequested();

                    Max = Math.Max(Math.Abs(_samples.Max), Math.Abs(_samples.Min));

                    Console.WriteLine($"- Peak sample amplitude for {Filename} is {Max}");

                    Loading = false;
                    return true;
                }
                catch (TaskCanceledException)
                {
                    // Blank out if cancelled
                    Max = 0;
                    SampleRate = 0;
                    Length = TimeSpan.Zero;
                    _samples?.Dispose();
                    _samples = null;
                    Loading = false;
                    return false;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.ToString();
                    Max = 0;
                    SampleRate = 0;
                    Length = TimeSpan.Zero;
                    _samples?.Dispose();
                    _samples = null;
                    Loading = false;
                    return false;
                }
                finally
                {
                    Changed?.Invoke(this, false);
                }
            }, token);
        }

        [Category("Data information")]
        [Description("The full text of any error message when loading the file")]
        public string ErrorMessage { get; private set; }

        [Category("Source")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [Description("The filename to be rendered")]
        public string Filename
        {
            get => _filename;
            set
            {
                _filename = value;
                Changed?.Invoke(this, true);
                if (_filename != "" && string.IsNullOrEmpty(_name))
                {
                    Name = GuessNameFromMultidumperFilename(_filename);
                }
            }
        }

        [Category("Triggering")]
        [Description("The algorithm to use for rendering")]
        [TypeConverter(typeof(TriggerAlgorithmTypeConverter))]
        [JsonConverter(typeof(TriggerAlgorithmJsonConverter))]
        public ITriggerAlgorithm Algorithm
        {
            get => _algorithm;
            set
            {
                _algorithm = value;
                Changed?.Invoke(this, false);
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
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The line colour")]
        public Color LineColor
        {
            get => _lineColor;
            set
            {
                _lineColor = value;
                Changed?.Invoke(this, false);
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
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The fill colour. Set to transparent to have no fill.")]
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                _fillColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The width of the zero line")]
        public float ZeroLineWidth
        {
            get => _zeroLineWidth;
            set
            {
                _zeroLineWidth = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The color of the zero line")]
        public Color ZeroLineColor
        {
            get => _zeroLineColor;
            set
            {
                _zeroLineColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The color of the border")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The width of the border")]
        public float BorderWidth
        {
            get => _borderWidth;
            set
            {
                _borderWidth = value;
                Changed?.Invoke(this, false);
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
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The font for the channel label")]
        public Font LabelFont
        {
            get => _labelFont;
            set
            {
                _labelFont = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Display")]
        [Description("The color for the channel label")]
        public Color LabelColor
        {
            get => _labelColor;
            set
            {
                _labelColor = value;
                Changed?.Invoke(this, false);
            }
        }

        /*
        [Category("Adjustment")]
        [Description("High pass frequency adjustment. -1 means disabled. Use a value like 10 to remove DC offsets.")]
        public float HighPassFilterFrequency
        {
            get => _highPassFilterFrequency;
            set
            {
                _highPassFilterFrequency = value;
                Changed?.Invoke(this, false);
            }
        }
        */

        [Category("Adjustment")]
        [Description("Vertical scaling. This may be set by the auto-scaler.")]
        public float Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Adjustment")]
        [Description("View width, in ms")]
        [JsonIgnore]
        public float ViewWidthInMilliseconds
        {
            get => SampleRate == 0 ? 0 : (float)_viewWidthInSamples * 1000 / SampleRate;
            set
            {
                _viewWidthInSamples = (int) (value / 1000 * SampleRate);
                Changed?.Invoke(this, false);
            }
        }

        [Category("Adjustment")]
        [Description("View width, in samples")]
        public int ViewWidthInSamples
        {
            get => _viewWidthInSamples;
            set
            {
                _viewWidthInSamples = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Data information")]
        [Description("Peak amplitude for the channel")]
        [JsonIgnore]
        public float Max { get; private set; }

        [Browsable(false)]
        [JsonIgnore]
        public int SampleCount { get; private set; }

        [Category("Data information")]
        [Description("Duration of the channel")]
        [JsonIgnore]
        public TimeSpan Length { get; private set; }

        [Category("Data information")]
        [Description("Sampling rate of the channel")]
        [JsonIgnore]
        public int SampleRate { get; private set; }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        [Browsable(false)]
        [JsonIgnore]
        public bool IsSilent => Max == 0.0;

        [Browsable(false)]
        [JsonIgnore]
        public bool Loading { get; private set; } = true;

        [Browsable(false)]
        [JsonIgnore]
        public bool IsEmpty { get; private set; }

        internal int X { get; set; }
        internal int Y { get; set; }

        internal int Width { get; set; }
        internal int Height { get; set; }

        internal float GetSample(int sampleIndex)
        {
            return sampleIndex < 0 || sampleIndex >= _samples.Count ? 0 : _samples[sampleIndex] * Scale;
        }

        internal int GetTriggerPoint(int frameIndexSamples, int frameSamples, int previousTriggerPoint)
        {
            return Algorithm.GetTriggerPoint(this, frameIndexSamples, frameIndexSamples + frameSamples * (TriggerLookaheadFrames + 1), previousTriggerPoint);
        }

        internal static string GuessNameFromMultidumperFilename(string filename)
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

                index = namePart.IndexOf(" - SN76489 #", StringComparison.Ordinal);
                if (index > -1)
                {
                    if (int.TryParse(namePart.Substring(index + 12), out index))
                    {
                        switch (index)
                        {
                            case 0:
                            case 1:
                            case 2:
                                return $"SN76489 Square {index + 1}";
                            case 3:
                                return "SN76489 Noise";
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

        public class TriggerAlgorithmJsonConverter: JsonConverter<ITriggerAlgorithm>
        {
            public override void WriteJson(JsonWriter writer, ITriggerAlgorithm value, JsonSerializer serializer)
            {
                writer.WriteValue(value.GetType().Name);
            }

            public override ITriggerAlgorithm ReadJson(JsonReader reader, Type objectType, ITriggerAlgorithm existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var type = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .FirstOrDefault(t => 
                        typeof(ITriggerAlgorithm).IsAssignableFrom(t) && 
                        t.Name.ToLowerInvariant().Equals(reader.Value.ToString().ToLowerInvariant()));
                if (type != null)
                {
                    return Activator.CreateInstance(type) as ITriggerAlgorithm;
                }

                return existingValue;
            }
        }

        public void Dispose()
        {
            _samples?.Dispose();
            _labelFont?.Dispose();
        }
    }
}