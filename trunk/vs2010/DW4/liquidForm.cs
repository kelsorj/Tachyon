using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for liquidForm.
	/// </summary>
	public class liquidForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		public System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Label labelLiquid;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// helpers
		private bool m_bCreateNew = false;
		private System.Windows.Forms.Button saveButton;
		public System.Windows.Forms.TextBox textBoxLiquidFactor;
		private System.Windows.Forms.Button buttonCalculate;
		private System.Windows.Forms.Button cancelButton;
		private string m_strLiquidName;

		public liquidForm(string strLiquidName, bool bCreateNew)
		{
			InitializeComponent();

			string strLabel = "Liquid: " + strLiquidName;
			Text = strLabel;
			labelLiquid.Text = strLabel;
			
			m_bCreateNew = bCreateNew;
			m_strLiquidName = strLiquidName;
			textBoxName.Text = strLiquidName;

			// load data form xml file and populate
			// must be done in liquidForm_Load()
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(liquidForm));
            this.labelLiquid = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.textBoxLiquidFactor = new System.Windows.Forms.TextBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.buttonCalculate = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // labelLiquid
            // 
            this.labelLiquid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelLiquid.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLiquid.ForeColor = System.Drawing.Color.White;
            this.labelLiquid.Location = new System.Drawing.Point(24, 24);
            this.labelLiquid.Name = "labelLiquid";
            this.labelLiquid.Size = new System.Drawing.Size(536, 24);
            this.labelLiquid.TabIndex = 1;
            this.labelLiquid.Text = "Liquid:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(384, 120);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(176, 160);
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(32, 83);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Name:";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(32, 106);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 16);
            this.label2.TabIndex = 9;
            this.label2.Text = "Liquid factor:";
            // 
            // textBoxName
            // 
            this.textBoxName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxName.Location = new System.Drawing.Point(128, 80);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(328, 21);
            this.textBoxName.TabIndex = 2;
            this.textBoxName.TextChanged += new System.EventHandler(this.textBoxName_TextChanged);
            this.textBoxName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxName_KeyDown);
            this.textBoxName.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxName_Validating);
            // 
            // textBoxLiquidFactor
            // 
            this.textBoxLiquidFactor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxLiquidFactor.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxLiquidFactor.Location = new System.Drawing.Point(128, 104);
            this.textBoxLiquidFactor.Name = "textBoxLiquidFactor";
            this.textBoxLiquidFactor.Size = new System.Drawing.Size(40, 21);
            this.textBoxLiquidFactor.TabIndex = 3;
            this.textBoxLiquidFactor.Text = "1.00";
            this.textBoxLiquidFactor.TextChanged += new System.EventHandler(this.textBoxPressure_TextChanged);
            this.textBoxLiquidFactor.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxPressure_KeyDown);
            this.textBoxLiquidFactor.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPressure_Validating);
            // 
            // saveButton
            // 
            this.saveButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(32, 280);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(72, 23);
            this.saveButton.TabIndex = 22;
            this.saveButton.Text = "&Save";
            this.saveButton.Visible = false;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // buttonCalculate
            // 
            this.buttonCalculate.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCalculate.ForeColor = System.Drawing.Color.White;
            this.buttonCalculate.Location = new System.Drawing.Point(176, 104);
            this.buttonCalculate.Name = "buttonCalculate";
            this.buttonCalculate.Size = new System.Drawing.Size(104, 23);
            this.buttonCalculate.TabIndex = 23;
            this.buttonCalculate.Text = "Ca&lculate...";
            this.buttonCalculate.Click += new System.EventHandler(this.buttonCalculate_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.ForeColor = System.Drawing.Color.White;
            this.cancelButton.Location = new System.Drawing.Point(480, 280);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 23);
            this.cancelButton.TabIndex = 24;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // liquidForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(568, 318);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.buttonCalculate);
            this.Controls.Add(this.textBoxLiquidFactor);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.labelLiquid);
            this.Controls.Add(this.saveButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "liquidForm";
            this.Text = "Liquid";
            this.Load += new System.EventHandler(this.liquidForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.liquidForm_Closing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		public void Save()
		{
			if( !ValidateFactor() ) return;
			mainForm mf = (mainForm)this.MdiParent;

			if (mf.m_User.UserLevel < 3)
			{
				MessageBox.Show(this, "Only administrators can change liquid data.\nNew liquid data NOT saved...", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			m_strLiquidName = textBoxName.Text;

			if( mf.m_xmlData.LiquidExist( m_strLiquidName ) )
			{
				DialogResult res = MessageBox.Show(this, "Another liquid exists with this name.\r\nDo you want to overwrite it?", "Save", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
				if( res == DialogResult.Cancel )
				{
					return;
				}
			}

			mf.m_xmlData.DeleteLiquid(m_strLiquidName, mf.treeView);			
			mf.m_xmlData.SaveLiquid(this, mf.treeView);

			Text = "Liquid: " + textBoxName.Text;
			labelLiquid.Text = Text;
			Tag = false;
			saveButton.Hide();
		}

		private void liquidForm_Load(object sender, System.EventArgs e)
		{
			if (!m_bCreateNew)
			{
				mainForm mf = (mainForm)this.MdiParent;
				mf.m_xmlData.LoadLiquid(m_strLiquidName, this);
			}

			Text = "Liquid: " + textBoxName.Text;
			labelLiquid.Text = Text;
			Tag = false;
			saveButton.Hide();

			labelLiquid.Select();
		}

		private void textBoxName_TextChanged(object sender, System.EventArgs e)
		{
			Tag = true;
			if (!Text.EndsWith("*"))
			{
				Text += "*";
			}
			labelLiquid.Text = Text;
			saveButton.Show();
		}

		private void textBoxPressure_TextChanged(object sender, System.EventArgs e)
		{
			textBoxName_TextChanged(sender, e);
		}

		private void liquidForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;
			if (mf.m_User.UserLevel < 3)
			{
				return;
			}

			if ((bool)Tag)
			{
				DialogResult DR = MessageBox.Show("Save liquid " + "\"" + textBoxName.Text + "\"" + "?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

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

		private void textBoxName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
		
		}

		private void textBoxPressure_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if( !ValidateFactor() ) return;
		}

		bool ValidateFactor()
		{
			double LiquidFactor = 0;

			try
			{
				LiquidFactor = Convert.ToDouble(textBoxLiquidFactor.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				LiquidFactor = 1;
				textBoxLiquidFactor.Text = LiquidFactor.ToString("F2");
				return false;
			}

			if (LiquidFactor < 0.8 || LiquidFactor > 2.5)
			{
				MessageBox.Show("Liquid factor out of range. Must be between 0.8 and 2.5\nResetting...", "Liquid Factor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				LiquidFactor = 1;
				textBoxLiquidFactor.Text = LiquidFactor.ToString("F2");
				return false;
			}

			double test = Math.IEEERemainder(100 * LiquidFactor, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Liquid factor must be set in multiples of 0.01\nResetting...", "Liquid factor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxLiquidFactor.Text = LiquidFactor.ToString("F2");
				return false;
			}
			return true;
		}

		private void textBoxName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxLiquidFactor.Focus();
			}
		}

		private void textBoxPressure_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxName.Focus();
			}
		}

		private void saveButton_Click(object sender, System.EventArgs e)
		{
			Save();
			this.Close();
		}

		private void buttonCalculate_Click(object sender, System.EventArgs e)
		{
			pressureCalculatorForm PCF = new pressureCalculatorForm();
			string strPressure = textBoxLiquidFactor.Text;
			DialogResult DR = PCF._ShowDialog(ref strPressure);
			if (DR == DialogResult.OK)
			{
				textBoxLiquidFactor.Text = strPressure;
			}
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}
