using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for dispenseCard.
	/// </summary>
	public class dispenseCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.ComboBox comboBoxInlet;
		public System.Windows.Forms.Label labelProgramStep;
		private System.Windows.Forms.Label label6;
		public System.Windows.Forms.TextBox textBoxVolume;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.TextBox textBoxLiquidFactor;
		private System.Windows.Forms.Button buttonSettings;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label8;
		public System.Windows.Forms.TextBox textBoxPressure;
		private System.Windows.Forms.Label minimizeLabel;
		private System.Windows.Forms.PictureBox maximize;
		private System.Windows.Forms.PictureBox minimize;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.PictureBox icon;
		bool m_minimize = true;

		public dispenseCard()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(dispenseCard));
			this.deleteButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.maximize = new System.Windows.Forms.PictureBox();
			this.minimize = new System.Windows.Forms.PictureBox();
			this.minimizeLabel = new System.Windows.Forms.Label();
			this.textBoxPressure = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.buttonSettings = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.textBoxVolume = new System.Windows.Forms.TextBox();
			this.textBoxLiquidFactor = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxInlet = new System.Windows.Forms.ComboBox();
			this.labelProgramStep = new System.Windows.Forms.Label();
			this.icon = new System.Windows.Forms.PictureBox();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// deleteButton
			// 
			this.deleteButton.BackColor = System.Drawing.Color.IndianRed;
			this.deleteButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("deleteButton.BackgroundImage")));
			this.deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.deleteButton.Location = new System.Drawing.Point(368, 10);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(13, 12);
			this.deleteButton.TabIndex = 0;
			this.deleteButton.TabStop = false;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.panel1.Controls.Add(this.icon);
			this.panel1.Controls.Add(this.maximize);
			this.panel1.Controls.Add(this.minimize);
			this.panel1.Controls.Add(this.minimizeLabel);
			this.panel1.Controls.Add(this.textBoxPressure);
			this.panel1.Controls.Add(this.label5);
			this.panel1.Controls.Add(this.label8);
			this.panel1.Controls.Add(this.buttonSettings);
			this.panel1.Controls.Add(this.label7);
			this.panel1.Controls.Add(this.textBoxVolume);
			this.panel1.Controls.Add(this.textBoxLiquidFactor);
			this.panel1.Controls.Add(this.label6);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.textBoxName);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.comboBoxInlet);
			this.panel1.Controls.Add(this.deleteButton);
			this.panel1.Cursor = System.Windows.Forms.Cursors.Default;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(400, 190);
			this.panel1.TabIndex = 0;
			this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
			// 
			// maximize
			// 
			this.maximize.BackColor = System.Drawing.Color.IndianRed;
			this.maximize.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("maximize.BackgroundImage")));
			this.maximize.Location = new System.Drawing.Point(352, 10);
			this.maximize.Name = "maximize";
			this.maximize.Size = new System.Drawing.Size(13, 12);
			this.maximize.TabIndex = 29;
			this.maximize.TabStop = false;
			this.maximize.Visible = false;
			this.maximize.Click += new System.EventHandler(this.maximize_Click);
			// 
			// minimize
			// 
			this.minimize.BackColor = System.Drawing.Color.IndianRed;
			this.minimize.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("minimize.BackgroundImage")));
			this.minimize.Location = new System.Drawing.Point(352, 10);
			this.minimize.Name = "minimize";
			this.minimize.Size = new System.Drawing.Size(13, 12);
			this.minimize.TabIndex = 28;
			this.minimize.TabStop = false;
			this.minimize.Click += new System.EventHandler(this.minimize_Click);
			// 
			// minimizeLabel
			// 
			this.minimizeLabel.BackColor = System.Drawing.Color.Transparent;
			this.minimizeLabel.Location = new System.Drawing.Point(48, 24);
			this.minimizeLabel.Name = "minimizeLabel";
			this.minimizeLabel.Size = new System.Drawing.Size(336, 23);
			this.minimizeLabel.TabIndex = 27;
			this.minimizeLabel.Visible = false;
			// 
			// textBoxPressure
			// 
			this.textBoxPressure.Cursor = System.Windows.Forms.Cursors.Default;
			this.textBoxPressure.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxPressure.ForeColor = System.Drawing.Color.Black;
			this.textBoxPressure.Location = new System.Drawing.Point(192, 144);
			this.textBoxPressure.Name = "textBoxPressure";
			this.textBoxPressure.ReadOnly = true;
			this.textBoxPressure.Size = new System.Drawing.Size(120, 21);
			this.textBoxPressure.TabIndex = 26;
			this.textBoxPressure.Text = "550";
			this.textBoxPressure.TextChanged += new System.EventHandler(this.textBoxPressure_TextChanged);
			// 
			// label5
			// 
			this.label5.BackColor = System.Drawing.Color.Transparent;
			this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label5.Location = new System.Drawing.Point(112, 146);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(67, 20);
			this.label5.TabIndex = 25;
			this.label5.Text = "Pressure";
			// 
			// label8
			// 
			this.label8.BackColor = System.Drawing.Color.Transparent;
			this.label8.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label8.Location = new System.Drawing.Point(320, 146);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(48, 19);
			this.label8.TabIndex = 23;
			this.label8.Text = "mBar";
			// 
			// buttonSettings
			// 
			this.buttonSettings.BackColor = System.Drawing.SystemColors.Control;
			this.buttonSettings.Location = new System.Drawing.Point(320, 72);
			this.buttonSettings.Name = "buttonSettings";
			this.buttonSettings.Size = new System.Drawing.Size(56, 20);
			this.buttonSettings.TabIndex = 22;
			this.buttonSettings.Text = "&Settings";
			this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click_1);
			// 
			// label7
			// 
			this.label7.BackColor = System.Drawing.Color.Transparent;
			this.label7.Font = new System.Drawing.Font("Verdana", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label7.Location = new System.Drawing.Point(112, 16);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(112, 27);
			this.label7.TabIndex = 14;
			this.label7.Text = "Dispense";
			// 
			// textBoxVolume
			// 
			this.textBoxVolume.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxVolume.ForeColor = System.Drawing.Color.Black;
			this.textBoxVolume.Location = new System.Drawing.Point(192, 96);
			this.textBoxVolume.Name = "textBoxVolume";
			this.textBoxVolume.Size = new System.Drawing.Size(120, 21);
			this.textBoxVolume.TabIndex = 3;
			this.textBoxVolume.Text = "";
			this.textBoxVolume.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxVolume_KeyDown);
			this.textBoxVolume.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxVolume_Validating);
			this.textBoxVolume.TextChanged += new System.EventHandler(this.textBoxVolume_TextChanged);
			// 
			// textBoxLiquidFactor
			// 
			this.textBoxLiquidFactor.Cursor = System.Windows.Forms.Cursors.Default;
			this.textBoxLiquidFactor.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxLiquidFactor.ForeColor = System.Drawing.Color.Black;
			this.textBoxLiquidFactor.Location = new System.Drawing.Point(192, 120);
			this.textBoxLiquidFactor.Name = "textBoxLiquidFactor";
			this.textBoxLiquidFactor.ReadOnly = true;
			this.textBoxLiquidFactor.Size = new System.Drawing.Size(120, 21);
			this.textBoxLiquidFactor.TabIndex = 4;
			this.textBoxLiquidFactor.Text = "";
			// 
			// label6
			// 
			this.label6.BackColor = System.Drawing.Color.Transparent;
			this.label6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label6.Location = new System.Drawing.Point(320, 99);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(48, 19);
			this.label6.TabIndex = 9;
			this.label6.Text = "µl";
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.Transparent;
			this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label4.Location = new System.Drawing.Point(112, 49);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(72, 23);
			this.label4.TabIndex = 5;
			this.label4.Text = "Liquid";
			// 
			// textBoxName
			// 
			this.textBoxName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxName.ForeColor = System.Drawing.Color.Black;
			this.textBoxName.Location = new System.Drawing.Point(192, 48);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.ReadOnly = true;
			this.textBoxName.Size = new System.Drawing.Size(120, 21);
			this.textBoxName.TabIndex = 0;
			this.textBoxName.Text = "";
			this.textBoxName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxName_KeyDown);
			this.textBoxName.TextChanged += new System.EventHandler(this.textBoxName_TextChanged);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(112, 122);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(67, 20);
			this.label1.TabIndex = 3;
			this.label1.Text = "Liq. Fact.";
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.Color.Transparent;
			this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label3.Location = new System.Drawing.Point(112, 96);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(75, 20);
			this.label3.TabIndex = 1;
			this.label3.Text = "Volume";
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.Location = new System.Drawing.Point(112, 72);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 23);
			this.label2.TabIndex = 1;
			this.label2.Text = "Inlet";
			// 
			// comboBoxInlet
			// 
			this.comboBoxInlet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxInlet.DropDownWidth = 56;
			this.comboBoxInlet.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxInlet.ItemHeight = 12;
			this.comboBoxInlet.Items.AddRange(new object[] {
															   "1",
															   "2",
															   "3",
															   "4"});
			this.comboBoxInlet.Location = new System.Drawing.Point(192, 72);
			this.comboBoxInlet.Name = "comboBoxInlet";
			this.comboBoxInlet.Size = new System.Drawing.Size(120, 20);
			this.comboBoxInlet.TabIndex = 2;
			this.comboBoxInlet.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxInlet_KeyDown);
			this.comboBoxInlet.SelectedIndexChanged += new System.EventHandler(this.comboBoxInlet_SelectedIndexChanged);
			// 
			// labelProgramStep
			// 
			this.labelProgramStep.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelProgramStep.ForeColor = System.Drawing.Color.White;
			this.labelProgramStep.Location = new System.Drawing.Point(400, 8);
			this.labelProgramStep.Name = "labelProgramStep";
			this.labelProgramStep.Size = new System.Drawing.Size(40, 23);
			this.labelProgramStep.TabIndex = 2;
			this.labelProgramStep.Text = "0";
			this.labelProgramStep.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.labelProgramStep.MouseDown += new System.Windows.Forms.MouseEventHandler(this.labelProgramStep_MouseDown);
			// 
			// icon
			// 
			this.icon.BackColor = System.Drawing.Color.Transparent;
			this.icon.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("icon.BackgroundImage")));
			this.icon.Location = new System.Drawing.Point(10, 14);
			this.icon.Name = "icon";
			this.icon.Size = new System.Drawing.Size(26, 25);
			this.icon.TabIndex = 30;
			this.icon.TabStop = false;
			this.icon.Visible = false;
			// 
			// dispenseCard
			// 
			this.Controls.Add(this.labelProgramStep);
			this.Controls.Add(this.panel1);
			this.Name = "dispenseCard";
			this.Size = new System.Drawing.Size(440, 190);
			this.Load += new System.EventHandler(this.dispenseCard_Load);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public void ValidateAll()
		{
			programForm pf = (programForm)Parent.Parent;
			if( pf.m_loBase )
			{
				comboBoxInlet.SelectedIndex = 3;
				comboBoxInlet.Enabled = false;
			}
			else
			{
				comboBoxInlet.Enabled = true;
			}

			textBoxVolume_Validating(null, null);
		}

		private void deleteButton_Click(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;

			if (pf.IsPartOfRepeat(this))
			{
				MessageBox.Show("Can not delete cards within repeats.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			DialogResult DR = MessageBox.Show("Are you sure you want to delete this step?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (DR == DialogResult.Yes)
			{
				pf.DeleteProgramCard(this);
			}
		}

		private void labelProgramStep_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			//DoDragDrop("dispensecard", DragDropEffects.All);
		}

		private void panel1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.ProgramCardToBeMoved = this;
			//DoDragDrop("dispensecard", DragDropEffects.All);
		}

		private void dispenseCard_Load(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			double dVolume = 2;
			textBoxVolume.Text = dVolume.ToString("F1");

			textBoxLiquidFactor.Text = pf.inlets[0].LiqFact;

			//this.comboBoxInlet_SelectedIndexChanged(null, null);
			
			pf.m_dispenseCardList.Add(this);			

			if( pf.m_loBase )
			{
				comboBoxInlet.SelectedIndex = 3;
				comboBoxInlet.Enabled = false;
			}
			else
			{
				comboBoxInlet.SelectedIndex = 0;
				comboBoxInlet.Enabled = true;
			}


			pf.CardChanged(this);
		}

		private void comboBoxInlet_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(Parent != null)
			{
				programForm pf = (programForm)Parent.Parent;
				pf.InletChanged(this);
				pf.CardChanged(this);
			}
		}

		private void textBoxVolume_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			double volume = 0;

			textBoxVolume.Text = textBoxVolume.Text.Replace(",",".");

			programForm pf = (programForm)Parent.Parent;

			try
			{
				volume = Convert.ToDouble(textBoxVolume.Text.Replace(",", "."));
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double defaultVolume = 2;
				textBoxVolume.Text = defaultVolume.ToString("F1");
				volume = defaultVolume;
				pf.m_changeAllowed = false;
			}

			// find plate format
			int nPlateType = 0;
			double MaxVolume = 0;
			for (int i = 0; i < pf.CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)pf.CardArray[i];
				if (PGE.strCardName == "platecard")
				{
					PlateCard PC = (PlateCard)PGE.uc;
					nPlateType = PC.GetPlateType();
					MaxVolume = PC.GetPlateMaxVolume();
					break;
				}
			}


			if( nPlateType > 3 ) nPlateType -= 3;

			// 96 plate format
			switch (nPlateType)
			{
				case 1: // 96
				{
					if (volume > MaxVolume || volume < 2)
					{
						string strMessage = "Volume out of range. Must be between 2 and ";
						strMessage += MaxVolume.ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						volume = 2;
						textBoxVolume.Text = volume.ToString("F1");
					}
			
					double test = Math.IEEERemainder(volume, 1);
					if (test != 0)
					{
						MessageBox.Show("Volume must be set in multiples of 1.\nResetting...", "Volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						volume = 1;
						textBoxVolume.Text = volume.ToString();
					}
				}
					break;
				case 2: // 384
				{
					if (volume > MaxVolume || volume < 0.5)
					{
						string strMessage = "Volume out of range. Must be between 0.5 and ";
						strMessage += MaxVolume.ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						volume = 0.5;
						textBoxVolume.Text = volume.ToString("F1");
					}
			
					double test = Math.IEEERemainder(volume * 10, 5);
					//if (test != 0)
					if (Math.Abs(test) > 0.0001) // hack...
					{
						MessageBox.Show("Volume must be set in multiples of 0.5.\nResetting...", "Volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						volume = 0.5;
						textBoxVolume.Text = volume.ToString("F1");
					}
				}
					break;
				case 3: // 1536
				{
					if (volume > MaxVolume || volume < 0.5)
					{
						string strMessage = "Volume out of range. Must be between 0.5 and ";
						strMessage += MaxVolume.ToString();
						strMessage += ".\nResetting...";
						MessageBox.Show(strMessage, "Volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						volume = 0.5;
						textBoxVolume.Text = volume.ToString("F1");
					}
			
					double test = Math.IEEERemainder(volume * 10, 1);
					//if (test != 0)
					if (Math.Abs(test) > 0.0001) // hack...
					{
						MessageBox.Show("Volume must be set in multiples of 0.1.\nResetting...", "Volume", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						volume = 0.5;
						textBoxVolume.Text = volume.ToString("F1");
					}
				}
					break;
			}
			this.minimizeLabel.Text = string.Format( "Inlet: {0} - Volume {1}µl - Liq.fact: {2} - Pressure {3}mbar",
				comboBoxInlet.Text, textBoxVolume.Text, textBoxLiquidFactor.Text, textBoxPressure.Text );
			pf.CardChanged(this);
			pf.Tag = false;
		}

		private void textBoxName_TextChanged(object sender, System.EventArgs e)
		{
			if(Parent != null)
			{
				programForm pf = (programForm)Parent.Parent;
				pf.CardChanged(this);
			}
		}

		private void textBoxLiquidFactor_TextChanged(object sender, System.EventArgs e)
		{
			
			if(Parent != null)
			{
				programForm pf = (programForm)Parent.Parent;
				pf.CardChanged(this);
			}
		}

		private void textBoxVolume_TextChanged(object sender, System.EventArgs e)
		{
			//commented this back in - mikael
			if(Parent != null && textBoxVolume.Text.Length > 0 )
			{
				programForm pf = (programForm)Parent.Parent;
				pf.CardChanged(this);
				//textBoxVolume.Focus();
			}
		}

		private void textBoxName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxInlet.Focus();
			}
		}

		private void comboBoxInlet_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxVolume.Focus();
			}
		}

		private void textBoxVolume_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxLiquidFactor.Focus();
			}
		}

		private void buttonSettings_Click_1(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			mainForm mf = (mainForm)pf.MdiParent;

			DispenseSettings ds = new DispenseSettings();
			DialogResult DR = ds._ShowDialog((Convert.ToInt32(this.comboBoxInlet.Text)-1),this, mf, pf);
			pf.InletChanged(this);
			pf.CardChanged(this);
		}

		private void textBoxPressure_TextChanged(object sender, System.EventArgs e)
		{
			if(Parent != null)
			{
				programForm pf = (programForm)Parent.Parent;
				pf.CardChanged(this);
			}
		}

		public void SetMinimizeLabel()
		{
			this.minimizeLabel.Text = string.Format( "Inlet: {0} - Volume {1}µl - Liq.fact: {2} - Pressure {3}mbar",
				comboBoxInlet.Text, textBoxVolume.Text, textBoxLiquidFactor.Text, textBoxPressure.Text );
		}

		void DrawSmallCard()
		{
			if( !m_minimize ) return;
			m_minimize = !m_minimize;
			this.panel1.BackgroundImage = Image.FromFile( "images/smallcardbg.bmp" );
			this.comboBoxInlet.Hide();
			this.label1.Hide();
			this.label2.Hide();
			this.label3.Hide();
			this.label4.Hide();
			this.label5.Hide();
			this.label6.Hide();
			this.label7.Hide();
			this.label8.Hide();
			this.textBoxLiquidFactor.Hide();
			this.textBoxName.Hide();
			this.textBoxPressure.Hide();
			this.textBoxVolume.Hide();
			this.Size = new System.Drawing.Size(440, 66);
			this.panel1.Size = new System.Drawing.Size(400, 66);
			SetMinimizeLabel();
			this.minimizeLabel.Visible = true;
			if( this.minimizeLabel.Text.Length > 60 )
			{
				this.minimizeLabel.Top = 22;
				this.minimizeLabel.Height = 26;
			}
			else
			{
				this.minimizeLabel.Top = 24;
				this.minimizeLabel.Height = 23;
			}
			this.icon.Show();

			programForm pf = (programForm)Parent.Parent;
			int offset = 190-66;
			offset = -offset;
			int programStep = int.Parse( labelProgramStep.Text );
			pf.RepositionCards( offset, programStep );
			minimize.Visible = false;
			maximize.Visible = true;
		}

		void DrawBigCard()
		{
			if( m_minimize ) return;
			m_minimize = !m_minimize;
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(dispenseCard));
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.comboBoxInlet.Show();
			this.label1.Show();
			this.label2.Show();
			this.label3.Show();
			this.label4.Show();
			this.label5.Show();
			this.label6.Show();
			this.label7.Show();
			this.label8.Show();
			this.textBoxLiquidFactor.Show();
			this.textBoxName.Show();
			this.textBoxPressure.Show();
			this.textBoxVolume.Show();
			this.Size = new System.Drawing.Size(440, 190);
			this.panel1.Size = new System.Drawing.Size(400, 190);
			this.minimizeLabel.Visible = false;
			this.icon.Hide();

			programForm pf = (programForm)Parent.Parent;
			int offset = 190-66;
			int programStep = int.Parse( labelProgramStep.Text );
			pf.RepositionCards( offset, programStep );
			minimize.Visible = true;
			maximize.Visible = false;
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			DrawSmallCard();
		}

		private void maximize_Click(object sender, System.EventArgs e)
		{
			DrawBigCard();
		}

		private void minimize_Click(object sender, System.EventArgs e)
		{
			DrawSmallCard();
		}
	}
}
