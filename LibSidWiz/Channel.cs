using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using LibSidWiz.Triggers;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LibSidWiz
{
    /// <summary>
    /// Wraps a single "voice", and also deals with loading the data into memory
    /// </summary>
    public class Channel: IDisposable
    {
        private readonly bool _autoReloadOnSettingChanged;
        private SampleBuffer _samples;
        private SampleBuffer _samplesForTrigger;
        private string _filename;
        private string _externalTriggerFilename;
        private ITriggerAlgorithm _algorithm;
        private int _triggerLookaheadFrames; // Default to current frame only
        private int _triggerLookaheadOnFailureFrames = 2; // Default to 2 frames ahead
        private Color _lineColor = Color.White;
        private string _label = "";
        private float _lineWidth = 3;
        private float _scale = 1.0f;
        private int _viewWidthInSamples = 1500;
        private Color _fillColor = Color.Transparent;
        private float _zeroLineWidth;
        private Color _zeroLineColor = Color.Transparent;
        private Font _labelFont;
        private Color _labelColor = Color.Transparent;
        private Color _borderColor = Color.Transparent;
        private float _borderWidth;
        private ContentAlignment _labelAlignment = ContentAlignment.TopLeft;
        private Padding _labelMargins = new(0, 0, 0, 0);
        private bool _invertedTrigger;
        private bool _borderEdges = true;
        private Color _backgroundColor = Color.Transparent;
        private bool _clip;
        private Sides _side = Sides.Mix;
        private bool _smoothLines = true;
        private bool _filter;
        private bool _renderIfSilent;
        private double _fillBase;

        public Channel(bool autoReloadOnSettingChanged)
        {
            _autoReloadOnSettingChanged = autoReloadOnSettingChanged;
        }

        public enum Sides
        {
            Left,
            Right,
            Mix
        }

        public event Action<Channel, bool> Changed;

        public Task<bool> LoadDataAsync(CancellationToken token = new())
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    ErrorMessage = "";

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
                    Loading = true;

                    Console.WriteLine($"- Reading {Filename}");
                    _samples = new SampleBuffer(Filename, Side, HighPassFilter);
                    SampleRate = _samples.SampleRate;
                    Length = _samples.Length;

                    token.ThrowIfCancellationRequested();

                    _samples.Analyze();

                    SampleCount = _samples.Count;

                    token.ThrowIfCancellationRequested();

                    Max = Math.Max(Math.Abs(_samples.Max), Math.Abs(_samples.Min));

                    Console.WriteLine($"- Peak sample amplitude for {Filename} is {Max}");

                    // Point at the same SampleBuffer
                    _samplesForTrigger = string.IsNullOrEmpty(ExternalTriggerFilename) 
                        ? _samples 
                        : new SampleBuffer(ExternalTriggerFilename, Side, HighPassFilter);

                    Loading = false;
                    return true;
                }
                catch (TaskCanceledException)
                {
                    // Blank out if cancelled
                    Max = 0;
                    SampleRate = 0;
                    Length = TimeSpan.Zero;
                    if (_samplesForTrigger != _samples)
                    {
                        _samplesForTrigger?.Dispose();
                    }
                    _samplesForTrigger = null;
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
                    if (_samplesForTrigger != _samples)
                    {
                        _samplesForTrigger?.Dispose();
                    }
                    _samplesForTrigger = null;
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

        [Category("Data")]
        [Description("The full text of any error message when loading the file")]
        [JsonIgnore]
        public string ErrorMessage { get; private set; }

        [Category("Data")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [Description("The filename to be rendered")]
        public string Filename
        {
            get => _filename;
            set
            {
                bool needReload = value != _filename;
                _filename = value;
                Changed?.Invoke(this, needReload);
                if (_filename != "" && string.IsNullOrEmpty(_label))
                {
                    Label = GuessNameFromMultidumperFilename(_filename);
                }
            }
        }

        [Category("Triggering")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [Description("The filename to use for oscilloscope triggering. Leave blank to use the channel's sound data.")]
        public string ExternalTriggerFilename
        {
            get => _externalTriggerFilename;
            set
            {
                bool needReload = value != _externalTriggerFilename;
                _externalTriggerFilename = value;
                // Change algorithm to RisingEdgeTrigger when using an external trigger
                _algorithm = new RisingEdgeTrigger();
                Changed?.Invoke(this, needReload);
            }
        }

        [Category("Data")]
        [Description("The channel to use from the file (if stereo)")]
        public Sides Side
        {
            get => _side;
            set
            {
                bool needReload = value != _side;
                _side = value;
                Changed?.Invoke(this, needReload);
                if (_autoReloadOnSettingChanged)
                {
                    LoadDataAsync();
                }
            }
        }

        [Category("Data")]
        [Description("If enabled, high pass filtering will be used to remove DC offsets")]
        public bool HighPassFilter
        {
            get => _filter;
            set
            {
                bool needReload = value != _filter;
                _filter = value;
                Changed?.Invoke(this, needReload);
                if (_autoReloadOnSettingChanged)
                {
                    LoadDataAsync();
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

        [Category("Triggering")]
        [Description("How many frames to allow the triggering algorithm to look ahead, when nothing is found with the default lookahead.")]
        public int TriggerLookaheadOnFailureFrames
        {
            get => _triggerLookaheadOnFailureFrames;
            set
            {
                _triggerLookaheadOnFailureFrames = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("The line colour")]
        [Editor(typeof(MyColorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MyColorConverter))]
        public Color LineColor
        {
            get => _lineColor;
            set
            {
                _lineColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
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

        [Category("Appearance")]
        [Description("The fill colour. Set to transparent to have no fill.")]
        [Editor(typeof(MyColorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MyColorConverter))]
        public Color FillColor
        {
            get => _fillColor;
            set
            {
                _fillColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("The base of the fill. Set to 0 for the centre line, -1 to fill from the bottom and 1 for the top. Other values also work.")]
        public double FillBase
        {
            get => _fillBase;
            set
            {
                _fillBase = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("Whether to draw lines pixelated (false) or anti-aliased (true)")]
        public bool SmoothLines
        {
            get => _smoothLines;
            set
            {
                _smoothLines = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
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

        [Category("Appearance")]
        [Description("The color of the zero line")]
        [Editor(typeof(MyColorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MyColorConverter))]
        public Color ZeroLineColor
        {
            get => _zeroLineColor;
            set
            {
                _zeroLineColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("The color of the border")]
        [Editor(typeof(MyColorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MyColorConverter))]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
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

        [Category("Appearance")]
        [Description("Whether to draw the outer edges of any border boxes")]
        public bool BorderEdges
        {
            get => _borderEdges;
            set
            {
                _borderEdges = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("A background colour for the channel. This is layered above any background image, and can be transparent.")]
        [Editor(typeof(MyColorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MyColorConverter))]
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("The label for the channel")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
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

        [Category("Appearance")]
        [Description("The color for the channel label")]
        [Editor(typeof(MyColorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(MyColorConverter))]
        public Color LabelColor
        {
            get => _labelColor;
            set
            {
                _labelColor = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("The alignment for the channel label")]
        public ContentAlignment LabelAlignment
        {
            get => _labelAlignment;
            set
            {
                _labelAlignment = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Appearance")]
        [Description("The margins for the chanel label")]
        public Padding LabelMargins
        {
            get => _labelMargins;
            set
            {
                _labelMargins = value;
                Changed?.Invoke(this, false);
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
                Changed?.Invoke(this, false);
            }
        }

        [Category("Adjustment")]
        [Description("Whether to constrain the waveform to its screen area when scaled past 100%")]
        public bool Clip
        {
            get => _clip;
            set
            {
                _clip = value;
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

        [Category("Triggering")]
        [Description("Set to true to trigger in the opposite direction")]
        // ReSharper disable once MemberCanBePrivate.Global
        public bool InvertedTrigger
        {
            get => _invertedTrigger;
            set
            {
                _invertedTrigger = value;
                Changed?.Invoke(this, false);
            }
        }

        [Category("Data")]
        [Description("Peak amplitude for the channel")]
        [JsonIgnore]
        public float Max { get; private set; }

        [Browsable(false)]
        [JsonIgnore]
        public long SampleCount { get; private set; }

        [Category("Data")]
        [Description("Duration of the channel")]
        [JsonIgnore]
        public TimeSpan Length { get; private set; }

        [Category("Data")]
        [Description("Sampling rate of the channel")]
        [JsonIgnore]
        public int SampleRate { get; private set; }

        [Category("Appearance")]
        [Description("Whether to render silent channels normally. If false, a warning message is shown instead.")]
        public bool RenderIfSilent
        {
            get => _renderIfSilent;
            set
            {
                _renderIfSilent = value;
                Changed?.Invoke(this, false);
            }
        }

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

        [Browsable(false)]
        [JsonIgnore]
        internal Rectangle Bounds { get; set; }

        internal float GetSample(int sampleIndex, bool forTrigger = true)
        {
            var source = forTrigger ? _samplesForTrigger : _samples;
            return sampleIndex < 0 || sampleIndex >= source.Count ? 0 : source[sampleIndex] * Scale * (forTrigger && InvertedTrigger ? -1 : 1);
        }

        internal int GetTriggerPoint(int frameIndexSamples, int frameSamples, int previousTriggerPoint)
        {
            // Try at default settings
            var result = Algorithm.GetTriggerPoint(this, frameIndexSamples, frameIndexSamples + frameSamples * (TriggerLookaheadFrames + 1), previousTriggerPoint);

            if (result < frameIndexSamples)
            {
                // Try again
                result = Algorithm.GetTriggerPoint(this, frameIndexSamples, frameIndexSamples + frameSamples * (TriggerLookaheadOnFailureFrames + 1), previousTriggerPoint);
            }

            if (result < frameIndexSamples)
            {
                // Default on failure
                result = frameIndexSamples;
            }

            return result;
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
                        return $"YM2413 Tone {index + 1}";
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
        // ReSharper disable once MemberCanBePrivate.Global
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

        // ReSharper disable once MemberCanBePrivate.Global
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
                        t.Name.ToLowerInvariant().Equals(reader.Value?.ToString().ToLowerInvariant()));
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
            if (_samplesForTrigger != _samples)
            {
                _samplesForTrigger.Dispose();
            }
            _labelFont?.Dispose();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            });
        }

        public void FromJson(string json, bool preserveSource)
        {
            if (preserveSource)
            {
                JsonConvert.PopulateObject(json, this, new JsonSerializerSettings
                {
                    ContractResolver = new PreservingContractResolver()
                });
            }
            else
            {
                JsonConvert.PopulateObject(json, this);
            }
        }

        private class PreservingContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (property.PropertyName == nameof(Filename) ||
                    property.PropertyName == nameof(Label) ||
                    property.PropertyName == nameof(ExternalTriggerFilename))
                {
                    property.Ignored = true;
                }
                return property;
            }
        }

        public bool IsMono()
        {
            if (Side == Sides.Left || Side == Sides.Right)
            {
                return true;
            }

            using var reader = new WaveFileReader(_filename);
            var sp = reader.ToSampleProvider().ToStereo();
            if (sp.WaveFormat.Channels == 1)
            {
                return true;
            }

            int bufferSize = sp.WaveFormat.SampleRate * 10;
            var buffer = new float[bufferSize];
            sp.Read(buffer, 0, bufferSize);
            for (int i = 0; i < bufferSize; i += 2)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (buffer[i] != buffer[i + 1])
                {
                    return false;
                }
            }

            return true;
        }
    }
}