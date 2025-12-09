using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for plateForm.
	/// </summary>
	public class plateForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		public System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		public System.Windows.Forms.TextBox textBoxHeight;
		public System.Windows.Forms.ComboBox comboBoxFormat;
		public System.Windows.Forms.TextBox textBoxMaxVolume;
		private System.Windows.Forms.Label label13;
		public System.Windows.Forms.TextBox textBoxYo;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		public System.Windows.Forms.TextBox textBoxdbwc;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.TextBox textBoxDepth;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label labelPlate;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// helpers
		private bool m_bCreateNew = false;
		public System.Windows.Forms.TextBox textBoxTypeNo;
		private string m_strPlateName;
		private System.Windows.Forms.Button saveButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Label label20;
		public System.Windows.Forms.TextBox textBoxdbwc2;
		public System.Windows.Forms.TextBox textBoxYo2;
		private System.Windows.Forms.Label LoBaseLbl;
		public System.Windows.Forms.CheckBox LoBaseCb;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label10;
		public System.Windows.Forms.TextBox textBoxWellDiameter;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.Label label22;
		public System.Windows.Forms.ComboBox comboBoxShape;
		private string m_strPlateType;

		public plateForm(string strPlateName, string strPlateType, bool bCreateNew)
		{
			InitializeComponent();

			string strFormat = "";
			if (strPlateType == "1")
			{
				strFormat = "96";
			}
			else if (strPlateType == "2")
			{
				strFormat = "384";
			}
			else if (strPlateType == "3")
			{
				strFormat = "1536";
			}

			string strLabel = "Plate: " + strPlateName + " (Format: " + strFormat + ")";
			Text = strLabel;
			labelPlate.Text = strLabel;

			m_bCreateNew = bCreateNew;
			m_strPlateName = strPlateName;
			m_strPlateType = strPlateType;
			textBoxName.Text = strPlateName;

			if (bCreateNew)
			{
			}
			else
			{
				// load data form xml file and populate
				// must be done in plateForm_Load()
			}
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

		public void Save()
		{
			mainForm mf = (mainForm)this.MdiParent;

			if (mf.m_User.UserLevel < 3)
			{
				MessageBox.Show(this, "Only administrators can change plate data.\nNew plate data NOT saved...", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			m_strPlateName = textBoxName.Text;
			int nPlateType = comboBoxFormat.SelectedIndex + 1;
			m_strPlateType = nPlateType.ToString();

			if( mf.m_xmlData.PlateExist( m_strPlateType, m_strPlateName ) )
			{
				DialogResult res = MessageBox.Show(this, "Another plate exists with this name.\r\nDo you want to overwrite it?", "Save", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
				if( res == DialogResult.Cancel )
				{
					return;
				}
			}

			
			mf.m_xmlData.DeletePlate(m_strPlateType, m_strPlateName, mf.treeView);
						
			mf.m_xmlData.SavePlate(this, mf.treeView);

			string strFormat = "";
			if (m_strPlateType == "1")
			{
				strFormat = "96";
			}
			else if (m_strPlateType == "2")
			{
				strFormat = "384";
			}
			else if (m_strPlateType == "3")
			{
				strFormat = "1536";
			}

			Text = "Plate: " + textBoxName.Text + " (Format: " + strFormat + ")";
			labelPlate.Text = Text;
			Tag = false;
			saveButton.Hide();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(plateForm));
            this.textBoxHeight = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxFormat = new System.Windows.Forms.ComboBox();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textBoxTypeNo = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.labelPlate = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxMaxVolume = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.textBoxYo = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.textBoxdbwc = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxDepth = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.textBoxdbwc2 = new System.Windows.Forms.TextBox();
            this.textBoxYo2 = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.LoBaseLbl = new System.Windows.Forms.Label();
            this.LoBaseCb = new System.Windows.Forms.CheckBox();
            this.textBoxWellDiameter = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.comboBoxShape = new System.Windows.Forms.ComboBox();
            this.label22 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxHeight
            // 
            this.textBoxHeight.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxHeight.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxHeight.Location = new System.Drawing.Point(176, 264);
            this.textBoxHeight.Name = "textBoxHeight";
            this.textBoxHeight.Size = new System.Drawing.Size(72, 21);
            this.textBoxHeight.TabIndex = 10;
            this.textBoxHeight.Text = "0";
            this.textBoxHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxHeight.TextChanged += new System.EventHandler(this.textBoxHeight_TextChanged);
            this.textBoxHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxHeight_KeyDown);
            this.textBoxHeight.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxHeight_Validating);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(16, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(152, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(16, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(160, 24);
            this.label2.TabIndex = 0;
            this.label2.Text = "Catalog number:";
            // 
            // comboBoxFormat
            // 
            this.comboBoxFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFormat.DropDownWidth = 72;
            this.comboBoxFormat.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxFormat.Items.AddRange(new object[] {
            "96",
            "384",
            "1536"});
            this.comboBoxFormat.Location = new System.Drawing.Point(176, 120);
            this.comboBoxFormat.Name = "comboBoxFormat";
            this.comboBoxFormat.Size = new System.Drawing.Size(72, 21);
            this.comboBoxFormat.TabIndex = 4;
            this.comboBoxFormat.Validating += new System.ComponentModel.CancelEventHandler(this.comboBoxFormat_Validating);
            this.comboBoxFormat.SelectedIndexChanged += new System.EventHandler(this.comboBoxFormat_SelectedIndexChanged);
            this.comboBoxFormat.SelectedValueChanged += new System.EventHandler(this.comboBoxFormat_SelectedValueChanged);
            this.comboBoxFormat.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxFormat_KeyDown);
            this.comboBoxFormat.TextChanged += new System.EventHandler(this.comboBoxFormat_TextChanged);
            // 
            // textBoxName
            // 
            this.textBoxName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxName.Location = new System.Drawing.Point(176, 72);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(248, 21);
            this.textBoxName.TabIndex = 2;
            this.textBoxName.TextChanged += new System.EventHandler(this.textBoxName_TextChanged);
            this.textBoxName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxName_KeyDown);
            this.textBoxName.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxName_Validating);
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(256, 264);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(104, 24);
            this.label8.TabIndex = 0;
            this.label8.Text = "mm";
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(256, 120);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(104, 24);
            this.label9.TabIndex = 0;
            this.label9.Text = "wells";
            // 
            // textBoxTypeNo
            // 
            this.textBoxTypeNo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxTypeNo.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxTypeNo.Location = new System.Drawing.Point(176, 96);
            this.textBoxTypeNo.Name = "textBoxTypeNo";
            this.textBoxTypeNo.Size = new System.Drawing.Size(248, 21);
            this.textBoxTypeNo.TabIndex = 3;
            this.textBoxTypeNo.Text = "description";
            this.textBoxTypeNo.TextChanged += new System.EventHandler(this.textBoxTypeNo_TextChanged);
            this.textBoxTypeNo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxTypeNo_KeyDown);
            this.textBoxTypeNo.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxTypeNo_Validating);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(16, 264);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(152, 24);
            this.label4.TabIndex = 0;
            this.label4.Text = "Height:";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(16, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(152, 24);
            this.label5.TabIndex = 0;
            this.label5.Text = "Format:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.Location = new System.Drawing.Point(368, 344);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(208, 48);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // labelPlate
            // 
            this.labelPlate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPlate.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlate.Location = new System.Drawing.Point(16, 16);
            this.labelPlate.Name = "labelPlate";
            this.labelPlate.Size = new System.Drawing.Size(584, 24);
            this.labelPlate.TabIndex = 1;
            this.labelPlate.Text = "Plate:";
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.Transparent;
            this.label11.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Location = new System.Drawing.Point(17, 336);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(151, 24);
            this.label11.TabIndex = 6;
            this.label11.Text = "Max volume:";
            // 
            // textBoxMaxVolume
            // 
            this.textBoxMaxVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxMaxVolume.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxMaxVolume.Location = new System.Drawing.Point(176, 336);
            this.textBoxMaxVolume.Name = "textBoxMaxVolume";
            this.textBoxMaxVolume.Size = new System.Drawing.Size(72, 21);
            this.textBoxMaxVolume.TabIndex = 13;
            this.textBoxMaxVolume.Text = "10";
            this.textBoxMaxVolume.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxMaxVolume.TextChanged += new System.EventHandler(this.textBoxMaxVolume_TextChanged);
            this.textBoxMaxVolume.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxMaxVolume_KeyDown);
            this.textBoxMaxVolume.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxMaxVolume_Validating);
            // 
            // label12
            // 
            this.label12.BackColor = System.Drawing.Color.Transparent;
            this.label12.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.White;
            this.label12.Location = new System.Drawing.Point(256, 336);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(104, 24);
            this.label12.TabIndex = 8;
            this.label12.Text = "µl";
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.Transparent;
            this.label13.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(16, 168);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(152, 24);
            this.label13.TabIndex = 9;
            this.label13.Text = "Row offset:";
            // 
            // textBoxYo
            // 
            this.textBoxYo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxYo.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxYo.Location = new System.Drawing.Point(176, 168);
            this.textBoxYo.Name = "textBoxYo";
            this.textBoxYo.Size = new System.Drawing.Size(72, 21);
            this.textBoxYo.TabIndex = 6;
            this.textBoxYo.Text = "0";
            this.textBoxYo.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxYo.TextChanged += new System.EventHandler(this.textBoxYo_TextChanged);
            this.textBoxYo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxYo_KeyDown);
            this.textBoxYo.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxYo_Validating);
            // 
            // label14
            // 
            this.label14.BackColor = System.Drawing.Color.Transparent;
            this.label14.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.Color.White;
            this.label14.Location = new System.Drawing.Point(256, 168);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(104, 24);
            this.label14.TabIndex = 11;
            this.label14.Text = "mm";
            // 
            // label15
            // 
            this.label15.BackColor = System.Drawing.Color.Transparent;
            this.label15.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.ForeColor = System.Drawing.Color.White;
            this.label15.Location = new System.Drawing.Point(256, 192);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(104, 24);
            this.label15.TabIndex = 14;
            this.label15.Text = "mm";
            // 
            // textBoxdbwc
            // 
            this.textBoxdbwc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxdbwc.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxdbwc.Location = new System.Drawing.Point(176, 192);
            this.textBoxdbwc.Name = "textBoxdbwc";
            this.textBoxdbwc.Size = new System.Drawing.Size(72, 21);
            this.textBoxdbwc.TabIndex = 7;
            this.textBoxdbwc.Text = "0";
            this.textBoxdbwc.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxdbwc.TextChanged += new System.EventHandler(this.textBoxdbwc_TextChanged);
            this.textBoxdbwc.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxdbwc_KeyDown);
            this.textBoxdbwc.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxdbwc_Validating);
            // 
            // label16
            // 
            this.label16.BackColor = System.Drawing.Color.Transparent;
            this.label16.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.ForeColor = System.Drawing.Color.White;
            this.label16.Location = new System.Drawing.Point(16, 192);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(152, 24);
            this.label16.TabIndex = 12;
            this.label16.Text = "Row spacing:";
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(256, 288);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 24);
            this.label3.TabIndex = 16;
            this.label3.Text = "mm";
            // 
            // textBoxDepth
            // 
            this.textBoxDepth.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxDepth.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxDepth.Location = new System.Drawing.Point(176, 288);
            this.textBoxDepth.Name = "textBoxDepth";
            this.textBoxDepth.Size = new System.Drawing.Size(72, 21);
            this.textBoxDepth.TabIndex = 11;
            this.textBoxDepth.Text = "0";
            this.textBoxDepth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxDepth.TextChanged += new System.EventHandler(this.textBoxDepth_TextChanged);
            this.textBoxDepth.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxDepth_KeyDown);
            this.textBoxDepth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxDepth_Validating);
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(16, 288);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(152, 24);
            this.label7.TabIndex = 15;
            this.label7.Text = "Well depth:";
            // 
            // saveButton
            // 
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.saveButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(16, 400);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(72, 23);
            this.saveButton.TabIndex = 20;
            this.saveButton.Text = "&Save";
            this.saveButton.Visible = false;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.Location = new System.Drawing.Point(512, 400);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 22;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // textBoxdbwc2
            // 
            this.textBoxdbwc2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxdbwc2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxdbwc2.Location = new System.Drawing.Point(176, 240);
            this.textBoxdbwc2.Name = "textBoxdbwc2";
            this.textBoxdbwc2.Size = new System.Drawing.Size(72, 21);
            this.textBoxdbwc2.TabIndex = 9;
            this.textBoxdbwc2.Text = "0";
            this.textBoxdbwc2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxdbwc2.TextChanged += new System.EventHandler(this.textBoxdbwc2_TextChanged);
            this.textBoxdbwc2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxdbwc2_KeyDown);
            this.textBoxdbwc2.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxdbwc2_Validating);
            // 
            // textBoxYo2
            // 
            this.textBoxYo2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxYo2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxYo2.Location = new System.Drawing.Point(176, 216);
            this.textBoxYo2.Name = "textBoxYo2";
            this.textBoxYo2.Size = new System.Drawing.Size(72, 21);
            this.textBoxYo2.TabIndex = 8;
            this.textBoxYo2.Text = "0";
            this.textBoxYo2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxYo2.TextChanged += new System.EventHandler(this.textBoxYo2_TextChanged);
            this.textBoxYo2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxYo2_KeyDown);
            this.textBoxYo2.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxYo_Validating);
            // 
            // label17
            // 
            this.label17.BackColor = System.Drawing.Color.Transparent;
            this.label17.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Location = new System.Drawing.Point(256, 240);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(104, 24);
            this.label17.TabIndex = 28;
            this.label17.Text = "mm";
            // 
            // label18
            // 
            this.label18.BackColor = System.Drawing.Color.Transparent;
            this.label18.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.White;
            this.label18.Location = new System.Drawing.Point(16, 240);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(152, 24);
            this.label18.TabIndex = 27;
            this.label18.Text = "Column spacing:";
            // 
            // label19
            // 
            this.label19.BackColor = System.Drawing.Color.Transparent;
            this.label19.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.Color.White;
            this.label19.Location = new System.Drawing.Point(256, 216);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(104, 24);
            this.label19.TabIndex = 26;
            this.label19.Text = "mm";
            // 
            // label20
            // 
            this.label20.BackColor = System.Drawing.Color.Transparent;
            this.label20.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.White;
            this.label20.Location = new System.Drawing.Point(16, 216);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(152, 24);
            this.label20.TabIndex = 25;
            this.label20.Text = "Column offset:";
            // 
            // LoBaseLbl
            // 
            this.LoBaseLbl.BackColor = System.Drawing.Color.Transparent;
            this.LoBaseLbl.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoBaseLbl.ForeColor = System.Drawing.Color.White;
            this.LoBaseLbl.Location = new System.Drawing.Point(16, 360);
            this.LoBaseLbl.Name = "LoBaseLbl";
            this.LoBaseLbl.Size = new System.Drawing.Size(152, 24);
            this.LoBaseLbl.TabIndex = 29;
            this.LoBaseLbl.Text = "Extended rim:";
            // 
            // LoBaseCb
            // 
            this.LoBaseCb.Location = new System.Drawing.Point(176, 352);
            this.LoBaseCb.Name = "LoBaseCb";
            this.LoBaseCb.Size = new System.Drawing.Size(104, 24);
            this.LoBaseCb.TabIndex = 14;
            this.LoBaseCb.CheckedChanged += new System.EventHandler(this.LoBaseCb_CheckedChanged);
            this.LoBaseCb.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LoBaseCb_KeyDown);
            // 
            // textBoxWellDiameter
            // 
            this.textBoxWellDiameter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxWellDiameter.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxWellDiameter.Location = new System.Drawing.Point(176, 312);
            this.textBoxWellDiameter.Name = "textBoxWellDiameter";
            this.textBoxWellDiameter.Size = new System.Drawing.Size(72, 21);
            this.textBoxWellDiameter.TabIndex = 12;
            this.textBoxWellDiameter.Text = "0.80";
            this.textBoxWellDiameter.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBoxWellDiameter.TextChanged += new System.EventHandler(this.textBoxWellDiameter_TextChanged);
            this.textBoxWellDiameter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxWellDiameter_KeyDown);
            this.textBoxWellDiameter.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxDiameter_Validating);
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.White;
            this.label6.Location = new System.Drawing.Point(256, 312);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(104, 24);
            this.label6.TabIndex = 33;
            this.label6.Text = "mm";
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(16, 312);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(152, 24);
            this.label10.TabIndex = 32;
            this.label10.Text = "Bottom well diameter:";
            // 
            // label21
            // 
            this.label21.BackColor = System.Drawing.Color.Transparent;
            this.label21.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.ForeColor = System.Drawing.Color.White;
            this.label21.Location = new System.Drawing.Point(256, 144);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(104, 24);
            this.label21.TabIndex = 35;
            this.label21.Text = "- bottom";
            // 
            // comboBoxShape
            // 
            this.comboBoxShape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxShape.DropDownWidth = 72;
            this.comboBoxShape.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxShape.Items.AddRange(new object[] {
            "Flat",
            "U",
            "V"});
            this.comboBoxShape.Location = new System.Drawing.Point(176, 144);
            this.comboBoxShape.Name = "comboBoxShape";
            this.comboBoxShape.Size = new System.Drawing.Size(72, 21);
            this.comboBoxShape.TabIndex = 5;
            this.comboBoxShape.SelectedIndexChanged += new System.EventHandler(this.comboBoxShape_SelectedIndexChanged);
            this.comboBoxShape.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxShape_KeyDown);
            // 
            // label22
            // 
            this.label22.BackColor = System.Drawing.Color.Transparent;
            this.label22.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.ForeColor = System.Drawing.Color.White;
            this.label22.Location = new System.Drawing.Point(16, 144);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(152, 24);
            this.label22.TabIndex = 34;
            this.label22.Text = "Well shape:";
            // 
            // plateForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(608, 439);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.comboBoxShape);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.textBoxWellDiameter);
            this.Controls.Add(this.textBoxdbwc2);
            this.Controls.Add(this.textBoxYo2);
            this.Controls.Add(this.textBoxDepth);
            this.Controls.Add(this.textBoxdbwc);
            this.Controls.Add(this.textBoxYo);
            this.Controls.Add(this.textBoxMaxVolume);
            this.Controls.Add(this.textBoxHeight);
            this.Controls.Add(this.textBoxTypeNo);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.LoBaseCb);
            this.Controls.Add(this.LoBaseLbl);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.labelPlate);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.comboBoxFormat);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "plateForm";
            this.Text = "Plate";
            this.Load += new System.EventHandler(this.plateForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.plateForm_Closing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void plateForm_Load(object sender, System.EventArgs e)
		{
			if (!m_bCreateNew)
			{
				mainForm mf = (mainForm)this.MdiParent;
				mf.m_xmlData.LoadPlate(m_strPlateName, m_strPlateType, this);
			}
			else
			{
				comboBoxFormat.SelectedIndex = 0;
			}

			string strFormat = "";
			if (m_strPlateType == "1")
			{
				strFormat = "96";
			}
			else if (m_strPlateType == "2")
			{
				strFormat = "384";
			}
			else if (m_strPlateType == "3")
			{
				strFormat = "1536";
			}

			Text = "Plate: " + textBoxName.Text + " (Format: " + strFormat + ")";
			labelPlate.Text = Text;
			Tag = false;
			saveButton.Hide();

			labelPlate.Select();
		}

		private void textBoxName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
		
		}

		private void textBoxTypeNo_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void comboBoxFormat_SelectedIndexChanged(object sender, System.EventArgs e)
		{
		
		}

		private void textBoxYo_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxdbwc_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxYo2_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged( sender, e );
		}

		private void textBoxdbwc2_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged( sender, e );
		}

		private void textBoxHeight_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxDepth_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxMaxVolume_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxASPOffset_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxName_TextChanged(object sender, System.EventArgs e)
		{
			Tag = true;
			if (!Text.EndsWith("*"))
			{
				Text += "*";
				saveButton.Show();
			}

			labelPlate.Text = Text;
		}

		private void comboBoxFormat_TextChanged(object sender, System.EventArgs e)
		{
			double d = 0;
			switch (comboBoxFormat.SelectedIndex)
			{
				case 0:
					d = 9.000;
					textBoxdbwc.Text = d.ToString("F3");
					textBoxdbwc2.Text = d.ToString("F3");
					break;
				case 1:
					d = 4.500;
					textBoxdbwc.Text = d.ToString("F3");
					textBoxdbwc2.Text = d.ToString("F3");
					break;
				case 2:
					d = 2.250;
					textBoxdbwc.Text = d.ToString("F3");
					textBoxdbwc2.Text = d.ToString("F3");
					break;
			}

//			textBoxASPOffset_Validating(null, null);
			textBoxMaxVolume_Validating(null, null);
			textBoxdbwc_Validating(null, null);
			textBoxdbwc2_Validating(null, null);

			textBoxName_TextChanged(sender, e);
		}

		private void textBoxTypeNo_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
		
		}

		private void comboBoxFormat_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
		
		}

		private void textBoxYo_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if( sender == null ) return;

			TextBox field = (TextBox)sender;

			string fieldName = field.Name == "textBoxYo" ? "Row offset" : "Column offset";

			double Yo = 0;

			try
			{
				Yo = Convert.ToDouble(field.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				field.Text = Yo.ToString("F2");
				return;
			}

			if (Yo < 0)
			{
				MessageBox.Show( fieldName + " must be positive.\nResetting...", fieldName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Yo = 0;
				field.Text = Yo.ToString("F2");
				return;
			}

			if (Yo > 30)
			{
				MessageBox.Show( fieldName + " must be less than 30mm.\nResetting...", fieldName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Yo = 0;
				field.Text = Yo.ToString("F2");
				return;
			}

			double test = Math.IEEERemainder(100 * Yo, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show( fieldName + " must be set in multiples of 0.01.\nResetting...", fieldName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				field.Text = Yo.ToString("F2");
				return;
			}
		}

		private void textBoxdbwc_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double DBWC = 0;

			try
			{
				DBWC = Convert.ToDouble(textBoxdbwc.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d3 = 0;
				switch (comboBoxFormat.SelectedIndex)
				{
					case 0:
						d3 = 9.000;
						textBoxdbwc.Text = d3.ToString("F3");
						break;
					case 1:
						d3 = 4.500;
						textBoxdbwc.Text = d3.ToString("F3");
						break;
					case 2:
						d3 = 2.250;
						textBoxdbwc.Text = d3.ToString("F3");
						break;
				}
				return;
			}

			double d = 0;
			double deltaValue = 0.25;
			switch (comboBoxFormat.SelectedIndex)
			{
				case 0:
					d = 9.000;
					if (DBWC < d - deltaValue || DBWC > d + deltaValue)
					{
						string strMessage = "Row spacing out of range.\nValues should range from ";
						strMessage += (d - deltaValue).ToString();
						strMessage += " to ";
						strMessage += (d + deltaValue).ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Row spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						textBoxdbwc.Text = d.ToString("F3");
						return;
					}
					break;
				case 1:
					d = 4.500;
					if (DBWC < d - deltaValue || DBWC > d + deltaValue)
					{
						string strMessage = "Row spacing out of range.\nValues should range from ";
						strMessage += (d - deltaValue).ToString();
						strMessage += " to ";
						strMessage += (d + deltaValue).ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Row spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						textBoxdbwc.Text = d.ToString("F3");
						return;
					}
					break;
				case 2:
					d = 2.250;
					if (DBWC < d - deltaValue || DBWC > d + deltaValue)
					{
						string strMessage = "Row spacing out of range.\nValues should range from ";
						strMessage += (d - deltaValue).ToString();
						strMessage += " to ";
						strMessage += (d + deltaValue).ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Row spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						textBoxdbwc.Text = d.ToString("F3");
						return;
					}
					break;
			}


			double test = Math.IEEERemainder(DBWC * 1000, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show( "Row spacing must be set in multiples of 0.001.\nResetting...", "Row spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d2 = 0;
				switch (comboBoxFormat.SelectedIndex)
				{
					case 0:
						d2 = 9.000;
						textBoxdbwc.Text = d2.ToString("F3");
						break;
					case 1:
						d2 = 4.500;
						textBoxdbwc.Text = d2.ToString("F3");
						break;
					case 2:
						d2 = 2.250;
						textBoxdbwc.Text = d2.ToString("F3");
						break;
				}
				return;
			}
		}

		private void textBoxdbwc2_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double DBWC = 0;

			try
			{
				DBWC = Convert.ToDouble(textBoxdbwc2.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d3 = 0;
				switch (comboBoxFormat.SelectedIndex)
				{
					case 0:
						d3 = 9.000;
						textBoxdbwc2.Text = d3.ToString("F3");
						break;
					case 1:
						d3 = 4.500;
						textBoxdbwc2.Text = d3.ToString("F3");
						break;
					case 2:
						d3 = 2.250;
						textBoxdbwc2.Text = d3.ToString("F3");
						break;
				}
				return;
			}

			double d = 0;
			double deltaValue = 0.25;
			switch (comboBoxFormat.SelectedIndex)
			{
				case 0:
					d = 9.000;
					if (DBWC < d - deltaValue || DBWC > d + deltaValue)
					{
						string strMessage = "Column spacing out of range.\nValues should range from ";
						strMessage += (d - deltaValue).ToString();
						strMessage += " to ";
						strMessage += (d + deltaValue).ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Column spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						textBoxdbwc2.Text = d.ToString("F3");
						return;
					}
					break;
				case 1:
					d = 4.500;
					if (DBWC < d - deltaValue || DBWC > d + deltaValue)
					{
						string strMessage = "Column spacing out of range.\nValues should range from ";
						strMessage += (d - deltaValue).ToString();
						strMessage += " to ";
						strMessage += (d + deltaValue).ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Column spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						textBoxdbwc2.Text = d.ToString("F3");
						return;
					}
					break;
				case 2:
					d = 2.250;
					if (DBWC < d - deltaValue || DBWC > d + deltaValue)
					{
						string strMessage = "Column spacing out of range.\nValues should range from ";
						strMessage += (d - deltaValue).ToString();
						strMessage += " to ";
						strMessage += (d + deltaValue).ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Column spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						textBoxdbwc2.Text = d.ToString("F3");
						return;
					}
					break;
			}


			double test = Math.IEEERemainder(DBWC * 1000, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show( "Column spacing must be set in multiples of 0.001.\nResetting...", "Column spacing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d2 = 0;
				switch (comboBoxFormat.SelectedIndex)
				{
					case 0:
						d2 = 9.000;
						textBoxdbwc2.Text = d2.ToString("F3");
						break;
					case 1:
						d2 = 4.500;
						textBoxdbwc2.Text = d2.ToString("F3");
						break;
					case 2:
						d2 = 2.250;
						textBoxdbwc2.Text = d2.ToString("F3");
						break;
				}
				return;
			}
		}

		private void textBoxHeight_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double Height = 0;

			try
			{
				Height = Convert.ToDouble(textBoxHeight.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d = 0;
				textBoxHeight.Text = d.ToString("F2");
				return;
			}

			if (Height < 0)
			{
				MessageBox.Show("Height must be positive.\nResetting...", "Height", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Height = 0;
				textBoxHeight.Text = Height.ToString("F2");
				return;
			}

			if (Height > 19)
			{
				MessageBox.Show("Height must be less than 19mm.\nResetting...", "Height", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Height = 0;
				textBoxHeight.Text = Height.ToString("F2");
				return;
			}

			double test = Math.IEEERemainder(Height * 100, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Height must be set in multiples of 0.01.\nResetting...", "Height", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxHeight.Text = Height.ToString("F2");
				return;
			}

			double Depth = Convert.ToDouble(textBoxDepth.Text);
			if (Depth > Height)
			{
				MessageBox.Show("Depth must be less than height.\nResetting...", "Depth", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Depth = 0;
				textBoxDepth.Text = Depth.ToString("F2");
				return;
			}
		}

		private void textBoxDepth_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double Depth = 0;

			try
			{
				Depth = Convert.ToDouble(textBoxDepth.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d = 0;
				textBoxDepth.Text = d.ToString("F2");
				return;
			}

			if (Depth < 0)
			{
				MessageBox.Show("Depth must be positive.\nResetting...", "Depth", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Depth = 0;
				textBoxDepth.Text = Depth.ToString("F2");
				return;
			}

			double dHeight = Convert.ToDouble(textBoxHeight.Text);
			if (Depth > dHeight)
			{
				MessageBox.Show("Depth must be less than height.\nResetting...", "Depth", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Depth = 0;
				textBoxDepth.Text = Depth.ToString("F2");
				return;
			}

			double test = Math.IEEERemainder(Depth * 100, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Depth must be set in multiples of 0.01.\nResetting...", "Depth", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxDepth.Text = Depth.ToString("F2");
				return;
			}
		}

		private void textBoxMaxVolume_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double MaxVolume = 0;

			try
			{
				MaxVolume = Convert.ToDouble(textBoxMaxVolume.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d = 0.0;
				switch (comboBoxFormat.SelectedIndex)
				{
					case 0:
						d = 2;
						break;
					case 1:
						d = 0.5;
						break;
					case 2:
						d = 0.5;
						break;
				}
				textBoxMaxVolume.Text = d.ToString("F1");
				return;
			}

		{
			double d = 0;
			double minMaxVolume = 0;
			switch (comboBoxFormat.SelectedIndex)
			{
				case 0:
					d = 500;
					minMaxVolume = 2;
					break;
				case 1:
					d = 150;
					minMaxVolume = 0.5;
					break;
				case 2:
					d = 20;
					minMaxVolume = 0.5;
					break;
			}
			if (MaxVolume > d || MaxVolume < minMaxVolume)
			{
				string strMessage = "Max volume out of range.\nValues should range from ";
				strMessage += minMaxVolume;
				strMessage += " to ";
				strMessage += d.ToString();
				strMessage += ".\nResetting...";
				MessageBox.Show(strMessage, "Max volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxMaxVolume.Text = minMaxVolume.ToString("F1");
				return;
			}
		}

			double test = Math.IEEERemainder(MaxVolume * 10, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Max volume must be set in multiples of 0.1.\nResetting...", "Max volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxMaxVolume.Text = MaxVolume.ToString("F1");
				return;
			}
		}

		private void textBoxDiameter_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double maxDiameter = 0;
			double diameter = 0.8;

			try
			{
				maxDiameter = Convert.ToDouble(textBoxWellDiameter.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxWellDiameter.Text = diameter.ToString("F2");
				return;
			}

			double d = 0;
			
			switch (comboBoxFormat.SelectedIndex)
			{
				case 0:
					d = 9.20;
					break;
				case 1:
					d = 4.60;
					break;
				case 2:
					d = 2.40;
					break;
			}
			if (maxDiameter > d || maxDiameter < diameter)
			{
				string strMessage = "Bottom well diameter out of range.\nValues should range from ";
				strMessage += diameter.ToString( "F2" );
				strMessage += "mm to ";
				strMessage += d.ToString( "F2" );
				strMessage += "mm.\nResetting...";
				MessageBox.Show(strMessage, "Max volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxWellDiameter.Text = diameter.ToString("F2");
				return;
			}

			string num = maxDiameter.ToString();
			string[] parts = num.Split('.');
			
			//double test = Math.IEEERemainder(maxDiameter * 10, 1);
			//if (test != 0)
			if( parts.Length > 1 && parts[1].Length > 2 )
			//if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Bottom well diameter must be set in multiples of 0.01.\nResetting...", "Bottom well diameter", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxWellDiameter.Text = maxDiameter.ToString("F2");
				return;
			}
			saveButton.Show();
		}


		private void comboBoxFormat_SelectedValueChanged(object sender, System.EventArgs e)
		{
			comboBoxFormat_TextChanged(sender, e);
		}

		private void plateForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;
			if (mf.m_User.UserLevel < 3)
			{
				return;
			}

			if ((bool)Tag)
			{
				DialogResult DR = MessageBox.Show("Save plate " + "\"" + textBoxName.Text + "\"" + "?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

				if (DR == DialogResult.Yes)
				{
					Save();
				}
				else if (DR == DialogResult.Cancel)
				{
					e.Cancel = true;	
				}
			}
		}

		private void textBoxName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxTypeNo.Focus();
			}
		}

		private void textBoxTypeNo_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxFormat.Focus();
			}
		}

		private void comboBoxFormat_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxShape.Focus();
			}
		}

		private void textBoxYo_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxdbwc.Focus();
			}
		}

		private void textBoxdbwc_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxYo2.Focus();
			}
		}

		private void textBoxYo2_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxdbwc2.Focus();
			}
		}

		private void textBoxdbwc2_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxHeight.Focus();
			}		
		}

		private void textBoxHeight_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxDepth.Focus();
			}
		}

		private void textBoxDepth_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxWellDiameter.Focus();
			}
		}

		private void textBoxMaxVolume_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				LoBaseCb.Focus();
			}
		}

//		private void textBoxASPOffset_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
//		{
//			if (e.KeyData == Keys.Enter)
//			{
//				textBoxName.Focus();
//			}
//		}

		private void saveButton_Click(object sender, System.EventArgs e)
		{
			Save();
			this.Close();
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void LoBaseCb_CheckedChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void textBoxWellDiameter_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void comboBoxShape_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			saveButton.Show();
		}

		private void comboBoxShape_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxYo.Focus();
			}
		}

		private void textBoxWellDiameter_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxMaxVolume.Focus();
			}
		}

		private void LoBaseCb_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxName.Focus();
			}
		}
	}
}
