using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using VB = Microsoft.VisualBasic;

namespace AQ3
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class mainForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.StatusBar statusBar1;
		private System.Windows.Forms.ImageList imageListToolbar;
		private System.Windows.Forms.MainMenu mainMenu;
		private System.Windows.Forms.ImageList imageListTreeView;
		private System.Windows.Forms.ContextMenu contextMenuTreeView;
		private System.Windows.Forms.MdiClient mdiClient;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewFiles;
		private System.Windows.Forms.MenuItem menuItem17;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFilesNew;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.MenuItem mainMenuItemCascade;
		private System.Windows.Forms.MenuItem mainMenuItemAbout;
		private System.Windows.Forms.MenuItem mainMenuItemWindow;
		private System.Windows.Forms.MenuItem mainMenuItemTile;
		private System.Windows.Forms.MenuItem mainMenuItemHelp;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewProgram;
		private System.Windows.Forms.MenuItem contextMenuTreeViewProgramOpen;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem contextMenuTreeViewProgramDelete;
		public System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ToolBar toolBarMain;
		
		private Point m_pointContextMenu;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewFile;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFileNewProgram;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFileDelete;
		private System.Windows.Forms.MenuItem mainMenuItemFile;
		private System.Windows.Forms.MenuItem mainMenuItemFileExit;
		private System.Windows.Forms.MenuItem menuItem13;
		private System.Windows.Forms.MenuItem menuItem12;
		private System.Windows.Forms.MenuItem menuItem19;
		private System.Windows.Forms.ToolBarButton toolBarButtonNew;
		private System.Windows.Forms.ToolBarButton toolBarButtonSave;
		private System.Windows.Forms.ToolBarButton toolBarButtonSaveAllReal;
		private System.Windows.Forms.ToolBarButton toolBarButtonUpload;
		private System.Windows.Forms.ToolBarButton toolBarButtonDownload;
		private System.Windows.Forms.ToolBarButton toolBarButtonSeparatorOne;
		private System.Windows.Forms.ContextMenu contextMenuToolBarNew;
		private System.Windows.Forms.MenuItem mainMenuItemFileNew;
		private System.Windows.Forms.MenuItem mainMenuItemFileNewFile;
		private System.Windows.Forms.MenuItem mainMenuItemFileNewProgram;
		private System.Windows.Forms.MenuItem mainMenuItemFileNewPlate;
		private System.Windows.Forms.MenuItem mainMenuItemFileNewLiquid;
		private System.Windows.Forms.MenuItem mainMenuItemFileNewUser;
		private System.Windows.Forms.MenuItem mainMenuItemFileClose;
		private System.Windows.Forms.MenuItem mainMenuItemSaveActive;
		private System.Windows.Forms.MenuItem mainMenuItemFileSaveAll;
		private System.Windows.Forms.MenuItem mainMenuItemFileUploadFile;
		private System.Windows.Forms.MenuItem mainMenuItemFileDownloadFile;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewProgram;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewFile;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewPlate;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewLiquid;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewUser;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewPlateGroup;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewPlates;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewPlate;
		private System.Windows.Forms.MenuItem contextMenuTreeViewPlateOpen;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem contextMenuTreeViewPlateDelete;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewLiquids;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewLiquids;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewLiquid;
		private System.Windows.Forms.MenuItem contextMenuTreeViewLiquidOpen;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem contextMenuTreeViewLiquidDelete;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewUsers;
		private System.Windows.Forms.MenuItem contextMenuToolbarNewUsers;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewUser;
		private System.Windows.Forms.MenuItem contextMenuTreeViewUserOpen;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem contextMenuTreeViewUserDelete;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewConfig;
		private System.Windows.Forms.MenuItem contextMenuTreeViewConfigOpen;
		
		// 
		public XmlData m_xmlData;
		public CUser m_User;
		public string m_printerName;
		private int m_nNewProgramNumber = 1;
		private int m_nNewPlateNumber = 1;
		private int m_nNewLiquidNumber = 1;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewFileProtected;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFileProtectedCopy;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewProgramProtected;
		private System.Windows.Forms.MenuItem contextMenuTreeViewProgramProtectedCopy;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFilesPaste;
		private System.Windows.Forms.MenuItem menuItem6;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFilePaste;
		private System.Windows.Forms.MenuItem menuItem7;
		private System.Windows.Forms.MenuItem contextMenuTreeViewProgramCopy;
		private System.Windows.Forms.MenuItem contextMenuTreeViewFileRename;
		private System.Windows.Forms.MenuItem contextMenuTreeViewProgramRename;
		private int m_nNewUserNumber = 1;
		private string strOldFileName = "";
		private string strOldProgramName = "";
		private bool bFileNameEdited = false;
		private System.Windows.Forms.ContextMenu contextMenuTreeViewCurrent;
		private System.Windows.Forms.MenuItem contextMenuTreeViewCurrentDelete;
		private System.Drawing.Printing.PrintDocument printDocument;
		private System.Windows.Forms.ToolBarButton toolBarButtonPrint;
		private System.Windows.Forms.ToolBarButton toolBarButtonSeparatorTwo;
		private System.Windows.Forms.PrintDialog printDialog;
		private System.Windows.Forms.PrintPreviewDialog printPreviewDialog;
		private bool bProgramNameEdited = false;
		private static CultureInfo m_machineCulture;
		private byte m_deviceCode = 0;

		public byte DeviceCode
		{
			get
			{
				if( m_deviceCode == 0 )
				{
					try
					{
						m_deviceCode = Utilities.GetDeviceCode( m_xmlData.GetCommPort() );
					}
					catch( Exception )
					{
						MessageBox.Show( "Could not contact BNX1536", "BNX1536 Communication", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}
				return m_deviceCode;
			}
		}

		private void OpenProgramForm(string strFileNameInternal, string strProgramName, string strUsername, bool bCreateNew)
		{
			if (bCreateNew)
			{
				string strNewProgramName;
				string strNewFileName;

				if (strProgramName.Length > 0)
				{
					strNewProgramName = strProgramName;
				}
				else
				{
					strNewProgramName = "New Program ";
					strNewProgramName += m_nNewProgramNumber.ToString();
				}
				if (strFileNameInternal.Length > 0)
				{
					strNewFileName = strFileNameInternal;
				}
				else
				{
					strNewFileName = "New File";
				}

				programForm newMDIChild = new programForm(strNewFileName, strNewProgramName, strUsername, bCreateNew);
				newMDIChild.MdiParent = this;
				newMDIChild.Show();
				m_nNewProgramNumber++;
			}
			else
			{
				bool bAlreadyOpen = false;
				int iForm = 0;
				
				for (iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
				{
					string strFormName = "Program: " + strProgramName + " (File: " + strFileNameInternal + ", User: " + strUsername + ")";
					strFormName = strFormName.Replace( "File: ____BNX1536_, User: ____BNX1536_", "Current BNX1536 Program" );
					string mdiProgramName = mdiClient.MdiChildren[iForm].Text.ToString().Replace("*", "");
					if (strFormName == mdiProgramName )
					{
						bAlreadyOpen = true;
						break;
					}
				}

				if (bAlreadyOpen)
				{
					if( mdiClient.MdiChildren[iForm].WindowState == FormWindowState.Minimized )
					{
						mdiClient.MdiChildren[iForm].WindowState = FormWindowState.Normal;
					}
					mdiClient.MdiChildren[iForm].Activate();
				}
				else
				{
					programForm newMDIChild = new programForm(strFileNameInternal, strProgramName, strUsername, bCreateNew);
					newMDIChild.MdiParent = this;
					newMDIChild.Show();
				}
			}			
		}

		private void OpenPlateForm(string strPlateName, string strPlateType, bool bCreateNew)
		{
			string strNewPlateName = "New Plate";

			if (bCreateNew)
			{
				strNewPlateName += " ";
				strNewPlateName += m_nNewPlateNumber.ToString();
				plateForm newMDIChild = new plateForm(strNewPlateName, strPlateType, bCreateNew);
				newMDIChild.MdiParent = this;
				newMDIChild.Show();
				m_nNewPlateNumber++;
			}
			else
			{
				bool bAlreadyOpen = false;
				int iForm = 0;
				
				for (iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
				{
					string strFormat = "";
					if (strPlateType == "1")
					{
						strFormat = "96";
					}
					else if (strPlateType == "2")
					{
						strFormat = "384";
					}
					else if (strPlateType == "3")
					{
						strFormat = "1536";
					}
					string strFormName = "Plate: " + strPlateName + " (Format: " + strFormat + ")";

					if (strFormName == mdiClient.MdiChildren[iForm].Text.ToString().Replace("*", ""))
					{
						bAlreadyOpen = true;
						break;
					}
				}

				if (bAlreadyOpen)
				{
					mdiClient.MdiChildren[iForm].Activate();
				}
				else
				{
					plateForm newMDIChild = new plateForm(strPlateName, strPlateType, bCreateNew);
					newMDIChild.MdiParent = this;
					newMDIChild.Show();
				}
			}			
		}

		private void OpenLiquidForm(string strLiquidName, bool bCreateNew)
		{
			string strNewLiquidName = "New Liquid";

			if (bCreateNew)
			{
				strNewLiquidName += " ";
				strNewLiquidName += m_nNewLiquidNumber.ToString();
				liquidForm newMDIChild = new liquidForm(strNewLiquidName, bCreateNew);
				newMDIChild.MdiParent = this;
				newMDIChild.Show();
				m_nNewLiquidNumber++;
			}
			else
			{
				bool bAlreadyOpen = false;
				int iForm = 0;
				
				for (iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
				{
					string strFormName = "Liquid: " + strLiquidName;
					if (strFormName == mdiClient.MdiChildren[iForm].Text.ToString().Replace("*", ""))
					{
						bAlreadyOpen = true;
						break;
					}
				}

				if (bAlreadyOpen)
				{
					mdiClient.MdiChildren[iForm].Activate();
				}
				else
				{
					liquidForm newMDIChild = new liquidForm(strLiquidName, bCreateNew);
					newMDIChild.MdiParent = this;
					newMDIChild.Show();
				}
			}			
		}

		private void OpenUserForm(string strUserName, bool bCreateNew)
		{
			string strNewUserName = "New User";

			if (bCreateNew)
			{
				strNewUserName += " ";
				strNewUserName += m_nNewUserNumber.ToString();
				userForm newMDIChild = new userForm(strNewUserName, bCreateNew);
				newMDIChild.MdiParent = this;
				newMDIChild.Show();
				m_nNewUserNumber++;
			}
			else
			{
				bool bAlreadyOpen = false;
				int iForm = 0;
				
				for (iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
				{
					string strFormName = "User: " + strUserName;
					if (strFormName == mdiClient.MdiChildren[iForm].Text.ToString().Replace("*", ""))
					{
						bAlreadyOpen = true;
						break;
					}
				}

				if (bAlreadyOpen)
				{
					mdiClient.MdiChildren[iForm].Activate();
				}
				else
				{
					userForm newMDIChild = new userForm(strUserName, bCreateNew);
					newMDIChild.MdiParent = this;
					newMDIChild.Show();
				}
			}			
		}

		private void OpenConfigForm()
		{
			bool bAlreadyOpen = false;
			int iForm = 0;
				
			for (iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
			{
				if ("Communication" == mdiClient.MdiChildren[iForm].Text.ToString().Replace("*", ""))
				{
					bAlreadyOpen = true;
					break;
				}
			}

			if (bAlreadyOpen)
			{
				mdiClient.MdiChildren[iForm].Activate();
			}
			else
			{
				configForm newMDIChild = new configForm();
				newMDIChild.MdiParent = this;
				newMDIChild.Show();
			}			
		}

		private void CloseProgramForm(string strProgramName)
		{
			for (int iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
			{
				if (strProgramName == mdiClient.MdiChildren[iForm].Text.ToString())
				{
					mdiClient.MdiChildren[iForm].Close();
					break;
				}
			}
		}

		public mainForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mainForm));
            this.contextMenuTreeView = new System.Windows.Forms.ContextMenu();
            this.imageListTreeView = new System.Windows.Forms.ImageList(this.components);
            this.mainMenuItemCascade = new System.Windows.Forms.MenuItem();
            this.mainMenuItemAbout = new System.Windows.Forms.MenuItem();
            this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.mainMenuItemFile = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileNew = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileNewProgram = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileNewFile = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileNewPlate = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileNewLiquid = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileNewUser = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileClose = new System.Windows.Forms.MenuItem();
            this.menuItem13 = new System.Windows.Forms.MenuItem();
            this.mainMenuItemSaveActive = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileSaveAll = new System.Windows.Forms.MenuItem();
            this.menuItem19 = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileUploadFile = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileDownloadFile = new System.Windows.Forms.MenuItem();
            this.menuItem12 = new System.Windows.Forms.MenuItem();
            this.mainMenuItemFileExit = new System.Windows.Forms.MenuItem();
            this.mainMenuItemWindow = new System.Windows.Forms.MenuItem();
            this.mainMenuItemTile = new System.Windows.Forms.MenuItem();
            this.mainMenuItemHelp = new System.Windows.Forms.MenuItem();
            this.imageListToolbar = new System.Windows.Forms.ImageList(this.components);
            this.mdiClient = new System.Windows.Forms.MdiClient();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.contextMenuTreeViewFiles = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewFilesNew = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewFilesPaste = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewFile = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewFileNewProgram = new System.Windows.Forms.MenuItem();
            this.menuItem17 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewFileDelete = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewFileRename = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewFilePaste = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewProgram = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewProgramOpen = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewProgramDelete = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewProgramRename = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewProgramCopy = new System.Windows.Forms.MenuItem();
            this.toolBarMain = new System.Windows.Forms.ToolBar();
            this.toolBarButtonNew = new System.Windows.Forms.ToolBarButton();
            this.contextMenuToolBarNew = new System.Windows.Forms.ContextMenu();
            this.contextMenuToolbarNewProgram = new System.Windows.Forms.MenuItem();
            this.contextMenuToolbarNewFile = new System.Windows.Forms.MenuItem();
            this.contextMenuToolbarNewPlate = new System.Windows.Forms.MenuItem();
            this.contextMenuToolbarNewLiquid = new System.Windows.Forms.MenuItem();
            this.contextMenuToolbarNewUser = new System.Windows.Forms.MenuItem();
            this.toolBarButtonSave = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonSaveAllReal = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonSeparatorOne = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonPrint = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonSeparatorTwo = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonUpload = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonDownload = new System.Windows.Forms.ToolBarButton();
            this.treeView = new System.Windows.Forms.TreeView();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.contextMenuTreeViewPlateGroup = new System.Windows.Forms.ContextMenu();
            this.contextMenuToolbarNewPlates = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewPlate = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewPlateOpen = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewPlateDelete = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewLiquids = new System.Windows.Forms.ContextMenu();
            this.contextMenuToolbarNewLiquids = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewLiquid = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewLiquidOpen = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewLiquidDelete = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewUsers = new System.Windows.Forms.ContextMenu();
            this.contextMenuToolbarNewUsers = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewUser = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewUserOpen = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewUserDelete = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewConfig = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewConfigOpen = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewFileProtected = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewFileProtectedCopy = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewProgramProtected = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewProgramProtectedCopy = new System.Windows.Forms.MenuItem();
            this.contextMenuTreeViewCurrent = new System.Windows.Forms.ContextMenu();
            this.contextMenuTreeViewCurrentDelete = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // contextMenuTreeView
            // 
            this.contextMenuTreeView.Popup += new System.EventHandler(this.contextMenuTreeView_Popup);
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
            // mainMenuItemCascade
            // 
            this.mainMenuItemCascade.Index = 1;
            this.mainMenuItemCascade.Text = "&Cascade";
            this.mainMenuItemCascade.Click += new System.EventHandler(this.menuItemCascade_Click);
            // 
            // mainMenuItemAbout
            // 
            this.mainMenuItemAbout.Index = 0;
            this.mainMenuItemAbout.Text = "&About BNX1536...";
            this.mainMenuItemAbout.Click += new System.EventHandler(this.mainMenuItemAbout_Click);
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mainMenuItemFile,
            this.mainMenuItemWindow,
            this.mainMenuItemHelp});
            // 
            // mainMenuItemFile
            // 
            this.mainMenuItemFile.Index = 0;
            this.mainMenuItemFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mainMenuItemFileNew,
            this.mainMenuItemFileClose,
            this.menuItem13,
            this.mainMenuItemSaveActive,
            this.mainMenuItemFileSaveAll,
            this.menuItem19,
            this.mainMenuItemFileUploadFile,
            this.mainMenuItemFileDownloadFile,
            this.menuItem12,
            this.mainMenuItemFileExit});
            this.mainMenuItemFile.Text = "&File";
            // 
            // mainMenuItemFileNew
            // 
            this.mainMenuItemFileNew.Index = 0;
            this.mainMenuItemFileNew.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mainMenuItemFileNewProgram,
            this.mainMenuItemFileNewFile,
            this.mainMenuItemFileNewPlate,
            this.mainMenuItemFileNewLiquid,
            this.mainMenuItemFileNewUser});
            this.mainMenuItemFileNew.Text = "&New";
            // 
            // mainMenuItemFileNewProgram
            // 
            this.mainMenuItemFileNewProgram.Index = 0;
            this.mainMenuItemFileNewProgram.Text = "&Program...";
            this.mainMenuItemFileNewProgram.Click += new System.EventHandler(this.mainMenuItemFileNewProgram_Click);
            // 
            // mainMenuItemFileNewFile
            // 
            this.mainMenuItemFileNewFile.Index = 1;
            this.mainMenuItemFileNewFile.Text = "&File...";
            this.mainMenuItemFileNewFile.Click += new System.EventHandler(this.mainMenuItemFileNewFile_Click);
            // 
            // mainMenuItemFileNewPlate
            // 
            this.mainMenuItemFileNewPlate.Index = 2;
            this.mainMenuItemFileNewPlate.Text = "Pl&ate...";
            this.mainMenuItemFileNewPlate.Click += new System.EventHandler(this.mainMenuItemFileNewPlate_Click);
            // 
            // mainMenuItemFileNewLiquid
            // 
            this.mainMenuItemFileNewLiquid.Index = 3;
            this.mainMenuItemFileNewLiquid.Text = "&Liquid...";
            this.mainMenuItemFileNewLiquid.Click += new System.EventHandler(this.mainMenuItemFileNewLiquid_Click);
            // 
            // mainMenuItemFileNewUser
            // 
            this.mainMenuItemFileNewUser.Index = 4;
            this.mainMenuItemFileNewUser.Text = "&User...";
            this.mainMenuItemFileNewUser.Click += new System.EventHandler(this.mainMenuItemFileNewUser_Click);
            // 
            // mainMenuItemFileClose
            // 
            this.mainMenuItemFileClose.Index = 1;
            this.mainMenuItemFileClose.Text = "&Close";
            this.mainMenuItemFileClose.Click += new System.EventHandler(this.mainMenuItemFileClose_Click);
            // 
            // menuItem13
            // 
            this.menuItem13.Index = 2;
            this.menuItem13.Text = "-";
            // 
            // mainMenuItemSaveActive
            // 
            this.mainMenuItemSaveActive.Index = 3;
            this.mainMenuItemSaveActive.Text = "&Save Active";
            this.mainMenuItemSaveActive.Click += new System.EventHandler(this.mainMenuItemSaveActive_Click);
            // 
            // mainMenuItemFileSaveAll
            // 
            this.mainMenuItemFileSaveAll.Index = 4;
            this.mainMenuItemFileSaveAll.Text = "Save A&ll";
            this.mainMenuItemFileSaveAll.Click += new System.EventHandler(this.mainMenuItemFileSaveAll_Click);
            // 
            // menuItem19
            // 
            this.menuItem19.Index = 5;
            this.menuItem19.Text = "-";
            // 
            // mainMenuItemFileUploadFile
            // 
            this.mainMenuItemFileUploadFile.Index = 6;
            this.mainMenuItemFileUploadFile.Text = "&Upload File...";
            this.mainMenuItemFileUploadFile.Click += new System.EventHandler(this.mainMenuItemFileUploadFile_Click);
            // 
            // mainMenuItemFileDownloadFile
            // 
            this.mainMenuItemFileDownloadFile.Index = 7;
            this.mainMenuItemFileDownloadFile.Text = "&Download File...";
            this.mainMenuItemFileDownloadFile.Click += new System.EventHandler(this.mainMenuItemFileDownloadFile_Click);
            // 
            // menuItem12
            // 
            this.menuItem12.Index = 8;
            this.menuItem12.Text = "-";
            // 
            // mainMenuItemFileExit
            // 
            this.mainMenuItemFileExit.Index = 9;
            this.mainMenuItemFileExit.Text = "E&xit";
            this.mainMenuItemFileExit.Click += new System.EventHandler(this.mainMenuItemFileExit_Click);
            // 
            // mainMenuItemWindow
            // 
            this.mainMenuItemWindow.Index = 1;
            this.mainMenuItemWindow.MdiList = true;
            this.mainMenuItemWindow.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mainMenuItemTile,
            this.mainMenuItemCascade});
            this.mainMenuItemWindow.Text = "&Window";
            // 
            // mainMenuItemTile
            // 
            this.mainMenuItemTile.Index = 0;
            this.mainMenuItemTile.Text = "&Tile";
            this.mainMenuItemTile.Click += new System.EventHandler(this.menuItemTile_Click);
            // 
            // mainMenuItemHelp
            // 
            this.mainMenuItemHelp.Index = 2;
            this.mainMenuItemHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mainMenuItemAbout});
            this.mainMenuItemHelp.Text = "&Help";
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
            // 
            // mdiClient
            // 
            this.mdiClient.BackColor = System.Drawing.Color.White;
            this.mdiClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mdiClient.Location = new System.Drawing.Point(203, 28);
            this.mdiClient.Name = "mdiClient";
            this.mdiClient.Size = new System.Drawing.Size(813, 665);
            this.mdiClient.TabIndex = 5;
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 693);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Size = new System.Drawing.Size(1016, 20);
            this.statusBar1.TabIndex = 1;
            this.statusBar1.Text = "Ready";
            // 
            // contextMenuTreeViewFiles
            // 
            this.contextMenuTreeViewFiles.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewFilesNew,
            this.menuItem1,
            this.contextMenuTreeViewFilesPaste});
            // 
            // contextMenuTreeViewFilesNew
            // 
            this.contextMenuTreeViewFilesNew.Index = 0;
            this.contextMenuTreeViewFilesNew.Text = "&New File...";
            this.contextMenuTreeViewFilesNew.Click += new System.EventHandler(this.contextMenuTreeViewFilesNew_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 1;
            this.menuItem1.Text = "-";
            // 
            // contextMenuTreeViewFilesPaste
            // 
            this.contextMenuTreeViewFilesPaste.Index = 2;
            this.contextMenuTreeViewFilesPaste.Text = "&Paste";
            this.contextMenuTreeViewFilesPaste.Click += new System.EventHandler(this.contextMenuTreeViewFilesPaste_Click);
            // 
            // contextMenuTreeViewFile
            // 
            this.contextMenuTreeViewFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewFileNewProgram,
            this.menuItem17,
            this.contextMenuTreeViewFileDelete,
            this.contextMenuTreeViewFileRename,
            this.menuItem6,
            this.contextMenuTreeViewFilePaste});
            // 
            // contextMenuTreeViewFileNewProgram
            // 
            this.contextMenuTreeViewFileNewProgram.Index = 0;
            this.contextMenuTreeViewFileNewProgram.Text = "New Program...";
            this.contextMenuTreeViewFileNewProgram.Click += new System.EventHandler(this.contextMenuTreeViewFileNewProgram_Click);
            // 
            // menuItem17
            // 
            this.menuItem17.Index = 1;
            this.menuItem17.Text = "-";
            // 
            // contextMenuTreeViewFileDelete
            // 
            this.contextMenuTreeViewFileDelete.Index = 2;
            this.contextMenuTreeViewFileDelete.Text = "&Delete";
            this.contextMenuTreeViewFileDelete.Click += new System.EventHandler(this.contextMenuTreeViewFileDelete_Click);
            // 
            // contextMenuTreeViewFileRename
            // 
            this.contextMenuTreeViewFileRename.Index = 3;
            this.contextMenuTreeViewFileRename.Text = "&Rename";
            this.contextMenuTreeViewFileRename.Click += new System.EventHandler(this.contextMenuTreeViewFileRename_Click);
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 4;
            this.menuItem6.Text = "-";
            // 
            // contextMenuTreeViewFilePaste
            // 
            this.contextMenuTreeViewFilePaste.Index = 5;
            this.contextMenuTreeViewFilePaste.Text = "&Paste";
            this.contextMenuTreeViewFilePaste.Click += new System.EventHandler(this.contextMenuTreeViewFilePaste_Click);
            // 
            // contextMenuTreeViewProgram
            // 
            this.contextMenuTreeViewProgram.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewProgramOpen,
            this.menuItem3,
            this.contextMenuTreeViewProgramDelete,
            this.contextMenuTreeViewProgramRename,
            this.menuItem7,
            this.contextMenuTreeViewProgramCopy});
            // 
            // contextMenuTreeViewProgramOpen
            // 
            this.contextMenuTreeViewProgramOpen.Index = 0;
            this.contextMenuTreeViewProgramOpen.Text = "&Open";
            this.contextMenuTreeViewProgramOpen.Click += new System.EventHandler(this.contextMenuTreeViewProgramOpen_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 1;
            this.menuItem3.Text = "-";
            // 
            // contextMenuTreeViewProgramDelete
            // 
            this.contextMenuTreeViewProgramDelete.Index = 2;
            this.contextMenuTreeViewProgramDelete.Text = "&Delete";
            this.contextMenuTreeViewProgramDelete.Click += new System.EventHandler(this.contextMenuTreeViewProgramDelete_Click);
            // 
            // contextMenuTreeViewProgramRename
            // 
            this.contextMenuTreeViewProgramRename.Index = 3;
            this.contextMenuTreeViewProgramRename.Text = "&Rename";
            this.contextMenuTreeViewProgramRename.Click += new System.EventHandler(this.contextMenuTreeViewProgramRename_Click);
            // 
            // menuItem7
            // 
            this.menuItem7.Index = 4;
            this.menuItem7.Text = "-";
            // 
            // contextMenuTreeViewProgramCopy
            // 
            this.contextMenuTreeViewProgramCopy.Index = 5;
            this.contextMenuTreeViewProgramCopy.Text = "&Copy";
            this.contextMenuTreeViewProgramCopy.Click += new System.EventHandler(this.contextMenuTreeViewProgramCopy_Click);
            // 
            // toolBarMain
            // 
            this.toolBarMain.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this.toolBarMain.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.toolBarButtonNew,
            this.toolBarButtonSave,
            this.toolBarButtonSaveAllReal,
            this.toolBarButtonSeparatorOne,
            this.toolBarButtonPrint,
            this.toolBarButtonSeparatorTwo,
            this.toolBarButtonUpload,
            this.toolBarButtonDownload});
            this.toolBarMain.DropDownArrows = true;
            this.toolBarMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolBarMain.ImageList = this.imageListToolbar;
            this.toolBarMain.Location = new System.Drawing.Point(0, 0);
            this.toolBarMain.Name = "toolBarMain";
            this.toolBarMain.ShowToolTips = true;
            this.toolBarMain.Size = new System.Drawing.Size(1016, 28);
            this.toolBarMain.TabIndex = 6;
            this.toolBarMain.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
            this.toolBarMain.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBarMain_ButtonClick);
            // 
            // toolBarButtonNew
            // 
            this.toolBarButtonNew.DropDownMenu = this.contextMenuToolBarNew;
            this.toolBarButtonNew.ImageIndex = 0;
            this.toolBarButtonNew.Name = "toolBarButtonNew";
            this.toolBarButtonNew.Style = System.Windows.Forms.ToolBarButtonStyle.DropDownButton;
            this.toolBarButtonNew.ToolTipText = "New Program";
            // 
            // contextMenuToolBarNew
            // 
            this.contextMenuToolBarNew.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuToolbarNewProgram,
            this.contextMenuToolbarNewFile,
            this.contextMenuToolbarNewPlate,
            this.contextMenuToolbarNewLiquid,
            this.contextMenuToolbarNewUser});
            // 
            // contextMenuToolbarNewProgram
            // 
            this.contextMenuToolbarNewProgram.Index = 0;
            this.contextMenuToolbarNewProgram.Text = "&Program...";
            this.contextMenuToolbarNewProgram.Click += new System.EventHandler(this.mainMenuItemFileNewProgram_Click);
            // 
            // contextMenuToolbarNewFile
            // 
            this.contextMenuToolbarNewFile.Index = 1;
            this.contextMenuToolbarNewFile.Text = "&File...";
            this.contextMenuToolbarNewFile.Click += new System.EventHandler(this.mainMenuItemFileNewFile_Click);
            // 
            // contextMenuToolbarNewPlate
            // 
            this.contextMenuToolbarNewPlate.Index = 2;
            this.contextMenuToolbarNewPlate.Text = "Pl&ate...";
            this.contextMenuToolbarNewPlate.Click += new System.EventHandler(this.mainMenuItemFileNewPlate_Click);
            // 
            // contextMenuToolbarNewLiquid
            // 
            this.contextMenuToolbarNewLiquid.Index = 3;
            this.contextMenuToolbarNewLiquid.Text = "&Liquid...";
            this.contextMenuToolbarNewLiquid.Click += new System.EventHandler(this.mainMenuItemFileNewLiquid_Click);
            // 
            // contextMenuToolbarNewUser
            // 
            this.contextMenuToolbarNewUser.Index = 4;
            this.contextMenuToolbarNewUser.Text = "&User...";
            this.contextMenuToolbarNewUser.Click += new System.EventHandler(this.mainMenuItemFileNewUser_Click);
            // 
            // toolBarButtonSave
            // 
            this.toolBarButtonSave.ImageIndex = 1;
            this.toolBarButtonSave.Name = "toolBarButtonSave";
            this.toolBarButtonSave.ToolTipText = "Save Active";
            // 
            // toolBarButtonSaveAllReal
            // 
            this.toolBarButtonSaveAllReal.ImageIndex = 2;
            this.toolBarButtonSaveAllReal.Name = "toolBarButtonSaveAllReal";
            this.toolBarButtonSaveAllReal.ToolTipText = "Save All";
            // 
            // toolBarButtonSeparatorOne
            // 
            this.toolBarButtonSeparatorOne.Name = "toolBarButtonSeparatorOne";
            this.toolBarButtonSeparatorOne.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // toolBarButtonPrint
            // 
            this.toolBarButtonPrint.ImageIndex = 5;
            this.toolBarButtonPrint.Name = "toolBarButtonPrint";
            this.toolBarButtonPrint.ToolTipText = "Print";
            // 
            // toolBarButtonSeparatorTwo
            // 
            this.toolBarButtonSeparatorTwo.Name = "toolBarButtonSeparatorTwo";
            this.toolBarButtonSeparatorTwo.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // toolBarButtonUpload
            // 
            this.toolBarButtonUpload.ImageIndex = 3;
            this.toolBarButtonUpload.Name = "toolBarButtonUpload";
            this.toolBarButtonUpload.ToolTipText = "Upload file to BNX1536";
            // 
            // toolBarButtonDownload
            // 
            this.toolBarButtonDownload.ImageIndex = 4;
            this.toolBarButtonDownload.Name = "toolBarButtonDownload";
            this.toolBarButtonDownload.ToolTipText = "Download file from BNX1536";
            // 
            // treeView
            // 
            this.treeView.AllowDrop = true;
            this.treeView.BackColor = System.Drawing.SystemColors.Window;
            this.treeView.ContextMenu = this.contextMenuTreeView;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Left;
            this.treeView.HotTracking = true;
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageListTreeView;
            this.treeView.Location = new System.Drawing.Point(0, 28);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.Size = new System.Drawing.Size(200, 665);
            this.treeView.TabIndex = 7;
            this.treeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView_AfterLabelEdit);
            this.treeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeView_ItemDrag);
            this.treeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeView_DragDrop);
            this.treeView.DragOver += new System.Windows.Forms.DragEventHandler(this.treeView_DragOver);
            this.treeView.DoubleClick += new System.EventHandler(this.treeView_DoubleClick);
            this.treeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeView_KeyDown);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(200, 28);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 665);
            this.splitter1.TabIndex = 8;
            this.splitter1.TabStop = false;
            // 
            // contextMenuTreeViewPlateGroup
            // 
            this.contextMenuTreeViewPlateGroup.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuToolbarNewPlates});
            // 
            // contextMenuToolbarNewPlates
            // 
            this.contextMenuToolbarNewPlates.Index = 0;
            this.contextMenuToolbarNewPlates.Text = "&New Plate...";
            this.contextMenuToolbarNewPlates.Click += new System.EventHandler(this.contextMenuToolbarNewPlates_Click);
            // 
            // contextMenuTreeViewPlate
            // 
            this.contextMenuTreeViewPlate.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewPlateOpen,
            this.menuItem2,
            this.contextMenuTreeViewPlateDelete});
            // 
            // contextMenuTreeViewPlateOpen
            // 
            this.contextMenuTreeViewPlateOpen.Index = 0;
            this.contextMenuTreeViewPlateOpen.Text = "&Open";
            this.contextMenuTreeViewPlateOpen.Click += new System.EventHandler(this.contextMenuTreeViewPlateOpen_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "-";
            // 
            // contextMenuTreeViewPlateDelete
            // 
            this.contextMenuTreeViewPlateDelete.Index = 2;
            this.contextMenuTreeViewPlateDelete.Text = "&Delete";
            this.contextMenuTreeViewPlateDelete.Click += new System.EventHandler(this.contextMenuTreeViewPlateDelete_Click);
            // 
            // contextMenuTreeViewLiquids
            // 
            this.contextMenuTreeViewLiquids.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuToolbarNewLiquids});
            // 
            // contextMenuToolbarNewLiquids
            // 
            this.contextMenuToolbarNewLiquids.Index = 0;
            this.contextMenuToolbarNewLiquids.Text = "&New Liquid...";
            this.contextMenuToolbarNewLiquids.Click += new System.EventHandler(this.contextMenuToolbarNewLiquids_Click);
            // 
            // contextMenuTreeViewLiquid
            // 
            this.contextMenuTreeViewLiquid.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewLiquidOpen,
            this.menuItem4,
            this.contextMenuTreeViewLiquidDelete});
            // 
            // contextMenuTreeViewLiquidOpen
            // 
            this.contextMenuTreeViewLiquidOpen.Index = 0;
            this.contextMenuTreeViewLiquidOpen.Text = "&Open";
            this.contextMenuTreeViewLiquidOpen.Click += new System.EventHandler(this.contextMenuTreeViewLiquidOpen_Click);
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 1;
            this.menuItem4.Text = "-";
            // 
            // contextMenuTreeViewLiquidDelete
            // 
            this.contextMenuTreeViewLiquidDelete.Index = 2;
            this.contextMenuTreeViewLiquidDelete.Text = "&Delete";
            this.contextMenuTreeViewLiquidDelete.Click += new System.EventHandler(this.contextMenuTreeViewLiquidDelete_Click);
            // 
            // contextMenuTreeViewUsers
            // 
            this.contextMenuTreeViewUsers.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuToolbarNewUsers});
            // 
            // contextMenuToolbarNewUsers
            // 
            this.contextMenuToolbarNewUsers.Index = 0;
            this.contextMenuToolbarNewUsers.Text = "&New User...";
            this.contextMenuToolbarNewUsers.Click += new System.EventHandler(this.contextMenuToolbarNewUsers_Click);
            // 
            // contextMenuTreeViewUser
            // 
            this.contextMenuTreeViewUser.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewUserOpen,
            this.menuItem5,
            this.contextMenuTreeViewUserDelete});
            // 
            // contextMenuTreeViewUserOpen
            // 
            this.contextMenuTreeViewUserOpen.Index = 0;
            this.contextMenuTreeViewUserOpen.Text = "&Open";
            this.contextMenuTreeViewUserOpen.Click += new System.EventHandler(this.contextMenuTreeViewUserOpen_Click);
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 1;
            this.menuItem5.Text = "-";
            // 
            // contextMenuTreeViewUserDelete
            // 
            this.contextMenuTreeViewUserDelete.Index = 2;
            this.contextMenuTreeViewUserDelete.Text = "&Delete";
            this.contextMenuTreeViewUserDelete.Click += new System.EventHandler(this.contextMenuTreeViewUserDelete_Click);
            // 
            // contextMenuTreeViewConfig
            // 
            this.contextMenuTreeViewConfig.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewConfigOpen});
            // 
            // contextMenuTreeViewConfigOpen
            // 
            this.contextMenuTreeViewConfigOpen.Index = 0;
            this.contextMenuTreeViewConfigOpen.Text = "&Open";
            this.contextMenuTreeViewConfigOpen.Click += new System.EventHandler(this.contextMenuTreeViewConfigOpen_Click);
            // 
            // contextMenuTreeViewFileProtected
            // 
            this.contextMenuTreeViewFileProtected.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewFileProtectedCopy});
            // 
            // contextMenuTreeViewFileProtectedCopy
            // 
            this.contextMenuTreeViewFileProtectedCopy.Index = 0;
            this.contextMenuTreeViewFileProtectedCopy.Text = "&Copy";
            this.contextMenuTreeViewFileProtectedCopy.Click += new System.EventHandler(this.contextMenuTreeViewFileProtectedCopy_Click);
            // 
            // contextMenuTreeViewProgramProtected
            // 
            this.contextMenuTreeViewProgramProtected.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewProgramProtectedCopy});
            // 
            // contextMenuTreeViewProgramProtectedCopy
            // 
            this.contextMenuTreeViewProgramProtectedCopy.Index = 0;
            this.contextMenuTreeViewProgramProtectedCopy.Text = "&Copy";
            this.contextMenuTreeViewProgramProtectedCopy.Click += new System.EventHandler(this.contextMenuTreeViewProgramProtectedCopy_Click);
            // 
            // contextMenuTreeViewCurrent
            // 
            this.contextMenuTreeViewCurrent.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.contextMenuTreeViewCurrentDelete});
            // 
            // contextMenuTreeViewCurrentDelete
            // 
            this.contextMenuTreeViewCurrentDelete.Index = 0;
            this.contextMenuTreeViewCurrentDelete.Text = "&Delete";
            this.contextMenuTreeViewCurrentDelete.Click += new System.EventHandler(this.contextMenuTreeViewCurrentDelete_Click);
            // 
            // mainForm
            // 
            this.ClientSize = new System.Drawing.Size(1016, 713);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.toolBarMain);
            this.Controls.Add(this.statusBar1);
            this.Controls.Add(this.mdiClient);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.Menu = this.mainMenu;
            this.Name = "mainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BioNex BNX1536";
            this.Load += new System.EventHandler(this.mainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeView_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private void InitializeComponent2()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(mainForm));
			this.printDocument = new System.Drawing.Printing.PrintDocument();
			this.printDialog = new System.Windows.Forms.PrintDialog();
			this.printPreviewDialog = new System.Windows.Forms.PrintPreviewDialog();

			//
			// printDialog
			//
			this.printDialog.Document = this.printDocument;

			// 
			// printPreviewDialog
			// 
			this.printPreviewDialog.AutoScrollMargin = new System.Drawing.Size(0, 0);
			this.printPreviewDialog.AutoScrollMinSize = new System.Drawing.Size(0, 0);
			this.printPreviewDialog.ClientSize = new System.Drawing.Size(400, 300);
			this.printPreviewDialog.Enabled = true;
			this.printPreviewDialog.Icon = ((System.Drawing.Icon)(resources.GetObject("printPreviewDialog.Icon")));
			this.printPreviewDialog.Location = new System.Drawing.Point(704, 128);
			this.printPreviewDialog.MinimumSize = new System.Drawing.Size(375, 250);
			this.printPreviewDialog.Name = "printPreviewDialog";
			this.printPreviewDialog.TransparencyKey = System.Drawing.Color.Empty;
			this.printPreviewDialog.Visible = false;
		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			m_machineCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			CultureInfo ci = new CultureInfo("en-US", true);
			System.Threading.Thread.CurrentThread.CurrentCulture = ci;

			CustomExceptionHandler eh = new CustomExceptionHandler();
			Application.ThreadException += new ThreadExceptionEventHandler(eh.OnThreadException);

			Application.Run(new mainForm());
		}

		public static CultureInfo MachineCulture()
		{
			return m_machineCulture;
		}

		private void mainForm_Load(object sender, System.EventArgs e)
		{
			// create xml data class
			m_xmlData = new XmlData();

			try
			{
				m_xmlData.LoadAll();
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "BNX1536 error");
				Application.Exit();
			}
			
			// create user class
			m_User = new CUser();

			// show login screen
			int nUserLevel = 0;
			while (nUserLevel == 0)
			{
				Login login = new Login();
				DialogResult DR = login._ShowDialog(m_User, this);
				if (DialogResult.Cancel == DR)
				{
					Close();
					break;
				}

				try
				{
					nUserLevel = m_xmlData.VerifyUser(m_User.Username, m_User.Password);
					m_User.UserLevel = nUserLevel;
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}

				if (nUserLevel == 0)
				{
					MessageBox.Show("Login failed!\n\nRemember that passwords are case sensitive!", "Login failed", MessageBoxButtons.OK);
				}
			}

			InitializeComponent2();

			// polulate tree
			try
			{
				m_xmlData.PopulateTree(treeView, this, false);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "BNX1536 error");
				Application.Exit();
			}
		}

		private void treeView_DoubleClick(object sender, System.EventArgs e)
		{
			string strNodeFullPath = treeView.SelectedNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());
			
			switch (strNodeArray.Length)
			{
				case 1: // level one node doubleclicked
					if ("communication" == strNodeArray[0].ToLower()) // config
					{
						// check if already open... and get its index
						bool bExists = false;
						int iForm = 0;
						for (iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
						{
							if (strNodeArray[0] == mdiClient.MdiChildren[iForm].Text.ToString())
							{
								bExists = true;
								break;
							}
						}

						if (bExists)
						{
							mdiClient.MdiChildren[iForm].Activate();
						}
						else
						{
							OpenConfigForm();
						}
					}
					break;
				case 2: // level two node doubleclicked
					if ("users" == strNodeArray[0].ToLower()) // user
					{
						OpenUserForm(strNodeArray[1], false);
					}
					else if ("liquids" == strNodeArray[0].ToLower()) // liquid
					{
						OpenLiquidForm(strNodeArray[1], false);
					}
					else if ("current bnx1536 programs" == strNodeArray[0].ToLower())
					{
						OpenProgramForm("____BNX1536_", strNodeArray[1], "____BNX1536_", false);
					}
					break;
				case 3: // level three node doubleclicked
					if (strNodeArray[0].ToLower().EndsWith("files")) // program
					{
						string strOwner = "";
						if (strNodeArray[0].ToLower() == "my files")
						{
							strOwner = m_User.Username;
						}
						else
						{
							int nRemoveFromIndex = strNodeArray[0].IndexOf("\'s files");
							strOwner = strNodeArray[0].Substring(0, nRemoveFromIndex);
						}

						OpenProgramForm(strNodeArray[1], strNodeArray[2], strOwner, false);
					}
					else if ("plates" == strNodeArray[0].ToLower()) // plate
					{
						string strPlateType = "";
						if (strNodeArray[1] == "96")
						{
							strPlateType = "1";
						}
						else if (strNodeArray[1] == "384")
						{
							strPlateType = "2";
						}
						if (strNodeArray[1] == "1536")
						{
							strPlateType = "3";
						}

						OpenPlateForm(strNodeArray[2], strPlateType, false);
					}
				break;
			}
		}

		private void menuItemTile_Click(object sender, System.EventArgs e)
		{
			mdiClient.LayoutMdi(MdiLayout.TileHorizontal);
		}

		private void menuItemCascade_Click(object sender, System.EventArgs e)
		{
			mdiClient.LayoutMdi(MdiLayout.Cascade);
		}

		private void contextMenuTreeView_Popup(object sender, System.EventArgs e)
		{
			m_pointContextMenu = treeView.PointToClient(Cursor.Position);
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			contextMenuTreeView.MenuItems.Clear();

			switch (strNodeArray.Length)
			{
				case 1:
					if ("my files" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewFiles);
					}
					else if ("plates" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewPlateGroup);
					}
					else if ("users" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewUsers);
					}
					else if ("liquids" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewLiquids);
					}
					else if ("config" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewConfig);
					}
					break;
				case 2:
					if ("my files" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewFile);
					}
					else if ("current bnx1536 programs" == strNodeArray[0].ToLower())
					{
						if (m_User.UserLevel == 3)
						{
							contextMenuTreeView.MergeMenu(contextMenuTreeViewProgramProtected);
							contextMenuTreeView.MenuItems.Add(new MenuItem("-"));
							contextMenuTreeView.MergeMenu(contextMenuTreeViewCurrent);
						}
					}
					else if (strNodeArray[0].ToLower().EndsWith("files"))
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewFileProtected);
					}
					else if ("plates" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewPlateGroup);
					}
					else if ("liquids" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewLiquid);
					}
					else if ("users" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewUser);
					}
					break;
				case 3:
					if ("my files" == strNodeArray[0].ToLower())
					{		
						contextMenuTreeView.MergeMenu(contextMenuTreeViewProgram);
					}
					else if (strNodeArray[0].ToLower().EndsWith("files"))
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewProgramProtected);
					}
					else if ("plates" == strNodeArray[0].ToLower())
					{
						contextMenuTreeView.MergeMenu(contextMenuTreeViewPlate);
					}
					break;
			}
		}

		private void contextMenuTreeViewFileOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			treeNode.Toggle();
		}

		private void mainMenuItemAbout_Click(object sender, System.EventArgs e)
		{
			About about = new About(this);
			about.Show();
		}

		private void contextMenuTreeViewFilesOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			treeNode.Toggle();
		}

		private void contextMenuTreeViewFilesNew_Click(object sender, System.EventArgs e)
		{
			mainMenuItemFileNewFile_Click(sender, e);
		}

		private void treeView_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			TreeNode treeNode = treeView.SelectedNode;

			if (e.KeyData == Keys.Delete)
			{
				string strNodeFullPath = treeView.SelectedNode.FullPath.ToString();
				string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());			
			
				switch (strNodeArray.Length)
				{
					case 2:
						if ("current bnx1536 programs" == strNodeArray[0].ToLower())
						{
							if (m_User.UserLevel == 3)
							{
								string strMessage = "This will delete the program " + "\"" + treeNode.Text + "\".\n";
								strMessage += "Do you want to continue?";
								DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
								if (DR == DialogResult.Yes)
								{
									try
									{
										m_xmlData.DeleteProgram("____BNX1536_", treeNode.Text, "____BNX1536_");
										treeNode.Remove();
									}
									catch (Exception exception)
									{
										MessageBox.Show(exception.Message, "BNX1536 error");
										Application.Exit();
									}
								}
							}
						}
						else if ("my files" == strNodeArray[0].ToLower())
						{
							string strMessage = "This will delete the file " + "\"" + treeNode.Text + "\"" + " and all it's programs.\n";
							strMessage += "Do you want to continue?";
							DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
							if (DR == DialogResult.Yes)
							{
								try
								{
									m_xmlData.DeleteFile(treeNode.Text, m_User.Username, this, treeView);
									treeNode.Remove();
								}
								catch (Exception exception)
								{
									MessageBox.Show(exception.Message, "BNX1536 error");
									Application.Exit();
								}
							}
						}
						else if ("liquids" == strNodeArray[0].ToLower())
						{
							string strMessage = "This will delete the liquid " + "\"" + treeNode.Text + "\".\n";
							strMessage += "Do you want to continue?";
							DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
							if (DR == DialogResult.Yes)
							{
								try
								{
									m_xmlData.DeleteLiquid(treeNode.Text, treeView);
								}
								catch (Exception exception)
								{
									MessageBox.Show(exception.Message, "BNX1536 error");
									Application.Exit();
								}

								for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
								{
									if (mdiClient.MdiChildren[i].Text.StartsWith("Liquid: " + treeNode.Text))
									{
										mdiClient.MdiChildren[i].Tag = false;
										mdiClient.MdiChildren[i].Close();
										break;
									}
								}
							}
						}
						else if ("users" == strNodeArray[0].ToLower())
						{
							if (m_User.Username == treeNode.Text)
							{
								MessageBox.Show(this, "You can not delete yourself!", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
								return;
							}

							if (m_xmlData.CountAdministrators() < 2)
							{
								MessageBox.Show(this, "You can not delete the last administrator.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
								return;
							}

							string strMessage = "This will delete the user " + "\"" + treeNode.Text + "\" and all of his or her files and programs.\n";
							strMessage += "Do you want to continue?";
							DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
							if (DR == DialogResult.Yes)
							{
								try
								{
									m_xmlData.DeleteUser(treeNode.Text, treeView, this);
								}
								catch (Exception exception)
								{
									MessageBox.Show(exception.Message, "BNX1536 error");
									Application.Exit();
								}
								for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
								{
									if (mdiClient.MdiChildren[i].Text.StartsWith("User: " + treeNode.Text))
									{
										mdiClient.MdiChildren[i].Tag = false;
										mdiClient.MdiChildren[i].Close();
										break;
									}
								}
							}
						}
						break;
					case 3:
						if ("my files" == strNodeArray[0].ToLower())
						{
							string strMessage = "This will delete the program " + "\"" + treeNode.Text + "\".\n";
							strMessage += "Do you want to continue?";
							DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
							if (DR == DialogResult.Yes)
							{
								try
								{
									m_xmlData.DeleteProgram(strNodeArray[1], treeNode.Text, m_User.Username);
									treeNode.Remove();
								}
								catch (Exception exception)
								{
									MessageBox.Show(exception.Message, "BNX1536 error");
									Application.Exit();
								}
							}
						}
						else if ("plates" == strNodeArray[0].ToLower())
						{
							string strMessage = "This will delete the plate " + "\"" + treeNode.Text + "\".\n";
							strMessage += "Do you want to continue?";
							DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
							if (DR == DialogResult.Yes)
							{
								try
								{
									m_xmlData.DeletePlate(strNodeArray[1], treeNode.Text, treeView);
								}
								catch (Exception exception)
								{
									MessageBox.Show(exception.Message, "BNX1536 error");
									Application.Exit();
								}

								for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
								{
									if (mdiClient.MdiChildren[i].Text.StartsWith("Plate: " + treeNode.Text))
									{
										mdiClient.MdiChildren[i].Tag = false;
										mdiClient.MdiChildren[i].Close();
										break;
									}
								}
							}
						}
						break;
				}
			}
			else if (e.KeyData == Keys.Enter)
			{
				treeView_DoubleClick( sender, e );				
			}
			else if (e.KeyData == Keys.F2)
			{
				/*
				string strNodeFullPath = treeView.SelectedNode.FullPath.ToString();
				string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());			
			
				switch (strNodeArray.Length)
				{
					case 2:
						if ("my files" == strNodeArray[0].ToLower())
						{
							bFileNameEdited = true;
							strOldFileName = treeNode.Text;
							treeNode.BeginEdit();
						}
						break;
					case 3:
						if ("my files" == strNodeArray[0].ToLower())
						{
							bProgramNameEdited = true;
							strOldProgramName = treeNode.Text;
							strOldFileName = treeNode.Parent.Text;
							treeNode.BeginEdit();
						}
						break;
				}
				*/
			}
		}

		private void contextMenuTreeViewFileNewProgram_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			treeNode.Expand();
			OpenProgramForm(treeNode.Text, "", m_User.Username, true);
		}

		private void contextMenuTreeViewFileDelete_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strMessage = "This will delete the file " + "\"" + treeNode.Text + "\"" + " and all it's programs.\n";
			strMessage += "Do you want to continue?";
			DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (DR == DialogResult.Yes)
			{
				try
				{
					m_xmlData.DeleteFile(treeNode.Text, m_User.Username, this, treeView);
					treeNode.Remove();
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}
			}			
		}

		private void contextMenuTreeViewFileRename_Click(object sender, System.EventArgs e)
		{
			/*
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			bFileNameEdited = true;
			strOldFileName = treeNode.Text;
			treeNode.BeginEdit();
			*/

			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			strOldFileName = treeNode.Text;
			string strNewFileName = treeNode.Text;

			FileRenameForm FRF = new FileRenameForm();
			DialogResult DR = FRF._ShowDialog(ref strNewFileName);

			if (DR == DialogResult.OK)
			{
				// not allowed...
				string target = "\\/:*?\"<>|\"";
				char[] anyOf = target.ToCharArray();
				if (strNewFileName.IndexOfAny(anyOf) > -1 || strNewFileName.Length < 1)
				{
					MessageBox.Show("File and program names can not contain any af these charaters:\n\n\t \\/:*?\"<>|\n\n or be of zero length.", "Names", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					m_xmlData.RenameFile(strOldFileName, strNewFileName, m_User.Username);
					treeNode.Text = strNewFileName;

					// rename program form as well
					for (int iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
					{
						string strFormSubName = " (File: " + strOldFileName + ", User: " + m_User.Username + ")";
					
						if (mdiClient.MdiChildren[iForm].Text.IndexOf(strFormSubName) > -1)
						{
							programForm pf = (programForm)mdiClient.MdiChildren[iForm];
							pf.m_strFileNameInternal = strNewFileName;
							string strNewFormSubName = " (File: " + strNewFileName + ", User: " + m_User.Username + ")";
							if (mdiClient.MdiChildren[iForm].Text.EndsWith("*"))
							{
								mdiClient.MdiChildren[iForm].Text = mdiClient.MdiChildren[iForm].Text.Replace(strFormSubName, strNewFormSubName);
								mdiClient.MdiChildren[iForm].Text += "*";
								pf.labelFileUser.Text =  "File: " + strNewFileName + ", User: " + m_User.Username + "*";
							}
							else
							{
								mdiClient.MdiChildren[iForm].Text = mdiClient.MdiChildren[iForm].Text.Replace(strFormSubName, strNewFormSubName);
								pf.labelFileUser.Text =  "File: " + strNewFileName + ", User: " + m_User.Username;
							}
						}
					}
				}
			}
		}

		private void contextMenuTreeViewProgramOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			string strOwner = "";
			if (strNodeArray[0].ToLower() == "my files")
			{
				strOwner = m_User.Username;
			}
			else
			{
				int nRemoveFromIndex = strNodeArray[0].IndexOf("\'s files");
				strOwner = strNodeArray[0].Substring(0, nRemoveFromIndex);
			}

			OpenProgramForm(strNodeArray[1], strNodeArray[2], strOwner, false);
		}

		private void contextMenuTreeViewProgramDelete_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strMessage = "This will delete the program " + "\"" + treeNode.Text + "\".\n";
			strMessage += "Do you want to continue?";
			DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (DR == DialogResult.Yes)
			{
				string strNodeFullPath = treeNode.FullPath.ToString();
				string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());
				
				try
				{
					m_xmlData.DeleteProgram(strNodeArray[1], treeNode.Text, m_User.Username);
					treeNode.Remove();
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}
			}
		}

		private void contextMenuTreeViewProgramRename_Click(object sender, System.EventArgs e)
		{
			/*
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			bProgramNameEdited = true;
			strOldFileName = treeNode.Parent.Text;
			strOldProgramName = treeNode.Text;
			treeNode.BeginEdit();
			*/

			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			strOldFileName = treeNode.Parent.Text;
			strOldProgramName = treeNode.Text;
			string strNewProgramName = treeNode.Text;

			ProgramRenameForm PRF = new ProgramRenameForm();
			DialogResult DR = PRF._ShowDialog(ref strNewProgramName);

			if (DR == DialogResult.OK)
			{
				// not allowed...
				string target = "\\/:*?\"<>|\"";
				char[] anyOf = target.ToCharArray();
				if (strNewProgramName.IndexOfAny(anyOf) > -1 || strNewProgramName.Length < 1)
				{
					MessageBox.Show("File and program names can not contain any af these charaters:\n\n\t \\/:*?\"<>|\n\n or be of zero length.", "Names", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					m_xmlData.RenameProgram(strOldFileName, strOldProgramName, strNewProgramName, m_User.Username);
					treeNode.Text = strNewProgramName;

					// rename program form as well
					for (int iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
					{
						string strFormName = "Program: " + strOldProgramName + " (File: " + strOldFileName + ", User: " + m_User.Username + ")";
						if (mdiClient.MdiChildren[iForm].Text.StartsWith(strFormName))
						{
							programForm pf = (programForm)mdiClient.MdiChildren[iForm];
							pf.m_strProgramName = strNewProgramName;
							string strNewFormName = "Program: " + strNewProgramName + " (File: " + strOldFileName + ", User: " + m_User.Username + ")";
							if (mdiClient.MdiChildren[iForm].Text.EndsWith("*"))
							{
								mdiClient.MdiChildren[iForm].Text = strNewFormName + "*";
								pf.labelProgram.Text =  "Program: " + strNewProgramName + "*";
							}
							else
							{
								mdiClient.MdiChildren[iForm].Text = strNewFormName;
								pf.labelProgram.Text =  "Program: " + strNewProgramName;
							}
							break;
						}
					}
				}
			}
		}

		private void contextMenuTreeViewPlatesOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			treeNode.Toggle();
		}

		private void contextMenuTreeViewPlatesNew_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			treeNode.Expand();
			TreeNode treeNodeNew = treeNode.Nodes.Add("New Brand");
			treeNodeNew.BeginEdit();
		}

		private void toolBarMain_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			switch(toolBarMain.Buttons.IndexOf(e.Button))
			{
				case 0:	// new
					OpenProgramForm("", "", m_User.Username, true);
					break;
				case 1: // save
					SaveActive();
					break; 
				case 2: // save all
					SaveAll();
					break; 
				case 4: // print
					Print();
					break;
				case 6: // upload
					UploadFile();
					break;
				case 7: // download
					DownloadFile();
					break;
			}
		}

		private void mainMenuItemToolsRS232Console_Click(object sender, System.EventArgs e)
		{
			RS232InputOutputForm RS232IOForm = new RS232InputOutputForm(this);
			RS232IOForm.ShowDialog();
		}

		private void mainMenuItemFileNewFile_Click(object sender, System.EventArgs e)
		{
			NewFileForm NFF = new NewFileForm();

			string strFileName = "";
			DialogResult DR = NFF._ShowDialog(ref strFileName);
			if (DR == DialogResult.OK)
			{
				// add node to xml data file
				try
				{
					if (m_xmlData.CreateFile(strFileName, m_User.Username))
					{
						// add node to treeview
						TreeNode treeNodeFile = new TreeNode(strFileName, 7, 7);
						treeView.Nodes[0].Nodes.Add(treeNodeFile);
					}
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}
			}
		}

		private void mainMenuItemFileNewProgram_Click(object sender, System.EventArgs e)
		{
			OpenProgramForm("", "", m_User.Username, true);
		}

		private void mainMenuItemFileNewPlate_Click(object sender, System.EventArgs e)
		{
			OpenPlateForm("", "", true);
		}

		private void mainMenuItemFileNewLiquid_Click(object sender, System.EventArgs e)
		{
			OpenLiquidForm("", true);
		}

		private void mainMenuItemFileNewUser_Click(object sender, System.EventArgs e)
		{
			OpenUserForm("", true);
		}

		private void mainMenuItemFileClose_Click(object sender, System.EventArgs e)
		{
			if (mdiClient.MdiChildren.Length > 0)
			{
				ActiveMdiChild.Close();
			}
		}

		private void mainMenuItemSaveActive_Click(object sender, System.EventArgs e)
		{
			SaveActive();
		}

		private void mainMenuItemFileSaveAll_Click(object sender, System.EventArgs e)
		{
			SaveAll();
		}

		private void mainMenuItemFileUploadFile_Click(object sender, System.EventArgs e)
		{
			UploadFile();
		}

		private void mainMenuItemFileDownloadFile_Click(object sender, System.EventArgs e)
		{
			DownloadFile();
		}

		private void mainMenuItemFileExit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void SaveAll()
		{
			for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
			{
				if (mdiClient.MdiChildren[i].Text.StartsWith("Program"))
				{
					programForm PF = (programForm)mdiClient.MdiChildren[i];
					PF.Save();
				}
				else if (mdiClient.MdiChildren[i].Text.StartsWith("Plate"))
				{
					plateForm PF = (plateForm)mdiClient.MdiChildren[i];
					PF.Save();
				}
				else if (mdiClient.MdiChildren[i].Text.StartsWith("Liquid"))
				{
					liquidForm LF = (liquidForm)mdiClient.MdiChildren[i];
					LF.Save();
				}
				else if (mdiClient.MdiChildren[i].Text.StartsWith("User"))
				{
					userForm UF = (userForm)mdiClient.MdiChildren[i];
					UF.Save();
				}
				else if (mdiClient.MdiChildren[i].Text.StartsWith("Config"))
				{
					configForm CF = (configForm)mdiClient.MdiChildren[i];
					CF.Save();
				}
			}		
		}

		private void AskSaveAllBeforePrint()
		{
			for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
			{
				if( mdiClient.MdiChildren[i].Text.EndsWith( "*" ) )
				{
					DialogResult DR = MessageBox.Show("You have unsaved programs.\r\nDo you want to save these before printing?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (DR == DialogResult.Yes)
					{
						SaveAll();
					}
					break;					
				}
			}		
		}

		private void SaveActive()
		{
			if (mdiClient.MdiChildren.Length > 0 && ActiveMdiChild.Text.StartsWith("Program"))
			{
				programForm PF = (programForm)ActiveMdiChild;
				PF.Save();
			}
			else if (mdiClient.MdiChildren.Length > 0 && ActiveMdiChild.Text.StartsWith("Plate"))
			{
				plateForm PF = (plateForm)ActiveMdiChild;
				PF.Save();
			}
			else if (mdiClient.MdiChildren.Length > 0 && ActiveMdiChild.Text.StartsWith("Liquid"))
			{
				liquidForm LF = (liquidForm)ActiveMdiChild;
				LF.Save();
			}
			else if (mdiClient.MdiChildren.Length > 0 && ActiveMdiChild.Text.StartsWith("User"))
			{
				userForm UF = (userForm)ActiveMdiChild;
				UF.Save();
			}
			else if (mdiClient.MdiChildren.Length > 0 && ActiveMdiChild.Text.StartsWith("Communication"))
			{
				configForm CF = (configForm)ActiveMdiChild;
				CF.Save();
			}
		}

		private void UploadFile()
		{
			if (m_User.UserLevel > 1)
			{
				for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
				{
					if (mdiClient.MdiChildren[i].Text.StartsWith("Program: "))
					{
						if ((bool)mdiClient.MdiChildren[i].Tag == true)
						{
							char[] TrimChars = new char[1];
							TrimChars[0] = '*';
							string strProgramName = mdiClient.MdiChildren[i].Text.TrimEnd(TrimChars);
							DialogResult DR = MessageBox.Show(this, "Save \"" + strProgramName + "\" before upload?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (DR == DialogResult.Yes)
							{
								programForm PF = (programForm)mdiClient.MdiChildren[i];
								PF.Save();	
							}
						}
					}
				}
			}

			FileSelectForm FSF = new FileSelectForm();

			string strFileName = "";
			string strOwner = "";
			DialogResult DR2 = FSF._ShowDialog(ref strFileName, ref strOwner, "Select file to upload", this);
			if (DR2 == DialogResult.OK)
			{
				try
				{
					statusBar1.Text = "Uploading data. Please wait...";
					m_xmlData.SaveFileToAQ3(strFileName, strOwner, this);
					statusBar1.Text = "Ready.";
				}
				catch (Exception exception)
				{
					exception = exception;
					//MessageBox.Show(exception.Message, "BNX1536 error");
					//Application.Exit();
					configForm newMDIChild = new configForm();
					newMDIChild.MdiParent = this;
					newMDIChild.Show();
				}
			}
		}

		private void DownloadFile()
		{
			try
			{
				statusBar1.Text = "Downloading data. Please wait...";
				m_xmlData.LoadFileFromAQ3(this);
				statusBar1.Text = "Ready.";
			}
			catch (Exception exception)
			{
				exception = exception;
				MessageBox.Show(exception.Message, "BNX1536 error");
				//Application.Exit();
				OpenConfigForm();
			}
		}

		private void Print()
		{
			AskSaveAllBeforePrint();
			PrintForm PF = new PrintForm();
			DialogResult DR = PF._ShowDialog(this);

			//activate and redraw ourselves after a print
			VB.Interaction.AppActivate( Process.GetCurrentProcess().Id );
			this.Activate();
			this.Refresh();
			

			/*
			if (DR == DialogResult.OK)
			{
				try
				{
					statusBar1.Text = "Uploading data. Please wait...";
					m_xmlData.SaveFileToAQ3(strFileName, strOwner);
					statusBar1.Text = "Ready.";
				}
				catch (Exception exception)
				{
					exception = exception;
					//MessageBox.Show(exception.Message, "BNX1536 error");
					//Application.Exit();
					configForm newMDIChild = new configForm();
					newMDIChild.MdiParent = this;
					newMDIChild.Show();
				}
			}*/
		}

		private void contextMenuToolbarNewPlates_Click(object sender, System.EventArgs e)
		{
			OpenPlateForm("", "", true);
		}

		private void contextMenuTreeViewPlateOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			string strPlateType = "";
			if (strNodeArray[1] == "96")
			{
				strPlateType = "1";
			}
			else if (strNodeArray[1] == "384")
			{
				strPlateType = "2";
			}
			if (strNodeArray[1] == "1536")
			{
				strPlateType = "3";
			}

			OpenPlateForm(strNodeArray[2], strPlateType, false);
		}

		private void contextMenuTreeViewPlateDelete_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strMessage = "This will delete the plate " + "\"" + treeNode.Text + "\".\n";
			strMessage += "Do you want to continue?";
			DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (DR == DialogResult.Yes)
			{
				string strNodeFullPath = treeNode.FullPath.ToString();
				string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());
								
				try
				{
					m_xmlData.DeletePlate(strNodeArray[1], treeNode.Text, treeView);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}

				for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
				{
					if (mdiClient.MdiChildren[i].Text.StartsWith("Plate: " + treeNode.Text))
					{
						mdiClient.MdiChildren[i].Tag = false;
						mdiClient.MdiChildren[i].Close();
						break;
					}
				}
			}					
		}

		private void contextMenuToolbarNewLiquids_Click(object sender, System.EventArgs e)
		{
			OpenLiquidForm("", true);
		}

		private void contextMenuTreeViewLiquidOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());
			OpenLiquidForm(strNodeArray[1], false);
		}

		private void contextMenuTreeViewLiquidDelete_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strMessage = "This will delete the liquid " + "\"" + treeNode.Text + "\".\n";
			strMessage += "Do you want to continue?";
			DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (DR == DialogResult.Yes)
			{
				try
				{
					m_xmlData.DeleteLiquid(treeNode.Text, treeView);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}

				for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
				{
					if (mdiClient.MdiChildren[i].Text.StartsWith("Liquid: " + treeNode.Text))
					{
						mdiClient.MdiChildren[i].Tag = false;
						mdiClient.MdiChildren[i].Close();
						break;
					}
				}
			}
		}

		private void contextMenuToolbarNewUsers_Click(object sender, System.EventArgs e)
		{
			OpenUserForm("", true);
		}

		private void contextMenuTreeViewUserOpen_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());
			OpenUserForm(strNodeArray[1], false);
		}

		private void contextMenuTreeViewUserDelete_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);

			if (m_User.Username == treeNode.Text)
			{
				MessageBox.Show(this, "You can not delete yourself!", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			string strMessage = "This will delete the user " + "\"" + treeNode.Text + "\" and all of his or her files and programs.\n";
			strMessage += "Do you want to continue?";
			DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (DR == DialogResult.Yes)
			{
				try
				{
					m_xmlData.DeleteUser(treeNode.Text, treeView, this);
					for (int i = 0; i < mdiClient.MdiChildren.Length; i++)
					{
						if (mdiClient.MdiChildren[i].Text.StartsWith("User: " + treeNode.Text))
						{
							mdiClient.MdiChildren[i].Tag = false;
							mdiClient.MdiChildren[i].Close();
							break;
						}
					}
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}
			}
		}

		private void contextMenuTreeViewConfigOpen_Click(object sender, System.EventArgs e)
		{
			OpenConfigForm();
		}

		private string ProgramCopyNameDW4 = "";
		private string ProgramCopyName = "";
		private string ProgramCopyOwner = "";
		private string ProgramCopyFileName = "";
		private string FileCopyName = "";
		private string FileCopyOwner = "";
		private void contextMenuTreeViewProgramCopy_Click(object sender, System.EventArgs e)
		{
			// copy program
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			ProgramCopyFileName = strNodeArray[1];
			ProgramCopyNameDW4 = null;

			ProgramCopyName = strNodeArray[2];
			ProgramCopyOwner = m_User.Username;
		}

		private void contextMenuTreeViewProgramProtectedCopy_Click(object sender, System.EventArgs e)
		{
			// copy program
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			if(treeNode.Parent.Text.ToLower() == "current bnx1536 programs")
			{
				// DW4
				ProgramCopyNameDW4 = strNodeArray[1];
			}
			else
			{
				ProgramCopyNameDW4 = null;
				ProgramCopyFileName = strNodeArray[1];

				ProgramCopyName = strNodeArray[2];
				int nRemoveFromIndex = strNodeArray[0].IndexOf("\'s files");
				ProgramCopyOwner = strNodeArray[0].Substring(0, nRemoveFromIndex);			
			}
		}

		private void contextMenuTreeViewFileProtectedCopy_Click(object sender, System.EventArgs e)
		{
			// copy file
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			FileCopyName = strNodeArray[1];

			int nRemoveFromIndex = strNodeArray[0].IndexOf("\'s files");
			FileCopyOwner = strNodeArray[0].Substring(0, nRemoveFromIndex);
		}

		private void contextMenuTreeViewFilesPaste_Click(object sender, System.EventArgs e)
		{
			// paste file
			try
			{
				TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
				m_xmlData.CopyFile(FileCopyName, FileCopyOwner, m_User.Username, treeNode);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "BNX1536 error");
				Application.Exit();
			}
		}		
		
		private void contextMenuTreeViewFilePaste_Click(object sender, System.EventArgs e)
		{
			// paste program
			try
			{
				TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
				if(ProgramCopyNameDW4 != null)
				{
					// We got a DW4 file here
					m_xmlData.CopyProgramDW4(ProgramCopyNameDW4, m_User.Username, treeNode.Text, treeNode);
				}
				else
				{
					m_xmlData.CopyProgram(ProgramCopyName, ProgramCopyFileName, ProgramCopyOwner, m_User.Username, treeNode.Text, treeNode);
				}
				
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "BNX1536 error");
				Application.Exit();
			}
		}

		private void treeView_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
		{
			Point ClickedPoint = treeView.PointToClient(Cursor.Position);
			//MessageBox.Show(treeView.PointToClient(Cursor.Position).ToString());
			TreeNode treeNode = treeView.GetNodeAt(ClickedPoint);

			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			if (strNodeArray[0].ToLower() == "my files" || strNodeArray[0].ToLower().EndsWith("files") || strNodeArray[0].ToLower().EndsWith("file"))
			{
				if (strNodeArray.Length == 2)
				{
					// copy file
					FileCopyName = strNodeArray[1];

					if (strNodeArray[0].ToLower() == "my files")
					{
						FileCopyOwner = m_User.Username;
					}
					else
					{
						int nRemoveFromIndex = strNodeArray[0].IndexOf("\'s files");
						FileCopyOwner = strNodeArray[0].Substring(0, nRemoveFromIndex);
					}

					DoDragDrop("file", DragDropEffects.All);
				}
				else if (strNodeArray.Length == 3)
				{
					// copy program
					ProgramCopyFileName = strNodeArray[1];
					ProgramCopyName = strNodeArray[2];

					if (strNodeArray[0].ToLower() == "my files")
					{
						ProgramCopyOwner = m_User.Username;
					}
					else
					{
						int nRemoveFromIndex = strNodeArray[0].IndexOf("\'s files");
						ProgramCopyOwner = strNodeArray[0].Substring(0, nRemoveFromIndex);			
					}

					DoDragDrop("program", DragDropEffects.All);
				}
			}
		}

		private void treeView_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			Point ClickedPoint = treeView.PointToClient(Cursor.Position);
			TreeNode treeNode = treeView.GetNodeAt(ClickedPoint);

			try
			{
				if (e.Data.GetData(DataFormats.Text).ToString() == "file")
				{
					m_xmlData.CopyFile(FileCopyName, FileCopyOwner, m_User.Username, treeNode);
				}		
				else if (e.Data.GetData(DataFormats.Text).ToString() == "program")
				{
					m_xmlData.CopyProgram(ProgramCopyName, ProgramCopyFileName, ProgramCopyOwner, m_User.Username, treeNode.Text, treeNode);
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "BNX1536 error");
				Application.Exit();
			}
		}

		private void treeView_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
		{
			Point ClickedPoint = treeView.PointToClient(Cursor.Position);
			TreeNode treeNode = treeView.GetNodeAt(ClickedPoint);

			string strNodeFullPath = treeNode.FullPath.ToString();
			string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());

			System.Diagnostics.Debug.WriteLine(strNodeArray[0].ToLower());

			if (strNodeArray[0].ToLower() == "my files")
			{
				if (e.Data.GetDataPresent(DataFormats.Text))
				{
					if (e.Data.GetData(DataFormats.Text).ToString() == "file" && strNodeArray.Length == 1)
					{
						e.Effect = DragDropEffects.Copy;
					}
					else if (e.Data.GetData(DataFormats.Text).ToString() == "program" && strNodeArray.Length == 2)
					{
						e.Effect = DragDropEffects.Copy;
					}
					else
					{
						e.Effect = DragDropEffects.None;
					}
				}
				else
				{
					e.Effect = DragDropEffects.None;
				}
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void treeView_AfterLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
		{
			if (null == e.Label)
			{
				return;
			}

			// not allowed...
			string target = "\\/:*?\"<>|\"";
			char[] anyOf = target.ToCharArray();
			if (e.Label.IndexOfAny(anyOf) > -1 || e.Label.Length < 1)
			{
				MessageBox.Show("File and program names can not contain any af these charaters:\n\n\t \\/:*?\"<>|\n\n or be of zero length.", "Names", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				if (bFileNameEdited)
				{
					bFileNameEdited = false;
					e.CancelEdit = true;
				}
				else if (bProgramNameEdited)
				{
					bProgramNameEdited = false;
					e.CancelEdit = true;
				}
				
				return;
			}

			if (bFileNameEdited)
			{
				m_xmlData.RenameFile(strOldFileName, e.Label, m_User.Username);
				bFileNameEdited = false;

				// rename program form as well
				for (int iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
				{
					string strFormSubName = " (File: " + strOldFileName + ", User: " + m_User.Username + ")";
					
					if (mdiClient.MdiChildren[iForm].Text.IndexOf(strFormSubName) > -1)
					{
						programForm pf = (programForm)mdiClient.MdiChildren[iForm];
						pf.m_strFileNameInternal = e.Label;
						string strNewFormSubName = " (File: " + e.Label + ", User: " + m_User.Username + ")";
						if (mdiClient.MdiChildren[iForm].Text.EndsWith("*"))
						{
							mdiClient.MdiChildren[iForm].Text = mdiClient.MdiChildren[iForm].Text.Replace(strFormSubName, strNewFormSubName);
							mdiClient.MdiChildren[iForm].Text += "*";
							pf.labelFileUser.Text =  "File: " + e.Label + ", User: " + m_User.Username + "*";
						}
						else
						{
							mdiClient.MdiChildren[iForm].Text = mdiClient.MdiChildren[iForm].Text.Replace(strFormSubName, strNewFormSubName);
							pf.labelFileUser.Text =  "File: " + e.Label + ", User: " + m_User.Username;
						}
					}
				}
			}
			else if (bProgramNameEdited)
			{
				m_xmlData.RenameProgram(strOldFileName, strOldProgramName, e.Label, m_User.Username);
				bProgramNameEdited = false;

				// rename program form as well
				for (int iForm = 0; iForm < mdiClient.MdiChildren.Length; iForm++)
				{
					string strFormName = "Program: " + strOldProgramName + " (File: " + strOldFileName + ", User: " + m_User.Username + ")";
					if (mdiClient.MdiChildren[iForm].Text.StartsWith(strFormName))
					{
						programForm pf = (programForm)mdiClient.MdiChildren[iForm];
						pf.m_strProgramName = e.Label;
						string strNewFormName = "Program: " + e.Label + " (File: " + strOldFileName + ", User: " + m_User.Username + ")";
						if (mdiClient.MdiChildren[iForm].Text.EndsWith("*"))
						{
							mdiClient.MdiChildren[iForm].Text = strNewFormName + "*";
							pf.labelProgram.Text =  "Program: " + e.Label + "*";
						}
						else
						{
							mdiClient.MdiChildren[iForm].Text = strNewFormName;
							pf.labelProgram.Text =  "Program: " + e.Label;
						}
						break;
					}
				}
			}
		}

		private void contextMenuTreeViewCurrentDelete_Click(object sender, System.EventArgs e)
		{
			TreeNode treeNode = treeView.GetNodeAt(m_pointContextMenu);
			string strMessage = "This will delete the program " + "\"" + treeNode.Text + "\".\n";
			strMessage += "Do you want to continue?";
			DialogResult DR = MessageBox.Show(this, strMessage, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
			if (DR == DialogResult.Yes)
			{
				string strNodeFullPath = treeNode.FullPath.ToString();
				string[] strNodeArray = strNodeFullPath.Split(treeView.PathSeparator.ToCharArray());
				
				try
				{
					m_xmlData.DeleteProgram("____BNX1536_", treeNode.Text, "____BNX1536_");
					treeNode.Remove();
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message, "BNX1536 error");
					Application.Exit();
				}
			}		
		}
	}
}