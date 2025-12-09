using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Xml;
using System.Threading;
using System.Runtime.InteropServices;

namespace AQ3
{
	/// <summary>
	/// Summary description for Login.
	/// </summary>
	public class PrintForm : System.Windows.Forms.Form
	{
		[DllImport("msvcrt.dll")]
		private static extern int _controlfp(int IN_New, int IN_Mask);
		
		private const int _MCW_EW = 0x8001F;
		private const int _EM_INVALID = 0x10;

		private System.Windows.Forms.Label label10;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Button printButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.TreeView treeViewSheets;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ImageList imageListTreeView;
		private System.Windows.Forms.Button previewButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.ListView listViewPrintJobs;		
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.ColumnHeader colSheetType;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.ColumnHeader colOwner;

		private mainForm m_mf = null;
		//private MyPrintDocument document = new MyPrintDocument();
		private System.Drawing.Printing.PrintDocument document;
		private System.Windows.Forms.Button buttonSettings;
		
		ArrayList m_jobItems = new ArrayList();
		ListView m_lv = new ListView();
		bool m_preview = false;
		int m_previewHelper = 0;
		ListView listViewPrintJobs2 = new ListView();
		int m_lastProgramStep = 0;
		int m_pageNum = 0;
		int m_liquidCount = 0;
		private System.Windows.Forms.ColumnHeader colFile;
		bool m_firstPage = true;

		public enum JobType {Program, CurrentProgram, Plate, Liquid, User};
		
		private void FixFPU()
		{
			_controlfp(_MCW_EW, _EM_INVALID);
		}

		public struct PrintJob
		{
			public string owner;
			public string name;
			public string path;
			public TreeNode sourceNode;
			public Color sourceColor;
			public JobType type;
		}
		
