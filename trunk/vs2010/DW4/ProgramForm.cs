using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;

using System.Diagnostics;

namespace AQ3
{
	/// <summary>
	/// Summary description for Form2.
	/// </summary>
	public struct Inlet
	{
		public string Liquid;
		public string LiqFact;
		public string Pressure;

		public Inlet(string liquid, string liqFact, string pressure)
		{
			this.Liquid = liquid;
			this.LiqFact = liqFact;
			this.Pressure = pressure;
		}
	}
		
	public class programForm : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components;
		public System.Windows.Forms.Panel programPanel;
		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.PictureBox pictureBox1;
		public System.Windows.Forms.Label labelProgram;

		// public helpers
		public UserControl ProgramCardToBeMoved;

		// the main card list
		public ArrayList CardArray = new ArrayList();
		public ArrayList RepeatCardArray = new ArrayList();
		private ArrayList EmptyCardArray = new ArrayList();
		private Hashtable m_inletList = new Hashtable();
		public ArrayList m_dispenseCardList = new ArrayList();
		public ArrayList m_aspirateCardList = new ArrayList();
		public Inlet[] inlets = new Inlet[4];
		
		// positioning
		private int leftOffset = 20;
		public int topOffset = 70;
		private int plateHeight = 190;
		private int rowColDiff = 66;
		//private int plateHeight = 171;
		private System.Windows.Forms.ImageList imageListToolbar;
		private int emptySmallSizeCardHeight = 15;

		// helpers
		private bool m_bCreateNew = false;
		public string m_strFileNameInternal;
		public string m_strProgramName;
		private string m_strUsername;
		private System.Windows.Forms.Panel panelRepeatCardGhost;
		public System.Windows.Forms.Label labelFileUser;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private bool m_bFirstTimeSave = false;
		public bool m_loBase = false;
		public bool m_changeAllowed = true;
		private System.Windows.Forms.Button minMaxBtn;
		private bool m_maximizeCards = true;


