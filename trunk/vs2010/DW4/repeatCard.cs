using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for repeatCard.
	/// </summary>
	public class repeatCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panelCenter;
		private System.Windows.Forms.Panel panelArmUpper;
		private System.Windows.Forms.Panel panelArmLower;
		private System.Windows.Forms.Panel panelUpper;
		private System.Windows.Forms.Panel panelLower;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.ComboBox comboBoxFrom;
		public System.Windows.Forms.ComboBox comboBoxTo;
		public System.Windows.Forms.ComboBox comboBoxRepeats;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public bool EditMode = false;

		private int m_OldTo = -1;
		private int m_OldFrom = -1;
		int m_To = -1;
		private System.Windows.Forms.Panel panelCenterSmall;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		public System.Windows.Forms.ComboBox comboBox1;
		public System.Windows.Forms.ComboBox comboBox2;
		public System.Windows.Forms.ComboBox comboBox3;
		private System.Windows.Forms.Button deleteButtonSmall;
		private System.Windows.Forms.Label SmallLabel;
		int m_From = -1;
		public bool DrawSmall = false;
//		int m_originalHeight = 0;
//		int m_originalTop = 0;

		public repeatCard()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call

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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(repeatCard));
			this.panelCenter = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxFrom = new System.Windows.Forms.ComboBox();
			this.comboBoxRepeats = new System.Windows.Forms.ComboBox();
			this.comboBoxTo = new System.Windows.Forms.ComboBox();
			this.deleteButton = new System.Windows.Forms.Button();
			this.panelCenterSmall = new System.Windows.Forms.Panel();
			this.SmallLabel = new System.Windows.Forms.Label();
			this.deleteButtonSmall = new System.Windows.Forms.Button();
			this.panelArmUpper = new System.Windows.Forms.Panel();
			this.panelArmLower = new System.Windows.Forms.Panel();
			this.panelUpper = new System.Windows.Forms.Panel();
			this.panelLower = new System.Windows.Forms.Panel();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.comboBox3 = new System.Windows.Forms.ComboBox();
			this.panelCenter.SuspendLayout();
			this.panelCenterSmall.SuspendLayout();
			this.SuspendLayout();
			// 
			// panelCenter
			// 
			this.panelCenter.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.panelCenter.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelCenter.BackgroundImage")));
			this.panelCenter.Controls.Add(this.label3);
			this.panelCenter.Controls.Add(this.label2);
			this.panelCenter.Controls.Add(this.label1);
			this.panelCenter.Controls.Add(this.comboBoxFrom);
			this.panelCenter.Controls.Add(this.comboBoxRepeats);
			this.panelCenter.Controls.Add(this.comboBoxTo);
			this.panelCenter.Controls.Add(this.deleteButton);
			this.panelCenter.Location = new System.Drawing.Point(0, 15);
			this.panelCenter.Name = "panelCenter";
			this.panelCenter.Size = new System.Drawing.Size(169, 128);
			this.panelCenter.TabIndex = 0;
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.Color.Transparent;
			this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label3.Location = new System.Drawing.Point(66, 33);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(35, 16);
			this.label3.TabIndex = 7;
			this.label3.Text = "Rep";
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.Location = new System.Drawing.Point(65, 80);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(35, 16);
			this.label2.TabIndex = 6;
			this.label2.Text = "End";
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(65, 56);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 16);
			this.label1.TabIndex = 5;
			this.label1.Text = "Start";
			// 
			// comboBoxFrom
			// 
			this.comboBoxFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFrom.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxFrom.Location = new System.Drawing.Point(104, 77);
			this.comboBoxFrom.Name = "comboBoxFrom";
			this.comboBoxFrom.Size = new System.Drawing.Size(40, 20);
			this.comboBoxFrom.TabIndex = 4;
			this.comboBoxFrom.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxFrom_KeyDown);
			this.comboBoxFrom.SelectedIndexChanged += new System.EventHandler(this.comboBoxFrom_SelectedIndexChanged);
			// 
			// comboBoxRepeats
			// 
			this.comboBoxRepeats.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRepeats.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxRepeats.Items.AddRange(new object[] {
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
			this.comboBoxRepeats.Location = new System.Drawing.Point(104, 32);
			this.comboBoxRepeats.Name = "comboBoxRepeats";
			this.comboBoxRepeats.Size = new System.Drawing.Size(40, 20);
			this.comboBoxRepeats.TabIndex = 3;
			this.comboBoxRepeats.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxRepeats_KeyDown);
			this.comboBoxRepeats.SelectedIndexChanged += new System.EventHandler(this.comboBoxRepeats_SelectedIndexChanged);
			// 
			// comboBoxTo
			// 
			this.comboBoxTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTo.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxTo.Location = new System.Drawing.Point(104, 54);
			this.comboBoxTo.Name = "comboBoxTo";
			this.comboBoxTo.Size = new System.Drawing.Size(40, 20);
			this.comboBoxTo.TabIndex = 2;
			this.comboBoxTo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxTo_KeyDown);
			this.comboBoxTo.SelectedIndexChanged += new System.EventHandler(this.comboBoxTo_SelectedIndexChanged);
			// 
			// deleteButton
			// 
			this.deleteButton.BackColor = System.Drawing.Color.IndianRed;
			this.deleteButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("deleteButton.BackgroundImage")));
			this.deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.deleteButton.Location = new System.Drawing.Point(149, 33);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(13, 12);
			this.deleteButton.TabIndex = 1;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// panelCenterSmall
			// 
			this.panelCenterSmall.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.panelCenterSmall.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelCenterSmall.BackgroundImage")));
			this.panelCenterSmall.Controls.Add(this.SmallLabel);
			this.panelCenterSmall.Controls.Add(this.deleteButtonSmall);
			this.panelCenterSmall.Location = new System.Drawing.Point(0, 54);
			this.panelCenterSmall.Name = "panelCenterSmall";
			this.panelCenterSmall.Size = new System.Drawing.Size(169, 51);
			this.panelCenterSmall.TabIndex = 8;
			this.panelCenterSmall.Visible = false;
			// 
			// SmallLabel
			// 
			this.SmallLabel.BackColor = System.Drawing.Color.Transparent;
			this.SmallLabel.Location = new System.Drawing.Point(48, 8);
			this.SmallLabel.Name = "SmallLabel";
			this.SmallLabel.Size = new System.Drawing.Size(48, 40);
			this.SmallLabel.TabIndex = 3;
			this.SmallLabel.Text = "label7";
			// 
			// deleteButtonSmall
			// 
			this.deleteButtonSmall.BackColor = System.Drawing.Color.IndianRed;
			this.deleteButtonSmall.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("deleteButtonSmall.BackgroundImage")));
			this.deleteButtonSmall.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.deleteButtonSmall.Location = new System.Drawing.Point(96, 8);
			this.deleteButtonSmall.Name = "deleteButtonSmall";
			this.deleteButtonSmall.Size = new System.Drawing.Size(13, 12);
			this.deleteButtonSmall.TabIndex = 2;
			this.deleteButtonSmall.Click += new System.EventHandler(this.deleteButtonSmall_Click);
			// 
			// panelArmUpper
			// 
			this.panelArmUpper.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
			this.panelArmUpper.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelArmUpper.BackgroundImage")));
			this.panelArmUpper.Location = new System.Drawing.Point(0, 15);
			this.panelArmUpper.Name = "panelArmUpper";
			this.panelArmUpper.Size = new System.Drawing.Size(169, 0);
			this.panelArmUpper.TabIndex = 1;
			// 
			// panelArmLower
			// 
			this.panelArmLower.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
			this.panelArmLower.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelArmLower.BackgroundImage")));
			this.panelArmLower.Location = new System.Drawing.Point(0, 143);
			this.panelArmLower.Name = "panelArmLower";
			this.panelArmLower.Size = new System.Drawing.Size(169, 0);
			this.panelArmLower.TabIndex = 2;
			// 
			// panelUpper
			// 
			this.panelUpper.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.panelUpper.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelUpper.BackgroundImage")));
			this.panelUpper.Location = new System.Drawing.Point(0, 0);
			this.panelUpper.Name = "panelUpper";
			this.panelUpper.Size = new System.Drawing.Size(169, 15);
			this.panelUpper.TabIndex = 3;
			// 
			// panelLower
			// 
			this.panelLower.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.panelLower.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelLower.BackgroundImage")));
			this.panelLower.Location = new System.Drawing.Point(0, 143);
			this.panelLower.Name = "panelLower";
			this.panelLower.Size = new System.Drawing.Size(169, 15);
			this.panelLower.TabIndex = 4;
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.Transparent;
			this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label4.Location = new System.Drawing.Point(66, 33);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(35, 16);
			this.label4.TabIndex = 7;
			this.label4.Text = "Rep";
			// 
			// label5
			// 
			this.label5.BackColor = System.Drawing.Color.Transparent;
			this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label5.Location = new System.Drawing.Point(65, 80);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(35, 16);
			this.label5.TabIndex = 6;
			this.label5.Text = "End";
			// 
			// label6
			// 
			this.label6.BackColor = System.Drawing.Color.Transparent;
			this.label6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label6.Location = new System.Drawing.Point(65, 56);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(35, 16);
			this.label6.TabIndex = 5;
			this.label6.Text = "Start";
			// 
			// comboBox1
			// 
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBox1.Location = new System.Drawing.Point(104, 77);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(40, 20);
			this.comboBox1.TabIndex = 4;
			// 
			// comboBox2
			// 
			this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox2.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBox2.Items.AddRange(new object[] {
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
			this.comboBox2.Location = new System.Drawing.Point(104, 32);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(40, 20);
			this.comboBox2.TabIndex = 3;
			// 
			// comboBox3
			// 
			this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox3.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBox3.Location = new System.Drawing.Point(104, 54);
			this.comboBox3.Name = "comboBox3";
			this.comboBox3.Size = new System.Drawing.Size(40, 20);
			this.comboBox3.TabIndex = 2;
			// 
			// repeatCard
			// 
			this.BackColor = System.Drawing.Color.Gainsboro;
			this.Controls.Add(this.panelCenterSmall);
			this.Controls.Add(this.panelCenter);
			this.Controls.Add(this.panelLower);
			this.Controls.Add(this.panelUpper);
			this.Controls.Add(this.panelArmLower);
			this.Controls.Add(this.panelArmUpper);
			this.Name = "repeatCard";
			this.Size = new System.Drawing.Size(169, 158);
			this.LocationChanged += new System.EventHandler(this.repeatCard_LocationChanged);
			this.Resize += new System.EventHandler(this.repeatCard_Resize);
			this.panelCenter.ResumeLayout(false);
			this.panelCenterSmall.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void deleteButton_Click(object sender, System.EventArgs e)
		{
			DialogResult DR = MessageBox.Show("Are you sure you want to delete this step?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (DR == DialogResult.Yes)
			{
				programForm pf = (programForm)Parent.Parent;
				pf.DeleteProgramCard(this);				
			}
		}

		private void comboBoxRepeats_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;

			//check overlap
			if (!pf.IsOverlapped(comboBoxFrom.SelectedIndex + 1, comboBoxTo.SelectedIndex + 1, this))
			{
				pf.CardChanged(this);
				pf.RepositionRepeats();
			}
		}

		private void comboBoxTo_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if( EditMode ) return;
			programForm pf = (programForm)Parent.Parent;

			//MIK:
			//Stop moving below endpoint
			//comboBoxFrom.SelectedIndex is -1 on init so have to check for that as well
			if( comboBoxTo.SelectedIndex >= comboBoxFrom.SelectedIndex && comboBoxFrom.SelectedIndex != -1  )
			{
				comboBoxTo.SelectedIndex -= 1;
				return;
			}

			bool overlapped = pf.IsOverlapped(comboBoxFrom.SelectedIndex + 1, comboBoxTo.SelectedIndex + 1, this);
			if (!overlapped)
			{
				pf.CardChanged(this);
				pf.RepositionRepeats();
				m_OldTo = comboBoxTo.SelectedIndex;
			}
			else
			{
				if (-1 != m_OldTo)
				{
					comboBoxTo.SelectedIndex = m_OldTo;
				}
			}

			if( overlapped && m_To != -1 )
			{
				comboBoxTo.SelectedIndex = m_To;
			}
			m_To = comboBoxTo.SelectedIndex;
		}
		
		private void comboBoxFrom_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if( EditMode ) return;
			programForm pf = (programForm)Parent.Parent;

			//MIK:
			//Stop moving below startpoint
			if( comboBoxFrom.SelectedIndex <= comboBoxTo.SelectedIndex )
			{
				comboBoxFrom.SelectedIndex += 1;
				return;
			}
			
			bool overlapped = pf.IsOverlapped(comboBoxFrom.SelectedIndex + 1, comboBoxTo.SelectedIndex + 1, this);
			if (!overlapped)
			{
				pf.CardChanged(this);
				pf.RepositionRepeats();
				m_OldFrom = comboBoxFrom.SelectedIndex;
			}
			else
			{
				if (-1 != m_OldFrom)
				{
					comboBoxFrom.SelectedIndex = m_OldFrom;
				}
			}

			if( overlapped && m_From != -1 )
			{
				comboBoxFrom.SelectedIndex = m_From;
			}

			m_From = comboBoxFrom.SelectedIndex;
		}

		private void comboBoxRepeats_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxTo.Focus();
			}
		}

		private void comboBoxTo_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxFrom.Focus();
			}
		}

		private void comboBoxFrom_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxRepeats.Focus();
			}
		}

		private void repeatCard_Resize(object sender, System.EventArgs e)
		{
			if( DrawSmall )
			{
				panelCenter.Hide();
				panelCenterSmall.Show();
				char lineBreak = (char)10;
				SmallLabel.Text = string.Format( @"R: {0}{3}S: {1}{3}E: {2}", comboBoxRepeats.Text, comboBoxTo.Text, comboBoxFrom.Text, lineBreak );
			}
			else
			{
				panelCenter.Show();
				panelCenterSmall.Hide();
			}
		}

		private void deleteButtonSmall_Click(object sender, System.EventArgs e)
		{
			DialogResult DR = MessageBox.Show("Are you sure you want to delete this step?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (DR == DialogResult.Yes)
			{
				programForm pf = (programForm)Parent.Parent;
				pf.DeleteProgramCard(this);				
			}
		}

		private void repeatCard_LocationChanged(object sender, System.EventArgs e)
		{
			char lineBreak = (char)10;
			SmallLabel.Text = string.Format( @"R: {0}{3}S: {1}{3}E: {2}", comboBoxRepeats.Text, comboBoxTo.Text, comboBoxFrom.Text, lineBreak );
		}
	}
}
