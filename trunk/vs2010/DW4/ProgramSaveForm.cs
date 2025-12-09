using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for ProgramSaveForm.
	/// </summary>
	public class ProgramSaveForm : System.Windows.Forms.Form
	{
		public System.Windows.Forms.Label labelProgram;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxFileName;
		private System.Windows.Forms.TextBox textBoxProgramName;
		private System.Windows.Forms.Button buttonSave;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TreeView treeViewFiles;
		private System.Windows.Forms.ImageList imageListTreeView;
		private System.ComponentModel.IContainer components;

		private mainForm m_mf;

		public ProgramSaveForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public DialogResult _ShowDialog(ref string strFileName, ref string strProgramName, mainForm mf)
		{
			textBoxFileName.Text = strFileName;
			textBoxProgramName.Text = strProgramName;
			m_mf = mf;
			ShowDialog();

			strFileName = textBoxFileName.Text;
			strProgramName = textBoxProgramName.Text;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgramSaveForm));
            this.labelProgram = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFileName = new System.Windows.Forms.TextBox();
            this.textBoxProgramName = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.treeViewFiles = new System.Windows.Forms.TreeView();
            this.imageListTreeView = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // labelProgram
            // 
            this.labelProgram.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProgram.ForeColor = System.Drawing.Color.White;
            this.labelProgram.Location = new System.Drawing.Point(24, 16);
            this.labelProgram.Name = "labelProgram";
            this.labelProgram.Size = new System.Drawing.Size(288, 24);
            this.labelProgram.TabIndex = 9;
            this.labelProgram.Text = "Save Program As:";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(24, 256);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 16);
            this.label2.TabIndex = 10;
            this.label2.Text = "File name:";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(24, 280);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 16);
            this.label1.TabIndex = 11;
            this.label1.Text = "Program name:";
            // 
            // textBoxFileName
            // 
            this.textBoxFileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxFileName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFileName.Location = new System.Drawing.Point(136, 256);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.Size = new System.Drawing.Size(168, 21);
            this.textBoxFileName.TabIndex = 12;
            this.textBoxFileName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxFileName_KeyDown);
            this.textBoxFileName.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxFileName_Validating);
            // 
            // textBoxProgramName
            // 
            this.textBoxProgramName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxProgramName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxProgramName.Location = new System.Drawing.Point(136, 280);
            this.textBoxProgramName.Name = "textBoxProgramName";
            this.textBoxProgramName.Size = new System.Drawing.Size(168, 21);
            this.textBoxProgramName.TabIndex = 13;
            this.textBoxProgramName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxProgramName_KeyDown);
            // 
            // buttonSave
            // 
            this.buttonSave.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSave.ForeColor = System.Drawing.Color.White;
            this.buttonSave.Location = new System.Drawing.Point(136, 320);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 14;
            this.buttonSave.Text = "&Save";
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.ForeColor = System.Drawing.Color.White;
            this.buttonCancel.Location = new System.Drawing.Point(224, 320);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 15;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // treeViewFiles
            // 
            this.treeViewFiles.HotTracking = true;
            this.treeViewFiles.ImageIndex = 0;
            this.treeViewFiles.ImageList = this.imageListTreeView;
            this.treeViewFiles.Location = new System.Drawing.Point(24, 48);
            this.treeViewFiles.Name = "treeViewFiles";
            this.treeViewFiles.SelectedImageIndex = 0;
            this.treeViewFiles.Size = new System.Drawing.Size(280, 200);
            this.treeViewFiles.TabIndex = 26;
            this.treeViewFiles.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewFiles_AfterSelect);
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
            // ProgramSaveForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(328, 360);
            this.Controls.Add(this.treeViewFiles);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.textBoxProgramName);
            this.Controls.Add(this.textBoxFileName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelProgram);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgramSaveForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Save Program As";
            this.Load += new System.EventHandler(this.ProgramSaveForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void buttonSave_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void treeViewFiles_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			textBoxFileName.Text = e.Node.Text;
		}

		private void ProgramSaveForm_Load(object sender, System.EventArgs e)
		{
			m_mf.m_xmlData.PopulateFileTree(treeViewFiles, m_mf, true);
		}

		private void textBoxFileName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxProgramName.Focus();
			}
		}

		private void textBoxProgramName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxFileName.Focus();
			}
		}

		private void textBoxFileName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			string target = "\\/:*?\"<>|\"";
			char[] anyOf = target.ToCharArray();
			if (textBoxFileName.Text.IndexOfAny(anyOf) > -1 || textBoxFileName.Text.Length < 1
				|| textBoxProgramName.Text.IndexOfAny(anyOf) > -1 || textBoxProgramName.Text.Length < 1)
			{
				MessageBox.Show("File and program names can not contain any af these charaters:\n\n\t \\/:*?\"<>|\n\n or be of zero length.", "Names", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				e.Cancel = true;
			}
		}
	}
}
