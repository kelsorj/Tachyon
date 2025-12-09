using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for soakCard.
	/// </summary>
	public class soakCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button deleteButton;
		public System.Windows.Forms.ComboBox comboBoxTime;
		public System.Windows.Forms.Label labelProgramStep;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label minimizeLabel;
		private System.Windows.Forms.PictureBox maximize;
		private System.Windows.Forms.PictureBox minimize;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.PictureBox icon;
		bool m_minimize = true;

		public soakCard()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(soakCard));
			this.panel1 = new System.Windows.Forms.Panel();
			this.minimizeLabel = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.deleteButton = new System.Windows.Forms.Button();
			this.comboBoxTime = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.maximize = new System.Windows.Forms.PictureBox();
			this.minimize = new System.Windows.Forms.PictureBox();
			this.labelProgramStep = new System.Windows.Forms.Label();
			this.icon = new System.Windows.Forms.PictureBox();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.panel1.Controls.Add(this.icon);
			this.panel1.Controls.Add(this.minimizeLabel);
			this.panel1.Controls.Add(this.label7);
			this.panel1.Controls.Add(this.deleteButton);
			this.panel1.Controls.Add(this.comboBoxTime);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.maximize);
			this.panel1.Controls.Add(this.minimize);
			this.panel1.Cursor = System.Windows.Forms.Cursors.Hand;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(400, 190);
			this.panel1.TabIndex = 0;
			this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
			// 
			// minimizeLabel
			// 
			this.minimizeLabel.BackColor = System.Drawing.Color.Transparent;
			this.minimizeLabel.Location = new System.Drawing.Point(48, 24);
			this.minimizeLabel.Name = "minimizeLabel";
			this.minimizeLabel.Size = new System.Drawing.Size(336, 23);
			this.minimizeLabel.TabIndex = 15;
			this.minimizeLabel.Visible = false;
			// 
			// label7
			// 
			this.label7.BackColor = System.Drawing.Color.Transparent;
			this.label7.Font = new System.Drawing.Font("Verdana", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label7.Location = new System.Drawing.Point(112, 16);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(72, 27);
			this.label7.TabIndex = 14;
			this.label7.Text = "Soak";
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
			// comboBoxTime
			// 
			this.comboBoxTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxTime.DropDownWidth = 56;
			this.comboBoxTime.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxTime.ItemHeight = 12;
			this.comboBoxTime.Items.AddRange(new object[] {
															  "0 s",
															  "2 s",
															  "5 s",
															  "10 s",
															  "20 s",
															  "30 s",
															  "40 s",
															  "1 min",
															  "2 min",
															  "5 min",
															  "10 min",
															  "30 min",
															  "1 h",
															  "2 h",
															  "5 h"});
			this.comboBoxTime.Location = new System.Drawing.Point(192, 99);
			this.comboBoxTime.Name = "comboBoxTime";
			this.comboBoxTime.Size = new System.Drawing.Size(136, 20);
			this.comboBoxTime.TabIndex = 0;
			this.comboBoxTime.SelectedIndexChanged += new System.EventHandler(this.comboBoxTime_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(112, 100);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(67, 20);
			this.label1.TabIndex = 1;
			this.label1.Text = "Time";
			// 
			// maximize
			// 
			this.maximize.BackColor = System.Drawing.Color.IndianRed;
			this.maximize.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("maximize.BackgroundImage")));
			this.maximize.Location = new System.Drawing.Point(352, 10);
			this.maximize.Name = "maximize";
			this.maximize.Size = new System.Drawing.Size(13, 12);
			this.maximize.TabIndex = 22;
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
			this.minimize.TabIndex = 21;
			this.minimize.TabStop = false;
			this.minimize.Click += new System.EventHandler(this.minimize_Click);
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
			this.icon.Size = new System.Drawing.Size(27, 27);
			this.icon.TabIndex = 23;
			this.icon.TabStop = false;
			this.icon.Visible = false;
			// 
			// soakCard
			// 
			this.Controls.Add(this.labelProgramStep);
			this.Controls.Add(this.panel1);
			this.Name = "soakCard";
			this.Size = new System.Drawing.Size(440, 190);
			this.Load += new System.EventHandler(this.soakCard_Load);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

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
			//DoDragDrop("soakcard", DragDropEffects.All);
		}

		private void panel1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			//programForm pf = (programForm)Parent.Parent;
			//pf.ProgramCardToBeMoved = this;
			//DoDragDrop("soakcard", DragDropEffects.All);
		}

		private void comboBoxTime_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void soakCard_Load(object sender, System.EventArgs e)
		{
			comboBoxTime.SelectedIndex = 0;
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		void DrawSmallCard()
		{
			if( !m_minimize ) return;
			m_minimize = !m_minimize;
			this.panel1.BackgroundImage = Image.FromFile( "images/smallcardbg.bmp" );
			this.comboBoxTime.Hide();
			this.label1.Hide();
			this.label7.Hide();
			this.Size = new System.Drawing.Size(440, 66);
			this.panel1.Size = new System.Drawing.Size(400, 66);
			this.minimizeLabel.Text = string.Format( "Time: {0}", comboBoxTime.Text );
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

			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(soakCard));
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.comboBoxTime.Show();
			this.label1.Show();
			this.label7.Show();
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

	}
}
