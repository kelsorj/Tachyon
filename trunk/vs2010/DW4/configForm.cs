using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for configForm.
	/// </summary>
	public class configForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.ComboBox comPort;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label labelConfig;
		private System.Windows.Forms.Button saveButton;
		private System.Windows.Forms.Button cancelButton;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public configForm()
		{
			InitializeComponent();

			string strLabel = "Commnication";
			Text = strLabel;
			labelConfig.Text = strLabel;

			// load data form xml file and populate
			// must be done in configForm_Load()
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(configForm));
            this.comPort = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.labelConfig = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // comPort
            // 
            this.comPort.DropDownWidth = 121;
            this.comPort.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comPort.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10"});
            this.comPort.Location = new System.Drawing.Point(110, 69);
            this.comPort.Name = "comPort";
            this.comPort.Size = new System.Drawing.Size(66, 21);
            this.comPort.TabIndex = 1;
            this.comPort.SelectedValueChanged += new System.EventHandler(this.comPort_SelectedValueChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(16, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "Ports (COM):";
            // 
            // labelConfig
            // 
            this.labelConfig.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelConfig.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConfig.ForeColor = System.Drawing.Color.White;
            this.labelConfig.Location = new System.Drawing.Point(16, 16);
            this.labelConfig.Name = "labelConfig";
            this.labelConfig.Size = new System.Drawing.Size(418, 27);
            this.labelConfig.TabIndex = 6;
            this.labelConfig.Text = "Communications Port";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(282, 110);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(151, 139);
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // saveButton
            // 
            this.saveButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(16, 256);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(72, 23);
            this.saveButton.TabIndex = 22;
            this.saveButton.Text = "&Save";
            this.saveButton.Visible = false;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.ForeColor = System.Drawing.Color.White;
            this.cancelButton.Location = new System.Drawing.Point(350, 256);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(74, 23);
            this.cancelButton.TabIndex = 23;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // configForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(442, 293);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.labelConfig);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comPort);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "configForm";
            this.Text = "Communication";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.configForm_Closing);
            this.Load += new System.EventHandler(this.configForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		public void Save()
		{
			mainForm mf = (mainForm)this.MdiParent;

			if (mf.m_User.UserLevel < 3)
			{
				MessageBox.Show(this, "Only administrators can change communication settings.\nNew communication settings NOT saved...", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			mf.m_xmlData.SaveConfig(this);

			Text = "Communication";
			labelConfig.Text = Text;
			Tag = false;
			saveButton.Hide();
		}

		private void configForm_Load(object sender, System.EventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;
			mf.m_xmlData.LoadConfig(this);

			Text = "Communication";
			labelConfig.Text = Text;
			Tag = false;
			saveButton.Hide();
		}

		private void comPort_SelectedValueChanged(object sender, System.EventArgs e)
		{
			Tag = true;
			if (!Text.EndsWith("*"))
			{
				Text += "*";
			}
			labelConfig.Text = Text;
			saveButton.Show();
		}

		private void configForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;
			if (mf.m_User.UserLevel < 3)
			{
				return;
			}

			if ((bool)Tag)
			{
				DialogResult DR = MessageBox.Show("Save communication?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

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

		private void saveButton_Click(object sender, System.EventArgs e)
		{
			Save();
			this.Close();
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}
