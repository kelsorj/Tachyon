using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for FileSelectForm.
	/// </summary>
	public class FileSelectForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonSelect;
		private System.Windows.Forms.TreeView treeViewFiles;
		private System.Windows.Forms.ImageList imageListTreeView;
		public System.Windows.Forms.Label labelDialog;
		private System.ComponentModel.IContainer components;

		private mainForm m_mf;

		public FileSelectForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public DialogResult _ShowDialog(ref string strFileName, ref string strOwner, string strLabel, mainForm mf)
		{
			labelDialog.Text = strLabel;
			this.Text = strLabel;

			m_mf = mf;
			ShowDialog();

			try
			{
				strFileName = treeViewFiles.SelectedNode.Text;

				// Find owner
				if (treeViewFiles.SelectedNode.Parent != null && treeViewFiles.SelectedNode.Parent.Text.ToLower() == "my files")
				{
					strOwner = mf.m_User.Username;
				}
				else
				{
					int nRemoveFromIndex = treeViewFiles.SelectedNode.Parent.Text.IndexOf("\'s files");
					strOwner = treeViewFiles.SelectedNode.Parent.Text.Substring(0, nRemoveFromIndex);
				}
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileSelectForm));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSelect = new System.Windows.Forms.Button();
            this.treeViewFiles = new System.Windows.Forms.TreeView();
            this.imageListTreeView = new System.Windows.Forms.ImageList(this.components);
            this.labelDialog = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.ForeColor = System.Drawing.Color.White;
            this.buttonCancel.Location = new System.Drawing.Point(263, 263);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 27;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonSelect
            // 
            this.buttonSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelect.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSelect.ForeColor = System.Drawing.Color.White;
            this.buttonSelect.Location = new System.Drawing.Point(175, 263);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(75, 23);
            this.buttonSelect.TabIndex = 26;
            this.buttonSelect.Text = "&Upload";
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            // 
            // treeViewFiles
            // 
            this.treeViewFiles.HotTracking = true;
            this.treeViewFiles.ImageIndex = 0;
            this.treeViewFiles.ImageList = this.imageListTreeView;
            this.treeViewFiles.Location = new System.Drawing.Point(15, 47);
            this.treeViewFiles.Name = "treeViewFiles";
            this.treeViewFiles.SelectedImageIndex = 0;
            this.treeViewFiles.Size = new System.Drawing.Size(328, 200);
            this.treeViewFiles.TabIndex = 25;
            this.treeViewFiles.DoubleClick += new System.EventHandler(this.treeViewFiles_DoubleClick);
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
            // labelDialog
            // 
            this.labelDialog.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDialog.ForeColor = System.Drawing.Color.White;
            this.labelDialog.Location = new System.Drawing.Point(15, 15);
            this.labelDialog.Name = "labelDialog";
            this.labelDialog.Size = new System.Drawing.Size(288, 25);
            this.labelDialog.TabIndex = 24;
            this.labelDialog.Text = "Select file";
            // 
            // FileSelectForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(358, 300);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSelect);
            this.Controls.Add(this.treeViewFiles);
            this.Controls.Add(this.labelDialog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FileSelectForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select File";
            this.Load += new System.EventHandler(this.FileSelectForm_Load);
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

		private void treeViewFiles_DoubleClick(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void FileSelectForm_Load(object sender, System.EventArgs e)
		{
			m_mf.m_xmlData.PopulateFileTree(treeViewFiles, m_mf, false);
		}
	}
}
