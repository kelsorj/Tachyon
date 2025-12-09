using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for pressureCalculatorForm.
	/// </summary>
	public class pressureCalculatorForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonUse;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Button buttonCalculate;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label7;
		public System.Windows.Forms.TextBox textBoxGetDispense;
		public System.Windows.Forms.TextBox textBoxTryDispense;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.TextBox textBoxCalculatedLiquidFactor;
		public System.Windows.Forms.TextBox textBoxTryLiquidFactor;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public pressureCalculatorForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public DialogResult _ShowDialog(ref string strLiquidFactor)
		{
			ShowDialog();

			strLiquidFactor = textBoxCalculatedLiquidFactor.Text;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(pressureCalculatorForm));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonUse = new System.Windows.Forms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.textBoxCalculatedLiquidFactor = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.buttonCalculate = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textBoxTryLiquidFactor = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxGetDispense = new System.Windows.Forms.TextBox();
            this.textBoxTryDispense = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.buttonCancel.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.ForeColor = System.Drawing.Color.White;
            this.buttonCancel.Location = new System.Drawing.Point(224, 256);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(200, 48);
            this.buttonCancel.TabIndex = 25;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = false;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonUse
            // 
            this.buttonUse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.buttonUse.Enabled = false;
            this.buttonUse.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonUse.ForeColor = System.Drawing.Color.White;
            this.buttonUse.Location = new System.Drawing.Point(16, 256);
            this.buttonUse.Name = "buttonUse";
            this.buttonUse.Size = new System.Drawing.Size(200, 48);
            this.buttonUse.TabIndex = 24;
            this.buttonUse.Text = "C&hange to new liquid factor setting";
            this.buttonUse.UseVisualStyleBackColor = false;
            this.buttonUse.Click += new System.EventHandler(this.buttonUse_Click);
            // 
            // label14
            // 
            this.label14.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label14.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.Color.White;
            this.label14.Location = new System.Drawing.Point(216, 219);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(104, 16);
            this.label14.TabIndex = 23;
            this.label14.Text = "should be used.";
            // 
            // textBoxCalculatedLiquidFactor
            // 
            this.textBoxCalculatedLiquidFactor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxCalculatedLiquidFactor.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCalculatedLiquidFactor.Location = new System.Drawing.Point(168, 216);
            this.textBoxCalculatedLiquidFactor.Name = "textBoxCalculatedLiquidFactor";
            this.textBoxCalculatedLiquidFactor.ReadOnly = true;
            this.textBoxCalculatedLiquidFactor.Size = new System.Drawing.Size(40, 21);
            this.textBoxCalculatedLiquidFactor.TabIndex = 22;
            this.textBoxCalculatedLiquidFactor.TabStop = false;
            this.textBoxCalculatedLiquidFactor.TextChanged += new System.EventHandler(this.textBoxCalculatedLiquidFactor_TextChanged);
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label13.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(16, 217);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(144, 18);
            this.label13.TabIndex = 21;
            this.label13.Text = "A new liquid factor of";
            // 
            // label12
            // 
            this.label12.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label12.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.White;
            this.label12.Location = new System.Drawing.Point(16, 192);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(168, 20);
            this.label12.TabIndex = 20;
            this.label12.Text = "New liquid factor setting:";
            // 
            // buttonCalculate
            // 
            this.buttonCalculate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.buttonCalculate.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCalculate.ForeColor = System.Drawing.Color.White;
            this.buttonCalculate.Location = new System.Drawing.Point(16, 160);
            this.buttonCalculate.Name = "buttonCalculate";
            this.buttonCalculate.Size = new System.Drawing.Size(408, 23);
            this.buttonCalculate.TabIndex = 19;
            this.buttonCalculate.Text = "Ca&lculate";
            this.buttonCalculate.UseVisualStyleBackColor = false;
            this.buttonCalculate.Click += new System.EventHandler(this.buttonCalculate_Click);
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label11.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Location = new System.Drawing.Point(16, 136);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(416, 16);
            this.label11.TabIndex = 18;
            this.label11.Text = "What liquid factor should be used to get the correct liquid level?";
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label9.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(16, 66);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(240, 16);
            this.label9.TabIndex = 17;
            this.label9.Text = "of liquid is dispensed into the wells.";
            // 
            // textBoxTryLiquidFactor
            // 
            this.textBoxTryLiquidFactor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxTryLiquidFactor.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxTryLiquidFactor.Location = new System.Drawing.Point(248, 88);
            this.textBoxTryLiquidFactor.Name = "textBoxTryLiquidFactor";
            this.textBoxTryLiquidFactor.Size = new System.Drawing.Size(40, 21);
            this.textBoxTryLiquidFactor.TabIndex = 15;
            this.textBoxTryLiquidFactor.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxTryPressure_KeyDown);
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label7.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(16, 90);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(224, 16);
            this.label7.TabIndex = 14;
            this.label7.Text = "Current liquid factor setting used:";
            // 
            // textBoxGetDispense
            // 
            this.textBoxGetDispense.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxGetDispense.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxGetDispense.Location = new System.Drawing.Point(344, 40);
            this.textBoxGetDispense.Name = "textBoxGetDispense";
            this.textBoxGetDispense.Size = new System.Drawing.Size(40, 21);
            this.textBoxGetDispense.TabIndex = 13;
            this.textBoxGetDispense.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxGetDispense_KeyDown);
            // 
            // textBoxTryDispense
            // 
            this.textBoxTryDispense.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxTryDispense.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxTryDispense.Location = new System.Drawing.Point(64, 40);
            this.textBoxTryDispense.Name = "textBoxTryDispense";
            this.textBoxTryDispense.Size = new System.Drawing.Size(40, 21);
            this.textBoxTryDispense.TabIndex = 11;
            this.textBoxTryDispense.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxTryDispense_KeyDown);
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(112, 43);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(232, 16);
            this.label5.TabIndex = 10;
            this.label5.Text = "µl of liquid is selected to dispense, ";
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(16, 42);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 16);
            this.label4.TabIndex = 9;
            this.label4.Text = "When";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.buttonCancel);
            this.groupBox1.Controls.Add(this.buttonUse);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.textBoxCalculatedLiquidFactor);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.buttonCalculate);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.textBoxTryLiquidFactor);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.textBoxGetDispense);
            this.groupBox1.Controls.Add(this.textBoxTryDispense);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(16, 16);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(440, 320);
            this.groupBox1.TabIndex = 14;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Liquid factor correction calculator";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(392, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(16, 16);
            this.label1.TabIndex = 26;
            this.label1.Text = "µl";
            // 
            // pressureCalculatorForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(472, 350);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "pressureCalculatorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Liquid factor correction calculator";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

		}
		#endregion

		private void buttonUse_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{

			DialogResult = DialogResult.Cancel;
		}

		private void buttonCalculate_Click(object sender, System.EventArgs e)
		{
			double TryLiquidFactor;
			double TryDispense;
			double GetDispense;

			try
			{
				TryDispense = Convert.ToDouble(textBoxTryDispense.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxTryDispense.Text = "0";
				return;
			}

			try
			{
				GetDispense = Convert.ToDouble(textBoxGetDispense.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxGetDispense.Text = "0";
				return;
			}

			try
			{
				TryLiquidFactor = Convert.ToDouble(textBoxTryLiquidFactor.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxTryLiquidFactor.Text = "1";
				return;
			}		

//			try
//			{
//				TryLiquidFactor = Convert.ToDouble(textBoxTryLiquidFactor.Text);
//				TryDispense = Convert.ToDouble(textBoxTryDispense.Text);
//				GetDispense = Convert.ToDouble(textBoxGetDispense.Text);
//			}
//			catch (Exception exception)
//			{
//				MessageBox.Show(exception.Message, "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//				return;
//			}
			
			if (0 == GetDispense)
			{
				MessageBox.Show("Can't calculate new value when you get nothing in your wells.", "Divide by zero", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			double CalculatedLiquidFactor = (TryDispense / GetDispense) * TryLiquidFactor;

			if (CalculatedLiquidFactor > 2.5)
			{
				MessageBox.Show("Calculated value is greater than possible setting.\nValue set to maximum!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				CalculatedLiquidFactor = 2.5;
			}
			else if (CalculatedLiquidFactor < 0.8)
			{
				MessageBox.Show("Calculated value is less than possible setting.\nValue set to minimum!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				CalculatedLiquidFactor = 0.8;
			}
			textBoxCalculatedLiquidFactor.Text = CalculatedLiquidFactor.ToString("F2");
		}

		private void textBoxTryDispense_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxGetDispense.Focus();
			}
		}

		private void textBoxGetDispense_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxTryLiquidFactor.Focus();
			}
		}

		private void textBoxTryPressure_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxTryDispense.Focus();
			}
		}

		private void textBoxCalculatedLiquidFactor_TextChanged(object sender, System.EventArgs e)
		{
			if (textBoxCalculatedLiquidFactor.Text.Length > 0)
			{
				buttonUse.Enabled = true;
			}
			else
			{
				buttonUse.Enabled = false;
			}
		}

//		private void textBoxTryDispense_Validating(object sender, System.ComponentModel.CancelEventArgs e)
//		{
//			double TryDispense = 0;
//
//			try
//			{
//				TryDispense = Convert.ToDouble(textBoxTryDispense.Text);
//			}
//			catch (Exception exception)
//			{
//				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//				textBoxTryDispense.Text = "0";
//				return;
//			}
//		}
//
//		private void textBoxGetDispense_Validating(object sender, System.ComponentModel.CancelEventArgs e)
//		{
//			double GetDispense = 0;
//
//			try
//			{
//				GetDispense = Convert.ToDouble(textBoxGetDispense.Text);
//			}
//			catch (Exception exception)
//			{
//				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//				textBoxGetDispense.Text = "0";
//				return;
//			}
//		}
//
//		private void textBoxTryLiquidFactor_Validating(object sender, System.ComponentModel.CancelEventArgs e)
//		{
//			double TryLiquidFactor = 0;
//
//			try
//			{
//				TryLiquidFactor = Convert.ToDouble(textBoxTryLiquidFactor.Text);
//			}
//			catch (Exception exception)
//			{
//				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//				textBoxTryLiquidFactor.Text = "1";
//				return;
//			}		
//		}
	}
}
