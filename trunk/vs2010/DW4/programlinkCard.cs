using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for programlinkCard.
	/// </summary>
	public class programlinkCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button deleteButton;
		public System.Windows.Forms.ComboBox comboBoxLinkProgram;
		public System.Windows.Forms.Label labelProgramStep;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label7;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public programlinkCard()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(programlinkCard));
			this.panel1 = new System.Windows.Forms.Panel();
			this.label7 = new System.Windows.Forms.Label();
			this.deleteButton = new System.Windows.Forms.Button();
			this.comboBoxLinkProgram = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.labelProgramStep = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.BackgroundImage = ((System.Drawing.Bitmap)(resources.GetObject("panel1.BackgroundImage")));
			this.panel1.Controls.AddRange(new System.Windows.Forms.Control[] {
																				 this.label7,
																				 this.deleteButton,
																				 this.comboBoxLinkProgram,
																				 this.label1});
			this.panel1.Cursor = System.Windows.Forms.Cursors.Default;
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(400, 171);
			this.panel1.TabIndex = 0;
			this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
			// 
			// label7
			// 
			this.label7.BackColor = System.Drawing.Color.Transparent;
			this.label7.Font = new System.Drawing.Font("Verdana", 14.25F, (System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic), System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label7.Location = new System.Drawing.Point(112, 16);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(160, 27);
			this.label7.TabIndex = 14;
			this.label7.Text = "Program link";
			// 
			// deleteButton
			// 
			this.deleteButton.BackColor = System.Drawing.Color.IndianRed;
			this.deleteButton.BackgroundImage = ((System.Drawing.Bitmap)(resources.GetObject("deleteButton.BackgroundImage")));
			this.deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.deleteButton.Location = new System.Drawing.Point(368, 10);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(13, 12);
			this.deleteButton.TabIndex = 0;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// comboBoxLinkProgram
			// 
			this.comboBoxLinkProgram.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxLinkProgram.DropDownWidth = 56;
			this.comboBoxLinkProgram.Font = new System.Drawing.Font("Verdana", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.comboBoxLinkProgram.ItemHeight = 12;
			this.comboBoxLinkProgram.Location = new System.Drawing.Point(192, 99);
			this.comboBoxLinkProgram.Name = "comboBoxLinkProgram";
			this.comboBoxLinkProgram.Size = new System.Drawing.Size(136, 20);
			this.comboBoxLinkProgram.TabIndex = 0;
			this.comboBoxLinkProgram.SelectedIndexChanged += new System.EventHandler(this.comboBoxLinkProgram_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(112, 100);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(67, 20);
			this.label1.TabIndex = 3;
			this.label1.Text = "Program";
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
			// programlinkCard
			// 
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.labelProgramStep,
																		  this.panel1});
			this.Name = "programlinkCard";
			this.Size = new System.Drawing.Size(440, 171);
			this.Load += new System.EventHandler(this.programlinkCard_Load);
			this.panel1.ResumeLayout(false);
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

		private void panel1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.ProgramCardToBeMoved = this;
			//DoDragDrop("programlinkcard", DragDropEffects.All);
		}

		private void labelProgramStep_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			//DoDragDrop("programlinkcard", DragDropEffects.All);
		}

		private void comboBoxLinkProgram_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void programlinkCard_Load(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			mainForm mf = (mainForm)pf.MdiParent;

			mf.m_xmlData.PopulateProgramComboBox(comboBoxLinkProgram, pf.m_strFileNameInternal);

			comboBoxLinkProgram.SelectedIndex = 0;
			pf.CardChanged(this);
		}
	}
}
