namespace ImageAnalysisTestForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.text_filename = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.picture = new System.Windows.Forms.PictureBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label_time = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picture)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Bitmap filename:";
            // 
            // text_filename
            // 
            this.text_filename.Location = new System.Drawing.Point(104, 13);
            this.text_filename.Name = "text_filename";
            this.text_filename.Size = new System.Drawing.Size(330, 20);
            this.text_filename.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(441, 9);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(27, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // picture
            // 
            this.picture.Location = new System.Drawing.Point(16, 75);
            this.picture.Name = "picture";
            this.picture.Size = new System.Drawing.Size(452, 372);
            this.picture.TabIndex = 3;
            this.picture.TabStop = false;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(16, 46);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(101, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Load Bitmap";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label_time
            // 
            this.label_time.AutoSize = true;
            this.label_time.Location = new System.Drawing.Point(13, 466);
            this.label_time.Name = "label_time";
            this.label_time.Size = new System.Drawing.Size(110, 13);
            this.label_time.TabIndex = 5;
            this.label_time.Text = "Operation took {0} ms";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 488);
            this.Controls.Add(this.label_time);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.picture);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.text_filename);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Image Analysis Test Form";
            ((System.ComponentModel.ISupportInitialize)(this.picture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox text_filename;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.PictureBox picture;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label_time;
    }
}

