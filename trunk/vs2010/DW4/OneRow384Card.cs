using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for OneRow384Card.
	/// </summary>
	public class OneRow384Card : System.Windows.Forms.UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public OneRow384Card()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(OneRow384Card));
			// 
			// OneRow384Card
			// 
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.Name = "OneRow384Card";
			this.Size = new System.Drawing.Size(168, 7);
			this.Click += new System.EventHandler(this.OneRow384Card_Click);

		}
		#endregion

		private void OneRow384Card_Click(object sender, System.EventArgs e)
		{
			try
			{
				PlateCard pc = (PlateCard)Parent.Parent.Parent;
				pc.panelRowClickSenser_Click(sender, e);
			}
			catch (Exception exception)
			{
				exception = exception;
				PlateCardRowsOnly pc = (PlateCardRowsOnly)Parent.Parent.Parent;
				pc.panelRowClickSenser_Click(sender, e);
			}
		}
	}
}