		public programForm(string strFileNameInternal, string strProgramName, string strUsername, bool bCreateNew)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );


			m_bCreateNew = bCreateNew;
			m_strFileNameInternal = strFileNameInternal;
			m_strProgramName = strProgramName;
			m_strUsername = strUsername;

			string strLabel = "";
			string strLabelWindow = "";
			string strFileUser = "";
			if (m_strUsername != "____BNX1536_")
			{
				strLabel = "Program: " + m_strProgramName;
				strLabelWindow = "Program: " + strProgramName + " (File: " + strFileNameInternal + ", User: " + strUsername + ")";
				strFileUser =  "File: " + m_strFileNameInternal + ", User: " + m_strUsername;
			}
			else
			{
				strLabel = "Program: " + m_strProgramName;
				strLabelWindow = "Program: " + strProgramName + " (Current BNX1536 Program)";
				strFileUser = "(Current BNX1536 Program)";
			}
			Text = strLabelWindow;
			labelProgram.Text = strLabel;
			labelFileUser.Text = strFileUser;

			if (bCreateNew)
			{
				m_bFirstTimeSave = true;

				// add default plate
				PlateCard PC = new PlateCard();
				PC.Left = leftOffset + programPanel.AutoScrollPosition.X;
				PC.Top = topOffset + programPanel.AutoScrollPosition.Y;
				PC.Parent = programPanel;

				ProgramGUIElement PGE = new ProgramGUIElement();
				PGE.strCardName = "platecard";
				PGE.uc = PC;
				CardArray.Add(PGE);
				CardChanged(PC);
			
				// add card placeholder
				EmptyFullSizeCard emptyFullSizeCard = new EmptyFullSizeCard();
				emptyFullSizeCard.Left = leftOffset + programPanel.AutoScrollPosition.X;
				//emptyFullSizeCard.Top = topOffset + plateHeight + programPanel.AutoScrollPosition.Y;
				emptyFullSizeCard.Top = topOffset + PC.Height + programPanel.AutoScrollPosition.Y;
				emptyFullSizeCard.Parent = programPanel;
				EmptyCardArray.Add(emptyFullSizeCard);
			}
			else
			{
				// load data form xml file and create the visuals
				// must be done in programForm_Load()
			}
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Aspirate"}, 0, System.Drawing.Color.Black, System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(196)))), ((int)(((byte)(254))))), new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "Dispense"}, 1, System.Drawing.Color.Black, System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(196)))), ((int)(((byte)(254))))), new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem(new string[] {
            "Soak"}, 2, System.Drawing.Color.Black, System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(196)))), ((int)(((byte)(254))))), new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem(new string[] {
            "Repeat"}, 5, System.Drawing.Color.Black, System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(196)))), ((int)(((byte)(254))))), new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem(new string[] {
            "Wells"}, 6, System.Drawing.Color.Black, System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(196)))), ((int)(((byte)(254))))), new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(programForm));
            this.listView = new System.Windows.Forms.ListView();
            this.minMaxBtn = new System.Windows.Forms.Button();
            this.imageListToolbar = new System.Windows.Forms.ImageList(this.components);
            this.programPanel = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labelFileUser = new System.Windows.Forms.Label();
            this.panelRepeatCardGhost = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.labelProgram = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.listView.SuspendLayout();
            this.programPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // listView
            // 
            this.listView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(196)))), ((int)(((byte)(254)))));
            this.listView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listView.Controls.Add(this.minMaxBtn);
            this.listView.Dock = System.Windows.Forms.DockStyle.Left;
            listViewItem1.StateImageIndex = 0;
            listViewItem1.UseItemStyleForSubItems = false;
            listViewItem2.Checked = true;
            listViewItem2.StateImageIndex = 1;
            listViewItem2.UseItemStyleForSubItems = false;
            listViewItem3.Checked = true;
            listViewItem3.StateImageIndex = 2;
            listViewItem3.UseItemStyleForSubItems = false;
            listViewItem4.Checked = true;
            listViewItem4.StateImageIndex = 5;
            listViewItem4.UseItemStyleForSubItems = false;
            listViewItem5.Checked = true;
            listViewItem5.StateImageIndex = 6;
            listViewItem5.UseItemStyleForSubItems = false;
            this.listView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5});
            this.listView.LargeImageList = this.imageListToolbar;
            this.listView.Location = new System.Drawing.Point(0, 0);
            this.listView.MultiSelect = false;
            this.listView.Name = "listView";
            this.listView.Scrollable = false;
            this.listView.Size = new System.Drawing.Size(76, 626);
            this.listView.TabIndex = 3;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.listView_ItemDrag);
            // 
            // minMaxBtn
            // 
            this.minMaxBtn.Location = new System.Drawing.Point(5, 352);
            this.minMaxBtn.Name = "minMaxBtn";
            this.minMaxBtn.Size = new System.Drawing.Size(64, 40);
            this.minMaxBtn.TabIndex = 4;
            this.minMaxBtn.Text = "Minimize Cards";
            this.minMaxBtn.Click += new System.EventHandler(this.button1_Click);
            // 
            // imageListToolbar
            // 
            this.imageListToolbar.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListToolbar.ImageStream")));
            this.imageListToolbar.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListToolbar.Images.SetKeyName(0, "");
            this.imageListToolbar.Images.SetKeyName(1, "");
            this.imageListToolbar.Images.SetKeyName(2, "");
            this.imageListToolbar.Images.SetKeyName(3, "");
            this.imageListToolbar.Images.SetKeyName(4, "");
            this.imageListToolbar.Images.SetKeyName(5, "");
            this.imageListToolbar.Images.SetKeyName(6, "");
            // 
            // programPanel
            // 
            this.programPanel.AllowDrop = true;
            this.programPanel.AutoScroll = true;
            this.programPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.programPanel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("programPanel.BackgroundImage")));
            this.programPanel.Controls.Add(this.label4);
            this.programPanel.Controls.Add(this.label3);
            this.programPanel.Controls.Add(this.labelFileUser);
            this.programPanel.Controls.Add(this.panelRepeatCardGhost);
            this.programPanel.Controls.Add(this.label2);
            this.programPanel.Controls.Add(this.label1);
            this.programPanel.Controls.Add(this.labelProgram);
            this.programPanel.Controls.Add(this.pictureBox1);
            this.programPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.programPanel.Location = new System.Drawing.Point(76, 0);
            this.programPanel.Name = "programPanel";
            this.programPanel.Size = new System.Drawing.Size(706, 626);
            this.programPanel.TabIndex = 1;
            this.programPanel.DoubleClick += new System.EventHandler(this.programPanel_DoubleClick);
            this.programPanel.DragOver += new System.Windows.Forms.DragEventHandler(this.programPanel_DragOver);
            this.programPanel.Click += new System.EventHandler(this.programPanel_Click);
            this.programPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.programPanel_DragDrop);
            this.programPanel.DragLeave += new System.EventHandler(this.programPanel_DragLeave);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(528, 208);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(168, 51);
            this.label4.TabIndex = 15;
            this.label4.Text = "- Edit parameters on program card to customize.";
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(528, 88);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(168, 32);
            this.label3.TabIndex = 14;
            this.label3.Text = "- Select wells to be used to dispense fluids.";
            // 
            // labelFileUser
            // 
            this.labelFileUser.BackColor = System.Drawing.Color.Transparent;
            this.labelFileUser.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFileUser.ForeColor = System.Drawing.Color.White;
            this.labelFileUser.Location = new System.Drawing.Point(22, 43);
            this.labelFileUser.Name = "labelFileUser";
            this.labelFileUser.Size = new System.Drawing.Size(496, 16);
            this.labelFileUser.TabIndex = 13;
            this.labelFileUser.Text = "File:";
            // 
            // panelRepeatCardGhost
            // 
            this.panelRepeatCardGhost.BackColor = System.Drawing.Color.Transparent;
            this.panelRepeatCardGhost.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelRepeatCardGhost.BackgroundImage")));
            this.panelRepeatCardGhost.Location = new System.Drawing.Point(528, 264);
            this.panelRepeatCardGhost.Name = "panelRepeatCardGhost";
            this.panelRepeatCardGhost.Size = new System.Drawing.Size(169, 158);
            this.panelRepeatCardGhost.TabIndex = 12;
            this.panelRepeatCardGhost.Visible = false;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(528, 136);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(168, 72);
            this.label2.TabIndex = 10;
            this.label2.Text = "- Click and drag desired program card icon from the toolbox to desktop to add or " +
                "repeat an operation.";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(528, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(168, 32);
            this.label1.TabIndex = 9;
            this.label1.Text = "- Select microplate name from Plate library.";
            // 
            // labelProgram
            // 
            this.labelProgram.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgram.BackColor = System.Drawing.Color.Transparent;
            this.labelProgram.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProgram.ForeColor = System.Drawing.Color.White;
            this.labelProgram.Location = new System.Drawing.Point(20, 16);
            this.labelProgram.Name = "labelProgram";
            this.labelProgram.Size = new System.Drawing.Size(676, 24);
            this.labelProgram.TabIndex = 8;
            this.labelProgram.Text = "Program:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(560, 488);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(136, 128);
            this.pictureBox1.TabIndex = 11;
            this.pictureBox1.TabStop = false;
            // 
            // programForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(782, 626);
            this.Controls.Add(this.programPanel);
            this.Controls.Add(this.listView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "programForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Program";
            this.Load += new System.EventHandler(this.programForm_Load);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.programForm_MouseWheel);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.programForm_Closing);
            this.listView.ResumeLayout(false);
            this.programPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		public void Save()
		{
			if( ProgramError( null, "Program full. Please remove cards, repeats or the number of ASP Offset changes." ) ) return;

			try
			{
				ValidateAllCards(false);
			}
			catch( Exception e )
			{
				MessageBox.Show( e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information );
				return;
			}
			// cannot save empty programs
			ProgramGUIElement PGETemp = (ProgramGUIElement)CardArray[0];
			PlateCard plateCard = (PlateCard)PGETemp.uc;
			if (plateCard.textBoxPlateName.Text.Length < 1)
			{
				MessageBox.Show(this, "Can not save empty programs!", "Program", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			mainForm mf = (mainForm)this.MdiParent;

			if (m_strUsername == "____BNX1536_")
			{
				MessageBox.Show(this, "Edited BNX1536 internal files must be saved to your user.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);

				ProgramSaveForm PSF = new ProgramSaveForm();
				m_strFileNameInternal = "New File";
				bool bValid = false;
				while (!bValid)
				{
					DialogResult DR = PSF._ShowDialog(ref m_strFileNameInternal, ref m_strProgramName, mf);
					if (DR != DialogResult.OK)
					{
						m_strFileNameInternal = "____BNX1536_";
						return;
					}
					if (mf.m_xmlData.GetNumberOfProgramsInFile(m_strFileNameInternal, mf.m_User.Username) < 99)
					{
						bValid = true;
					}
					else
					{
						MessageBox.Show(this, "File full. Please select another file name.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}
				
				// change user
				m_strUsername = mf.m_User.Username;
			}

			if (m_strUsername != mf.m_User.Username)
			{
				MessageBox.Show(this, "You are not allowed to alter other users programs.\nProgram NOT saved...", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			if (mf.m_User.UserLevel == 1)
			{
				MessageBox.Show(this, "Only supervisors and administrators are allowed to save programs.\nProgram NOT saved...", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			if (m_bFirstTimeSave)
			{
				ProgramSaveForm PSF = new ProgramSaveForm();

				bool bValid = false;
				while (!bValid)
				{
					DialogResult DR = PSF._ShowDialog(ref m_strFileNameInternal, ref m_strProgramName, mf);
					if (DR != DialogResult.OK)
					{
						return;
					}
					if (mf.m_xmlData.GetNumberOfProgramsInFile(m_strFileNameInternal, mf.m_User.Username) < 99)
					{
						bValid = true;
					}
					else
					{
						MessageBox.Show(this, "File full. Please select another file name.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}
			}

			// search for file
			TreeNode treeNodeFile = null;
			for (int nFile = 0; nFile < mf.treeView.Nodes[0].Nodes.Count; nFile++)
			{
				if (m_strFileNameInternal == mf.treeView.Nodes[0].Nodes[nFile].Text)
				{
					treeNodeFile = mf.treeView.Nodes[0].Nodes[nFile];
					break;
				}
			}
			if (null == treeNodeFile)
			{
				treeNodeFile = new TreeNode(m_strFileNameInternal, 7, 7);
				mf.treeView.Nodes[0].Nodes.Add(treeNodeFile);
			}

			// search for program
			bool bSave = true;
			TreeNode treeNodeProgram = null;
			for (int nProgram = 0; nProgram < treeNodeFile.Nodes.Count; nProgram++)
			{
				if (treeNodeFile.Nodes[nProgram].Text == m_strProgramName)
				{
					treeNodeProgram = treeNodeFile.Nodes[nProgram];
					if (m_bFirstTimeSave)
					{
						string strMessage = "\"" + m_strProgramName + "\"" + " in " + "\"" + m_strFileNameInternal + "\"" + " already exists.\n";
						strMessage += "Do you want to replace it?";
						DialogResult DR = MessageBox.Show(this, strMessage, "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
						if (DR == DialogResult.No)
						{
							bSave = false;
						}
					}
					break;
				}
			}
			if (null == treeNodeProgram)
			{
				treeNodeProgram = new TreeNode(m_strProgramName, 5, 5);
				treeNodeFile.Nodes.Add(treeNodeProgram);
			}
			if (bSave)
			{
				m_bFirstTimeSave = false;

				try
				{
					mf.m_xmlData.SaveProgram(CardArray, RepeatCardArray, m_strFileNameInternal, m_strProgramName, mf.m_User.Username);

					Text = "Program: " + m_strProgramName + " (File: " + m_strFileNameInternal + ", User: " + m_strUsername + ")";
					labelProgram.Text = "Program: " + m_strProgramName;
					Tag = false;
					labelFileUser.Text = "File: " + m_strFileNameInternal + ", User: " + m_strUsername;
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
				}
			}
		}

		public void AddProgramCardByDrop(object sender, System.Windows.Forms.DragEventArgs e, string strTrigger)
		{
			UserControl ProgramCard = null;
			bool bCanHaveCardsAfter = false;
			string strProgramCard = null;
			int plateFormat = ((ProgramGUIElement)CardArray[0]).platecard_format;

			// figure out instruction number
			int nInstructionNumber = CardArray.Count;
			
			// create new card
			string strKindOfCard = e.Data.GetData(DataFormats.Text).ToString();
			if (strKindOfCard == "ListViewItem: {Aspirate}")
			{
				ProgramCard = new aspirateCard();
				((aspirateCard)ProgramCard).labelProgramStep.Text = nInstructionNumber.ToString();
				bCanHaveCardsAfter = true;
				strProgramCard = "aspiratecard";
			}
			else if (strKindOfCard == "ListViewItem: {Dispense}")
			{
				ProgramCard = new dispenseCard();
				((dispenseCard)ProgramCard).labelProgramStep.Text = nInstructionNumber.ToString();
				bCanHaveCardsAfter = true;
				strProgramCard = "dispensecard";
			}
			else if (strKindOfCard == "ListViewItem: {Soak}")
			{
				ProgramCard = new soakCard();
				((soakCard)ProgramCard).labelProgramStep.Text = nInstructionNumber.ToString();
				bCanHaveCardsAfter = true;
				strProgramCard = "soakcard";
			}
			else if (strKindOfCard == "ListViewItem: {Repeat}")
			{

			}
			else if (strKindOfCard == "ListViewItem: {Wells}")
			{
				ProgramCard = new PlateCardRowsOnly();
				((PlateCardRowsOnly)ProgramCard).labelProgramStep.Text = nInstructionNumber.ToString();
				bCanHaveCardsAfter = true;
				strProgramCard = "platecardrowsonly";
			}

			// triggered by full-sized card at the end
			if (strTrigger == "EmptyFullSizeCard")
			{
				// get rid of sender (the full-sized empty program card... always at the end)
				((UserControl)sender).Dispose();
				EmptyCardArray.RemoveAt(EmptyCardArray.Count - 1);

				// placeholder card before
				EmptySmallSizeCard EmptyCardBefore = new EmptySmallSizeCard();
				EmptyCardBefore.Left = leftOffset + programPanel.AutoScrollPosition.X;
				//EmptyCardBefore.Top = nInstructionNumber * (plateHeight + emptySmallSizeCardHeight) + topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;

				int top = topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;
				for( int i=0; i<nInstructionNumber; i++ )
				{
					top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
				}
				EmptyCardBefore.Top = top;

				EmptyCardBefore.Parent = programPanel;
				EmptyCardArray.Add(EmptyCardBefore);

				// the card
				ProgramCard.Left = leftOffset + programPanel.AutoScrollPosition.X;
				//ProgramCard.Top = nInstructionNumber * (plateHeight + emptySmallSizeCardHeight) + topOffset + programPanel.AutoScrollPosition.Y;
				top = topOffset + programPanel.AutoScrollPosition.Y;
				for( int i=0; i<nInstructionNumber; i++ )
				{
					top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
				}
				ProgramCard.Top = top;

				try
				{
					ProgramCard.Parent = programPanel;
				}
				catch( NullReferenceException )
				{
					// I have no idea why this happens????
					// It happens just now and then
					// The parent is still assigned
				}
				ProgramGUIElement PGE = new ProgramGUIElement();
				PGE.strCardName = strProgramCard;
				PGE.uc = ProgramCard;
				CardArray.Add(PGE);
				CardChanged(ProgramCard);

				// placeholder card after
				if (bCanHaveCardsAfter)
				{
					EmptyFullSizeCard emptyFullSizeCard = new EmptyFullSizeCard();
					emptyFullSizeCard.Left = leftOffset + programPanel.AutoScrollPosition.X;
					//emptyFullSizeCard.Top = (nInstructionNumber + 1) * (plateHeight + emptySmallSizeCardHeight) + topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;
					top = topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;
					for( int i=0; i<=nInstructionNumber; i++ )
					{
						top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
					}
					emptyFullSizeCard.Top = top;
					emptyFullSizeCard.Parent = programPanel;
					EmptyCardArray.Add(emptyFullSizeCard);
				}
			}
			
			// triggered by small-sized card somewhere in-between
			else if (strTrigger == "EmptySmallSizeCard")
			{
				// check if this is a card which cannot have cards after... and ask user for decision
				bool bDeleteAfter = false;
			
				int nInsertCardPosition = 1;
				int nInsertEmptyCardPosition = 0;
				
				// move or delete cards after
				bool bFound = false;
				int nCardArraySizeInitial = CardArray.Count;
				for (int i = 0; i < nCardArraySizeInitial; i++)
				{
					if (!bFound) // search forward until we reach the triggering card
					{
						if ((UserControl)EmptyCardArray[i] == sender)
						{
							bFound = true;
							nInsertCardPosition = i + 1;
							nInsertEmptyCardPosition = i;

							if (bDeleteAfter) // delete it
							{
								UserControl EmptyCard = (UserControl)EmptyCardArray[i];
								EmptyCard.Dispose();
								EmptyCardArray.RemoveAt(i);
							}
							else // move it (visual... insert will move it in array)
							{
								UserControl EmptyCard = (UserControl)EmptyCardArray[i];
								//EmptyCard.Top += (plateHeight + emptySmallSizeCardHeight);								
								EmptyCard.Top += (ProgramCard.Height + emptySmallSizeCardHeight);
								if( plateFormat < 4 && ProgramCard is PlateCardRowsOnly )
								{
									EmptyCard.Top -= rowColDiff;
								}
							}
						}
					}
					else // triggering card is found. move or delete cards after.
					{
						if (bDeleteAfter) // delete cards after
						{
							ProgramGUIElement PGE = (ProgramGUIElement)CardArray[nInsertCardPosition];
							PGE.uc.Dispose();
							CardArray.RemoveAt(nInsertCardPosition);

							if (EmptyCardArray.Count > nInsertEmptyCardPosition)
							{
								UserControl EmptyCard = (UserControl)EmptyCardArray[nInsertEmptyCardPosition];
								EmptyCard.Dispose();
								EmptyCardArray.RemoveAt(nInsertEmptyCardPosition);
							}
						}
						else // move cards after (visual... insert will move them in array)
						{
							// move normal cards
							ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];

							PGE.uc.Top += (ProgramCard.Height + emptySmallSizeCardHeight);
							//PGE.uc.Top += (plateHeight + emptySmallSizeCardHeight);							
							if( plateFormat < 4 && ProgramCard is PlateCardRowsOnly )
							{
								PGE.uc.Top -= rowColDiff;
							}

							// ...and label them with the correct instruction number
							if (PGE.strCardName == "aspiratecard")
							{
								((aspirateCard)PGE.uc).labelProgramStep.Text = (i + 1).ToString();
							}
							else if (PGE.strCardName == "dispensecard")
							{
								((dispenseCard)PGE.uc).labelProgramStep.Text = (i + 1).ToString();
							}
							else if (PGE.strCardName == "soakcard")
							{
								((soakCard)PGE.uc).labelProgramStep.Text = (i + 1).ToString();
							}
							else if (PGE.strCardName == "platecardrowsonly")
							{
								((PlateCardRowsOnly)PGE.uc).labelProgramStep.Text = (i + 1).ToString();
							}

							// move empty cards
							if (EmptyCardArray.Count > i)
							{
								UserControl EmptyCard = (UserControl)EmptyCardArray[i];
								//EmptyCard.Top += (plateHeight + emptySmallSizeCardHeight);
								EmptyCard.Top += (ProgramCard.Height + emptySmallSizeCardHeight);
								if( plateFormat < 4 && ProgramCard is PlateCardRowsOnly )
								{
									EmptyCard.Top -= rowColDiff;
								}
							}
						}
					}
				}

				// handle repeat cards
				for (int j = 0; j < RepeatCardArray.Count; j++)
				{
					ProgramGUIElement PGER = (ProgramGUIElement)RepeatCardArray[j];
					repeatCard rcard = (repeatCard)PGER.uc;

					repeatCard newrcard = new repeatCard();
					for( int i=0; i<rcard.comboBoxFrom.Items.Count; i++ )
					{
						newrcard.comboBoxFrom.Items.Add( rcard.comboBoxFrom.Items[i] );
						newrcard.comboBoxTo.Items.Add( rcard.comboBoxFrom.Items[i] );
					}

					newrcard.comboBoxFrom.Items.Add( CardArray.Count.ToString() );
					newrcard.comboBoxTo.Items.Add( CardArray.Count.ToString() );

					int from = PGER.repeatcard_from;
					int to = PGER.repeatcard_to;
					// check if you insert card within a loop
					// if so leave the to pos where it was
					if( nInsertCardPosition > to && nInsertCardPosition <= from )
					{
						//expand repeat for cards placed inside it
						from++;
					}
					else if( nInsertCardPosition <= to )
					{
						// move repeat one down
						from++;
						to++;
					}
					PGER.repeatcard_from = from;
					PGER.repeatcard_to = to;

					newrcard.Parent = rcard.Parent;
					newrcard.Left = rcard.Left;
					newrcard.Height = rcard.Height;
					newrcard.Top = rcard.Top;
					newrcard.Top += (ProgramCard.Height + emptySmallSizeCardHeight);
					newrcard.comboBoxTo.Text = to.ToString();
					newrcard.comboBoxFrom.Text = from.ToString();
					newrcard.comboBoxRepeats.Text = rcard.comboBoxRepeats.Text;
					rcard.Dispose();
					PGER.uc = newrcard;


//					if (PGER.repeatcard_from >= nInsertCardPosition)
//					{
//						int from = PGER.repeatcard_from + 1;
//						PGER.repeatcard_from = from;
//						rcard.comboBoxFrom.Text = from.ToString();						
//						PGER.repeatcard_from = from;
//					}
//
//					if (PGER.repeatcard_to >= nInsertCardPosition)
//					{
//						int to = PGER.repeatcard_to + 1;
//						PGER.repeatcard_to = to;
//						rcard.comboBoxTo.Text = to.ToString();
//						PGER.repeatcard_to = to;
//					}
				}

				// placeholder card before
				EmptySmallSizeCard emptySmallSizeCard = new EmptySmallSizeCard();
				emptySmallSizeCard.Left = leftOffset + programPanel.AutoScrollPosition.X;
				//emptySmallSizeCard.Top = nInsertCardPosition * (plateHeight + emptySmallSizeCardHeight) + topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;
				int top = topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;
				for( int i=0; i<nInsertCardPosition; i++ )
				{
					top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
				}
				emptySmallSizeCard.Top = top;
								
				emptySmallSizeCard.Parent = programPanel;
				EmptyCardArray.Insert(nInsertEmptyCardPosition, emptySmallSizeCard);
									
				// the card
				if (strKindOfCard == "ListViewItem: {Aspirate}")
				{
					((aspirateCard)ProgramCard).labelProgramStep.Text = nInsertCardPosition.ToString();
				}
				else if (strKindOfCard == "ListViewItem: {Dispense}")
				{
					((dispenseCard)ProgramCard).labelProgramStep.Text = nInsertCardPosition.ToString();
				}
				else if (strKindOfCard == "ListViewItem: {Soak}")
				{
					((soakCard)ProgramCard).labelProgramStep.Text = nInsertCardPosition.ToString();
				}
				else if (strKindOfCard == "ListViewItem: {Wells}")
				{
					((PlateCardRowsOnly)ProgramCard).labelProgramStep.Text = nInsertCardPosition.ToString();
				}
				ProgramCard.Left = leftOffset + programPanel.AutoScrollPosition.X;
				//ProgramCard.Top = nInsertCardPosition * (plateHeight + emptySmallSizeCardHeight) + topOffset + programPanel.AutoScrollPosition.Y;
				top = topOffset + programPanel.AutoScrollPosition.Y;
				for( int i=0; i<nInsertCardPosition; i++ )
				{
					top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
				}
				ProgramCard.Top = top;

				ProgramCard.Parent = programPanel;
				ProgramGUIElement PGENew = new ProgramGUIElement();
				PGENew.strCardName = strProgramCard;
				PGENew.uc = ProgramCard;
				CardArray.Insert(nInsertCardPosition, PGENew);
				CardChanged(ProgramCard);
			}

			// scroll
			Point p = programPanel.AutoScrollPosition;
			p.Y = ProgramCard.Top - programPanel.AutoScrollPosition.Y;
			programPanel.AutoScrollPosition = p;
		}

		public void DeleteProgramCard(object sender)
		{			
			Tag = true;
			if (!Text.EndsWith("*") && m_strUsername != "____BNX1536_")
			{
				Text += "*";
				labelProgram.Text += "*";
			}

			// special case with repeatcards
			for (int i = 0; i < RepeatCardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];

				if (PGE.uc == sender)
				{
					PGE.uc.Dispose();
					RepeatCardArray.RemoveAt(i);
					return;
				}
			}

			// find card in CardArray and move cards after upwards
			bool bFound = false;
			int nIndexToBeDeleted = 0;
			int extraOffset = 0;
			int plateFormat = ((ProgramGUIElement)CardArray[0]).platecard_format;
			int pixelsToMove = 0;
			for (int i = 0; i < CardArray.Count; i++)
			{	
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];

				UserControl EmptyCard = null;
				if (EmptyCardArray.Count > i)
				{
					EmptyCard = (UserControl)EmptyCardArray[i];
				}

				if (!bFound)
				{
					if (PGE.uc == sender)
					{
						// delete card and placeholder before... but not until after
						nIndexToBeDeleted = i;
						bFound = true;
						int num = i+1;
						if( num < CardArray.Count )
						{
							pixelsToMove = PGE.uc.Top - ((ProgramGUIElement)CardArray[i+1]).uc.Top;
						}
					}
					else
					{
						// do nothing
					}
				}
				else
				{
					// move cards under upwards
					PGE.uc.Top += pixelsToMove;

					if (EmptyCard != null)
					{
						EmptyCard.Top += pixelsToMove;
					}

					ProgramGUIElement prevGUI = (ProgramGUIElement)CardArray[i-1];
					// adjust where we move it according to the previous cards height
					// this happens only in column mode
					if( i < CardArray.Count-1 && prevGUI.uc.Height > PGE.uc.Height )
					{
						extraOffset += rowColDiff;
					}
					if( i < CardArray.Count-1 && prevGUI.uc.Height < PGE.uc.Height )
					{
						extraOffset -= rowColDiff;
					}

					if (PGE.strCardName == "aspiratecard")
					{
						((aspirateCard)PGE.uc).labelProgramStep.Text = (i - 1).ToString();
					}
					else if (PGE.strCardName == "dispensecard")
					{
						((dispenseCard)PGE.uc).labelProgramStep.Text = (i - 1).ToString();
						m_inletList.Remove(((dispenseCard)PGE.uc).comboBoxInlet.Text);
					}
					else if (PGE.strCardName == "soakcard")
					{
						((soakCard)PGE.uc).labelProgramStep.Text = (i - 1).ToString();
					}
					else if (PGE.strCardName == "platecardrowsonly")
					{
						((PlateCardRowsOnly)PGE.uc).labelProgramStep.Text = (i - 1).ToString();
					}

				}				
			}

//			for( int i=nIndexToBeDeleted-1; i<EmptyCardArray.Count; i++ )
//			{
//				((UserControl)EmptyCardArray[i]).Top -= extraOffset;
//			}

//			((UserControl)EmptyCardArray[EmptyCardArray.Count-1]).Top -= extraOffset;

			bool bChangeLastEmptyCardToFull = false;
			if (CardArray.Count == (nIndexToBeDeleted + 1))
			{
				bChangeLastEmptyCardToFull = true;
			}

			// ...here is the place to delete
			ProgramGUIElement PGEToDelete = (ProgramGUIElement)CardArray[nIndexToBeDeleted];
			PGEToDelete.uc.Dispose();
			CardArray.RemoveAt(nIndexToBeDeleted);
			if (EmptyCardArray.Count > CardArray.Count)
			{
				UserControl EmptyCardToDelete = (UserControl)EmptyCardArray[nIndexToBeDeleted];
				EmptyCardToDelete.Dispose();
				EmptyCardArray.RemoveAt(nIndexToBeDeleted);
			}

			if (bChangeLastEmptyCardToFull )
			{
				UserControl EmptyCardLast = (UserControl)EmptyCardArray[EmptyCardArray.Count - 1];

				EmptyFullSizeCard emptyFullSizeCard = new EmptyFullSizeCard();
				emptyFullSizeCard.Left = EmptyCardLast.Left;
				emptyFullSizeCard.Top = EmptyCardLast.Top;
				emptyFullSizeCard.Parent = programPanel;

				EmptyCardLast.Dispose();
				EmptyCardArray.RemoveAt(EmptyCardArray.Count - 1);

				EmptyCardArray.Add(emptyFullSizeCard);
			}

			// compensate for cards which should have no cards after
			UserControl ucLast = (UserControl)EmptyCardArray[EmptyCardArray.Count - 1];
			if (ucLast.Height == emptySmallSizeCardHeight)
			{
				ucLast.Dispose();
				EmptyCardArray.RemoveAt(EmptyCardArray.Count - 1);
			}

			// update all repeatcard values
			for (int i = 0; i < RepeatCardArray.Count; i++)
			{
				//TODO: Something messes up here
				//Mikael
				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];

				repeatCard rcard = (repeatCard)PGE.uc;
				int nFrom = Convert.ToInt32(rcard.comboBoxFrom.Text);
				int nTo = Convert.ToInt32(rcard.comboBoxTo.Text);

				int newFrom = rcard.comboBoxFrom.SelectedIndex;
				int newTo = rcard.comboBoxTo.SelectedIndex;
				if( nIndexToBeDeleted < nTo )
				{
					newFrom = rcard.comboBoxFrom.SelectedIndex-1;
					newTo = rcard.comboBoxTo.SelectedIndex-1;
					rcard.Top = rcard.Top - 205;
				}

				rcard.comboBoxFrom.Items.Clear();
				for (int ic = 0; ic < CardArray.Count - 1; ic++)
				{
					rcard.comboBoxFrom.Items.Add((ic + 1).ToString());
				}

				rcard.EditMode = true;
				rcard.comboBoxFrom.SelectedIndex = newFrom;
				
				//				if (nFrom > rcard.comboBoxFrom.Items.Count)
//				{
//					rcard.comboBoxFrom.SelectedIndex = rcard.comboBoxFrom.Items.Count - 1;
//				}
//				else
//				{
//					rcard.comboBoxFrom.SelectedIndex = nFrom - 1;
//				}
				
				rcard.comboBoxTo.Items.Clear();
				for (int ic = 0; ic < CardArray.Count - 1; ic++)
				{				
					rcard.comboBoxTo.Items.Add((ic + 1).ToString());
				}
				rcard.comboBoxTo.SelectedIndex = newTo;
				rcard.EditMode = false;
				CardChanged( rcard ); // save changes

//				if (nTo > rcard.comboBoxTo.Items.Count)
//				{
//					rcard.comboBoxTo.SelectedIndex = rcard.comboBoxTo.Items.Count - 1;
//				}
//				else
//				{
//					rcard.comboBoxTo.SelectedIndex = nTo - 1;
//				}
				
				//rcard.comboBoxFrom.SelectedIndex = nIndexToBeDeleted - 1;

				// special
				//rcard.comboBoxTo.Items.RemoveAt(rcard.comboBoxTo.Items.Count - 1);
			}

			if(EmptyCardArray.Count > 0)
			{
				if(nIndexToBeDeleted > 0)
				{
					UserControl focus = (UserControl)EmptyCardArray[nIndexToBeDeleted-1];
					focus.Focus();
				}
				else
				{
					UserControl focus = (UserControl)EmptyCardArray[0];
					focus.Focus();
				}
			}

		}

		public void MoveProgramCard(object sender, string strTrigger)
		{
		}

		public bool IsOverlapped(int from, int to, UserControl RepeatCard)
		{
			bool bOverlapped = false;
			for (int i = 0; i < RepeatCardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];
				
				if (PGE.uc == RepeatCard)
				{
					// do not check if overlappet with itself...
					continue;
				}

				bOverlapped = true;
				if (from > PGE.repeatcard_from && to > PGE.repeatcard_from)
				{
					bOverlapped = false;
				}

				if (from < PGE.repeatcard_to && to < PGE.repeatcard_to)
				{
					bOverlapped = false;
				}

				if (bOverlapped)
				{
					break;
				}
			}

			return bOverlapped;
		}

		public bool IsPartOfRepeat(UserControl ProgramCard)
		{
			bool bRetVal = false;

			for (int i = 0; i < CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];

				if (PGE.uc == ProgramCard)
				{
					for (int j = 0; j < RepeatCardArray.Count; j++)
					{
						ProgramGUIElement PGERepeatCard = (ProgramGUIElement)RepeatCardArray[j];

						if (i <= PGERepeatCard.repeatcard_from && i >= PGERepeatCard.repeatcard_to)
						{
							bRetVal = true;
							break;
						}
					}
					break;
				}
			}

			return bRetVal;
		}

		// call this to update PlateCardRowsOnly cards
		public void PlateCardNameChanged(bool bNewFormat)
		{
			for (int i = 0; i < CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
				if (PGE.strCardName == "platecardrowsonly")
				{
					try
					{
						if (bNewFormat)
						{
							((PlateCardRowsOnly)PGE.uc).Clear();
						}
						else
						{
							((PlateCardRowsOnly)PGE.uc).RedrawCurrentPlate(false);
						}
					}
					catch (Exception exception)
					{
						exception = exception;
					}
				}
			}
		}

		public void ValidateAllCards( bool loose )
		{
			for (int i = 0; i < CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
				if( PGE.strCardName == "platecard" )
				{
					PlateCard plateCard = (PlateCard)PGE.uc;
					plateCard.ValidateAll( loose );
				}
				else if( PGE.strCardName == "platecardrowsonly" )
				{
					PlateCardRowsOnly rowOnly = (PlateCardRowsOnly)PGE.uc;
					rowOnly.ValidateAll( loose );
				}
				else if (PGE.strCardName == "aspiratecard")
				{
					try
					{
						aspirateCard aspCard = (aspirateCard)PGE.uc;
						aspCard.ValidateAll();
						
					}
					catch (Exception e)
					{
						e = e;
					}
				}
				else if (PGE.strCardName == "dispensecard")
				{
					try
					{
						dispenseCard dispCard = (dispenseCard)PGE.uc;
						dispCard.ValidateAll();
					}
						
					catch (Exception e)
					{
						e = e;
					}
				}
			}
		}

		public void InletChanged(UserControl uc)
		{
			foreach(dispenseCard card in m_dispenseCardList)
			{
				int inlet = card.comboBoxInlet.SelectedIndex;
				card.textBoxLiquidFactor.Text = inlets[inlet].LiqFact;
				card.textBoxName.Text = inlets[inlet].Liquid;
				card.textBoxPressure.Text = inlets[inlet].Pressure;
				card.SetMinimizeLabel();
			}
		}

		public void ASPOffsetChanged(UserControl uc)
		{
			aspirateCard ac = (aspirateCard)uc;
			foreach(aspirateCard card in m_aspirateCardList)
			{
				card.textBoxASPOffset.Text = ac.textBoxASPOffset.Text;
			}
		}
		
		public void RepositionCards( int offset, int programStep )
		{
			RepositionCards( offset, programStep, false );
		}

		public void RepositionCards( int offset, int programStep, bool wellModeAdjust )
		{
			programPanel.Hide();
//			this.SuspendLayout();

			int origOffset = offset;

			int[] offsetArray = new int[CardArray.Count];

			for( int i=programStep; i<EmptyCardArray.Count; i++ )
			{
				UserControl uc = (UserControl)EmptyCardArray[i];

				if( wellModeAdjust && ((ProgramGUIElement)CardArray[i]).uc is PlateCardRowsOnly )
				{
					if( offset < 0 )
						offset -= rowColDiff;
					else if( offset > 0 )
						offset += rowColDiff;
				}
				uc.Top += offset;
				offsetArray[i] = offset;
			}

//			offset = origOffset;

			for( int i=programStep+1; i<CardArray.Count; i++ )
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
				PGE.uc.Top += offsetArray[i-1];
			}

			offset = origOffset;

			RepositionRepeats();

//			for( int i=0; i<RepeatCardArray.Count; i++ )
//			{
//				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];
//
//				ProgramGUIElement PGE2 = (ProgramGUIElement)CardArray[PGE.repeatcard_to];
//				bool adjust = true;
//				if( PGE2.uc is PlateCardRowsOnly )
//				{
//					//don't adjust if start is on a rowchange card
//					adjust = false;
//					//PGE.uc.Top += offset;
//				}
//
//				PGE.uc.Top += offsetArray[PGE.repeatcard_to-1];
//				if( adjust )
//				{
//					if( offset < 0 )
//						PGE.uc.Top += rowColDiff;
//					else
//						PGE.uc.Top -= rowColDiff;
//				}
//			}

			//this.ResumeLayout();
			programPanel.Show();
		}

		public void RepositionRepeats()
		{
//			programPanel.Hide();
			for( int i=0; i<RepeatCardArray.Count; i++ )
			{
				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];
				ProgramGUIElement PGEStart = (ProgramGUIElement)CardArray[PGE.repeatcard_to];
				ProgramGUIElement PGEEnd = (ProgramGUIElement)CardArray[PGE.repeatcard_from];

				//int pos = PGEStart.uc.Bottom - (158/2) + emptySmallSizeCardHeight; //158 is height of a repeat of one step
				if( PGE.uc != null && PGEStart.uc != null && PGEEnd.uc != null )
				{
					// new code for repeat
					// set top to half that of the startcards top

					int pos = PGEStart.uc.Bottom - (PGEStart.uc.Height/2); //158 is height of a repeat of one step
					//int height = PGEEnd.uc.Bottom - PGEStart.uc.Bottom;
					int height = ( PGEEnd.uc.Bottom - (PGEEnd.uc.Height/2) ) - pos;

					((repeatCard)PGE.uc).DrawSmall = false;
					// 66 is the size of a minimized card
					if( (PGEStart.uc.Height == 66 || PGEEnd.uc.Height == 66) && PGE.repeatcard_from-PGE.repeatcard_to == 1 )
					{
						PGE.uc.Height = 0; // force redraw
						((repeatCard)PGE.uc).DrawSmall = true;
						height = 81;
						if( PGEEnd.uc.Height == 66 )
						{
							pos = PGEEnd.uc.Top - 55;
						}
					}
					
					PGE.uc.Top = pos;
					PGE.uc.Height = height;
				}
			}
//			programPanel.Show();
		}

		// call this to update xml data
		public void CardChanged(UserControl uc)
		{
			//programPanel.Hide();
			//InletChanged(uc);

			Tag = true;
			if (!Text.EndsWith("*") && m_strUsername != "____BNX1536_")
			{
				Text += "*";
				labelProgram.Text += "*";
			}

			bool bProgramCard = false;
			for (int i = 0; i < CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
				if (PGE.uc == uc)
				{
					bProgramCard = true;

					if (PGE.strCardName == "platecard")
					{
						try
						{
							PlateCard plateCard = (PlateCard)uc;
							PGE.platecard_asp_offset = Convert.ToDouble(plateCard.m_PlateProperties.strPlateASPOffset);
							PGE.platecard_dbwc = Convert.ToDouble(plateCard.m_PlateProperties.strPlateDbwc);
							PGE.platecard_dbwc2 = Convert.ToDouble(plateCard.m_PlateProperties.strPlateDbwc2);
							PGE.platecard_depth = Convert.ToDouble(plateCard.m_PlateProperties.strPlateDepth);
							PGE.platecard_format = Convert.ToInt32(plateCard.m_PlateProperties.strPlateType);
							if( ((PlateCard)uc).ColumnRb.Checked && PGE.platecard_format < 4 ) PGE.platecard_format += 3;
							PGE.platecard_height = Convert.ToDouble(plateCard.m_PlateProperties.strPlateHeight);
							PGE.platecard_max_volume = Convert.ToDouble(plateCard.m_PlateProperties.strPlateMaxVolume);
							PGE.platecard_name = plateCard.m_PlateProperties.strPlateName;
							PGE.platecard_offset = Convert.ToDouble(plateCard.m_PlateProperties.strPlateOffset);
							PGE.platecard_offset2 = Convert.ToDouble(plateCard.m_PlateProperties.strPlateOffset2);
							PGE.platecard_rows = plateCard.textBoxRows.Text;
							PGE.platecard_loBase = plateCard.m_PlateProperties.loBase;
							PGE.platecard_diameter = Convert.ToDouble(plateCard.m_PlateProperties.strPlateBottomWellDiameter);
							PGE.platecard_well_shape = plateCard.m_PlateProperties.strWellShape;
							m_loBase = PGE.platecard_loBase;
						}
						catch (Exception e)
						{
							e = e;
						}
					}
					else if (PGE.strCardName == "platecardrowsonly")
					{
						PlateCardRowsOnly rowsCard = (PlateCardRowsOnly)uc;
						PGE.platecardrowsonly_rows = rowsCard.textBoxRows.Text;
					}
					else if (PGE.strCardName == "aspiratecard")
					{
						try
						{
							aspirateCard aspCard = (aspirateCard)uc;
							PGE.aspiratecard_probe_height = Convert.ToDouble(aspCard.textBoxHeight.Text);
							PGE.aspiratecard_asp_offset = Convert.ToDouble(aspCard.textBoxASPOffset.Text);
							PGE.aspiratecard_time = Convert.ToInt32(aspCard.comboBoxTime.Text);
							PGE.aspiratecard_velocity = aspCard.comboBoxVelocity.SelectedIndex;
							PGE.aspiratecard_sweep = aspCard.checkBoxASPSweep.Checked;
						}
						catch (Exception e)
						{
							e = e;
						}
					}
					else if (PGE.strCardName == "dispensecard")
					{
						try
						{
							dispenseCard dispCard = (dispenseCard)uc;
							PGE.dispensecard_inlet = Convert.ToInt32(dispCard.comboBoxInlet.Text);
							PGE.dispensecard_volume = Convert.ToDouble(dispCard.textBoxVolume.Text);
							PGE.dispensecard_liquid_factor = Convert.ToDouble(dispCard.textBoxLiquidFactor.Text);
							PGE.dispensecard_liquid_name = dispCard.textBoxName.Text;
							PGE.dispensecard_disp_low = Convert.ToDouble(dispCard.textBoxPressure.Text);
						}
						catch (Exception e)
						{
							MessageBox.Show(e.Message);
							e = e;
						}
					}
					else if (PGE.strCardName == "soakcard")
					{
						soakCard sCard = (soakCard)uc;

						string strTime = sCard.comboBoxTime.Text;
						string strTimeClean = "";
						int nSecs = 0;
						
						if (strTime.EndsWith("s"))
						{
							strTimeClean = strTime.Replace("s", "");
							nSecs = Convert.ToInt32(strTimeClean);
						}
						else if (strTime.EndsWith("min"))
						{
							strTimeClean = strTime.Replace("min", "");
							nSecs = Convert.ToInt32(strTimeClean);
							nSecs *= 60;
						}
						else if (strTime.EndsWith("h"))
						{
							strTimeClean = strTime.Replace("h", "");
							nSecs = Convert.ToInt32(strTimeClean);
							nSecs *= 3600;
						}
						
						PGE.soakcard_time = nSecs;
					}
				}
			}
			bool bRepeatCard = false;
			for (int i = 0; i < RepeatCardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];

				if (PGE.uc == uc)
				{
					bRepeatCard = true;

					// save to xml data
					repeatCard rc = (repeatCard)uc;
					PGE.repeatcard_from = Convert.ToInt32(rc.comboBoxFrom.Text, 10);
					PGE.repeatcard_to = Convert.ToInt32(rc.comboBoxTo.Text, 10);
					PGE.repeatcard_repeats = Convert.ToInt32(rc.comboBoxRepeats.Text, 10);

					// check logic
					//if (PGE.repeatcard_to == rc.comboBoxTo.Items.Count)
					int nDelta = rc.comboBoxFrom.Items.Count - rc.comboBoxTo.Items.Count;
					if (PGE.repeatcard_to == (rc.comboBoxTo.Items.Count + nDelta))
					{
						PGE.repeatcard_to -= 1 ;
						rc.comboBoxTo.Text = PGE.repeatcard_to.ToString();
					}
					if (PGE.repeatcard_from - PGE.repeatcard_to < 1)
					{
						PGE.repeatcard_from = PGE.repeatcard_to + 1;
						rc.comboBoxFrom.Text = PGE.repeatcard_from.ToString();
					}
					// resize
					//rc.Height = 158;
					//int nCardPosition = Convert.ToInt32(rc.comboBoxTo.Text, 10);
					//int nRepeatCardMiddle = rc.Height / 2;

					//int nRepeatCardMiddlePos = topOffset;
					//for( int j=0; j<=nCardPosition; j++ )
					//{
					//	nRepeatCardMiddlePos += ((UserControl)((ProgramGUIElement)CardArray[j]).uc).Height + emptySmallSizeCardHeight;
					//}
					//nRepeatCardMiddlePos -= nRepeatCardMiddle;
					//nRepeatCardMiddlePos += programPanel.AutoScrollPosition.Y;
//---------------
					ProgramGUIElement PGEStart = (ProgramGUIElement)CardArray[PGE.repeatcard_to];
					ProgramGUIElement PGEEnd = (ProgramGUIElement)CardArray[PGE.repeatcard_from];
					int pos = PGEStart.uc.Bottom - (PGEStart.uc.Height/2);
					int height2 = PGEEnd.uc.Bottom - PGEStart.uc.Bottom;
					rc.Top = pos;
					rc.Height = height2;

//----------------
//					rc.Top = nRepeatCardMiddlePos;
//					int delta = PGE.repeatcard_from - PGE.repeatcard_to;
//					if (delta > 1)
//					{
//						PlateCard pc = (PlateCard)(((ProgramGUIElement)CardArray[0]).uc);
//						delta -= 1;
//						int height = 0;
//						for( int j=PGE.repeatcard_to+1; j<PGE.repeatcard_from; j++ )
//						{
//							ProgramGUIElement PGE2 = (ProgramGUIElement)CardArray[j];
//							if( PGE2.uc is PlateCardRowsOnly )
//							{
//								PlateCardRowsOnly pcro = (PlateCardRowsOnly)PGE2.uc;
//								int type = pcro.GetPlateType();
//								if( type > 3 && pc.RowRb.Checked )
//								{
//									height -= rowColDiff;
//								}
//								else if( type < 4 && pc.ColumnRb.Checked )
//								{
//									height += rowColDiff;
//								}
//							}
//							height += ((UserControl)((ProgramGUIElement)CardArray[j]).uc).Height + emptySmallSizeCardHeight;
//						}
//						rc.Height += height;
//					}
				}

				// update all repeatcard values
				if (!bRepeatCard && bProgramCard)
				{
					repeatCard rcard = (repeatCard)PGE.uc;

					// hack to fix repeats thingy reported in bug
					// it works :)
					if( rcard == null ) continue;
					
					rcard.comboBoxTo.Items.Clear();
					for (int ic = 0; ic < CardArray.Count - 2; ic++)
					{
						rcard.comboBoxTo.Items.Add((ic + 1).ToString());						
					}
					rcard.comboBoxTo.Text = PGE.repeatcard_to.ToString();

					rcard.comboBoxFrom.Items.Clear();
					for (int ic = 0; ic < CardArray.Count - 1; ic++)
					{
						rcard.comboBoxFrom.Items.Add((ic + 1).ToString());
					}
					rcard.comboBoxFrom.Text = PGE.repeatcard_from.ToString();
				}
			}
			//programPanel.Show();
		}

		private void listView_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
		{
			if( ProgramError( e.Item.ToString(), "Program full. Program card can not be added." ) ) return;

			DoDragDrop(e.Item.ToString(), DragDropEffects.All);
		}

		private bool ProgramError( string strKindOfCard, string errorMsg )
		{
			ProgramGUIElement PGETemp = (ProgramGUIElement)CardArray[0];
			PlateCard plateCard = (PlateCard)PGETemp.uc;
			
			// check if plate is selected
			if (plateCard.textBoxPlateName.Text.Length < 1)
			{
				MessageBox.Show(this, "A plate must be selected first!", "Program", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return true;
			}

			// check if number of program steps is exceeded
			//int nSteps = 0;
			int nSteps = RepeatCardArray.Count;
			double offset = 0;
			bool offsetSet = false;

			if( strKindOfCard != null ) nSteps++; //add one for the dragging card

			for (int i = 0; i < CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
				//Trace.WriteLine("Count: " + nSteps + " | Card: " + PGE.strCardName);
				if (PGE.strCardName == "platecardrowsonly")
				{
					nSteps += 2;
				}
				else if( PGE.strCardName == "aspiratecard" )
				{
					if( !offsetSet || PGE.aspiratecard_asp_offset == offset )
					{
						offsetSet = true;
						nSteps++;
					}
					else
					{
						nSteps += 2;
					}
					offset = PGE.aspiratecard_asp_offset;
					
				}
				else
				{
					nSteps += 1;
				}

				bool bProgramFull = false;
				if (strKindOfCard == "ListViewItem: {Wells}")
				{
					if (nSteps > 48)
					{
						bProgramFull = true;
					}//ListViewItem: {Dispense}
				}
				else
				{
					if (nSteps > 49)
					{
						bProgramFull = true;
					}
				}

				if (bProgramFull)
				{
					MessageBox.Show(this, errorMsg, "Program", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return true;
				}
			}
			return false;
		}

		private void programForm_Load(object sender, System.EventArgs e)
		{
			double lf = 1;
			inlets[0] = new Inlet("", lf.ToString("F2"), "550");
			inlets[1] = new Inlet("", lf.ToString("F2"), "550");
			inlets[2] = new Inlet("", lf.ToString("F2"), "550");
			inlets[3] = new Inlet("", lf.ToString("F2"), "550");
			
			if (!m_bCreateNew)
			{
				mainForm mf = (mainForm)this.MdiParent;
				try
				{
					mf.m_xmlData.LoadProgram(CardArray, RepeatCardArray, m_strFileNameInternal, m_strProgramName, m_strUsername);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}

				for (int nCard = 0; nCard < CardArray.Count; nCard++)
				{
					ProgramGUIElement PGE = (ProgramGUIElement)CardArray[nCard];
					UserControl ProgramCard = null;
					if (PGE.strCardName == "platecard")
					{
						ProgramCard = new PlateCard();
						ProgramCard.Parent = programPanel;

						((PlateCard)ProgramCard).m_PlateProperties.strPlateASPOffset = PGE.platecard_asp_offset.ToString("F1");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateDbwc = PGE.platecard_dbwc.ToString("F3");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateDbwc2 = PGE.platecard_dbwc2.ToString("F3");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateDepth = PGE.platecard_depth.ToString("F2");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateHeight = PGE.platecard_height.ToString("F2");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateMaxVolume = PGE.platecard_max_volume.ToString("F1");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateName = PGE.platecard_name;
						((PlateCard)ProgramCard).m_PlateProperties.strPlateOffset = PGE.platecard_offset.ToString("F2");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateOffset2 = PGE.platecard_offset2.ToString("F2");
						((PlateCard)ProgramCard).m_PlateProperties.strPlateType = PGE.platecard_format.ToString();
						((PlateCard)ProgramCard).m_PlateProperties.loBase = PGE.platecard_loBase;
						((PlateCard)ProgramCard).m_PlateProperties.strPlateBottomWellDiameter = PGE.platecard_diameter.ToString("F2");
						((PlateCard)ProgramCard).m_PlateProperties.strWellShape = PGE.platecard_well_shape;
((PlateCard)ProgramCard).m_update = true;
						((PlateCard)ProgramCard).textBoxPlateName.Text = PGE.platecard_name;
						((PlateCard)ProgramCard).textBoxRows.Text = PGE.platecard_rows;
						((PlateCard)ProgramCard).AllowedToChangeWellType( PGE.allowedToChangeWellType );
						((PlateCard)ProgramCard).ShowButtons();

						//((PlateCard)ProgramCard).m_update = true;
						((PlateCard)ProgramCard).RowRb.Checked = PGE.platecard_format > 3 ? false : true;
						((PlateCard)ProgramCard).ColumnRb.Checked = PGE.platecard_format < 4 ? false : true;
						((PlateCard)ProgramCard).m_update = false;

						((PlateCard)ProgramCard).RedrawCurrentPlate(false);

					
						m_loBase = PGE.platecard_loBase;
					}
					else if (PGE.strCardName == "platecardrowsonly")
					{
						ProgramCard = new PlateCardRowsOnly();
						ProgramCard.Parent = programPanel;

						((PlateCardRowsOnly)ProgramCard).textBoxRows.Text = PGE.platecardrowsonly_rows;
						((PlateCardRowsOnly)ProgramCard).RedrawCurrentPlate(false);

						((PlateCardRowsOnly)ProgramCard).labelProgramStep.Text = nCard.ToString();
					}
					else if (PGE.strCardName == "aspiratecard")
					{
						ProgramCard = new aspirateCard();
						ProgramCard.Parent = programPanel;

						((aspirateCard)ProgramCard).comboBoxVelocity.SelectedIndex = PGE.aspiratecard_velocity;
						((aspirateCard)ProgramCard).comboBoxTime.Text = PGE.aspiratecard_time.ToString();
						((aspirateCard)ProgramCard).textBoxHeight.Text = PGE.aspiratecard_probe_height.ToString("F1");
						((aspirateCard)ProgramCard).textBoxASPOffset.Text = PGE.aspiratecard_asp_offset.ToString("F1");
						((aspirateCard)ProgramCard).checkBoxASPSweep.Checked = PGE.aspiratecard_sweep;

						((aspirateCard)ProgramCard).labelProgramStep.Text = nCard.ToString();
					}
					else if (PGE.strCardName == "dispensecard")
					{
						ProgramCard = new dispenseCard();
						ProgramCard.Parent = programPanel;

						inlets[PGE.dispensecard_inlet-1] = new Inlet(PGE.dispensecard_liquid_name, PGE.dispensecard_liquid_factor.ToString("F2"), PGE.dispensecard_disp_low.ToString());
						((dispenseCard)ProgramCard).textBoxName.Text = PGE.dispensecard_liquid_name;
						((dispenseCard)ProgramCard).textBoxLiquidFactor.Text = PGE.dispensecard_liquid_factor.ToString("F2");
						((dispenseCard)ProgramCard).textBoxPressure.Text = Convert.ToString(PGE.dispensecard_disp_low.ToString());
						((dispenseCard)ProgramCard).textBoxVolume.Text = PGE.dispensecard_volume.ToString("F1");
						((dispenseCard)ProgramCard).comboBoxInlet.Text = PGE.dispensecard_inlet.ToString();
						((dispenseCard)ProgramCard).labelProgramStep.Text = nCard.ToString();
					}
					else if (PGE.strCardName == "soakcard")
					{
						ProgramCard = new soakCard();
						ProgramCard.Parent = programPanel;

						int nTime = PGE.soakcard_time;
						string strTime = "";
						if (nTime < 60)
						{
							strTime = nTime.ToString() + " s";
						}
						else if (nTime >= 60 && nTime < 3600)
						{
							nTime /= 60;
							strTime = nTime.ToString() + " min";
						}
						else if (nTime >= 3600)
						{
							nTime /= 3600;
							strTime = nTime.ToString() + " h";
						}

						((soakCard)ProgramCard).comboBoxTime.Text = strTime;

						((soakCard)ProgramCard).labelProgramStep.Text = nCard.ToString();
					}

					ProgramCard.Left = leftOffset + programPanel.AutoScrollPosition.X;
					if( EmptyCardArray.Count > 0 )
					{
						ProgramCard.Top = ((UserControl)EmptyCardArray[nCard-1]).Top + emptySmallSizeCardHeight;
					}
					else
					{
						ProgramCard.Top = topOffset;
					}
					
					PGE.uc = ProgramCard;

					// create empty card placeholders
					if (nCard == CardArray.Count - 1)
					//if (nCard == CardArray.Count )
					{
						EmptyFullSizeCard ecs = new EmptyFullSizeCard();
						ecs.Left = leftOffset + programPanel.AutoScrollPosition.X;
						int top = topOffset - emptySmallSizeCardHeight;
						for( int i=0; i<=nCard; i++ )
						{
							top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
						}
						ecs.Top = top;
						ecs.Parent = programPanel;
						EmptyCardArray.Add(ecs);
					}
					else
					{
						EmptySmallSizeCard ecf = new EmptySmallSizeCard();
						ecf.Left = leftOffset + programPanel.AutoScrollPosition.X;
						int top = topOffset - emptySmallSizeCardHeight;
						for( int i=0; i<=nCard; i++ )
						{
							top += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
						}
						ecf.Top = top;
						ecf.Parent = programPanel;
						EmptyCardArray.Add(ecf);
					}					
				}

				for (int nCard = 0; nCard < RepeatCardArray.Count; nCard++)
				{
					ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[nCard];

					repeatCard rc = new repeatCard();

					// update card values
					rc.comboBoxFrom.Items.Clear();
					rc.comboBoxTo.Items.Clear();
					for (int i = 0; i < CardArray.Count - 1; i++)
					{
						rc.comboBoxFrom.Items.Add((i + 1).ToString());
						rc.comboBoxTo.Items.Add((i + 1).ToString());
					}

					// position it
					rc.Parent = programPanel;
					rc.Left = 460;
					rc.Height = 158;
					int nCardPosition = PGE.repeatcard_to;
					int nRepeatCardMiddle = rc.Height / 2;

					int nRepeatCardMiddlePos = topOffset;
					for( int j=0; j<=nCardPosition; j++ )
					{
						nRepeatCardMiddlePos += ((UserControl)((ProgramGUIElement)CardArray[j]).uc).Height + emptySmallSizeCardHeight;
					}
					nRepeatCardMiddlePos -= nRepeatCardMiddle;
					nRepeatCardMiddlePos += programPanel.AutoScrollPosition.Y;

//					int nRepeatCardMiddlePos = nCardPosition * (plateHeight + emptySmallSizeCardHeight) + topOffset;
//					nRepeatCardMiddlePos += (plateHeight + emptySmallSizeCardHeight) - nRepeatCardMiddle;
//					nRepeatCardMiddlePos += programPanel.AutoScrollPosition.Y;
					rc.Top = nRepeatCardMiddlePos;
					int delta = PGE.repeatcard_from - PGE.repeatcard_to;
					if (delta > 1)
					{
						delta -= 1;
						int height = 0;
						for( int j=PGE.repeatcard_to+1; j<PGE.repeatcard_from; j++ )
						{
							height += ((UserControl)((ProgramGUIElement)CardArray[j]).uc).Height + emptySmallSizeCardHeight;
						}
						rc.Height += height;
						//rc.Height += (delta * (plateHeight + emptySmallSizeCardHeight));
					}

					rc.comboBoxTo.Text = PGE.repeatcard_to.ToString();
					rc.comboBoxFrom.Text = PGE.repeatcard_from.ToString();
					rc.comboBoxRepeats.Text = PGE.repeatcard_repeats.ToString();

					PGE.uc = rc;
				}
			}
			else
			{
				// handled in constructor
			}

			string strLabel = "";
			string strLabelWindow = "";
			string strFileUser = "";
			if (m_strUsername != "____BNX1536_")
			{
				strLabel = "Program: " + m_strProgramName;
				strLabelWindow = "Program: " + m_strProgramName + " (File: " + m_strFileNameInternal + ", User: " + m_strUsername + ")";
				strFileUser =  "File: " + m_strFileNameInternal + ", User: " + m_strUsername;
			}
			else
			{
				strLabel = "Program: " + m_strProgramName;
				strLabelWindow = "Program: " + m_strProgramName + " (Current BNX1536 Program)";
				strFileUser = "(Current BNX1536 Program)";
			}
			Text = strLabelWindow;
			labelProgram.Text = strLabel;
			labelFileUser.Text = strFileUser;
			Tag = false;

			RepositionRepeats();
		}

		int FindCardPos( double point )
		{
			for( int i=0; i<CardArray.Count; i++ )
			{

				ProgramGUIElement pge = (ProgramGUIElement)CardArray[i];
				if( ((ProgramGUIElement)CardArray[i]).uc.Bottom > point )
				{
					return i;
				}
			}
			return 0;
		}

		// for repeat cards
		private void programPanel_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			panelRepeatCardGhost.Hide();
			Point point = programPanel.PointToClient(Cursor.Position);

			// calculate card position			
			//double a = point.Y - topOffset - programPanel.AutoScrollPosition.Y;

			int nCardPosition = FindCardPos( point.Y );
//			if( nCardPosition == 0 ) nCardPosition++;

			//double b = plateHeight + emptySmallSizeCardHeight;
			//int nCardPosition = (int)a / (int)b;
			//int nCardCheckPosition = Convert.ToInt32( Math.Ceiling( a/b ) );
			//int nCardCheckPosition = Convert.ToInt32( Math.Ceiling( nCardPosition ) );
			//int nCardCheckPosition = nCardPosition;

			//mik
			//not allowed to let two loops use the same program step
			//this is ugly as hell, but I don't want to spend time
			//researching how to cancel the drop

			foreach( ProgramGUIElement elem in RepeatCardArray )
			{
				if( elem.repeatcard_from == nCardPosition || elem.repeatcard_to == nCardPosition+1
//					|| elem.repeatcard_from == nCardCheckPosition || elem.repeatcard_to == nCardCheckPosition
					)
				{
					//rc.Dispose();
					MessageBox.Show( this, "You cannot have two repeat functions working on the same program card.", "Invalid operation", MessageBoxButtons.OK, MessageBoxIcon.Warning  );
					return;
				}
			}

			//

			repeatCard rc = new repeatCard();

			// calculate card position
//			int nCardPosition = (point.Y - topOffset - programPanel.AutoScrollPosition.Y) / (plateHeight + emptySmallSizeCardHeight);
			int nRepeatCardMiddle = rc.Height / 2;

			int nRepeatCardMiddlePos = topOffset;
			for( int j=0; j<=nCardPosition; j++ )
			{
				nRepeatCardMiddlePos += ((ProgramGUIElement)CardArray[j]).uc.Height + emptySmallSizeCardHeight;
			}
			nRepeatCardMiddlePos -= nRepeatCardMiddle;
			nRepeatCardMiddlePos += programPanel.AutoScrollPosition.Y;

//			int nRepeatCardMiddlePos = nCardPosition * (plateHeight + emptySmallSizeCardHeight) + topOffset;
//			nRepeatCardMiddlePos += (plateHeight + emptySmallSizeCardHeight) - nRepeatCardMiddle;
//			nRepeatCardMiddlePos += programPanel.AutoScrollPosition.Y;
			rc.Top = nRepeatCardMiddlePos;
			rc.Left = 460 + programPanel.AutoScrollPosition.X;
			rc.Parent = programPanel;

			// update card values
			rc.comboBoxFrom.Items.Clear();
			rc.comboBoxTo.Items.Clear();
			for (int i = 0; i < CardArray.Count - 1; i++)
			{
				rc.comboBoxFrom.Items.Add((i + 1).ToString());
				rc.comboBoxTo.Items.Add((i + 1).ToString());
			}

			rc.comboBoxTo.Text = nCardPosition.ToString();
			rc.comboBoxFrom.Text = (nCardPosition + 1).ToString();
			rc.comboBoxRepeats.Text = (1).ToString();

			// add to card array
			ProgramGUIElement PGE = new ProgramGUIElement();
			PGE.strCardName = "repeatcard";
			PGE.uc = rc;
			RepeatCardArray.Add(PGE);
			CardChanged(rc);
			RepositionRepeats();
		}

		private void programPanel_DragLeave(object sender, System.EventArgs e)
		{
			panelRepeatCardGhost.Hide();
		}

		private void programPanel_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.Text))
			{
				if (e.Data.GetData(DataFormats.Text).ToString().EndsWith("{Repeat}"))
				{
					int nOffsetX = 420 + programPanel.AutoScrollPosition.X;
					int nOffsetX2 = 600 + + programPanel.AutoScrollPosition.X;
					int nOffsetY = 240 + programPanel.AutoScrollPosition.Y;
					//int nOffsetY2 = CardArray.Count * (plateHeight + emptySmallSizeCardHeight) - emptySmallSizeCardHeight + topOffset + programPanel.AutoScrollPosition.Y;

					int nOffsetY2 = topOffset - emptySmallSizeCardHeight + programPanel.AutoScrollPosition.Y;
					for( int i=0; i<CardArray.Count; i++ )
					{
						nOffsetY2 += ((UserControl)((ProgramGUIElement)CardArray[i]).uc).Height + emptySmallSizeCardHeight;
					}


					Point point = programPanel.PointToClient(Cursor.Position);
					if (point.X > nOffsetX && point.Y > nOffsetY && point.X < nOffsetX2 && point.Y < nOffsetY2)
					{
						//double a = point.Y - topOffset - programPanel.AutoScrollPosition.Y;
						int nCardPosition = FindCardPos( point.Y );
						//int nCardPosition = (point.Y - topOffset - programPanel.AutoScrollPosition.Y) / (plateHeight + emptySmallSizeCardHeight);
						if (CardArray.Count > 2 && nCardPosition < CardArray.Count - 1 && nCardPosition > 0)
						{
							bool bOverlap = false;
							for (int i = 0; i < RepeatCardArray.Count; i++)
							{
								ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];
								if (nCardPosition <= PGE.repeatcard_from && nCardPosition >= PGE.repeatcard_to)
								{
									bOverlap = true;
									break;
								}
							}

							ProgramGUIElement selected = CardArray[nCardPosition] as ProgramGUIElement;
							if( bOverlap || point.Y < (selected.uc.Bottom - selected.uc.Height/2 ) )
							//if (bOverlap || point.Y < topOffset + emptySmallSizeCardHeight + plateHeight + (plateHeight / 2) - programPanel.AutoScrollPosition.Y)
							//if (bOverlap || point.Y < topOffset + emptySmallSizeCardHeight + plateHeight + (plateHeight / 2) + programPanel.AutoScrollPosition.Y)
							{
								//string strDebug1 = "point.Y = " + point.Y.ToString();
								//Debug.WriteLine(strDebug1);

								//int n = topOffset + emptySmallSizeCardHeight + plateHeight + (plateHeight / 2) - programPanel.AutoScrollPosition.Y;
								//string strDebug2 = "topOffset + emptySmallSizeCardHeight + plateHeight + (plateHeight / 2) - programPanel.AutoScrollPosition.Y = " + n.ToString();
								//Debug.WriteLine(strDebug2);

								e.Effect = DragDropEffects.None;
								panelRepeatCardGhost.Hide();
							}
							else
							{
								//safe place to drop the icon
								e.Effect = DragDropEffects.Copy;
								panelRepeatCardGhost.Show();
								panelRepeatCardGhost.Left = 460 + programPanel.AutoScrollPosition.X;
								panelRepeatCardGhost.Top = point.Y;								
							}
						}
						else
						{
							e.Effect = DragDropEffects.None;
							panelRepeatCardGhost.Hide();
						}
					}
					else
					{
						e.Effect = DragDropEffects.None;
						panelRepeatCardGhost.Hide();
					}
				}
				else
				{
					e.Effect = DragDropEffects.None;
					panelRepeatCardGhost.Hide();
				}
			}
			else
			{
				e.Effect = DragDropEffects.None;
				panelRepeatCardGhost.Hide();
			}
		}

		private void programForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;
			if (mf.m_User.Username != m_strUsername)
			{
				return;
			}
			
			if (mf.m_User.UserLevel == 1)
			{
				return;
			}

			if ((bool)Tag && m_strUsername != "____BNX1536_")
			{
				DialogResult DR = MessageBox.Show("Save program " + "\"" + m_strProgramName + "\" (File: " + "\"" + m_strFileNameInternal + "\"" + ")?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

				if (DR == DialogResult.Yes)
				{
					Save();
				}
				else if (DR == DialogResult.Cancel)
				{
					e.Cancel = true;	
				}
			}
		}

		private void programForm_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			
		}

		private void programPanel_Click(object sender, System.EventArgs e)
		{
			programPanel.Focus();
		}

		private void programPanel_DoubleClick(object sender, System.EventArgs e)
		{
			programPanel.Focus();
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.programPanel.Hide();
			MinMax();
			this.programPanel.Show();
		}

		public bool Minimized
		{
			get
			{
				return !m_maximizeCards;
			}
		}

		public void MinMax()
		{			
			m_maximizeCards = !m_maximizeCards;
			if( m_maximizeCards )
			{
				minMaxBtn.Text = "Minimize Cards";
				//				for( int i=0; i<RepeatCardArray.Count; i++ )
				//				{
				//					ProgramGUIElement PGER = (ProgramGUIElement)RepeatCardArray[i];
				//					UserControl uc = (repeatCard)PGER.uc;
				//					MethodInfo mi = uc.GetType().GetMethod( "DrawBigCard", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic );
				//					if( mi != null )
				//					{
				//						mi.Invoke( uc, null );
				//					}
				//				}

				for( int i=1; i<CardArray.Count; i++ )
				{
					ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
					UserControl uc = PGE.uc;
					MethodInfo mi = uc.GetType().GetMethod( "DrawBigCard", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic );
					if( mi != null )
					{
						mi.Invoke( uc, null );
					}
				}

				RepositionRepeats();

			}
			else
			{
				minMaxBtn.Text = "Maximize Cards";

				for( int i=1; i<CardArray.Count; i++ )
				{
					ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
					UserControl uc = PGE.uc;
					MethodInfo mi = uc.GetType().GetMethod( "DrawSmallCard", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic );
					if( mi != null )
					{
						mi.Invoke( uc, null );
					}
				}

				RepositionRepeats();
			}			
		}
	}
}
