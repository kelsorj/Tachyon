using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for liquidSelectForm.
	/// </summary>
	public class liquidSelectForm : System.Windows.Forms.Form
	{
		public System.Windows.Forms.Label labelProgram;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonSelect;
		private System.Windows.Forms.TreeView treeViewLiquids;
		private System.Windows.Forms.ImageList imageListTreeView;
		private System.ComponentModel.IContainer components;

		private string m_strLiquidName;
		private mainForm m_mf;

		public liquidSelectForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public DialogResult _ShowDialog(ref string strLiquidName, ref string strPressure, mainForm mf)
		{
			m_strLiquidName = strLiquidName;
			
			m_mf = mf;
			ShowDialog();

			try
			{
				strLiquidName = treeViewLiquids.SelectedNode.Text;
				strPressure = (string)treeViewLiquids.SelectedNode.Tag;
			}
			catch (Exception e)
			{
				e = e;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(liquidSelectForm));
            this.labelProgram = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSelect = new System.Windows.Forms.Button();
            this.treeViewLiquids = new System.Windows.Forms.TreeView();
            this.imageListTreeView = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // labelProgram
            // 
            this.labelProgram.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProgram.ForeColor = System.Drawing.Color.White;
            this.labelProgram.Location = new System.Drawing.Point(16, 16);
            this.labelProgram.Name = "labelProgram";
            this.labelProgram.Size = new System.Drawing.Size(288, 24);
            this.labelProgram.TabIndex = 16;
            this.labelProgram.Text = "Select liquid";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.ForeColor = System.Drawing.Color.White;
            this.buttonCancel.Location = new System.Drawing.Point(264, 264);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 19;
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
            this.buttonSelect.TabIndex = 18;
            this.buttonSelect.Text = "&Select";
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            // 
            // treeViewLiquids
            // 
            this.treeViewLiquids.HotTracking = true;
            this.treeViewLiquids.ImageIndex = 0;
            this.treeViewLiquids.ImageList = this.imageListTreeView;
            this.treeViewLiquids.Location = new System.Drawing.Point(16, 48);
            this.treeViewLiquids.Name = "treeViewLiquids";
            this.treeViewLiquids.SelectedImageIndex = 0;
            this.treeViewLiquids.Size = new System.Drawing.Size(328, 200);
            this.treeViewLiquids.TabIndex = 20;
            this.treeViewLiquids.DoubleClick += new System.EventHandler(this.treeViewLiquids_DoubleClick);
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
            // liquidSelectForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(360, 302);
            this.Controls.Add(this.treeViewLiquids);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSelect);
            this.Controls.Add(this.labelProgram);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "liquidSelectForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Liquid selection";
            this.Load += new System.EventHandler(this.liquidSelectForm_Load);
            this.ResumeLayout(false);

		}
		#endregion

		private void buttonSelect_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void treeViewLiquids_DoubleClick(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void liquidSelectForm_Load(object sender, System.EventArgs e)
		{
			m_mf.m_xmlData.PopulateLiquidTree(treeViewLiquids);
		}
	}
}
