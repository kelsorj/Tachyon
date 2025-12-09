using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for EmptyFullSizeCard.
	/// </summary>
	public class EmptyFullSizeCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panelGhost;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EmptyFullSizeCard()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(EmptyFullSizeCard));
			this.panelGhost = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// panelGhost
			// 
			this.panelGhost.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelGhost.BackgroundImage")));
			this.panelGhost.Location = new System.Drawing.Point(0, 0);
			this.panelGhost.Name = "panelGhost";
			this.panelGhost.Size = new System.Drawing.Size(400, 190);
			this.panelGhost.TabIndex = 0;
			this.panelGhost.Visible = false;
			// 
			// EmptyFullSizeCard
			// 
			this.AllowDrop = true;
			this.Controls.Add(this.panelGhost);
			this.Name = "EmptyFullSizeCard";
			this.Size = new System.Drawing.Size(400, 190);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.EmptyFullSizeCard_DragEnter);
			this.DragLeave += new System.EventHandler(this.EmptyFullSizeCard_DragLeave);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.EmptyFullSizeCard_DragDrop);
			this.ResumeLayout(false);

		}
		#endregion

		private void EmptyFullSizeCard_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.Text))
			{
				if (e.Data.GetData(DataFormats.Text).ToString().EndsWith("{Repeat}"))
				{
					e.Effect = DragDropEffects.None;
					return;
				}

				if (e.Data.GetData(DataFormats.Text).ToString().StartsWith("ListViewItem:"))
				{
					e.Effect = DragDropEffects.Copy;
				}
				else
				{
					e.Effect = DragDropEffects.Move;
				}

				panelGhost.Show();
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void EmptyFullSizeCard_DragLeave(object sender, System.EventArgs e)
		{
			panelGhost.Hide();
		}

		private void EmptyFullSizeCard_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;

			string strKindOfObject = e.Data.GetData(DataFormats.Text).ToString();
			if (strKindOfObject == "aspiratecard" ||
				strKindOfObject == "dispensecard" ||
				strKindOfObject == "programlinkcard" ||
				strKindOfObject == "soakcard")
			{
				pf.MoveProgramCard(sender, "EmptyFullSizeCard");
			}
			else
			{
				pf.AddProgramCardByDrop(sender, e, "EmptyFullSizeCard");
			}
		}
	}
}
