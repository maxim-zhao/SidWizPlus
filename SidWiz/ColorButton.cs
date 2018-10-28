using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SidWiz
{
    /// <summary>
    /// Button that lets you pick a colour
    /// </summary>
    public partial class ColorButton : Button
    {
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
            using (var colorDialog = new ColorDialog{Color = _color, FullOpen = true})
            {
                if (colorDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                Color = colorDialog.Color;
            }
        }
    }
}
