using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for aspirateCard.
	/// </summary>
	public class aspirateCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.ComboBox comboBoxTime;
		public System.Windows.Forms.ComboBox comboBoxVelocity;
		public System.Windows.Forms.Label labelProgramStep;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		public System.Windows.Forms.TextBox textBoxHeight;
		private System.Windows.Forms.Label label7;
		public System.Windows.Forms.TextBox textBoxASPOffset;
		public System.Windows.Forms.CheckBox checkBoxASPSweep;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label minimizeLabel;
		private System.Windows.Forms.PictureBox minimize;
		private System.Windows.Forms.PictureBox maximize;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.PictureBox icon;
		private System.Windows.Forms.Label label9;		
		bool m_minimize = true;

		public aspirateCard()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(aspirateCard));
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkBoxASPSweep = new System.Windows.Forms.CheckBox();
			this.label9 = new System.Windows.Forms.Label();
			this.icon = new System.Windows.Forms.PictureBox();
			this.maximize = new System.Windows.Forms.PictureBox();
			this.minimize = new System.Windows.Forms.PictureBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxASPOffset = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.textBoxHeight = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.deleteButton = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxTime = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxVelocity = new System.Windows.Forms.ComboBox();
			this.minimizeLabel = new System.Windows.Forms.Label();
			this.labelProgramStep = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.Transparent;
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.panel1.Controls.Add(this.checkBoxASPSweep);
			this.panel1.Controls.Add(this.label9);
			this.panel1.Controls.Add(this.icon);
			this.panel1.Controls.Add(this.maximize);
			this.panel1.Controls.Add(this.minimize);
			this.panel1.Controls.Add(this.label8);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.textBoxASPOffset);
			this.panel1.Controls.Add(this.label7);
			this.panel1.Controls.Add(this.textBoxHeight);
			this.panel1.Controls.Add(this.label6);
			this.panel1.Controls.Add(this.label5);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.deleteButton);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.comboBoxTime);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.comboBoxVelocity);
			this.panel1.Controls.Add(this.minimizeLabel);
			this.panel1.Cursor = System.Windows.Forms.Cursors.Default;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(400, 190);
			this.panel1.TabIndex = 0;
			this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
			// 
			// checkBoxASPSweep
			// 
			this.checkBoxASPSweep.Location = new System.Drawing.Point(192, 152);
			this.checkBoxASPSweep.Name = "checkBoxASPSweep";
			this.checkBoxASPSweep.Size = new System.Drawing.Size(16, 16);
			this.checkBoxASPSweep.TabIndex = 23;
			this.checkBoxASPSweep.CheckedChanged += new System.EventHandler(this.checkBoxASPSweep_CheckedChanged);
			// 
			// label9
			// 
			this.label9.BackColor = System.Drawing.Color.Transparent;
			this.label9.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label9.Location = new System.Drawing.Point(112, 152);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(72, 16);
			this.label9.TabIndex = 22;
			this.label9.Text = "Sweep";
			// 
			// icon
			// 
			this.icon.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("icon.BackgroundImage")));
			this.icon.Location = new System.Drawing.Point(10, 14);
			this.icon.Name = "icon";
			this.icon.Size = new System.Drawing.Size(26, 25);
			this.icon.TabIndex = 21;
			this.icon.TabStop = false;
			this.icon.Visible = false;
			// 
			// maximize
			// 
			this.maximize.BackColor = System.Drawing.Color.IndianRed;
			this.maximize.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("maximize.BackgroundImage")));
			this.maximize.Location = new System.Drawing.Point(352, 10);
			this.maximize.Name = "maximize";
			this.maximize.Size = new System.Drawing.Size(13, 12);
			this.maximize.TabIndex = 20;
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
			this.minimize.TabIndex = 19;
			this.minimize.TabStop = false;
			this.minimize.Click += new System.EventHandler(this.minimize_Click);
			// 
			// label8
			// 
			this.label8.BackColor = System.Drawing.Color.Transparent;
			this.label8.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label8.Location = new System.Drawing.Point(112, 128);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(72, 16);
			this.label8.TabIndex = 16;
			this.label8.Text = "ASP Offset";
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.Transparent;
			this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label4.Location = new System.Drawing.Point(320, 128);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 19);
			this.label4.TabIndex = 15;
			this.label4.Text = "mm";
			// 
			// textBoxASPOffset
			// 
			this.textBoxASPOffset.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxASPOffset.ForeColor = System.Drawing.Color.Black;
			this.textBoxASPOffset.Location = new System.Drawing.Point(192, 128);
			this.textBoxASPOffset.Name = "textBoxASPOffset";
			this.textBoxASPOffset.Size = new System.Drawing.Size(120, 21);
			this.textBoxASPOffset.TabIndex = 14;
			this.textBoxASPOffset.Text = "";
			this.textBoxASPOffset.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxASPOffset_KeyDown);
			this.textBoxASPOffset.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxASPOffset_Validating);
			this.textBoxASPOffset.TextChanged += new System.EventHandler(this.textBoxASPOffset_TextChanged);
			// 
			// label7
			// 
			this.label7.BackColor = System.Drawing.Color.Transparent;
			this.label7.Font = new System.Drawing.Font("Verdana", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label7.Location = new System.Drawing.Point(112, 16);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(104, 27);
			this.label7.TabIndex = 13;
			this.label7.Text = "Aspirate";
			// 
			// textBoxHeight
			// 
			this.textBoxHeight.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxHeight.ForeColor = System.Drawing.Color.Black;
			this.textBoxHeight.Location = new System.Drawing.Point(192, 104);
			this.textBoxHeight.Name = "textBoxHeight";
			this.textBoxHeight.Size = new System.Drawing.Size(120, 21);
			this.textBoxHeight.TabIndex = 2;
			this.textBoxHeight.Text = "";
			this.textBoxHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxHeight_KeyDown);
			this.textBoxHeight.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxHeight_Validating);
			this.textBoxHeight.TextChanged += new System.EventHandler(this.textBoxHeight_TextChanged);
			// 
			// label6
			// 
			this.label6.BackColor = System.Drawing.Color.Transparent;
			this.label6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label6.Location = new System.Drawing.Point(320, 104);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(56, 19);
			this.label6.TabIndex = 4;
			this.label6.Text = "mm";
			// 
			// label5
			// 
			this.label5.BackColor = System.Drawing.Color.Transparent;
			this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label5.Location = new System.Drawing.Point(320, 80);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(56, 19);
			this.label5.TabIndex = 3;
			this.label5.Text = "sec";
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.Color.Transparent;
			this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label3.Location = new System.Drawing.Point(112, 104);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(80, 16);
			this.label3.TabIndex = 1;
			this.label3.Text = "Probe height";
			// 
			// deleteButton
			// 
			this.deleteButton.BackColor = System.Drawing.Color.IndianRed;
			this.deleteButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("deleteButton.BackgroundImage")));
			this.deleteButton.Cursor = System.Windows.Forms.Cursors.Default;
			this.deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.deleteButton.Location = new System.Drawing.Point(368, 10);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(13, 12);
			this.deleteButton.TabIndex = 0;
			this.deleteButton.TabStop = false;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.Location = new System.Drawing.Point(112, 80);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(43, 20);
			this.label2.TabIndex = 1;
			this.label2.Text = "Time";
			// 
			// comboBoxTime
			// 
			this.comboBoxTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTime.DropDownWidth = 56;
			this.comboBoxTime.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxTime.ItemHeight = 12;
			this.comboBoxTime.Items.AddRange(new object[] {
															  "0",
															  "1",
															  "2",
															  "3",
															  "4",
															  "5"});
			this.comboBoxTime.Location = new System.Drawing.Point(192, 80);
			this.comboBoxTime.Name = "comboBoxTime";
			this.comboBoxTime.Size = new System.Drawing.Size(120, 20);
			this.comboBoxTime.TabIndex = 1;
			this.comboBoxTime.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxTime_KeyDown);
			this.comboBoxTime.SelectedIndexChanged += new System.EventHandler(this.comboBoxTime_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(112, 56);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(72, 19);
			this.label1.TabIndex = 1;
			this.label1.Text = "Velocity";
			// 
			// comboBoxVelocity
			// 
			this.comboBoxVelocity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVelocity.DropDownWidth = 56;
			this.comboBoxVelocity.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxVelocity.ItemHeight = 12;
			this.comboBoxVelocity.Items.AddRange(new object[] {
																  "Low Speed",
																  "Medium Speed",
																  "High Speed"});
			this.comboBoxVelocity.Location = new System.Drawing.Point(192, 56);
			this.comboBoxVelocity.Name = "comboBoxVelocity";
			this.comboBoxVelocity.Size = new System.Drawing.Size(120, 20);
			this.comboBoxVelocity.TabIndex = 0;
			this.comboBoxVelocity.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxVelocity_KeyDown);
			this.comboBoxVelocity.SelectedIndexChanged += new System.EventHandler(this.comboBoxVelocity_SelectedIndexChanged);
			// 
			// minimizeLabel
			// 
			this.minimizeLabel.BackColor = System.Drawing.Color.Transparent;
			this.minimizeLabel.Location = new System.Drawing.Point(48, 21);
			this.minimizeLabel.Name = "minimizeLabel";
			this.minimizeLabel.Size = new System.Drawing.Size(336, 26);
			this.minimizeLabel.TabIndex = 18;
			this.minimizeLabel.Visible = false;
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
			// aspirateCard
			// 
			this.Controls.Add(this.labelProgramStep);
			this.Controls.Add(this.panel1);
			this.Name = "aspirateCard";
			this.Size = new System.Drawing.Size(440, 190);
			this.Load += new System.EventHandler(this.aspirateCard_Load);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public void ValidateAll()
		{
			textBoxASPOffset_Validating(null, null);
			textBoxHeight_Validating(null, null);

			programForm pf = (programForm)Parent.Parent;
			ProgramGUIElement pge = (ProgramGUIElement)pf.CardArray[0];

			double validOffset = (pge.platecard_diameter/2)-0.4;
			validOffset = Math.Round(validOffset,2);

			double ASPOffset = Convert.ToDouble(textBoxASPOffset.Text);
			checkBoxASPSweep.Enabled = IsSweepValid(pge, ASPOffset);

			if(checkBoxASPSweep.Enabled && Math.Abs( ASPOffset ) < 0.1)
			{
				if( checkBoxASPSweep.Checked ) 
					MessageBox.Show( "Valid ASP Offset values for sweep should range from -" + validOffset + " to " + validOffset +".",   "ASP Offset values not supported", MessageBoxButtons.OK );
				checkBoxASPSweep.Checked = false;				
			}

			//checkBoxASPSweep.Enabled = pge.platecard_well_shape == "0" && pge.platecard_diameter > 0.99;
			if( !checkBoxASPSweep.Enabled && checkBoxASPSweep.Checked )
			{
				checkBoxASPSweep.Checked = false;
				MessageBox.Show( "Sweep is not supported by the selected card, and has been removed.", "Sweep not supported", MessageBoxButtons.OK );
			}
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
			//DoDragDrop("aspiratecard", DragDropEffects.All);
		}

		private void panel1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.ProgramCardToBeMoved = this;
			//DoDragDrop("aspiratecard", DragDropEffects.All);
		}

		private void aspirateCard_Load(object sender, System.EventArgs e)
		{	
			comboBoxTime.SelectedIndex = 2;
			comboBoxVelocity.SelectedIndex = 2;

			double dHeight = 0;
			textBoxHeight.Text = dHeight.ToString("F1");
			
			programForm pf = (programForm)Parent.Parent;

			ProgramGUIElement pge = (ProgramGUIElement)pf.CardArray[0];
			checkBoxASPSweep.Enabled = pge.platecard_well_shape == "0" && pge.platecard_diameter > 0.99;

			if( pge.platecard_well_shape != "0" )
			{
				checkBoxASPSweep.Enabled = false;
				if( !checkBoxASPSweep.Enabled && checkBoxASPSweep.Checked )
				{
					checkBoxASPSweep.Checked = false;
					MessageBox.Show( "Sweep is not supported by the selected card, and has been removed.", "Sweep not supported", MessageBoxButtons.OK );
				}
//				ToolTip tip = new ToolTip();
//				tip.SetToolTip( checkBoxASPSweep, "Flat bottom well shape must be selected for the plate" );
			}

			try
			{		
				for (int nCard = 0; nCard < pf.CardArray.Count; nCard++)
				{
					ProgramGUIElement PGE = (ProgramGUIElement)pf.CardArray[nCard];
					if (PGE.strCardName == "aspiratecard" && PGE.uc != null)
					{
						dHeight = double.Parse( ((aspirateCard)PGE.uc).textBoxASPOffset.Text );
						//break;
					}
				
				}
			}
			catch{}
			textBoxASPOffset.Text = dHeight.ToString("F1");

//			foreach( Control ctrl in this.Parent.Controls )
//			{
//				if( ctrl is PlateCard )
//				{
//					PlateCard pc = (PlateCard)ctrl;
//					textBoxASPOffset.Text = pc.m_PlateProperties.strPlateASPOffset;
//					break;
//				}
//			}
			pf.m_aspirateCardList.Add(this);

			pf.CardChanged(this);
		}

		private void comboBoxVelocity_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void comboBoxTime_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void textBoxHeight_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			textBoxHeight.Text = textBoxHeight.Text.Replace(",",".");
			double WellDepth = 0;
			programForm pf = (programForm)Parent.Parent;
			for (int i = 0; i < pf.CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)pf.CardArray[i];
				if (PGE.strCardName == "platecard")
				{
					PlateCard PC = (PlateCard)PGE.uc;
					WellDepth = PC.GetPlateWellDepth();
					break;
				}
			}

			double height = 0;
			
			try
			{
				height = Convert.ToDouble(textBoxHeight.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double defaultHeight = 0;
				textBoxHeight.Text = defaultHeight.ToString("F1");
			}

			if (height > WellDepth || height < 0)
			{
				double MaxHeight = WellDepth;
				string strMessage = "Probe height out of range. Max height is ";
				strMessage += MaxHeight.ToString();
				strMessage += "mm.\nResetting...";
				MessageBox.Show(strMessage, "Probe height", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double defaultHeight = 0;
				textBoxHeight.Text = defaultHeight.ToString("F1");
			}
			
			double test = Math.IEEERemainder(height * 10, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("Probe height must be set in multiples of 0.1.\nResetting...", "Probe height", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxHeight.Text = height.ToString("F1");
			}

			string sweep = checkBoxASPSweep.Checked ? "- Sweep" : "";
			this.minimizeLabel.Text = string.Format( "Velocity: {0} - Time: {1}s - Height: {2}mm - Offset: {3}mm {4}", comboBoxVelocity.Text.Replace( " Speed", "" ), comboBoxTime.Text, textBoxHeight.Text, textBoxASPOffset.Text, sweep );

			textBoxASPOffset_Validating(sender, e);

		}

		private void textBoxHeight_TextChanged(object sender, System.EventArgs e)
		{
			if( textBoxHeight.Text.Length == 0 ) return;
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void comboBoxVelocity_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxTime.Focus();
			}
		}

		private void comboBoxTime_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
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
				textBoxASPOffset.Focus();
			}
		}

		private void textBoxASPOffset_TextChanged(object sender, System.EventArgs e)
		{
			try
			{
				if( textBoxASPOffset.Text.Length == 0 ) return;
				programForm pf = (programForm)Parent.Parent;
				pf.CardChanged(this);

				// allow asp change per card
				//				pf.ASPOffsetChanged(this);

				string sweep = checkBoxASPSweep.Checked ? "- Sweep" : "";
				this.minimizeLabel.Text = string.Format( "Velocity: {0} - Time: {1}s - Height: {2}mm - Offset: {3}mm {4}", comboBoxVelocity.Text.Replace( " Speed", "" ), comboBoxTime.Text, textBoxHeight.Text, textBoxASPOffset.Text, sweep );
			}
			catch{}
		}

		private void textBoxASPOffset_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxVelocity.Focus();
			}
		}

		private void textBoxASPOffset_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			textBoxASPOffset.Text = textBoxASPOffset.Text.Replace(",",".");
			programForm pf = (programForm)Parent.Parent;
			//int format = (pf.CardArray)._items[0].platecard_format
			ProgramGUIElement pge = (ProgramGUIElement)pf.CardArray[0];

			int format = pge.platecard_format;
			if( format > 3 ) format = format-3;
			double ASPOffset = 0;

			try
			{
				ASPOffset = Convert.ToDouble(textBoxASPOffset.Text);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message + "\nResetting...", "An Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				double d = 0;				
				switch (format)
				{
					case 1:
						d = 0.0;
						textBoxASPOffset.Text = d.ToString("F1");
						break;
					case 2:
						d = 0.0;
						textBoxASPOffset.Text = d.ToString("F1");
						break;
					case 3:
						d = 0.0;
						textBoxASPOffset.Text = d.ToString("F1");
						break;
				}
				string sweep = checkBoxASPSweep.Checked ? "- Sweep" : "";
				this.minimizeLabel.Text = string.Format( "Velocity: {0} - Time: {1}s - Height: {2}mm - Offset: {3}mm {4}", comboBoxVelocity.Text.Replace( " Speed", "" ), comboBoxTime.Text, textBoxHeight.Text, textBoxASPOffset.Text, sweep );
				return;
			}

		{
			double d = 0;
			double defaultASPOffset = 0;
			switch (format)
			{
				case 1:
					//d = 4.5;
					d = 4.6;
					break;
				case 2:
					//d = 2.25;
					d = 2.3;
					break;
				case 3:
					//d = 1.125;
					d = 1.2;
					break;
			}

			// correct for platecard diameter
			bool adjust = false;
			if( pge.platecard_diameter == 0 )
				pge.platecard_diameter = 0.8;

			if( pge.platecard_diameter >= 0.8 )
			{
				d = (pge.platecard_diameter / 2)-0.4;
				d = (double)Math.Round( d, 1 );
				adjust = true;
			}

			if (ASPOffset > d || ASPOffset < -d)
			{
				string strMessage = "ASP Offset out of range.\nValues should range from ";
				strMessage += "-" + d.ToString();
				strMessage += " to ";
				strMessage += d.ToString();
				if( adjust )
					strMessage += ".\nProbe diameter is 0.8mm";
				strMessage += ".\nResetting...";
				MessageBox.Show(strMessage, "ASP Offset", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxASPOffset.Text = defaultASPOffset.ToString("F1");
				string sweep = checkBoxASPSweep.Checked ? "- Sweep" : "";
				this.minimizeLabel.Text = string.Format( "Velocity: {0} - Time: {1}s - Height: {2}mm - Offset: {3}mm {4}", comboBoxVelocity.Text.Replace( " Speed", "" ), comboBoxTime.Text, textBoxHeight.Text, textBoxASPOffset.Text, sweep );
				return;
			}
		}

			double test = Math.IEEERemainder(ASPOffset * 10, 1);
			//if (test != 0)
			if (Math.Abs(test) > 0.0001) // hack...
			{
				MessageBox.Show("ASP Offset must be set in multiples of 0.1.\nResetting...", "ASP Offset", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxASPOffset.Text = ASPOffset.ToString("F1");
				string sweep = checkBoxASPSweep.Checked ? "- Sweep" : "";
				this.minimizeLabel.Text = string.Format( "Velocity: {0} - Time: {1}s - Height: {2}mm - Offset: {3}mm {4}", comboBoxVelocity.Text.Replace( " Speed", "" ), comboBoxTime.Text, textBoxHeight.Text, textBoxASPOffset.Text, sweep );
				return;
			}

			double validOffset = (pge.platecard_diameter/2)-0.4;
			validOffset = Math.Round(validOffset,2);

			checkBoxASPSweep.Enabled = IsSweepValid(pge, ASPOffset);

			if(checkBoxASPSweep.Enabled && Math.Abs( ASPOffset ) < 0.1)
			{
				if( checkBoxASPSweep.Checked )
				{
					MessageBox.Show( "Valid ASP Offset values for sweep should range from -" + validOffset + " to " + validOffset +".",   "ASP Offset values not supported", MessageBoxButtons.OK );
				}
				checkBoxASPSweep.Checked = false;				
			}

			if( !checkBoxASPSweep.Enabled && checkBoxASPSweep.Checked )
			{
				checkBoxASPSweep.Checked = false;
				MessageBox.Show( "Sweep is not supported by the selected card, and has been removed.", "Sweep not supported", MessageBoxButtons.OK );
			}
		}

		bool IsSweepValid(ProgramGUIElement pge, double ASPOffset)
		{
			double validOffset = (pge.platecard_diameter/2)-0.4;
			validOffset = Math.Round(validOffset,2);

			textBoxHeight.Text = textBoxHeight.Text.Replace(",",".");
			double height = Convert.ToDouble(textBoxHeight.Text);

			return pge.platecard_well_shape == "0"
				&& pge.platecard_diameter > 0.99
				//				&& Math.Abs( ASPOffset ) >= 0.1
				&& Math.Abs( ASPOffset ) <= validOffset
				&& height != 0;
		}

		void DrawSmallCard()
		{
			if( !m_minimize ) return;
			m_minimize = !m_minimize;
			this.panel1.BackgroundImage = Image.FromFile( "images/smallcardbg.bmp" );
			this.comboBoxTime.Hide();
			this.comboBoxVelocity.Hide();
			this.label1.Hide();
			this.label2.Hide();
			this.label3.Hide();
			this.label4.Hide();
			this.label5.Hide();
			this.label6.Hide();
			this.label7.Hide();
			this.label8.Hide();
			this.textBoxASPOffset.Hide();
			this.textBoxHeight.Hide();
			this.Size = new System.Drawing.Size(440, 66);
			this.panel1.Size = new System.Drawing.Size(400, 66);
			string sweep = checkBoxASPSweep.Checked ? "- Sweep" : "";
			this.minimizeLabel.Text = string.Format( "Velocity: {0} - Time: {1}s - Height: {2}mm - Offset: {3}mm {4}", comboBoxVelocity.Text.Replace( " Speed", "" ), comboBoxTime.Text, textBoxHeight.Text, textBoxASPOffset.Text, sweep );
			this.minimizeLabel.Visible = true;
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

			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(aspirateCard));
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.comboBoxTime.Show();
			this.comboBoxVelocity.Show();
			this.label1.Show();
			this.label2.Show();
			this.label3.Show();
			this.label4.Show();
			this.label5.Show();
			this.label6.Show();
			this.label7.Show();
			this.label8.Show();
			this.textBoxASPOffset.Show();
			this.textBoxHeight.Show();
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

		private void minimize_Click(object sender, System.EventArgs e)
		{
			DrawSmallCard();
		}

		private void maximize_Click(object sender, System.EventArgs e)
		{
			DrawBigCard();
		}

		private void checkBoxASPSweep_CheckedChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);

			ProgramGUIElement pge = (ProgramGUIElement)pf.CardArray[0];

			double validOffset = (pge.platecard_diameter/2)-0.4;
			validOffset = Math.Round(validOffset,2);

			double ASPOffset = Convert.ToDouble(textBoxASPOffset.Text);
			checkBoxASPSweep.Enabled = IsSweepValid(pge, ASPOffset);

			if(checkBoxASPSweep.Enabled && Math.Abs( ASPOffset ) < 0.1)
			{
				if( checkBoxASPSweep.Checked ) 
					MessageBox.Show( "Valid ASP Offset values for sweep should range from -" + validOffset + " to " + validOffset +".",   "ASP Offset values not supported", MessageBoxButtons.OK );
				checkBoxASPSweep.Checked = false;				
			}

		}
	}
}
