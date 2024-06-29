using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SidWizPlusGUI
{
    /// <inheritdoc />
    /// <summary>
    /// Button that lets you pick a color
    /// </summary>
    public partial class ColorButton : Button
    {
        public event EventHandler ColorChanged;

        private Color _color;

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                BackColor = value;
                ForeColor = _color.GetBrightness() < 0.5 ? Color.White : Color.Black;
                Text = _color.Name;
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ColorButton()
        {
            InitializeComponent();
        }

        public ColorButton(IContainer container)
        {
            container.Add(this);

            InitializeComponent();

            Click += OnClick;
        }

        private void OnClick(object sender, EventArgs e)
        {
            using var colorDialog = new Cyotek.Windows.Forms.ColorPickerDialog();
            colorDialog.Color = _color;
            colorDialog.ShowAlphaChannel = true;
            if (colorDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            Color = colorDialog.Color;
        }
    }
}
