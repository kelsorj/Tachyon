namespace TwoCameraCapture
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.imageBox1 = new Emgu.CV.UI.ImageBox();
            this.imageBox2 = new Emgu.CV.UI.ImageBox();
            this.imageBox3 = new Emgu.CV.UI.ImageBox();
            this.imageBox4 = new Emgu.CV.UI.ImageBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.leftright1 = new System.Windows.Forms.NumericUpDown();
            this.updown1 = new System.Windows.Forms.NumericUpDown();
            this.width1 = new System.Windows.Forms.NumericUpDown();
            this.height1 = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.leftright2 = new System.Windows.Forms.NumericUpDown();
            this.updown2 = new System.Windows.Forms.NumericUpDown();
            this.width2 = new System.Windows.Forms.NumericUpDown();
            this.height2 = new System.Windows.Forms.NumericUpDown();
            this.rotate1 = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.rotate2 = new System.Windows.Forms.NumericUpDown();
            this.button3 = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.imageBox5 = new Emgu.CV.UI.ImageBox();
            this.imageBox6 = new Emgu.CV.UI.ImageBox();
            this.thresholdBox = new System.Windows.Forms.NumericUpDown();
            this.minwidthBox = new System.Windows.Forms.NumericUpDown();
            this.gapBox = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.contourareaBox = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.leftright1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.updown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.width1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.height1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.leftright2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.updown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.width2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.height2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rotate1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rotate2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.thresholdBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minwidthBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gapBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.contourareaBox)).BeginInit();
            this.SuspendLayout();
            // 
            // imageBox1
            // 
            this.imageBox1.Location = new System.Drawing.Point(12, 12);
            this.imageBox1.Name = "imageBox1";
            this.imageBox1.Size = new System.Drawing.Size(122, 129);
            this.imageBox1.TabIndex = 2;
            this.imageBox1.TabStop = false;
            // 
            // imageBox2
            // 
            this.imageBox2.Location = new System.Drawing.Point(140, 147);
            this.imageBox2.Name = "imageBox2";
            this.imageBox2.Size = new System.Drawing.Size(122, 129);
            this.imageBox2.TabIndex = 3;
            this.imageBox2.TabStop = false;
            // 
            // imageBox3
            // 
            this.imageBox3.Location = new System.Drawing.Point(12, 417);
            this.imageBox3.Name = "imageBox3";
            this.imageBox3.Size = new System.Drawing.Size(122, 129);
            this.imageBox3.TabIndex = 4;
            this.imageBox3.TabStop = false;
            // 
            // imageBox4
            // 
            this.imageBox4.Location = new System.Drawing.Point(140, 282);
            this.imageBox4.Name = "imageBox4";
            this.imageBox4.Size = new System.Drawing.Size(122, 129);
            this.imageBox4.TabIndex = 5;
            this.imageBox4.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(313, 11);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 24);
            this.button1.TabIndex = 6;
            this.button1.Text = "Start Capture 1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(483, 11);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(90, 24);
            this.button2.TabIndex = 7;
            this.button2.Text = "Start Capture 2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // leftright1
            // 
            this.leftright1.Location = new System.Drawing.Point(313, 42);
            this.leftright1.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.leftright1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.leftright1.Name = "leftright1";
            this.leftright1.Size = new System.Drawing.Size(90, 20);
            this.leftright1.TabIndex = 8;
            this.leftright1.Value = new decimal(new int[] {
            389,
            0,
            0,
            0});
            this.leftright1.ValueChanged += new System.EventHandler(this.leftright1_ValueChanged);
            // 
            // updown1
            // 
            this.updown1.Location = new System.Drawing.Point(313, 68);
            this.updown1.Maximum = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            this.updown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.updown1.Name = "updown1";
            this.updown1.Size = new System.Drawing.Size(90, 20);
            this.updown1.TabIndex = 9;
            this.updown1.Value = new decimal(new int[] {
            198,
            0,
            0,
            0});
            this.updown1.ValueChanged += new System.EventHandler(this.updown1_ValueChanged);
            // 
            // width1
            // 
            this.width1.Location = new System.Drawing.Point(313, 94);
            this.width1.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.width1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.width1.Name = "width1";
            this.width1.Size = new System.Drawing.Size(90, 20);
            this.width1.TabIndex = 10;
            this.width1.Value = new decimal(new int[] {
            53,
            0,
            0,
            0});
            this.width1.ValueChanged += new System.EventHandler(this.width1_ValueChanged);
            // 
            // height1
            // 
            this.height1.Location = new System.Drawing.Point(313, 120);
            this.height1.Maximum = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            this.height1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.height1.Name = "height1";
            this.height1.Size = new System.Drawing.Size(90, 20);
            this.height1.TabIndex = 11;
            this.height1.Value = new decimal(new int[] {
            210,
            0,
            0,
            0});
            this.height1.ValueChanged += new System.EventHandler(this.height1_ValueChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(405, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Left / Right";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(405, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Up / Down";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(418, 98);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Width";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(416, 124);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Height";
            // 
            // leftright2
            // 
            this.leftright2.Location = new System.Drawing.Point(483, 44);
            this.leftright2.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.leftright2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.leftright2.Name = "leftright2";
            this.leftright2.Size = new System.Drawing.Size(90, 20);
            this.leftright2.TabIndex = 19;
            this.leftright2.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.leftright2.ValueChanged += new System.EventHandler(this.leftright2_ValueChanged);
            // 
            // updown2
            // 
            this.updown2.Location = new System.Drawing.Point(483, 68);
            this.updown2.Maximum = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            this.updown2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.updown2.Name = "updown2";
            this.updown2.Size = new System.Drawing.Size(90, 20);
            this.updown2.TabIndex = 18;
            this.updown2.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.updown2.ValueChanged += new System.EventHandler(this.updown2_ValueChanged);
            // 
            // width2
            // 
            this.width2.Location = new System.Drawing.Point(483, 94);
            this.width2.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.width2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.width2.Name = "width2";
            this.width2.Size = new System.Drawing.Size(90, 20);
            this.width2.TabIndex = 17;
            this.width2.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.width2.ValueChanged += new System.EventHandler(this.width2_ValueChanged);
            // 
            // height2
            // 
            this.height2.Location = new System.Drawing.Point(483, 117);
            this.height2.Maximum = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            this.height2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.height2.Name = "height2";
            this.height2.Size = new System.Drawing.Size(90, 20);
            this.height2.TabIndex = 16;
            this.height2.Value = new decimal(new int[] {
            150,
            0,
            0,
            0});
            this.height2.ValueChanged += new System.EventHandler(this.height2_ValueChanged);
            // 
            // rotate1
            // 
            this.rotate1.Location = new System.Drawing.Point(313, 146);
            this.rotate1.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.rotate1.Name = "rotate1";
            this.rotate1.Size = new System.Drawing.Size(90, 20);
            this.rotate1.TabIndex = 20;
            this.rotate1.ValueChanged += new System.EventHandler(this.rotate1_ValueChanged);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(415, 148);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(39, 13);
            this.label5.TabIndex = 21;
            this.label5.Text = "Rotate";
            // 
            // rotate2
            // 
            this.rotate2.Location = new System.Drawing.Point(483, 143);
            this.rotate2.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.rotate2.Name = "rotate2";
            this.rotate2.Size = new System.Drawing.Size(90, 20);
            this.rotate2.TabIndex = 22;
            this.rotate2.ValueChanged += new System.EventHandler(this.rotate2_ValueChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(313, 298);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 23;
            this.button3.Text = "Snap";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(313, 328);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(35, 13);
            this.label6.TabIndex = 24;
            this.label6.Text = "label6";
            // 
            // imageBox5
            // 
            this.imageBox5.Location = new System.Drawing.Point(12, 147);
            this.imageBox5.Name = "imageBox5";
            this.imageBox5.Size = new System.Drawing.Size(122, 129);
            this.imageBox5.TabIndex = 25;
            this.imageBox5.TabStop = false;
            // 
            // imageBox6
            // 
            this.imageBox6.Location = new System.Drawing.Point(12, 282);
            this.imageBox6.Name = "imageBox6";
            this.imageBox6.Size = new System.Drawing.Size(122, 129);
            this.imageBox6.TabIndex = 26;
            this.imageBox6.TabStop = false;
            // 
            // thresholdBox
            // 
            this.thresholdBox.Location = new System.Drawing.Point(313, 173);
            this.thresholdBox.Name = "thresholdBox";
            this.thresholdBox.Size = new System.Drawing.Size(90, 20);
            this.thresholdBox.TabIndex = 27;
            this.thresholdBox.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.thresholdBox.ValueChanged += new System.EventHandler(this.thresholdBox_ValueChanged);
            // 
            // minwidthBox
            // 
            this.minwidthBox.Location = new System.Drawing.Point(313, 200);
            this.minwidthBox.Name = "minwidthBox";
            this.minwidthBox.Size = new System.Drawing.Size(90, 20);
            this.minwidthBox.TabIndex = 28;
            this.minwidthBox.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.minwidthBox.ValueChanged += new System.EventHandler(this.minwidthBox_ValueChanged);
            // 
            // gapBox
            // 
            this.gapBox.Location = new System.Drawing.Point(313, 226);
            this.gapBox.Name = "gapBox";
            this.gapBox.Size = new System.Drawing.Size(90, 20);
            this.gapBox.TabIndex = 29;
            this.gapBox.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.gapBox.ValueChanged += new System.EventHandler(this.gapBox_ValueChanged);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(409, 175);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 13);
            this.label7.TabIndex = 30;
            this.label7.Text = "Threshold";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(409, 202);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(55, 13);
            this.label8.TabIndex = 31;
            this.label8.Text = "Min Width";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(418, 228);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(27, 13);
            this.label9.TabIndex = 32;
            this.label9.Text = "Gap";
            // 
            // contourareaBox
            // 
            this.contourareaBox.Location = new System.Drawing.Point(313, 252);
            this.contourareaBox.Name = "contourareaBox";
            this.contourareaBox.Size = new System.Drawing.Size(90, 20);
            this.contourareaBox.TabIndex = 33;
            this.contourareaBox.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(405, 254);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(69, 13);
            this.label10.TabIndex = 34;
            this.label10.Text = "Contour Area";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(877, 582);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.contourareaBox);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.gapBox);
            this.Controls.Add(this.minwidthBox);
            this.Controls.Add(this.thresholdBox);
            this.Controls.Add(this.imageBox6);
            this.Controls.Add(this.imageBox5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.rotate2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.rotate1);
            this.Controls.Add(this.leftright2);
            this.Controls.Add(this.updown2);
            this.Controls.Add(this.width2);
            this.Controls.Add(this.height2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.height1);
            this.Controls.Add(this.width1);
            this.Controls.Add(this.updown1);
            this.Controls.Add(this.leftright1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.imageBox4);
            this.Controls.Add(this.imageBox3);
            this.Controls.Add(this.imageBox2);
            this.Controls.Add(this.imageBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.imageBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.leftright1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.updown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.width1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.height1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.leftright2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.updown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.width2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.height2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rotate1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rotate2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.thresholdBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minwidthBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gapBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.contourareaBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Emgu.CV.UI.ImageBox imageBox1;
        private Emgu.CV.UI.ImageBox imageBox2;
        private Emgu.CV.UI.ImageBox imageBox3;
        private Emgu.CV.UI.ImageBox imageBox4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.NumericUpDown leftright1;
        private System.Windows.Forms.NumericUpDown updown1;
        private System.Windows.Forms.NumericUpDown width1;
        private System.Windows.Forms.NumericUpDown height1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown leftright2;
        private System.Windows.Forms.NumericUpDown updown2;
        private System.Windows.Forms.NumericUpDown width2;
        private System.Windows.Forms.NumericUpDown height2;
        private System.Windows.Forms.NumericUpDown rotate1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown rotate2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label6;
        private Emgu.CV.UI.ImageBox imageBox5;
        private Emgu.CV.UI.ImageBox imageBox6;
        private System.Windows.Forms.NumericUpDown thresholdBox;
        private System.Windows.Forms.NumericUpDown minwidthBox;
        private System.Windows.Forms.NumericUpDown gapBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown contourareaBox;
        private System.Windows.Forms.Label label10;
    }
}

