using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using LibSidWiz;

namespace SidWiz
{
    public partial class ChannelControl : UserControl
    {
        private readonly Channel _channel;

        public ChannelControl(Channel channel)
        {
            _channel = channel;
            InitializeComponent();
            PropertyGrid.SelectedObject = _channel;
            _channel.PropertyChanged += ChannelOnPropertyChanged;
            TitleLabel.Text = _channel.Name;
        }

        private void ChannelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyGrid.SelectedObject = _channel;
            TitleLabel.Text = _channel.Name;
        }

        private void ConfigureToggleButton_Click(object sender, EventArgs e)
        {
            // TODO: put it in a popup?
            PropertyGrid.Visible = !PropertyGrid.Visible;
        }
    }
}
