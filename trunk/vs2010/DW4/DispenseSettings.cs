using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for Login.
	/// </summary>
	public class DispenseSettings : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Button saveButton;
		public System.Windows.Forms.Panel panelPressure;
		private System.Windows.Forms.Label label5;
		public System.Windows.Forms.NumericUpDown numBoxPressure;
		private System.Windows.Forms.Label label8;
		public System.Windows.Forms.CheckBox checkBoxOverride;
		public System.Windows.Forms.TextBox textBoxLiquidFactor;
		private System.Windows.Forms.Button buttonPressureCalculator;
		private System.Windows.Forms.Button buttonLiquidSelector;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Label label1;

		private mainForm m_mf = null;
		private programForm m_pf = null;
		private System.Windows.Forms.Label labelHeader;
		private System.Windows.Forms.Button cancelButton;
		private int m_inlet = 0;

		public DispenseSettings()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DispenseSettings));
            this.saveButton = new System.Windows.Forms.Button();
            this.labelHeader = new System.Windows.Forms.Label();
            this.panelPressure = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.numBoxPressure = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxOverride = new System.Windows.Forms.CheckBox();
            this.textBoxLiquidFactor = new System.Windows.Forms.TextBox();
            this.buttonPressureCalculator = new System.Windows.Forms.Button();
            this.buttonLiquidSelector = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.panelPressure.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBoxPressure)).BeginInit();
            this.SuspendLayout();
            // 
            // saveButton
            // 
            this.saveButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(16, 160);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(72, 23);
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "&Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // labelHeader
            // 
            this.labelHeader.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHeader.ForeColor = System.Drawing.Color.White;
            this.labelHeader.Location = new System.Drawing.Point(0, 16);
            this.labelHeader.Name = "labelHeader";
            this.labelHeader.Size = new System.Drawing.Size(256, 24);
            this.labelHeader.TabIndex = 6;
            this.labelHeader.Text = "Inlet 1";
            this.labelHeader.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // panelPressure
            // 
            this.panelPressure.BackColor = System.Drawing.Color.Transparent;
            this.panelPressure.Controls.Add(this.label5);
            this.panelPressure.Controls.Add(this.numBoxPressure);
            this.panelPressure.Controls.Add(this.label8);
            this.panelPressure.Location = new System.Drawing.Point(16, 120);
            this.panelPressure.Name = "panelPressure";
            this.panelPressure.Size = new System.Drawing.Size(232, 32);
            this.panelPressure.TabIndex = 28;
            this.panelPressure.Visible = false;
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(1, 4);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 20);
            this.label5.TabIndex = 21;
            this.label5.Text = "Pressure";
            // 
            // numBoxPressure
            // 
            this.numBoxPressure.BackColor = System.Drawing.SystemColors.Window;
            this.numBoxPressure.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.numBoxPressure.Font = new System.Drawing.Font("Verdana", 8.25F);
            this.numBoxPressure.Location = new System.Drawing.Point(79, 2);
            this.numBoxPressure.Maximum = new decimal(new int[] {
            550,
            0,
            0,
            0});
            this.numBoxPressure.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numBoxPressure.Name = "numBoxPressure";
            this.numBoxPressure.Size = new System.Drawing.Size(89, 21);
            this.numBoxPressure.TabIndex = 20;
            this.numBoxPressure.Value = new decimal(new int[] {
            550,
            0,
            0,
            0});
            this.numBoxPressure.Validating += new System.ComponentModel.CancelEventHandler(this.numBoxPressure_Validating);
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(171, 6);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(48, 19);
            this.label8.TabIndex = 19;
            this.label8.Text = "mBar";
            // 
            // checkBoxOverride
            // 
            this.checkBoxOverride.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOverride.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBoxOverride.Location = new System.Drawing.Point(16, 106);
            this.checkBoxOverride.Name = "checkBoxOverride";
            this.checkBoxOverride.Size = new System.Drawing.Size(200, 16);
            this.checkBoxOverride.TabIndex = 27;
            this.checkBoxOverride.Text = "Override default pressure";
            this.checkBoxOverride.UseVisualStyleBackColor = false;
            this.checkBoxOverride.CheckStateChanged += new System.EventHandler(this.checkBoxOverride_CheckStateChanged);
            // 
            // textBoxLiquidFactor
            // 
            this.textBoxLiquidFactor.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxLiquidFactor.ForeColor = System.Drawing.Color.Black;
            this.textBoxLiquidFactor.Location = new System.Drawing.Point(96, 82);
            this.textBoxLiquidFactor.Name = "textBoxLiquidFactor";
            this.textBoxLiquidFactor.Size = new System.Drawing.Size(88, 21);
            this.textBoxLiquidFactor.TabIndex = 24;
            this.textBoxLiquidFactor.Text = "1,00";
            this.textBoxLiquidFactor.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxLiquidFactor_Validating);
            // 
            // buttonPressureCalculator
            // 
            this.buttonPressureCalculator.Location = new System.Drawing.Point(192, 82);
            this.buttonPressureCalculator.Name = "buttonPressureCalculator";
            this.buttonPressureCalculator.Size = new System.Drawing.Size(24, 22);
            this.buttonPressureCalculator.TabIndex = 26;
            this.buttonPressureCalculator.Text = "...";
            this.buttonPressureCalculator.Click += new System.EventHandler(this.buttonPressureCalculator_Click);
            // 
            // buttonLiquidSelector
            // 
            this.buttonLiquidSelector.Location = new System.Drawing.Point(192, 56);
            this.buttonLiquidSelector.Name = "buttonLiquidSelector";
            this.buttonLiquidSelector.Size = new System.Drawing.Size(24, 22);
            this.buttonLiquidSelector.TabIndex = 22;
            this.buttonLiquidSelector.Text = "...";
            this.buttonLiquidSelector.Click += new System.EventHandler(this.buttonLiquidSelector_Click);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(16, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 23);
            this.label4.TabIndex = 25;
            this.label4.Text = "Liquid";
            // 
            // textBoxName
            // 
            this.textBoxName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxName.ForeColor = System.Drawing.Color.Black;
            this.textBoxName.Location = new System.Drawing.Point(96, 56);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(88, 21);
            this.textBoxName.TabIndex = 21;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 84);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 20);
            this.label1.TabIndex = 23;
            this.label1.Text = "Liq. Fact.";
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.ForeColor = System.Drawing.Color.White;
            this.cancelButton.Location = new System.Drawing.Point(176, 160);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 23);
            this.cancelButton.TabIndex = 29;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // DispenseSettings
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(258, 192);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.panelPressure);
            this.Controls.Add(this.checkBoxOverride);
            this.Controls.Add(this.textBoxLiquidFactor);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.buttonPressureCalculator);
            this.Controls.Add(this.buttonLiquidSelector);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelHeader);
            this.Controls.Add(this.saveButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DispenseSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Inlet Settings";
            this.Load += new System.EventHandler(this.DispenseSettings_Load);
            this.panelPressure.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numBoxPressure)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void saveButton_Click(object sender, System.EventArgs e)
		{
			m_pf.inlets[m_inlet] = new Inlet(textBoxName.Text, textBoxLiquidFactor.Text, numBoxPressure.Value.ToString());
			DialogResult = DialogResult.OK;
			Close();
		}

		public DialogResult _ShowDialog(int inlet, dispenseCard card, mainForm mf, programForm pf)
		{
			m_mf = mf;
			m_pf = pf;
			m_inlet = inlet;

			this.labelHeader.Text = "Inlet " + (inlet+1);
			this.textBoxLiquidFactor.Text = card.textBoxLiquidFactor.Text;
			this.textBoxName.Text = card.textBoxName.Text;
			this.numBoxPressure.Value = Convert.ToDecimal(card.textBoxPressure.Text);

			ShowDialog();
			return DialogResult;
		}

		private void DispenseSettings_Load(object sender, System.EventArgs e)
		{
			//m_mf.m_xmlData.LoadUsersForLogin(this);

			//comboBoxUsername.SelectedIndex = 0;
			if(numBoxPressure.Value < 550)
			{
				checkBoxOverride.Visible = false;
				Point p = new Point(16,105);
				panelPressure.Location = p;
				panelPressure.Visible = true;
			}
		}

		private void buttonLiquidSelector_Click(object sender, System.EventArgs e)
		{
			liquidSelectForm LSF = new liquidSelectForm();
			string strLiquidName = textBoxName.Text;
			string strPressure = textBoxLiquidFactor.Text;

			//programForm pf = (programForm)Parent.Parent.Parent;

			DialogResult DR = LSF._ShowDialog(ref strLiquidName, ref strPressure, m_mf);
			if (DR == DialogResult.OK)
			{
				textBoxName.Text = strLiquidName;
				textBoxLiquidFactor.Text = strPressure;
			}
		}

		private void buttonPressureCalculator_Click(object sender, System.EventArgs e)
		{
			pressureCalculatorForm PCF = new pressureCalculatorForm();
			string strPressure = textBoxLiquidFactor.Text;
			DialogResult DR = PCF._ShowDialog(ref strPressure);
			if (DR == DialogResult.OK)
			{
				textBoxLiquidFactor.Text = strPressure;
			}
		}

		private void checkBoxOverride_CheckStateChanged(object sender, System.EventArgs e)
		{
			checkBoxOverride.Visible = false;
			Point p = new Point(16,105);
			panelPressure.Location = p;
			panelPressure.Visible = true;
		}

		private void numBoxPressure_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(numBoxPressure.Value > 550)
			{
				numBoxPressure.Value = 550;
			}
			else if(numBoxPressure.Value < 30)
			{
				numBoxPressure.Value = 30;
			}
		}

		private void textBoxLiquidFactor_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			textBoxLiquidFactor.Text = textBoxLiquidFactor.Text.Replace(",",".");
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
			}

			if (LiquidFactor > 2.5 || LiquidFactor < 0.8)
			{
				MessageBox.Show("Liquid factor out of range. Must be between 0.8 and 2.5\nResetting...", "Liquid factor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				LiquidFactor = 1;
				textBoxLiquidFactor.Text = LiquidFactor.ToString("F2");
			}
			
			double test = Math.IEEERemainder(100 * LiquidFactor, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Liquid factor must be set in multiples of 0.01\nResetting...", "Liquid factor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxLiquidFactor.Text = LiquidFactor.ToString("F2");
			}
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}