		public PrintForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintForm));
            this.printButton = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.treeViewSheets = new System.Windows.Forms.TreeView();
            this.imageListTreeView = new System.Windows.Forms.ImageList(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.previewButton = new System.Windows.Forms.Button();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.listViewPrintJobs = new System.Windows.Forms.ListView();
            this.colSheetType = new System.Windows.Forms.ColumnHeader();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colOwner = new System.Windows.Forms.ColumnHeader();
            this.colFile = new System.Windows.Forms.ColumnHeader();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.document = new System.Drawing.Printing.PrintDocument();
            this.SuspendLayout();
            // 
            // printButton
            // 
            this.printButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.printButton.ForeColor = System.Drawing.Color.White;
            this.printButton.Location = new System.Drawing.Point(8, 496);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(72, 23);
            this.printButton.TabIndex = 3;
            this.printButton.Text = "&Print";
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(8, 16);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(568, 40);
            this.label10.TabIndex = 6;
            this.label10.Text = "Print Data";
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.ForeColor = System.Drawing.Color.White;
            this.cancelButton.Location = new System.Drawing.Point(384, 496);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 23);
            this.cancelButton.TabIndex = 7;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // treeViewSheets
            // 
            this.treeViewSheets.BackColor = System.Drawing.Color.White;
            this.treeViewSheets.HideSelection = false;
            this.treeViewSheets.ImageIndex = 0;
            this.treeViewSheets.ImageList = this.imageListTreeView;
            this.treeViewSheets.Location = new System.Drawing.Point(8, 72);
            this.treeViewSheets.Name = "treeViewSheets";
            this.treeViewSheets.SelectedImageIndex = 0;
            this.treeViewSheets.Size = new System.Drawing.Size(456, 192);
            this.treeViewSheets.TabIndex = 8;
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
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(8, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "Choose sheets to print:";
            // 
            // previewButton
            // 
            this.previewButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.previewButton.ForeColor = System.Drawing.Color.White;
            this.previewButton.Location = new System.Drawing.Point(88, 496);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(72, 23);
            this.previewButton.TabIndex = 10;
            this.previewButton.Text = "Pre&view..";
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // buttonAdd
            // 
            this.buttonAdd.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAdd.ForeColor = System.Drawing.Color.White;
            this.buttonAdd.Location = new System.Drawing.Point(8, 272);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(72, 23);
            this.buttonAdd.TabIndex = 11;
            this.buttonAdd.Text = "&Add";
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // listViewPrintJobs
            // 
            this.listViewPrintJobs.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listViewPrintJobs.AllowColumnReorder = true;
            this.listViewPrintJobs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSheetType,
            this.colName,
            this.colOwner,
            this.colFile});
            this.listViewPrintJobs.FullRowSelect = true;
            this.listViewPrintJobs.GridLines = true;
            this.listViewPrintJobs.HideSelection = false;
            this.listViewPrintJobs.Location = new System.Drawing.Point(8, 328);
            this.listViewPrintJobs.MultiSelect = false;
            this.listViewPrintJobs.Name = "listViewPrintJobs";
            this.listViewPrintJobs.Size = new System.Drawing.Size(456, 160);
            this.listViewPrintJobs.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewPrintJobs.TabIndex = 12;
            this.listViewPrintJobs.UseCompatibleStateImageBehavior = false;
            this.listViewPrintJobs.View = System.Windows.Forms.View.Details;
            this.listViewPrintJobs.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewPrintJobs_ColumnClick);
            // 
            // colSheetType
            // 
            this.colSheetType.Text = "Sheet Type";
            this.colSheetType.Width = 100;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 228;
            // 
            // colOwner
            // 
            this.colOwner.Text = "Owner";
            this.colOwner.Width = 100;
            // 
            // colFile
            // 
            this.colFile.Text = "File";
            this.colFile.Width = 0;
            // 
            // buttonRemove
            // 
            this.buttonRemove.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRemove.ForeColor = System.Drawing.Color.White;
            this.buttonRemove.Location = new System.Drawing.Point(392, 296);
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Size = new System.Drawing.Size(72, 23);
            this.buttonRemove.TabIndex = 13;
            this.buttonRemove.Text = "&Remove";
            this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(8, 312);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(160, 16);
            this.label2.TabIndex = 14;
            this.label2.Text = "Sheets to be printed:";
            // 
            // buttonSettings
            // 
            this.buttonSettings.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSettings.ForeColor = System.Drawing.Color.White;
            this.buttonSettings.Location = new System.Drawing.Point(168, 496);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(120, 23);
            this.buttonSettings.TabIndex = 15;
            this.buttonSettings.Text = "Page &Settings";
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // document
            // 
            this.document.DocumentName = "print job";
            this.document.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.document_PrintPage);
            // 
            // PrintForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(474, 528);
            this.Controls.Add(this.buttonSettings);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonRemove);
            this.Controls.Add(this.listViewPrintJobs);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.treeViewSheets);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.printButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Print";
            this.Load += new System.EventHandler(this.PrintForm_Load);
            this.ResumeLayout(false);

		}
		#endregion


		public DialogResult _ShowDialog(mainForm mf)
		{
			m_mf = mf;

			ShowDialog();
			return DialogResult;
		}

		private void PrintForm_Load(object sender, System.EventArgs e)
		{
			try
			{
				m_mf.m_xmlData.PopulateTree(treeViewSheets, m_mf, true);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "BNX1536 error");
				Application.Exit();
			}

			//m_mf.m_xmlData.LoadUsersForLogin(this);
			//comboBoxUsername.SelectedIndex = 0;
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void buttonAdd_Click(object sender, System.EventArgs e)
		{
			TreeNode thisNode = treeViewSheets.SelectedNode;
			
			if(thisNode != null)
			{				
				string nodePath = thisNode.FullPath.ToString();
				
				// Is job already added?
				string[] nodePathItems = nodePath.Split(treeViewSheets.PathSeparator.ToCharArray());

				int depth = nodePathItems.Length;
				// depth = 1 (Root item         )
				// depth = 2 ( |---- Item       )
				// depth = 3 (        |--- Item )
				

				// Determine type
				string rootItem = nodePathItems[0].ToUpper();

				if(rootItem.Equals("MY FILES") && depth == 1)
				{
					// Loop all files under My Files
					foreach(TreeNode node in thisNode.Nodes)
					{
						foreach(TreeNode subNode in node.Nodes)
						{
							addJob(JobType.Program, m_mf.m_User.Username, subNode);
						}
					}
				}
				else if(rootItem.Equals("MY FILES") && depth == 2)
				{
					// Loop all programs inside a file under My Files
					foreach(TreeNode node in thisNode.Nodes)
					{
						addJob(JobType.Program, m_mf.m_User.Username, node);
					}
				}
				else if(rootItem.Equals("MY FILES") && depth == 3)
				{
					// Add job
					addJob(JobType.Program, m_mf.m_User.Username, thisNode);
				}
				else if(rootItem.ToLower().Equals("current bnx1536 programs") && depth == 1)
				{
					// Loop all programs under Current DW4 Programs
					foreach(TreeNode node in thisNode.Nodes)
					{
						addJob(JobType.CurrentProgram, m_mf.m_User.Username, node);
					}
				}
				else if(rootItem.ToLower().Equals("current bnx1536 programs") && depth == 2)
				{
					//addJob(JobType.CurrentProgram, m_mf.m_User.Username, thisNode);
					addJob(JobType.CurrentProgram, "____BNX1536_", thisNode);					
				}
				else if(rootItem.EndsWith("'S FILES") & depth == 1)
				{
					int pos = thisNode.Text.IndexOf("\'s files");
					string owner = thisNode.Text.Substring(0, pos);
					
					// Loop all files under a user
					foreach(TreeNode node in thisNode.Nodes)
					{
						foreach(TreeNode subNode in node.Nodes)
						{
							addJob(JobType.Program, owner, subNode);
						}
					}
				}
				else if(rootItem.EndsWith("'S FILES") & depth == 2)
				{
					int pos = thisNode.Parent.Text.IndexOf("\'s files");
					string owner = thisNode.Parent.Text.Substring(0, pos);
					
					// Loop all files under a user
					foreach(TreeNode node in thisNode.Nodes)
					{
						addJob(JobType.Program, owner, node);
					}
				}
				else if(rootItem.EndsWith("'S FILES") & depth == 3)
				{
					int pos = thisNode.Parent.Parent.Text.IndexOf("\'s files");
					string owner = thisNode.Parent.Parent.Text.Substring(0, pos);
					
					addJob(JobType.Program, owner, thisNode);
				}
				else if(rootItem.Equals("PLATES") && depth == 1)
				{
					foreach(TreeNode node in thisNode.Nodes)
					{
						foreach(TreeNode subNode in node.Nodes)
						{
							addJob(JobType.Plate, "", subNode);
						}
					}
				}
				else if(rootItem.Equals("PLATES") && depth == 2)
				{
					foreach(TreeNode node in thisNode.Nodes)
					{
						addJob(JobType.Plate, "", node);
					}
				}
				else if(rootItem.Equals("PLATES") && depth == 3)
				{
					addJob(JobType.Plate, "", thisNode);
				}
				else if(rootItem.Equals("LIQUIDS") && depth == 1)
				{
					foreach(TreeNode node in thisNode.Nodes)
					{
						addJob(JobType.Liquid, "", node);
					}
				}
				else if(rootItem.Equals("LIQUIDS") && depth == 2)
				{
					addJob(JobType.Liquid, "", thisNode);
				}
				/*
				else if(rootItem.Equals("USERS"))
				{
					job.type = JobType.User;
				}
				*/
			}
		}

		private void addJob(JobType type, string owner, TreeNode sourceNode)
		{
			string nodePath = sourceNode.FullPath.ToString();

			if(!nodePath.EndsWith("(Print)") && !m_jobItems.Contains(nodePath))
			{
				PrintJob job = new PrintJob();
				job.path = nodePath;
				job.name = sourceNode.Text;
				job.sourceNode = sourceNode;
				job.sourceColor = sourceNode.ForeColor;
				job.owner = owner;
				job.type = type;

				// Add to print listview
				ListViewItem item = new ListViewItem(job.type.ToString());
				item.SubItems.Add(job.name);
				item.SubItems.Add(job.owner);
						
				// Assign metadata
				item.Tag = job;

				// Show user that he has added this item for print
				sourceNode.ForeColor = Color.Red;
				sourceNode.Text += " (Print)";

				listViewPrintJobs.Items.Add(item);
				listViewPrintJobs.Sort();

				listViewPrintJobs2.Items.Add((ListViewItem)item.Clone());
				listViewPrintJobs2.Sort();
			}
		}

		private void buttonRemove_Click(object sender, System.EventArgs e)
		{
			foreach(ListViewItem item in listViewPrintJobs.SelectedItems)
			{
				PrintJob job = (PrintJob) item.Tag;
				removeJob(job, item);
				if(listViewPrintJobs.Items.Count > 0)
				{
					listViewPrintJobs.Items[0].Selected = true;
					listViewPrintJobs.Items[0].EnsureVisible();
					listViewPrintJobs.Items[0].Focused = true;
				}
			}
		}

		private void removeJob(PrintJob job, ListViewItem item)
		{
			// don't change the text if we do preview
			// this prohibits adding the programs several times
			// remove if we print normal, or from the previewpage
			// previewhelper will be 2 if we print from the preview page
			if( !m_preview || m_previewHelper != 1 )
			{
				job.sourceNode.Text = job.sourceNode.Text.Replace(" (Print)", "");
				job.sourceNode.ForeColor = job.sourceColor;
			}
			m_jobItems.Remove(job.path);
			listViewPrintJobs.Items.Remove(item);
		}

		private void previewButton_Click(object sender, System.EventArgs e)
		{
			if( listViewPrintJobs.Items.Count == 0 )
			{
				MessageBox.Show( this, "No sheets added.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information );
				return;
			}

			m_pageNum = 0;
			m_preview = true;
			m_previewHelper = 0;
			PrintPreviewDialog ppd = new PrintPreviewDialog();
//			PageSetupDialog psd = new PageSetupDialog();
			PrinterSettings ps = new PrinterSettings();

			if( m_mf.m_printerName != null ) ps.PrinterName = m_mf.m_printerName;
//			ps.FromPage = 1;
//			ps.ToPage = 1;
			ps.PrintRange = PrintRange.AllPages;

			document.PrinterSettings = ps;
			try
			{
				FixFPU();
				ppd.Document = this.document;
				ppd.FormBorderStyle = FormBorderStyle.Sizable;
				ppd.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
				ppd.SetDesktopBounds(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, (Screen.PrimaryScreen.Bounds.Width/100)*80, (Screen.PrimaryScreen.Bounds.Height/100)*80);
				ppd.UseAntiAlias = false;
				ppd.PrintPreviewControl.Zoom = 1.0;
			}
			catch(Exception)
			{
				MessageBox.Show("Error showing the print preview window - please check your printer drivers!");
			}


			foreach(ListViewItem item in listViewPrintJobs.Items)
			{
				m_lv.Items.Add((ListViewItem)item.Clone());
			}

			try
			{
				ppd.Icon = this.Icon;
				ppd.ShowDialog( this );
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			m_lv.Clear();
			m_preview = false;
			listViewPrintJobs2.Items.Clear();
			m_mf.m_printerName = ps.PrinterName;

			if( m_previewHelper > 1 )
			{
				//we have done a print from the preview page, and close the control
				this.Close();
			}
		}

		private void document_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
		{			
			m_previewHelper++; //use this to keep track of if we print from the previewpage
			Application.DoEvents();
			//document.OriginAtMargins = true;
			
			PageSettings ps = e.PageSettings;

			ps.Margins.Top = 100;
			ps.Margins.Bottom = 100;

			int nextY = 0;

			if( m_firstPage ) m_pageNum = 0;

			// Get first item
			if(listViewPrintJobs.Items.Count > 0)
			{
				m_firstPage = false;
				m_pageNum++;
				ListViewItem item = listViewPrintJobs.Items[0];
				string name = item.SubItems[1].Text;
				string owner = item.SubItems[2].Text;
				PrintJob job = (PrintJob) item.Tag;
				string[] path = job.path.Split( '\\' );
			
				switch( job.type )
				{
					case JobType.Program:
						// Program
						//if(job.type == JobType.Program)
					{
						// Progams
						//XmlNodeList programList = m_mf.m_xmlData.m_xmlData.SelectNodes("//file[@owner='" + owner + "']/program[@name='" + name + "']");
						string xpath = string.Format( "//file[@owner='{0}'][@name='{1}']/program[@name='{2}']", owner, path[1], name  );
						XmlNodeList programList = m_mf.m_xmlData.m_xmlData.SelectNodes( xpath );
						//XmlNodeList programList = m_mf.m_xmlData.m_xmlData.SelectNodes("//file[@owner='" + owner + "']/program[@name='" + name + "']");
						XmlNode program = programList[0];

						string fileName = program.ParentNode.Attributes["name"].Value;
						
						string programName = program.Attributes["name"].Value;

						// Cards
						XmlNodeList cardList = program.SelectNodes("./card[@name != 'platecard']");

						//string fileName = program.ParentNode.Attributes["name"].Value;
						//string owner = program.ParentNode.Attributes["owner"].Value;
					
						XmlNode plate = program.SelectNodes("./card[@name='platecard']")[0];
						string plateName = plate.Attributes["plate_name"].Value;
						string pcRows = plate.Attributes["rows"].Value;
						string format = plate.Attributes["format"].Value;
						int dFormat = int.Parse(format);
						string formatSearch = dFormat > 3 ? ((int)(dFormat-3)).ToString() : format;
						string height = plate.Attributes["height"].Value;
						string depth = plate.Attributes["depth"].Value;
						string yo = plate.Attributes["yo"].Value;						
						string dbwc = plate.Attributes["dbwc"].Value;
						string yo2 = "not defined";
						string dbwc2 = "not defined";						
						if( plate.Attributes["yo2"] != null )
						{
							yo2 = plate.Attributes["yo2"].Value + " mm";
							dbwc2 = plate.Attributes["dbwc2"].Value + " mm";							
						}
						string loBase = "No";
						if( plate.Attributes["lobase"] != null )
						{
							bool bLoBase = bool.Parse( plate.Attributes["lobase"].Value );
							loBase = bLoBase ? "Yes" : "No";
						}
						string maxVolume = plate.Attributes["max_volume"].Value;
//						string aspOffset = plate.Attributes["asp_offset"].Value;
						XmlNode node = m_mf.m_xmlData.m_xmlData.SelectSingleNode( string.Format( "//plates/group/plate[@name='{0}' and @format='{1}']", plateName, formatSearch ) );
						string catalog = "";
						if( node != null )
						{
							catalog = node.Attributes["type_no"].Value;
						}
					
						string wellDiam = "0";
						string wellShape = "Unknown";
						try
						{
							wellDiam = plate.Attributes["diameter"].Value;
							wellShape = plate.Attributes["shape"].Value;
							if( wellShape == "0" ) wellShape = "Flat-bottom";
							else if( wellShape == "1" ) wellShape = "U-bottom";
							else if( wellShape == "2" ) wellShape = "V-bottom";
						}
						catch{}

						string formatType = "";						
						int wellCount = 0;
						switch(format)
						{
							case "6":
								formatType = "1536 wells microplate";
								pcRows = Utilities.CardDisplayColumnString( pcRows, 32 );
								wellCount = 32;
								break;
							case "5":
								formatType = "384 wells microplate";
								pcRows = Utilities.CardDisplayColumnString( pcRows, 16 );
								wellCount = 16;
								break;
							case "4":
								formatType = "96 wells microplate";
								pcRows = Utilities.CardDisplayColumnString( pcRows, 8 );
								wellCount = 8;
								break;
							case "3":
								formatType = "1536 wells microplate";
								pcRows = Utilities.CardDisplayRowString( pcRows, 48 );
								wellCount = 48;
								break;
							case "2":
								formatType = "384 wells microplate";
								pcRows = Utilities.CardDisplayRowString( pcRows, 24 );
								wellCount = 24;
								break;
							case "1":
								formatType = "96 wells microplate";
								pcRows = Utilities.CardDisplayRowString( pcRows, 12 );
								wellCount = 12;
								break;
						}

						string formatText = int.Parse( format ) > 3 ? "Columns" : "Rows";

						nextY = Print.addLogo(e);
						nextY = Print.addTitle(e, nextY, "BNX1536 Program Sheet");
						nextY = Print.addProgramHeader(e, nextY+10, programName, fileName, owner, m_mf.m_User.Username, XmlData.PROGRAM_VERSION);
						nextY = Print.addSubTitle(e, nextY+10, "Plate info:");
						//nextY = Print.addPlateInfo(e, nextY+2, formatType, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm");
						nextY = Print.addPlateInfo(e, nextY+2, formatType, yo + " mm", dbwc + " mm", yo2, dbwc2, height + " mm", depth + " mm", maxVolume + " µl", plateName, catalog, formatText, loBase, wellDiam + " mm", wellShape );
						nextY = Print.addSubTitle(e, nextY+10, "Steps:");
						nextY = Print.addStepHeader(e, nextY+2, plateName, pcRows, int.Parse(format));

						Print.addFooter(e, DateTime.Now, m_pageNum );

						int step = 0;
					
						// Steps
						try
						{
							//find all repeats and sort them
							//not a very nice hack, but quicker than fix how
							//program is saved in the first place
							Hashtable repeatPos = new Hashtable();
							foreach( XmlNode card in cardList )
							{
								string cardType = card.Attributes["name"].Value;
								if( cardType == "repeatcard" )
								{
									int end = int.Parse( card.Attributes["from"].Value );
									repeatPos[end] = card;
								}
							}

							int[] pos = (int[])new ArrayList( repeatPos.Keys ).ToArray(typeof(int));
							XmlNode[] nodes = (XmlNode[])new ArrayList( repeatPos.Values ).ToArray(typeof(XmlNode));
							Array.Sort( pos, nodes );
							//repeat sort end

							//sort the repeats into the list for the printout
							ArrayList sortedList = new ArrayList( cardList.Count );						
							int count = 0;
							foreach( XmlNode card in cardList )
							{
								string cardType = card.Attributes["name"].Value;

								if( cardType == "repeatcard" )
								{
									//use the sorted list, instead of the unsorted from cardList
									//repeats are always at the end so this works
									XmlNode sortedCard = nodes[count];
									int end = int.Parse( sortedCard.Attributes["from"].Value );
									sortedList.Insert( end+count, sortedCard );
								
									//int end = int.Parse( card.Attributes["from"].Value );
									//sortedList.Insert( end+count, card );
									count++;
								}
								else
								{
									sortedList.Add( card );
								}
							}

							//foreach(XmlNode card in cardList)
							foreach(XmlNode card in sortedList)
							{
								string cardType = card.Attributes["name"].Value;

								step++;

								if( m_lastProgramStep > step-1 ) continue; //skip to correct step
								if( m_lastProgramStep > 0 )
								{
									//we're on page two of a program
									//and need to set parameters correct
									m_lastProgramStep = 0;
									step--;
									continue;									
								}
								

								switch(cardType.ToLower())
								{
									case "platecardrowsonly":
										string pcoRows = card.Attributes["rows"].Value;
										if( wellCount % 12 != 0 )
										{
											pcoRows = Utilities.CardDisplayColumnString( pcoRows, wellCount );
										}
										else
										{
											pcoRows = Utilities.CardDisplayRowString( pcoRows, wellCount );
										}
										nextY = Print.addStepRowSelector(e, nextY, Convert.ToString(step), pcoRows);
										break;
									case "soakcard":
										string soakTime = card.Attributes["time"].Value;
										nextY = Print.addStepSoak(e, nextY, Convert.ToString(step), soakTime + " sec");
										break;
									case "dispensecard":
										string liquid = card.Attributes["liquid_name"].Value;
										string inlet = card.Attributes["inlet"].Value;
										string volume = card.Attributes["volume"].Value;
										string lf = card.Attributes["liquid_factor"].Value;
										string pressure = card.Attributes["disp_low"].Value;
										nextY = Print.addStepDispense(e, nextY, Convert.ToString(step), liquid, inlet, volume + " µl", lf, pressure);
										break;
									case "repeatcard":
										step--;
										string start = card.Attributes["to"].Value;
										string end = card.Attributes["from"].Value;
										string repeats = card.Attributes["repeats"].Value;
										nextY = Print.addStepRepeat(e, nextY, "", start, end, repeats);
										break;
									case "aspiratecard":
										string velocity = card.Attributes["velocity"].Value;
										string aspTime = card.Attributes["time"].Value;
										string probeHeight = card.Attributes["probe_height"].Value;
										string aspOffset = card.Attributes["asp_offset"].Value;
								
										string velocityType = "";
								
									switch (velocity)
									{
										case "0":
											velocityType = "Low Speed";
											break;
										case "1":
											velocityType = "Medium Speed";
											break;
										case "2":
											velocityType = "High Speed";
											break;
									}
										bool sweep = false;
										try
										{
											sweep = bool.Parse( card.Attributes["sweep"].Value );
										}
										catch{}

										nextY = Print.addStepAspirate(e, nextY, Convert.ToString(step), velocityType, aspTime + " sec", probeHeight + " mm", aspOffset + " mm", sweep );
										break;
								}
								//do a new page for the rest
								//we're at the bottom
								if( nextY > ps.PaperSize.Height - 250 )
								{
									m_lastProgramStep = step;
									InsertPage2Program( item );
									break;
								}
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show( ex.Message + "\r\n" + ex.StackTrace);
						}
						removeJob(job, item);
						break;
					}			
						//else if(job.type == JobType.CurrentProgram)
					case JobType.CurrentProgram:
					{
						path[1] = "____BNX1536_";
						goto case JobType.Program;
						#region old duplicate code
						//
						//					// Progams
						//					XmlNodeList programList = m_mf.m_xmlData.m_xmlData.SelectNodes("//file[@owner='" + owner + "']/program[@name='" + name + "']");
						//					XmlNode program = programList[0];
						//						
						//					string programName = program.Attributes["name"].Value;
						//
						//					// Cards
						//					XmlNodeList cardList = program.SelectNodes("./card[@name != 'platecard']");
						//
						//					XmlNode plate = program.SelectNodes("./card[@name='platecard']")[0];
						//					string plateName = plate.Attributes["plate_name"].Value;
						//					string pcRows = plate.Attributes["rows"].Value;
						//					string format = plate.Attributes["format"].Value;
						//					string height = plate.Attributes["height"].Value;
						//					string depth = plate.Attributes["depth"].Value;
						//					string yo = plate.Attributes["yo"].Value;
						//					string maxVolume = plate.Attributes["max_volume"].Value;
						//					string dbwc = plate.Attributes["dbwc"].Value;
						//					string aspOffset = plate.Attributes["asp_offset"].Value;
						//					XmlNode node = m_mf.m_xmlData.m_xmlData.SelectSingleNode( string.Format( "//plates/group/plate[@name='{0}' and @format='{1}']", plateName, format ) );
						//					string catalog = "";
						//					if( node != null )
						//					{
						//						catalog = node.Attributes["type_no"].Value;
						//					}
						//					string formatType = "";
						//
						//					switch(format)
						//					{
						//						case "3":
						//							formatType = "1536 wells microplate";
						//							break;
						//						case "2":
						//							formatType = "384 wells microplate";
						//							break;
						//						case "1":
						//							formatType = "96 wells microplate";
						//							break;
						//					}
						//
						//					
						//					nextY = Print.addLogo(e);
						//					nextY = Print.addTitle(e, nextY, "BNX1536 Current Program Sheet");
						//					nextY = Print.addCurrentProgramHeader(e, nextY+10, programName, owner, m_mf.m_User.Username, XmlData.PROGRAM_VERSION);
						//					nextY = Print.addSubTitle(e, nextY+10, "Plate info:");
						//					nextY = Print.addPlateInfo(e, nextY+2, formatType, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm", plateName, catalog );
						//					nextY = Print.addSubTitle(e, nextY+10, "Steps:");
						//					nextY = Print.addStepHeader(e, nextY+2, plateName, pcRows);
						//					Print.addFooter(e, DateTime.Now);
						//
						//					int step = 0;
						//					
						//					// Steps
						//					try
						//					{
						//						foreach(XmlNode card in cardList)
						//						{
						//							string cardType = card.Attributes["name"].Value;
						//
						//							step++;
						//
						//							int maxY = e.MarginBounds.Height;
						//							if((nextY + 50) < maxY)
						//							{
						//								switch(cardType.ToLower())
						//								{
						//									case "platecardrowsonly":
						//										string pcoRows = card.Attributes["rows"].Value;
						//										nextY = Print.addStepRowSelector(e, nextY, Convert.ToString(step), pcoRows);
						//										break;
						//									case "soakcard":
						//										string soakTime = card.Attributes["time"].Value;
						//										nextY = Print.addStepSoak(e, nextY, Convert.ToString(step), soakTime + " sec");
						//										break;
						//									case "dispensecard":
						//										string liquid = card.Attributes["liquid_name"].Value;
						//										string inlet = card.Attributes["inlet"].Value;
						//										string volume = card.Attributes["volume"].Value;
						//										string lf = card.Attributes["liquid_factor"].Value;
						//										string pressure = card.Attributes["disp_low"].Value;
						//										nextY = Print.addStepDispense(e, nextY, Convert.ToString(step), liquid, inlet, volume + " µl", lf, pressure);
						//										break;
						//									case "repeatcard":
						//										string start = card.Attributes["from"].Value;
						//										string end = card.Attributes["to"].Value;
						//										string repeats = card.Attributes["repeats"].Value;
						//										nextY = Print.addStepRepeat(e, nextY, "", start, end, repeats);
						//										break;
						//									case "aspiratecard":
						//										string velocity = card.Attributes["velocity"].Value;
						//										string aspTime = card.Attributes["time"].Value;
						//										string probeHeight = card.Attributes["probe_height"].Value;
						//								
						//										string velocityType = "";
						//								
						//										switch (velocity)
						//										{
						//											case "1":
						//												velocityType = "Low Speed";
						//												break;
						//											case "3":
						//												velocityType = "Medium Speed";
						//												break;
						//											case "2":
						//												velocityType = "High Speed";
						//												break;
						//										}
						//										nextY = Print.addStepAspirate(e, nextY, Convert.ToString(step), velocityType, aspTime + " sec", probeHeight + " mm");
						//										break;
						//								}
						//							}
						//							removeJob(job, item);
						//						}
						//					}
						//					catch (Exception ex)
						//					{
						//						MessageBox.Show(ex.Message);
						//					}
						#endregion
					}
						//else if(job.type == JobType.Plate)
					case JobType.Plate:
					{
						nextY = Print.addLogo(e);
						nextY = Print.addTitle(e, nextY, "BNX1536 Plate Sheet");
						nextY = Print.addSimpleTitleHeader(e, nextY, m_mf.m_User.Username, XmlData.PROGRAM_VERSION);
						Print.addFooter(e, DateTime.Now, m_pageNum);

						ListViewItem plateItem = listViewPrintJobs.Items[0];

						// Plates
						XmlNodeList plateList = m_mf.m_xmlData.m_xmlData.SelectNodes("//plates/group/plate[@name='" + name + "']");
					
						foreach(XmlNode plate in plateList)
						{
							string format = plate.Attributes["format"].Value;
							switch(format)
							{
								case "3":
									string formatType = "1536 Wells Plate";
									nextY = Print.addSubTitle(e, nextY+10, formatType + "s:");
									
									foreach(ListViewItem plateItem2 in listViewPrintJobs.Items)
									{
										string itemName = plateItem2.SubItems[1].Text;
										PrintJob thisJob = (PrintJob) plateItem2.Tag;
										
										XmlNodeList plateList2 = m_mf.m_xmlData.m_xmlData.SelectNodes("//plates/group/plate[@name='" + itemName + "' and @format='3']");

										XmlNode node = m_mf.m_xmlData.m_xmlData.SelectSingleNode("//plates/group/plate[@name='" + itemName + "' and @format='3']");
										
										foreach(XmlNode plate2 in plateList2)
										{
											string plateName = plate2.Attributes["name"].Value;
											string height = plate2.Attributes["height"].Value;
											string depth = plate2.Attributes["depth"].Value;
											string yo = plate2.Attributes["yo"].Value;											
											string dbwc = plate2.Attributes["dbwc"].Value;
											string yo2 = "not defined";
											string dbwc2 = "not defined";
											if( plate2.Attributes["yo2"] != null )
											{
												yo2 = plate2.Attributes["yo2"].Value + " mm";											
												dbwc2 = plate2.Attributes["dbwc2"].Value + " mm";
											}
											string loBase = "No";
											if( plate2.Attributes["lobase"] != null )
											{
												bool bLoBase = bool.Parse( plate2.Attributes["lobase"].Value );
												loBase = bLoBase ? "Yes" : "No";
											}
											string maxVolume = plate2.Attributes["max_volume"].Value;
//											string aspOffset = plate2.Attributes["asp_offset"].Value;
											string catalog = "";
											if( node != null )
											{
												catalog = node.Attributes["type_no"].Value;
											}

											string wellDiam = "0";
											string wellShape = "Flat-bottom";
											try
											{
												wellDiam = plate2.Attributes["diameter"].Value;
												wellShape = plate2.Attributes["shape"].Value;
												if( wellShape == "0" ) wellShape = "Flat-bottom";
												else if( wellShape == "1" ) wellShape = "U-bottom";
												else if( wellShape == "2" ) wellShape = "V-bottom";
											}
											catch{}

											int maxY = e.MarginBounds.Height;
											if((nextY + 50) < maxY)
											{
												nextY = Print.addPlateInfo2(e, nextY+5, plateName, yo + " mm", dbwc + " mm", yo2, dbwc2, height + " mm", depth + " mm", maxVolume + " µl", catalog, loBase, wellDiam + " mm", wellShape );
												removeJob(thisJob, plateItem2);
											}
										}
									}
									break;
								case "2":
									formatType = "384 Wells Plate";
									nextY = Print.addSubTitle(e, nextY+10, formatType + "s:");
									
									foreach(ListViewItem plateItem2 in listViewPrintJobs.Items)
									{
										string itemName = plateItem2.SubItems[1].Text;
										PrintJob thisJob = (PrintJob) plateItem2.Tag;
										
										XmlNodeList plateList2 = m_mf.m_xmlData.m_xmlData.SelectNodes("//plates/group/plate[@name='" + itemName + "' and @format='2']");

										XmlNode node = m_mf.m_xmlData.m_xmlData.SelectSingleNode("//plates/group/plate[@name='" + itemName + "' and @format='2']");
										
										foreach(XmlNode plate2 in plateList2)
										{
											string plateName = plate2.Attributes["name"].Value;
											string height = plate2.Attributes["height"].Value;
											string depth = plate2.Attributes["depth"].Value;
											string yo = plate2.Attributes["yo"].Value;											
											string dbwc = plate2.Attributes["dbwc"].Value;
											string yo2 = "not defined";
											string dbwc2 = "not defined";
											if( plate2.Attributes["yo2"] != null )
											{
												yo2 = plate2.Attributes["yo2"].Value + " mm";						
												dbwc2 = plate2.Attributes["dbwc2"].Value + " mm";
											}
											string loBase = "No";
											if( plate2.Attributes["lobase"] != null )
											{
												bool bLoBase = bool.Parse( plate2.Attributes["lobase"].Value );
												loBase = bLoBase ? "Yes" : "No";
											}
											string maxVolume = plate2.Attributes["max_volume"].Value;
//											string aspOffset = plate2.Attributes["asp_offset"].Value;
											string catalog = "";
											if( node != null )
											{
												catalog = node.Attributes["type_no"].Value;
											}

											string wellDiam = "0";
											string wellShape = "Flat-bottom";
											try
											{
												wellDiam = plate2.Attributes["diameter"].Value;
												wellShape = plate2.Attributes["shape"].Value;
												if( wellShape == "0" ) wellShape = "Flat-bottom";
												else if( wellShape == "1" ) wellShape = "U-bottom";
												else if( wellShape == "2" ) wellShape = "V-bottom";
											}
											catch{}


											int maxY = e.MarginBounds.Height;
											if((nextY + 50) < maxY)
											{
												nextY = Print.addPlateInfo2(e, nextY+5, plateName, yo + " mm", dbwc + " mm", yo2, dbwc2, height + " mm", depth + " mm", maxVolume + " µl", catalog, loBase, wellDiam + " mm", wellShape );
												removeJob(thisJob, plateItem2);
											}
										}
									}
									break;
								case "1":
									formatType = "96 Wells Plate";
									nextY = Print.addSubTitle(e, nextY+10, formatType + "s:");
									
									foreach(ListViewItem plateItem2 in listViewPrintJobs.Items)
									{
										string itemName = plateItem2.SubItems[1].Text;
										PrintJob thisJob = (PrintJob) plateItem2.Tag;
										
										XmlNodeList plateList2 = m_mf.m_xmlData.m_xmlData.SelectNodes("//plates/group/plate[@name='" + itemName + "' and @format='1']");
									
										XmlNode node = m_mf.m_xmlData.m_xmlData.SelectSingleNode("//plates/group/plate[@name='" + itemName + "' and @format='1']");

										foreach(XmlNode plate2 in plateList2)
										{
											string plateName = plate2.Attributes["name"].Value;
											string height = plate2.Attributes["height"].Value;
											string depth = plate2.Attributes["depth"].Value;
											string yo = plate2.Attributes["yo"].Value;											
											string dbwc = plate2.Attributes["dbwc"].Value;
											string yo2 = "not defined";
											string dbwc2 = "not defined";
											if( plate2.Attributes["yo2"] != null )
											{
												yo2 = plate2.Attributes["yo2"].Value + " mm";						
												dbwc2 = plate2.Attributes["dbwc2"].Value + " mm";
											}
											string loBase = "No";
											if( plate2.Attributes["lobase"] != null )
											{
												bool bLoBase = bool.Parse( plate2.Attributes["lobase"].Value );
												loBase = bLoBase ? "Yes" : "No";
											}
											string maxVolume = plate2.Attributes["max_volume"].Value;
//											string aspOffset = plate2.Attributes["asp_offset"].Value;
											string catalog = "";
											if( node != null )
											{
												catalog = node.Attributes["type_no"].Value;
											}

											string wellDiam = "0";
											string wellShape = "Flat-bottom";
											try
											{
												wellDiam = plate2.Attributes["diameter"].Value;
												wellShape = plate2.Attributes["shape"].Value;
												if( wellShape == "0" ) wellShape = "Flat-bottom";
												else if( wellShape == "1" ) wellShape = "U-bottom";
												else if( wellShape == "2" ) wellShape = "V-bottom";
											}
											catch{}

											int maxY = e.MarginBounds.Height;
											if((nextY + 50) < maxY)
											{
												nextY = Print.addPlateInfo2(e, nextY+5, plateName, yo + " mm", dbwc + " mm", yo2, dbwc2, height + " mm", depth + " mm", maxVolume + " µl", catalog, loBase, wellDiam + " mm", wellShape );
												removeJob(thisJob, plateItem2);
											}

										}
									}
									break;
							}
						}
						break;
					}
						//else if(job.type == JobType.Liquid)
					case JobType.Liquid:
					{						
						nextY = Print.addLogo(e);
						nextY = Print.addTitle(e, nextY, "BNX1536 Liquid Sheet");
						nextY = Print.addSimpleTitleHeader(e, nextY, m_mf.m_User.Username, XmlData.PROGRAM_VERSION);
						nextY = Print.addSubTitle(e, nextY+10, "Liquids:");
						nextY = Print.addLiquidHeader(e, nextY);
						Print.addFooter(e, DateTime.Now, m_pageNum);

						int i = 1;
						if( m_liquidCount > 0 )
						{
							i = (m_liquidCount * (m_pageNum-1))+1;
							m_liquidCount = 0;
						}
						bool stopPage = false;
						foreach(ListViewItem liquidItem in listViewPrintJobs.Items)
						{
							m_liquidCount++;
							string itemName = liquidItem.SubItems[1].Text;
							PrintJob thisJob = (PrintJob) liquidItem.Tag;

							// Liquids
							XmlNodeList liquidList = m_mf.m_xmlData.m_xmlData.SelectNodes("//liquids/liquid[@name='" + itemName + "']");
					
							foreach(XmlNode liquid in liquidList)
							{
								string liquidName = liquid.Attributes["name"].Value;
								string lf = liquid.Attributes["liquid_factor"].Value;
								nextY = Print.addLiquidItem(e, nextY, Convert.ToString(i++), lf, liquidName);
								removeJob(thisJob, liquidItem);

								if(m_liquidCount > 41) // 41 is max rows on a page
								{
									stopPage = true;
									break;
								}								
							}
							if( stopPage ) break;
							
						}
						//removeJob(job, item);
						break;
					}
				}
	
				//new page
				if(listViewPrintJobs.Items.Count > 0 || m_liquidCount > 41)
				{					
					m_previewHelper--; // substract since it's the same print
					e.HasMorePages = true; // this triggers a new print
				}
				else
				{
					e.HasMorePages = false;
					foreach(ListViewItem item2 in m_lv.Items)
					{
						listViewPrintJobs.Items.Add((ListViewItem)item2.Clone());
					}
					m_firstPage = true;
				}
			}
			
			//nextY = Print.addCurrentProgramHeader(e, nextY, "New Program 1", "Administrator", m_mf.m_User.Username, XmlData.PROGRAM_VERSION);

			//e.HasMorePages = false;
		}

		/// <summary>
		/// No program is more than 2 pages so we hack in extra jobs
		/// </summary>
		/// <param name="srcItem"></param>
		void InsertPage2Program( ListViewItem srcItem )
		{
			for( int i=0; i<listViewPrintJobs2.Items.Count; i++ )
			{
				if( listViewPrintJobs2.Items[i].Text == srcItem.Text
					&& listViewPrintJobs2.Items[i].SubItems[1].Text == srcItem.SubItems[1].Text )
				{
					listViewPrintJobs.Items.Insert( 0, (ListViewItem)listViewPrintJobs2.Items[i].Clone() );
					break;
				}
			}
		}

		private bool showPrintDialog()
		{
			PrintDialog printDialog = new PrintDialog();
			printDialog.ShowNetwork = true;
			printDialog.AllowSelection = false; //we don't have a selection
			printDialog.AllowSomePages = false;
			printDialog.PrintToFile = false;

			PrinterSettings ps = new PrinterSettings();
			if( m_mf.m_printerName != null ) ps.PrinterName = m_mf.m_printerName;
			//ps.MaximumPage = -1;
			//ps.MinimumPage = 1;
			ps.FromPage = 1;
			ps.ToPage = 1;
			ps.PrintRange = PrintRange.AllPages;
			document.PrinterSettings = ps;


			printDialog.Document = this.document;
			DialogResult dr = printDialog.ShowDialog( this );
			if( dr == DialogResult.Cancel )
			{
				return false;
			}
			m_mf.m_printerName = ps.PrinterName;

			if( ps.DefaultPageSettings.Landscape )
			{
				MessageBox.Show( this, "Landscape print mode is not supported.\r\n\r\nResetting to portrait mode.", "Mode not supported", MessageBoxButtons.OK, MessageBoxIcon.Information );
				this.document.DefaultPageSettings.Landscape = false;
				return false;
			}

			return true;
		}
		
		private void printButton_Click(object sender, System.EventArgs e)
		{
			if( listViewPrintJobs.Items.Count == 0 )
			{
				MessageBox.Show( this, "No sheets added.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information );
				return;
			}

			m_pageNum = 0;
			try 
			{
				FixFPU();
				if( showPrintDialog() )
				{
					document.Print();
					Close();
				}
				else
				{
					return;
				}
				
			}
			catch(Exception)
			{
			}
			//DialogResult = DialogResult.OK;
			listViewPrintJobs2.Items.Clear();
		}

		private void buttonSettings_Click(object sender, System.EventArgs e)
		{
			showPrintDialog();
		}

		private void listViewPrintJobs_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
		{
			listViewPrintJobs.Sort();
		}
	}
}
