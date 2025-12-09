using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for PlateInfoForm.
	/// </summary>
	public class PlateInfoForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Label labelYo;
		private System.Windows.Forms.Label labelDBWC;
		private System.Windows.Forms.Label labelHeight;
		private System.Windows.Forms.Label labelDepth;
		private System.Windows.Forms.Label labelMaxVolume;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label labelDBWC2;
		private System.Windows.Forms.Label labelYo2;
		private System.Windows.Forms.Label labelLoBase;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelWellShape;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label labelFormat;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label labelBottomWell;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.Label label19;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public PlateInfoForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		//public DialogResult _ShowDialog(string Yo, string dbwc, string Yo2, string dbwc2, string height, string depth, string MaxVolume, string ASPOffset)
		public DialogResult _ShowDialog(string Yo, string dbwc, string Yo2, string dbwc2, string height, string depth, string MaxVolume, bool LoBase, string wellShape, string format, string bottomWell)
		{	
			labelYo.Text = Yo;
			labelDBWC.Text = dbwc;
			labelYo2.Text = Yo2;
			labelDBWC2.Text = dbwc2;
			labelHeight.Text = height;
			labelDepth.Text = depth;
			labelMaxVolume.Text = MaxVolume;
			labelLoBase.Text = LoBase ? "Yes" : "No";
			if( wellShape == "0" ) wellShape = "Flat-bottom";
			else if( wellShape == "1" ) wellShape = "U-bottom";
			else if( wellShape == "2" ) wellShape = "V-bottom";
			else if( wellShape == null ) wellShape = "Unknown";
			labelWellShape.Text = wellShape;
			if( format == "1" || format == "4" ) format = "96";
			else if( format == "2" || format == "5" ) format = "384";
			else if( format == "3" || format == "6" ) format = "1536";
			labelFormat.Text = format;
			labelBottomWell.Text = bottomWell;
			//labelASPOffset.Text = ASPOffset;
			
			ShowDialog();
			return DialogResult;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlateInfoForm));
            this.label3 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.labelYo = new System.Windows.Forms.Label();
            this.labelDBWC = new System.Windows.Forms.Label();
            this.labelHeight = new System.Windows.Forms.Label();
            this.labelDepth = new System.Windows.Forms.Label();
            this.labelMaxVolume = new System.Windows.Forms.Label();
            this.labelDBWC2 = new System.Windows.Forms.Label();
            this.labelYo2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.labelLoBase = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.labelWellShape = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelFormat = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.labelBottomWell = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(240, 184);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 24);
            this.label3.TabIndex = 37;
            this.label3.Text = "mm";
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(16, 184);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(144, 24);
            this.label7.TabIndex = 36;
            this.label7.Text = "Well depth:";
            // 
            // label15
            // 
            this.label15.BackColor = System.Drawing.Color.Transparent;
            this.label15.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.ForeColor = System.Drawing.Color.White;
            this.label15.Location = new System.Drawing.Point(240, 88);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(104, 24);
            this.label15.TabIndex = 35;
            this.label15.Text = "mm";
            // 
            // label16
            // 
            this.label16.BackColor = System.Drawing.Color.Transparent;
            this.label16.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.ForeColor = System.Drawing.Color.White;
            this.label16.Location = new System.Drawing.Point(16, 88);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(144, 24);
            this.label16.TabIndex = 34;
            this.label16.Text = "Row spacing:";
            // 
            // label14
            // 
            this.label14.BackColor = System.Drawing.Color.Transparent;
            this.label14.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.Color.White;
            this.label14.Location = new System.Drawing.Point(240, 64);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(104, 24);
            this.label14.TabIndex = 33;
            this.label14.Text = "mm";
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.Transparent;
            this.label13.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(16, 64);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(144, 24);
            this.label13.TabIndex = 30;
            this.label13.Text = "Row offset:";
            // 
            // label12
            // 
            this.label12.BackColor = System.Drawing.Color.Transparent;
            this.label12.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.White;
            this.label12.Location = new System.Drawing.Point(240, 232);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(104, 24);
            this.label12.TabIndex = 28;
            this.label12.Text = "µl";
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.Transparent;
            this.label11.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Location = new System.Drawing.Point(16, 232);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(144, 24);
            this.label11.TabIndex = 26;
            this.label11.Text = "Max volume:";
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(240, 160);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(104, 24);
            this.label8.TabIndex = 22;
            this.label8.Text = "mm";
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(16, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(144, 24);
            this.label4.TabIndex = 23;
            this.label4.Text = "Height:";
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.closeButton.ForeColor = System.Drawing.Color.White;
            this.closeButton.Location = new System.Drawing.Point(200, 288);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(72, 23);
            this.closeButton.TabIndex = 40;
            this.closeButton.Text = "&Close";
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // labelYo
            // 
            this.labelYo.BackColor = System.Drawing.Color.Transparent;
            this.labelYo.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelYo.ForeColor = System.Drawing.Color.White;
            this.labelYo.Location = new System.Drawing.Point(160, 64);
            this.labelYo.Name = "labelYo";
            this.labelYo.Size = new System.Drawing.Size(72, 16);
            this.labelYo.TabIndex = 41;
            this.labelYo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDBWC
            // 
            this.labelDBWC.BackColor = System.Drawing.Color.Transparent;
            this.labelDBWC.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDBWC.ForeColor = System.Drawing.Color.White;
            this.labelDBWC.Location = new System.Drawing.Point(160, 88);
            this.labelDBWC.Name = "labelDBWC";
            this.labelDBWC.Size = new System.Drawing.Size(72, 16);
            this.labelDBWC.TabIndex = 42;
            this.labelDBWC.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelHeight
            // 
            this.labelHeight.BackColor = System.Drawing.Color.Transparent;
            this.labelHeight.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHeight.ForeColor = System.Drawing.Color.White;
            this.labelHeight.Location = new System.Drawing.Point(160, 160);
            this.labelHeight.Name = "labelHeight";
            this.labelHeight.Size = new System.Drawing.Size(72, 16);
            this.labelHeight.TabIndex = 43;
            this.labelHeight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDepth
            // 
            this.labelDepth.BackColor = System.Drawing.Color.Transparent;
            this.labelDepth.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDepth.ForeColor = System.Drawing.Color.White;
            this.labelDepth.Location = new System.Drawing.Point(160, 184);
            this.labelDepth.Name = "labelDepth";
            this.labelDepth.Size = new System.Drawing.Size(72, 16);
            this.labelDepth.TabIndex = 44;
            this.labelDepth.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelMaxVolume
            // 
            this.labelMaxVolume.BackColor = System.Drawing.Color.Transparent;
            this.labelMaxVolume.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMaxVolume.ForeColor = System.Drawing.Color.White;
            this.labelMaxVolume.Location = new System.Drawing.Point(160, 232);
            this.labelMaxVolume.Name = "labelMaxVolume";
            this.labelMaxVolume.Size = new System.Drawing.Size(72, 16);
            this.labelMaxVolume.TabIndex = 45;
            this.labelMaxVolume.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDBWC2
            // 
            this.labelDBWC2.BackColor = System.Drawing.Color.Transparent;
            this.labelDBWC2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDBWC2.ForeColor = System.Drawing.Color.White;
            this.labelDBWC2.Location = new System.Drawing.Point(160, 136);
            this.labelDBWC2.Name = "labelDBWC2";
            this.labelDBWC2.Size = new System.Drawing.Size(72, 16);
            this.labelDBWC2.TabIndex = 52;
            this.labelDBWC2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelYo2
            // 
            this.labelYo2.BackColor = System.Drawing.Color.Transparent;
            this.labelYo2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelYo2.ForeColor = System.Drawing.Color.White;
            this.labelYo2.Location = new System.Drawing.Point(160, 112);
            this.labelYo2.Name = "labelYo2";
            this.labelYo2.Size = new System.Drawing.Size(72, 16);
            this.labelYo2.TabIndex = 51;
            this.labelYo2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(240, 136);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 24);
            this.label5.TabIndex = 50;
            this.label5.Text = "mm";
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(16, 136);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(144, 24);
            this.label9.TabIndex = 49;
            this.label9.Text = "Column spacing:";
            // 
            // label17
            // 
            this.label17.BackColor = System.Drawing.Color.Transparent;
            this.label17.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Location = new System.Drawing.Point(240, 112);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(104, 24);
            this.label17.TabIndex = 48;
            this.label17.Text = "mm";
            // 
            // label18
            // 
            this.label18.BackColor = System.Drawing.Color.Transparent;
            this.label18.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.White;
            this.label18.Location = new System.Drawing.Point(16, 112);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(144, 24);
            this.label18.TabIndex = 47;
            this.label18.Text = "Column offset:";
            // 
            // labelLoBase
            // 
            this.labelLoBase.BackColor = System.Drawing.Color.Transparent;
            this.labelLoBase.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLoBase.ForeColor = System.Drawing.Color.White;
            this.labelLoBase.Location = new System.Drawing.Point(160, 256);
            this.labelLoBase.Name = "labelLoBase";
            this.labelLoBase.Size = new System.Drawing.Size(72, 16);
            this.labelLoBase.TabIndex = 54;
            this.labelLoBase.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(16, 256);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(144, 24);
            this.label2.TabIndex = 53;
            this.label2.Text = "Extended rim:";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(16, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 24);
            this.label1.TabIndex = 55;
            this.label1.Text = "Well shape:";
            // 
            // labelWellShape
            // 
            this.labelWellShape.BackColor = System.Drawing.Color.Transparent;
            this.labelWellShape.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWellShape.ForeColor = System.Drawing.Color.White;
            this.labelWellShape.Location = new System.Drawing.Point(156, 40);
            this.labelWellShape.Name = "labelWellShape";
            this.labelWellShape.Size = new System.Drawing.Size(112, 16);
            this.labelWellShape.TabIndex = 56;
            this.labelWellShape.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.White;
            this.label6.Location = new System.Drawing.Point(16, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(144, 24);
            this.label6.TabIndex = 57;
            this.label6.Text = "Format:";
            // 
            // labelFormat
            // 
            this.labelFormat.BackColor = System.Drawing.Color.Transparent;
            this.labelFormat.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFormat.ForeColor = System.Drawing.Color.White;
            this.labelFormat.Location = new System.Drawing.Point(160, 16);
            this.labelFormat.Name = "labelFormat";
            this.labelFormat.Size = new System.Drawing.Size(72, 16);
            this.labelFormat.TabIndex = 58;
            this.labelFormat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(16, 208);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(152, 24);
            this.label10.TabIndex = 59;
            this.label10.Text = "Bottom well diameter:";
            // 
            // labelBottomWell
            // 
            this.labelBottomWell.BackColor = System.Drawing.Color.Transparent;
            this.labelBottomWell.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBottomWell.ForeColor = System.Drawing.Color.White;
            this.labelBottomWell.Location = new System.Drawing.Point(160, 208);
            this.labelBottomWell.Name = "labelBottomWell";
            this.labelBottomWell.Size = new System.Drawing.Size(72, 16);
            this.labelBottomWell.TabIndex = 60;
            this.labelBottomWell.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label20
            // 
            this.label20.BackColor = System.Drawing.Color.Transparent;
            this.label20.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.White;
            this.label20.Location = new System.Drawing.Point(240, 208);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(104, 24);
            this.label20.TabIndex = 61;
            this.label20.Text = "mm";
            // 
            // label19
            // 
            this.label19.BackColor = System.Drawing.Color.Transparent;
            this.label19.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.Color.White;
            this.label19.Location = new System.Drawing.Point(240, 16);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(104, 24);
            this.label19.TabIndex = 62;
            this.label19.Text = "wells";
            // 
            // PlateInfoForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(290, 319);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.labelBottomWell);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.labelFormat);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.labelWellShape);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelLoBase);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelDBWC2);
            this.Controls.Add(this.labelYo2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.labelMaxVolume);
            this.Controls.Add(this.labelDepth);
            this.Controls.Add(this.labelHeight);
            this.Controls.Add(this.labelDBWC);
            this.Controls.Add(this.labelYo);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label4);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlateInfoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Plate Info";
            this.ResumeLayout(false);

		}
		#endregion

		private void closeButton_Click(object sender, System.EventArgs e)
		{
			Close();
		}
	}
}
