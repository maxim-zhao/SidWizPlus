using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibSidWiz;

namespace SidWiz
{
    public partial class ChannelControl : UserControl
    {
        private readonly SidWizPlusGui _parent;

        private readonly WaveformRenderer _renderer = new WaveformRenderer();

        // Used to decide when to trigger a redraw
        private int _renderRequestCounter;

        public Channel Channel
        {
            get => _channel;
            set
            {
                if (_channel != null)
                {
                    _channel.PropertyChanged -= ChannelOnPropertyChanged;
                }
                _channel = value;
                PropertyGrid.SelectedObject = _channel;
                _channel.PropertyChanged += ChannelOnPropertyChanged;
                ChannelOnPropertyChanged(this, null);
            }
        }

        public Task LoadTask { get; private set; }

        private string _filename;
        private double _highPassFilterFrequency;
        private CancellationTokenSource _cancellationTokenSource;
        private Channel _channel;

        public ChannelControl(SidWizPlusGui parent)
        {
            _parent = parent;
            _renderer.RenderedLineWidthInSamples = 735; // TODO make per-channel anyway?
            InitializeComponent();
        }

        private void ChannelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update UI on the right thread
            BeginInvoke(new Action(() =>
            {
                PropertyGrid.SelectedObject = _channel;
                TitleLabel.Text = _channel.Name;
            }));

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_channel.Filename != _filename || _channel.HighPassFilterFrequency != _highPassFilterFrequency)
            {
                // Cancel any existing task
                _cancellationTokenSource?.Cancel();
                // Remember values
                _filename = _channel.Filename;
                _highPassFilterFrequency = _channel.HighPassFilterFrequency;
                // Start loading in a background thread
                _cancellationTokenSource = new CancellationTokenSource();
                LoadTask = Task.Factory.StartNew(() => _channel.LoadData(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            Render();
        }

        private void Render()
        {
            // We signal a request to render by incrementing the counter...
            Interlocked.Increment(ref _renderRequestCounter);
            // We start rendering on a background thread
            Task.Factory.StartNew(() =>
            {
                // But we don't draw unless decrementing gets us to 0.
                int count = Interlocked.Decrement(ref _renderRequestCounter);
                if (count == 0 && _channel != null && _channel.SampleCount != 0)
                {
                    var bm = new Bitmap(PictureBox.Size.Width, PictureBox.Size.Height);
                    _renderer.RenderFrame(bm, _channel, _channel.MaxOffset);
                    PictureBox.BeginInvoke(new Action(() =>
                    {
                        var oldImage = PictureBox.Image;
                        PictureBox.Image = bm;
                        oldImage?.Dispose();
                    }));
                }
            });
        }

        private void ConfigureToggleButton_Click(object sender, EventArgs e)
        {
            // TODO: put it in a popup?
            PropertyGrid.Visible = !PropertyGrid.Visible;
        }

        private void Close_Click(object sender, EventArgs e)
        {
            _parent.RemoveChannel(this);
        }

        private void Left_Click(object sender, EventArgs e)
        {
            _parent.MoveChannel(this, -1);
        }

        private void Right_Click(object sender, EventArgs e)
        {
            _parent.MoveChannel(this, +1);
        }

        private void PictureBox_Resize(object sender, EventArgs e)
        {
            Render();
        }
    }
}
