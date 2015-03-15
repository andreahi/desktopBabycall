using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace desktopBabycall
{
    public partial class Video : Form
    {
        public Video()
        {
            InitializeComponent();
        }
        public void updatePicture(Image img, int imgHeight, int imgWidth)
        {
          
            videoBox.Image = img;
            videoBox.Height = imgHeight;
            videoBox.Width = imgWidth;

        }
        private void Video_Load(object sender, EventArgs e)
        {

        }
    }
}
