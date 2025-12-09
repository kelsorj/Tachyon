using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for plateSelectorForm.
	/// </summary>
	public class plateSelectorForm : System.Windows.Forms.Form
	{
		public System.Windows.Forms.Label labelProgram;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonSelect;
		private System.Windows.Forms.TreeView treeViewPlates;
		private System.Windows.Forms.ImageList imageListTreeView;
		private System.ComponentModel.IContainer components;

		private mainForm m_mf;

		public plateSelectorForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public DialogResult _ShowDialog(ref PlateProperties pp, mainForm mf)
		{
			m_mf = mf;
			ShowDialog();

			if (DialogResult == DialogResult.OK)
			{
				try
				{
					pp = (PlateProperties)treeViewPlates.SelectedNode.Tag;
				
				}
				catch (Exception e)
				{
					e = e;
				}
			}

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(plateSelectorForm));
            this.labelProgram = new System.Windows.Forms.Label();
            this.treeViewPlates = new System.Windows.Forms.TreeView();
            this.imageListTreeView = new System.Windows.Forms.ImageList(this.components);
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSelect = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelProgram
            // 
            this.labelProgram.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProgram.ForeColor = System.Drawing.Color.White;
            this.labelProgram.Location = new System.Drawing.Point(16, 16);
            this.labelProgram.Name = "labelProgram";
            this.labelProgram.Size = new System.Drawing.Size(288, 24);
            this.labelProgram.TabIndex = 17;
            this.labelProgram.Text = "Select plate";
            // 
            // treeViewPlates
            // 
            this.treeViewPlates.HotTracking = true;
            this.treeViewPlates.ImageIndex = 0;
            this.treeViewPlates.ImageList = this.imageListTreeView;
            this.treeViewPlates.Location = new System.Drawing.Point(16, 48);
            this.treeViewPlates.Name = "treeViewPlates";
            this.treeViewPlates.SelectedImageIndex = 0;
            this.treeViewPlates.Size = new System.Drawing.Size(328, 200);
            this.treeViewPlates.TabIndex = 21;
            this.treeViewPlates.DoubleClick += new System.EventHandler(this.treeViewPlates_DoubleClick);
            // 
            // imageListTreeView
            // 
            this.imageListTreeView.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListTreeView.ImageStream")));
            this.imageListTreeView.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListTreeView.Images.SetKeyName(0, "");
            this.imageListTreeView.Images.SetKeyName(1, "");
            this.imageListTreeView.Images.SetKeyName(2, "");
            this.imageListTreeView.Images.SetKeyName(3, "");
            this.imageListTreeView.Images.SetKeyName(4, "");
            this.imageListTreeView.Images.SetKeyName(5, "");
            this.imageListTreeView.Images.SetKeyName(6, "");
            this.imageListTreeView.Images.SetKeyName(7, "");
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.ForeColor = System.Drawing.Color.White;
            this.buttonCancel.Location = new System.Drawing.Point(264, 264);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 23;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonSelect
            // 
            this.buttonSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelect.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSelect.ForeColor = System.Drawing.Color.White;
            this.buttonSelect.Location = new System.Drawing.Point(176, 264);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(75, 23);
            this.buttonSelect.TabIndex = 22;
            this.buttonSelect.Text = "&Select";
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            // 
            // plateSelectorForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(358, 300);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSelect);
            this.Controls.Add(this.treeViewPlates);
            this.Controls.Add(this.labelProgram);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "plateSelectorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Plate library";
            this.Load += new System.EventHandler(this.plateSelectorForm_Load);
            this.ResumeLayout(false);

		}
		#endregion

		private void buttonSelect_Click(object sender, System.EventArgs e)
		{
			string strNodeFullPath = treeViewPlates.SelectedNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeViewPlates.PathSeparator.ToCharArray());

			if (strNodeArray.Length == 2)
			{
				DialogResult = DialogResult.OK;
			}
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void treeViewPlates_DoubleClick(object sender, System.EventArgs e)
		{
			string strNodeFullPath = treeViewPlates.SelectedNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeViewPlates.PathSeparator.ToCharArray());

			if (strNodeArray.Length == 2)
			{
				DialogResult = DialogResult.OK;
			}
		}

		private void plateSelectorForm_Load(object sender, System.EventArgs e)
		{
			m_mf.m_xmlData.PopulatePlateTree(treeViewPlates);
		}
	}
}
