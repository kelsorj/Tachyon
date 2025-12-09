using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for EmptySmallSizeCard.
	/// </summary>
	public class EmptySmallSizeCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panelGhost;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EmptySmallSizeCard()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(EmptySmallSizeCard));
			this.panelGhost = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// panelGhost
			// 
			this.panelGhost.BackgroundImage = ((System.Drawing.Bitmap)(resources.GetObject("panelGhost.BackgroundImage")));
			this.panelGhost.Name = "panelGhost";
			this.panelGhost.Size = new System.Drawing.Size(400, 15);
			this.panelGhost.TabIndex = 0;
			this.panelGhost.Visible = false;
			// 
			// EmptySmallSizeCard
			// 
			this.AllowDrop = true;
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.panelGhost});
			this.Name = "EmptySmallSizeCard";
			this.Size = new System.Drawing.Size(400, 15);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.EmptySmallSizeCard_DragEnter);
			this.DragLeave += new System.EventHandler(this.EmptySmallSizeCard_DragLeave);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.EmptySmallSizeCard_DragDrop);
			this.ResumeLayout(false);

		}
		#endregion

		private void EmptySmallSizeCard_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
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

		private void EmptySmallSizeCard_DragLeave(object sender, System.EventArgs e)
		{
			panelGhost.Hide();
		}

		private void EmptySmallSizeCard_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;

			string strKindOfObject = e.Data.GetData(DataFormats.Text).ToString();
			if (strKindOfObject == "aspiratecard" ||
				strKindOfObject == "dispensecard" ||
				strKindOfObject == "programlinkcard" ||
				strKindOfObject == "soakcard")
			{
				panelGhost.Hide();
				pf.MoveProgramCard(sender, "EmptySmallSizeCard");
			}
			else
			{
				panelGhost.Hide();
				pf.AddProgramCardByDrop(sender, e, "EmptySmallSizeCard");
			}
		}
	}
}
