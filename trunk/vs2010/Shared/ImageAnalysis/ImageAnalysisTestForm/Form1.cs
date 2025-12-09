using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImageAnalysisTestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "BMP files (*.bmp)|*.bmp";
            dlg.RestoreDirectory = true;
            DialogResult result = dlg.ShowDialog();
            if( result == DialogResult.OK) {
                text_filename.Text = dlg.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if( text_filename.Text == "")
                return;
            Bitmap bmp = new Bitmap( text_filename.Text);
            DateTime dt = DateTime.Now;
            Bitmap binarized = ImageAnalysis.ImagerBitmap.GetBlackAndWhiteBitmap( bmp);
            TimeSpan ts = DateTime.Now - dt;
            label_time.Text = String.Format( "Operation took {0} ms", ts.TotalMilliseconds);
            picture.Image = binarized;
        }
    }
}
