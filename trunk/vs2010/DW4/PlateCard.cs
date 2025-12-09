using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace AQ3
{
	/// <summary>
	/// Summary description for PlateCard.
	/// </summary>
	public class PlateCard : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button buttonPlateSelector;
		public System.Windows.Forms.TextBox textBoxPlateName;
		public System.Windows.Forms.TextBox textBoxRows;
		private System.Windows.Forms.Label label1;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Panel panelInner96;
		private System.Windows.Forms.Panel panelInner384;
		private System.Windows.Forms.Panel panelInner1536;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel panelInnerNone;

		public PlateProperties m_PlateProperties = new PlateProperties();		
		private System.Windows.Forms.Button buttonSelectAll;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.Button buttonPlateInfo;
		public System.Windows.Forms.RadioButton RowRb;
		public System.Windows.Forms.RadioButton ColumnRb;		
		
		public ArrayList m_PlateRowsArray = new ArrayList();
//		public ArrayList m_PlateColumnsArray = new ArrayList();
		int m_nMaxRows = 0;
		int m_nMaxColumns = 0;
		public bool m_update = false;
		bool m_allowedToChangeWellType = true;
		bool m_prevRowState = true;
		public System.Windows.Forms.TextBox textBoxWells;
		int m_prevCardType = -1;
		int m_prevPrevCardType = -1;
		bool m_clearAll = false;
		bool m_newCard = false;

		string m_strOldFormat = "";

		public PlateCard()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PlateCard));
			this.panel1 = new System.Windows.Forms.Panel();
			this.textBoxWells = new System.Windows.Forms.TextBox();
			this.ColumnRb = new System.Windows.Forms.RadioButton();
			this.RowRb = new System.Windows.Forms.RadioButton();
			this.buttonPlateInfo = new System.Windows.Forms.Button();
			this.buttonClear = new System.Windows.Forms.Button();
			this.buttonSelectAll = new System.Windows.Forms.Button();
			this.panelInnerNone = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.panelInner1536 = new System.Windows.Forms.Panel();
			this.panelInner384 = new System.Windows.Forms.Panel();
			this.panelInner96 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxRows = new System.Windows.Forms.TextBox();
			this.buttonPlateSelector = new System.Windows.Forms.Button();
			this.textBoxPlateName = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel1.SuspendLayout();
			this.panelInnerNone.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.panel1.Controls.Add(this.textBoxWells);
			this.panel1.Controls.Add(this.ColumnRb);
			this.panel1.Controls.Add(this.RowRb);
			this.panel1.Controls.Add(this.buttonPlateInfo);
			this.panel1.Controls.Add(this.buttonClear);
			this.panel1.Controls.Add(this.buttonSelectAll);
			this.panel1.Controls.Add(this.panelInnerNone);
			this.panel1.Controls.Add(this.panelInner1536);
			this.panel1.Controls.Add(this.panelInner384);
			this.panel1.Controls.Add(this.panelInner96);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.textBoxRows);
			this.panel1.Controls.Add(this.buttonPlateSelector);
			this.panel1.Controls.Add(this.textBoxPlateName);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(400, 256);
			this.panel1.TabIndex = 0;
			// 
			// textBoxWells
			// 
			this.textBoxWells.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textBoxWells.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxWells.ForeColor = System.Drawing.Color.Black;
			this.textBoxWells.Location = new System.Drawing.Point(240, 64);
			this.textBoxWells.Multiline = true;
			this.textBoxWells.Name = "textBoxWells";
			this.textBoxWells.ReadOnly = true;
			this.textBoxWells.Size = new System.Drawing.Size(142, 64);
			this.textBoxWells.TabIndex = 24;
			this.textBoxWells.TabStop = false;
			this.textBoxWells.Text = "";
			// 
			// ColumnRb
			// 
			this.ColumnRb.BackColor = System.Drawing.Color.Transparent;
			this.ColumnRb.Enabled = false;
			this.ColumnRb.Location = new System.Drawing.Point(75, 203);
			this.ColumnRb.Name = "ColumnRb";
			this.ColumnRb.Size = new System.Drawing.Size(67, 24);
			this.ColumnRb.TabIndex = 23;
			this.ColumnRb.Text = "Columns";
			this.ColumnRb.CheckedChanged += new System.EventHandler(this.ColumnRb_CheckedChanged);
			// 
			// RowRb
			// 
			this.RowRb.BackColor = System.Drawing.Color.Transparent;
			this.RowRb.Enabled = false;
			this.RowRb.Location = new System.Drawing.Point(22, 203);
			this.RowRb.Name = "RowRb";
			this.RowRb.Size = new System.Drawing.Size(53, 24);
			this.RowRb.TabIndex = 22;
			this.RowRb.Text = "Rows";
			this.RowRb.CheckedChanged += new System.EventHandler(this.RowRb_CheckedChanged);
			// 
			// buttonPlateInfo
			// 
			this.buttonPlateInfo.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonPlateInfo.Location = new System.Drawing.Point(240, 37);
			this.buttonPlateInfo.Name = "buttonPlateInfo";
			this.buttonPlateInfo.Size = new System.Drawing.Size(110, 23);
			this.buttonPlateInfo.TabIndex = 21;
			this.buttonPlateInfo.Text = "Plate Info...";
			this.buttonPlateInfo.Visible = false;
			this.buttonPlateInfo.Click += new System.EventHandler(this.buttonPlateInfo_Click);
			// 
			// buttonClear
			// 
			this.buttonClear.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonClear.Location = new System.Drawing.Point(314, 137);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.Size = new System.Drawing.Size(68, 23);
			this.buttonClear.TabIndex = 20;
			this.buttonClear.Text = "Clear";
			this.buttonClear.Visible = false;
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
			// 
			// buttonSelectAll
			// 
			this.buttonSelectAll.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonSelectAll.Location = new System.Drawing.Point(240, 137);
			this.buttonSelectAll.Name = "buttonSelectAll";
			this.buttonSelectAll.Size = new System.Drawing.Size(68, 23);
			this.buttonSelectAll.TabIndex = 19;
			this.buttonSelectAll.Text = "Select All";
			this.buttonSelectAll.Visible = false;
			this.buttonSelectAll.Click += new System.EventHandler(this.buttonSelectAll_Click);
			// 
			// panelInnerNone
			// 
			this.panelInnerNone.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(165)), ((System.Byte)(196)), ((System.Byte)(254)));
			this.panelInnerNone.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelInnerNone.Controls.Add(this.label2);
			this.panelInnerNone.Cursor = System.Windows.Forms.Cursors.Hand;
			this.panelInnerNone.Location = new System.Drawing.Point(16, 16);
			this.panelInnerNone.Name = "panelInnerNone";
			this.panelInnerNone.Size = new System.Drawing.Size(124, 182);
			this.panelInnerNone.TabIndex = 18;
			this.panelInnerNone.Click += new System.EventHandler(this.panelInnerNone_Click);
			this.panelInnerNone.DoubleClick += new System.EventHandler(this.panelInnerNone_DoubleClick);
			// 
			// label2
			// 
			this.label2.Cursor = System.Windows.Forms.Cursors.Hand;
			this.label2.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.Black;
			this.label2.Location = new System.Drawing.Point(6, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(113, 51);
			this.label2.TabIndex = 0;
			this.label2.Text = "Please select plate";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.label2.Click += new System.EventHandler(this.label2_Click);
			this.label2.DoubleClick += new System.EventHandler(this.label2_DoubleClick);
			// 
			// panelInner1536
			// 
			this.panelInner1536.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelInner1536.BackgroundImage")));
			this.panelInner1536.Location = new System.Drawing.Point(16, 16);
			this.panelInner1536.Name = "panelInner1536";
			this.panelInner1536.Size = new System.Drawing.Size(124, 182);
			this.panelInner1536.TabIndex = 17;
			this.panelInner1536.Click += new System.EventHandler(this.panelRowClickSenser_Click);
			this.panelInner1536.DoubleClick += new System.EventHandler(this.panelRowClickSenser_Click);
			// 
			// panelInner384
			// 
			this.panelInner384.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelInner384.BackgroundImage")));
			this.panelInner384.Location = new System.Drawing.Point(16, 16);
			this.panelInner384.Name = "panelInner384";
			this.panelInner384.Size = new System.Drawing.Size(124, 182);
			this.panelInner384.TabIndex = 16;
			this.panelInner384.Click += new System.EventHandler(this.panelRowClickSenser_Click);
			this.panelInner384.DoubleClick += new System.EventHandler(this.panelRowClickSenser_Click);
			// 
			// panelInner96
			// 
			this.panelInner96.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelInner96.BackgroundImage")));
			this.panelInner96.Location = new System.Drawing.Point(16, 16);
			this.panelInner96.Name = "panelInner96";
			this.panelInner96.Size = new System.Drawing.Size(124, 182);
			this.panelInner96.TabIndex = 14;
			this.panelInner96.Click += new System.EventHandler(this.panelRowClickSenser_Click);
			this.panelInner96.DoubleClick += new System.EventHandler(this.panelRowClickSenser_Click);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(192, 81);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(39, 23);
			this.label1.TabIndex = 13;
			this.label1.Text = "Wells";
			// 
			// textBoxRows
			// 
			this.textBoxRows.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textBoxRows.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxRows.ForeColor = System.Drawing.Color.Black;
			this.textBoxRows.Location = new System.Drawing.Point(240, 64);
			this.textBoxRows.Multiline = true;
			this.textBoxRows.Name = "textBoxRows";
			this.textBoxRows.ReadOnly = true;
			this.textBoxRows.Size = new System.Drawing.Size(142, 64);
			this.textBoxRows.TabIndex = 12;
			this.textBoxRows.TabStop = false;
			this.textBoxRows.Text = "";
			this.textBoxRows.Visible = false;
			this.textBoxRows.TextChanged += new System.EventHandler(this.textBoxRows_TextChanged);
			// 
			// buttonPlateSelector
			// 
			this.buttonPlateSelector.Location = new System.Drawing.Point(358, 11);
			this.buttonPlateSelector.Name = "buttonPlateSelector";
			this.buttonPlateSelector.Size = new System.Drawing.Size(24, 22);
			this.buttonPlateSelector.TabIndex = 0;
			this.buttonPlateSelector.Text = "...";
			this.buttonPlateSelector.Click += new System.EventHandler(this.buttonPlateSelector_Click);
			// 
			// textBoxPlateName
			// 
			this.textBoxPlateName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textBoxPlateName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textBoxPlateName.ForeColor = System.Drawing.Color.Black;
			this.textBoxPlateName.Location = new System.Drawing.Point(240, 12);
			this.textBoxPlateName.Name = "textBoxPlateName";
			this.textBoxPlateName.ReadOnly = true;
			this.textBoxPlateName.Size = new System.Drawing.Size(110, 21);
			this.textBoxPlateName.TabIndex = 7;
			this.textBoxPlateName.TabStop = false;
			this.textBoxPlateName.Text = "";
			this.textBoxPlateName.TextChanged += new System.EventHandler(this.textBoxPlateName_TextChanged);
			// 
			// label4
			// 
			this.label4.BackColor = System.Drawing.Color.Transparent;
			this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label4.Location = new System.Drawing.Point(193, 13);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(39, 23);
			this.label4.TabIndex = 6;
			this.label4.Text = "Plate";
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.SystemColors.Control;
			this.panel2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel2.BackgroundImage")));
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(400, 190);
			this.panel2.TabIndex = 0;
			// 
			// PlateCard
			// 
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panel2);
			this.Name = "PlateCard";
			this.Size = new System.Drawing.Size(400, 256);
			this.Load += new System.EventHandler(this.PlateCard_Load);
			this.panel1.ResumeLayout(false);
			this.panelInnerNone.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public void ValidateAll()
		{
			foreach( PlateRow row in m_PlateRowsArray )
			{
				if( row.m_bSelected )
					return;
			}
			throw new Exception( "You have to select at least one row/column." );
		}

		public void ValidateAll( bool loose )
		{
			if( !loose ) ValidateAll();
		}

		public int GetPlateType()
		{			
			return Convert.ToInt32(m_PlateProperties.strPlateType);
		}

		public double GetPlateWellDepth()
		{
			return Convert.ToDouble(m_PlateProperties.strPlateDepth);
		}

		public double GetPlateMaxVolume()
		{
			return Convert.ToDouble(m_PlateProperties.strPlateMaxVolume);
		}

		private void SelectAllRows()
		{
			AllowedToChangeWellType( m_allowedToChangeWellType );
			//RowRb.Enabled = true;
			//ColumnRb.Enabled = true;

			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
			int left = 11;
			int width = 168;
			if( nPlateType > 3 )
			{
				nPlateType -= 3;
				left = 1;
				width = 112;
			}

			// create rows for display and fill array
			for (int i = 0; i < m_nMaxRows; i++)
			{
				UserControl uc = null;
				switch (nPlateType)
				{
					case 1: // 96
						uc = new OneRow96Card();
						uc.Left = left;
						uc.Top = i * 14 + 14;
						uc.Parent = panelInner96;
						break;
					case 2: // 384
						uc = new OneRow384Card();
						uc.Left = left;
						uc.Top = i * 7 + 14;
						uc.Parent = panelInner384;
						break;
					case 3: // 1536
						uc = new OneRow1536Card();
						uc.Left = left;
						
						uc.Top = 14 + (i * 4);
						if (i > 0)
						{
							if ((i % 2) != 0)
							{
								uc.Top -= (i + 1) / 2;
							}
							else
							{
								uc.Top -= i / 2;
							}
						}

						uc.Parent = panelInner1536;
						break;
				}
				uc.Width = width;

				PlateRow pr = new PlateRow(true, uc);
				m_PlateRowsArray.Add(pr);
			}

			if( RowRb.Checked )
			{
				DisplayTextOfSelectedRows();
			}
			else
			{
				DisplayTextOfSelectedColumns();
			}
		}

//		private void SelectAllColumns()
//		{
//			RowRb.Enabled = true;
//			ColumnRb.Enabled = true;
//			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
//
//			// create rows for display and fill array
//			for (int i = 0; i < m_nMaxColumns; i++)
//			{
//				UserControl uc = null;
//				switch (nPlateType)
//				{
//					case 1:
//					case 4: // 96
//						uc = new OneCol96Card();
//						uc.Left = 11 + (i*14);
//						uc.Top = 14;
//						uc.Parent = panelInner96;
//						break;
//					case 2:
//					case 5: // 384
//						uc = new OneCol384Card();
//						uc.Left = 11 + (i*7);
//						uc.Top = 14;
//						uc.Parent = panelInner384;
//						break;
//					case 3:
//					case 6: // 1536
//						uc = new OneCol1536Card();
//						uc.Left = 11 + (i*4);
//						
//						uc.Top = 14;
//						if (i > 0)
//						{
//							if ((i % 2) != 0)
//							{
//								uc.Left -= (i + 1) / 2;
//								//uc.Top -= (i + 1) / 2;
//							}
//							else
//							{
//								uc.Left -= i / 2;
//								//uc.Top -= i / 2;
//							}
//						}
//
//						uc.Parent = panelInner1536;
//						break;
//				}
//
//				PlateRow pr = new PlateRow(true, uc);
//				m_PlateColumnsArray.Add(pr);
//			}
//
//			DisplayTextOfSelectedColumns();
//		}

		private void Clear()
		{
			textBoxRows.Text = "";
			m_clearAll = true;
			RedrawCurrentPlate(false);
		}

		private void DisplaySelectedRows()
		{
			AllowedToChangeWellType( m_allowedToChangeWellType );
			//RowRb.Enabled = true;
			//ColumnRb.Enabled = true;

			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
			int left = 11;
			int width = 168;
			if( nPlateType > 3 )
			{
				nPlateType -= 3;
				left = 1;
				width = 112;
				try
				{
					textBoxRows.Text = Utilities.ColumnsToRows( textBoxRows.Text );
				}
				catch{}
			}
			
			// build m_PlateRowsArray from text string in text box
			for (int i = 0; i < m_nMaxRows; i++)
			{
				UserControl uc = null;
				switch (nPlateType)
				{
					case 1: // 96
						uc = new OneRow96Card();
						uc.Left = left;
						uc.Top = i * 14 + 14;
						uc.Parent = panelInner96;
						break;
					case 2: // 384
						uc = new OneRow384Card();
						uc.Left = left;
						uc.Top = i * 7 + 14;
						uc.Parent = panelInner384;
						break;
					case 3: // 1536
						uc = new OneRow1536Card();
						uc.Left = left;
						
						uc.Top = 14 + (i * 4);
						if (i > 0)
						{
							if ((i % 2) != 0)
							{
								uc.Top -= (i + 1) / 2;
							}
							else
							{
								uc.Top -= i / 2;
							}
						}

						uc.Parent = panelInner1536;
						break;
				}
				uc.Width = width;

				// ... here is where it is determined it's visual/selected state
				string strRowCharacterString = Utilities.GetRowCharacterString(i);
				
				bool bFound = false;
				if (strRowCharacterString.Length == 1)
				{
					for (int iChar = 0; iChar < textBoxRows.Text.Length; iChar++)
					{
						if (textBoxRows.Text[iChar] == strRowCharacterString[0])
						{
							if (iChar < textBoxRows.Text.Length - 1)
							{
								if (textBoxRows.Text[iChar + 1] != strRowCharacterString[0])
								{
									bFound = true;
								}
							}
							else
							{
								bFound = true;
							}
							break;
						}
					}
				}
				else
				{
					if (-1 != textBoxRows.Text.IndexOf(strRowCharacterString))
					{
						bFound = true;
					}
				}

				if (bFound)
				{
					PlateRow pr = new PlateRow(true, uc);
					m_PlateRowsArray.Add(pr);
				}
				else
				{
					uc.Hide();
					PlateRow pr = new PlateRow(false, uc);
					m_PlateRowsArray.Add(pr);
				}
			}

			if( RowRb.Checked )
			{
				DisplayTextOfSelectedRows();
			}
			else
			{
				DisplayTextOfSelectedColumns();
			}
		}

//		private void DisplaySelectedColumns()
//		{
//			RowRb.Enabled = true;
//			ColumnRb.Enabled = true;
//
//			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
//			
//			// build m_PlateRowsArray from text string in text box
//			for (int i = 0; i < m_nMaxColumns; i++)
//			{
//				UserControl uc = null;
//				switch (nPlateType)
//				{
//					case 4: // 96
//						uc = new OneCol96Card();
//						uc.Left = 11 + (i*14);
//						uc.Top = 14;
//						uc.Parent = panelInner96;
//						break;
//					case 5: // 384
//						uc = new OneCol384Card();
//						uc.Left = 11 + (i*7);
//						uc.Top = 14;
//						uc.Parent = panelInner384;
//						break;
//					case 6: // 1536
//						uc = new OneCol1536Card();
//						uc.Left = 11 + (i*4);
//						
//						uc.Top = 14;
//						if (i > 0)
//						{
//							if ((i % 2) != 0)
//							{
//								uc.Left -= (i + 1) / 2;
//								//uc.Top -= (i + 1) / 2;
//							}
//							else
//							{
//								uc.Left -= i / 2;
//								//uc.Top -= i / 2;
//							}
//						}
//
//						uc.Parent = panelInner1536;
//						break;
//				}
//
//				// ... here is where it is determined it's visual/selected state
//				
//				bool bFound = false;
//				string pattern = string.Format( @"\b{0}\b", i+1 );
//				Match m = Regex.Match( textBoxRows.Text, pattern );
//				bFound = m.Success;
//
//				if (bFound)
//				{
//					PlateRow pr = new PlateRow(true, uc);
//					m_PlateColumnsArray.Add(pr);
//				}
//				else
//				{
//					uc.Hide();
//					PlateRow pr = new PlateRow(false, uc);
//					m_PlateColumnsArray.Add(pr);
//				}
//			}
//
//			DisplayTextOfSelectedColumns();
//		}

		

		private void DisplayTextOfSelectedRows()
		{
			string strSelected = "";
			for (int i = 0; i < m_nMaxRows; i++)
			{
				PlateRow pr = (PlateRow)m_PlateRowsArray[i];

				if (pr.m_bSelected)
				{
					char c = 'A';
					c += (char)i;

					if (i < 26)
					{
						strSelected += c;
					}
					else
					{
						switch (i)
						{
							case 26:
								strSelected += "AA";
								break;
							case 27:
								strSelected += "BB";
								break;
							case 28:
								strSelected += "CC";
								break;
							case 29:
								strSelected += "DD";
								break;
							case 30:
								strSelected += "EE";
								break;
							case 31:
								strSelected += "FF";
								break;
							case 32:
								strSelected += "GG";
								break;
							case 33:
								strSelected += "HH";
								break;
							case 34:
								strSelected += "II";
								break;
							case 35:
								strSelected += "JJ";
								break;
							case 36:
								strSelected += "KK";
								break;
							case 37:
								strSelected += "LL";
								break;
							case 38:
								strSelected += "MM";
								break;
							case 39:
								strSelected += "NN";
								break;
							case 40:
								strSelected += "OO";
								break;
							case 41:
								strSelected += "PP";
								break;
							case 42:
								strSelected += "QQ";
								break;
							case 43:
								strSelected += "RR";
								break;
							case 44:
								strSelected += "SS";
								break;
							case 45:
								strSelected += "TT";
								break;
							case 46:
								strSelected += "UU";
								break;
							case 47:
								strSelected += "VV";
								break;
						}
					}
				}
			}

			textBoxRows.Text = strSelected;
			textBoxWells.Text = Utilities.CardDisplayRowString( strSelected, m_nMaxColumns );
		}

		private void DisplayTextOfSelectedColumns()
		{
			string strSelected = "";
			ArrayList numberList = new ArrayList();
			for (int i = 0; i < m_PlateRowsArray.Count; i++)
			{
				PlateRow pr = (PlateRow)m_PlateRowsArray[i];

				if (pr.m_bSelected)
				{
					numberList.Add( i+1 );

					char c = '1';
					c += (char)i;

					if (i < 9)
					{
						strSelected += c;
						strSelected += " ";
					}
					else
					{
						int num = i+1;
						strSelected += " " + num.ToString();
					}
				}
			}
			
			textBoxRows.Text = strSelected;
			textBoxWells.Text = Utilities.CardDisplayColumnString((int[])numberList.ToArray(typeof(int)), m_nMaxRows);
		}

		public void panelRowClickSenser_Click(object sender, System.EventArgs e)
		{
			Point point = panelInner384.PointToClient(Cursor.Position);

			if (point.Y < 14)
			{
				return;
			}
			
			int nPlateType = Convert.ToInt16(m_PlateProperties.strPlateType);
			if( nPlateType > 3 ) nPlateType -= 3;
			int nRow = 0;
			switch (nPlateType)
			{
				case 1: // 96
					nRow = (point.Y - 14) / 14;
					break;
				case 2: // 384
					nRow = (point.Y - 14) / 7;
					break;
				case 3: // 1536
					double dRow = (point.Y - 14) / 3.5;
					//nRow = Convert.ToInt32(dRow);
					nRow = (int)dRow;
//					if (nRow > 31)
//					{
//						nRow = 31;
//					}
					break;
			}

			// stepping = 2 for 384 and 1536 plates
			if (nPlateType == 2 || nPlateType == 3)
			{
				if ((nRow % 2) != 0)
				{
					nRow--;
				}
			}

			// special - somewhat hackish...
			if (nPlateType == 1) // 96
			{
				PlateRow pr = (PlateRow)m_PlateRowsArray[nRow];
				
				if (pr.m_bSelected)
				{
					pr.m_uc.Hide();
					pr.m_bSelected = false;
				}
				else
				{
					pr.m_uc.Show();
					pr.m_bSelected = true;
				}
				
				if( RowRb.Checked )
				{
					DisplayTextOfSelectedRows();
				}
				else
				{
					DisplayTextOfSelectedColumns();
				}

				return;
			}

			// extra test
			bool[] bValidArray = new bool[m_nMaxRows];
			for (int i = 0; i < m_nMaxRows; i++)
			{
				bValidArray[i] = ((PlateRow)m_PlateRowsArray[i]).m_bSelected;
			}
			bValidArray[nRow] = !bValidArray[nRow];
			if (nRow < m_nMaxRows - 1)
			{
				bValidArray[nRow + 1] = !bValidArray[nRow + 1];
			}
			for (int i = 0; i < m_nMaxRows; i++)
			{
				bool bValid = true;
				if (bValidArray[i])
				{
					bValid = false;
					if (i < m_nMaxRows - 1)
					{
						if (bValidArray[i + 1])
						{
							bValid = true;
							i++;
						}
					}
				}
				if (!bValid)
				{
					return;
				}
			}

			if (nRow != m_nMaxRows - 1)
			{
				PlateRow pr = (PlateRow)m_PlateRowsArray[nRow];
				PlateRow prNext = (PlateRow)m_PlateRowsArray[nRow + 1];
				if (pr.m_bSelected == prNext.m_bSelected)
				{
					if (pr.m_bSelected)
					{
						pr.m_uc.Hide();
						pr.m_bSelected = false;
						prNext.m_uc.Hide();
						prNext.m_bSelected = false;
					}
					else
					{
						pr.m_uc.Show();
						pr.m_bSelected = true;
						prNext.m_uc.Show();
						prNext.m_bSelected = true;
					}
				}

				if( RowRb.Checked )
				{
					DisplayTextOfSelectedRows();
				}
				else
				{
					DisplayTextOfSelectedColumns();
				}
			}
		}

//		public void panelColClickSenser_Click(object sender, System.EventArgs e)
//		{
//			Point point = panelInner384.PointToClient(Cursor.Position);
//
//			if (point.X < 11)
//			{
//				return;
//			}
//			
//			int nPlateType = Convert.ToInt16(m_PlateProperties.strPlateType);
//			int nCol = 0;
//			switch (nPlateType)
//			{
//				case 4: // 96
//					nCol = (point.X - 11) / 14;
//					break;
//				case 5: // 384
//					nCol = (point.X - 11) / 7;
//					break;
//				case 6: // 1536
//					//double dCol = (point.X - 11) / 7;
//					double dCol = (point.X - 11) / 3.5;
//					//nRow = Convert.ToInt32(dRow);
//					nCol = (int)dCol;
//					if (nCol > 47)
//					{
//						nCol = 47;
//					}
//					break;
//			}
//
//			// stepping = 2 for 384 and 1536 plates
//			if (nPlateType == 5 || nPlateType == 6)
//			{
//				if ((nCol % 2) != 0)
//				{
//					nCol--;
//				}
//			}
//
//			// special - somewhat hackish...
//			if (nPlateType == 4) // 96
//			{
//				PlateRow pr = (PlateRow)m_PlateColumnsArray[nCol];
//				
//				if (pr.m_bSelected)
//				{
//					pr.m_uc.Hide();
//					pr.m_bSelected = false;
//				}
//				else
//				{
//					pr.m_uc.Show();
//					pr.m_bSelected = true;
//				}
//				
//				DisplayTextOfSelectedColumns();
//
//				return;
//			}
//
//			// extra test
//			bool[] bValidArray = new bool[m_nMaxColumns];
//			for (int i = 0; i < m_nMaxColumns; i++)
//			{
//				bValidArray[i] = ((PlateRow)m_PlateColumnsArray[i]).m_bSelected;
//			}
//			bValidArray[nCol] = !bValidArray[nCol];
//			if (nCol < m_nMaxColumns - 1)
//			{
//				bValidArray[nCol + 1] = !bValidArray[nCol + 1];
//			}
//			for (int i = 0; i < m_nMaxColumns; i++)
//			{
//				bool bValid = true;
//				if (bValidArray[i])
//				{
//					bValid = false;
//					if (i < m_nMaxColumns - 1)
//					{
//						if (bValidArray[i + 1])
//						{
//							bValid = true;
//							i++;
//						}
//					}
//				}
//				if (!bValid)
//				{
//					return;
//				}
//			}
//
//			if (nCol != m_nMaxColumns - 1)
//			{
//				PlateRow pr = (PlateRow)m_PlateColumnsArray[nCol];
//				PlateRow prNext = (PlateRow)m_PlateColumnsArray[nCol + 1];
//				if (pr.m_bSelected == prNext.m_bSelected)
//				{
//					if (pr.m_bSelected)
//					{
//						pr.m_uc.Hide();
//						pr.m_bSelected = false;
//						prNext.m_uc.Hide();
//						prNext.m_bSelected = false;
//					}
//					else
//					{
//						pr.m_uc.Show();
//						pr.m_bSelected = true;
//						prNext.m_uc.Show();
//						prNext.m_bSelected = true;
//					}
//				}
//
//				DisplayTextOfSelectedColumns();
//			}
//		}

		private void buttonPlateSelector_Click(object sender, System.EventArgs e)
		{
			plateSelectorForm PSF = new plateSelectorForm();
			
			programForm pf = (programForm)Parent.Parent;

			mainForm mf = (mainForm)pf.MdiParent;

			m_strOldFormat = m_PlateProperties.strPlateType;
			DialogResult DR = PSF._ShowDialog(ref m_PlateProperties, mf);

			string strPreviousName = textBoxPlateName.Text;
			if (DR == DialogResult.OK)
			{
				m_newCard = true;
				pf.programPanel.Hide();
				SetRowColumn();
				if( !Utilities.SupportColumns )
				{					
					buttonSelectAll.Show();
					buttonClear.Show();
					buttonPlateInfo.Show();
				}
				else
				{
					this.label2.Text = "Please select rows or columns";
				}

				// special case. Same plate selected, but parametres could be changed in plate library.
				if (strPreviousName == textBoxPlateName.Text)
				{
					pf.CardChanged(this);
					pf.PlateCardNameChanged(false);
				}

				AllowedToChangeWellType( true );

				RefreshCards();
				//if( m_prevCardType != -1 )
				if( m_prevPrevCardType == m_prevCardType )
				{
					pf.ValidateAllCards( true );
				}
				//SetRowColumn();

				
				pf.programPanel.Show();
			}
		}

		public void RedrawCurrentPlate(bool bSelectAll)
		{
			this.Hide();
			this.SuspendLayout();
			textBoxPlateName.Text = m_PlateProperties.strPlateName;
				

			if( RowRb.Checked || ColumnRb.Checked )
			{
				// hide all inner panels
				panelInner96.Hide();
				panelInner384.Hide();
				panelInner1536.Hide();
				panelInnerNone.Hide();

				// show proper inner panel
				int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
				if( nPlateType < 4 )
				{
					m_update = true;
					RowRb.Checked = true;
					m_update = false;
				}

				if( nPlateType > 3 && !bSelectAll )
				{
					m_update = true;
					ColumnRb.Checked = true;
					m_update = false;
				}

				switch (nPlateType)
				{
					case 1: // 96
						panelInner96.Show();
						m_nMaxRows = 8; // A - H
						m_nMaxColumns = 12; // 1-12
						break;
					case 2: // 384
						panelInner384.Show();
						m_nMaxRows = 16; // A - P
						m_nMaxColumns = 24; // 1-24
						break;
					case 3: // 1536
						panelInner1536.Show();
						m_nMaxRows = 32; // A - FF
						m_nMaxColumns = 48; // 1-48
						break;
					case 4: // 96 col
						panelInner96.Show();
						m_nMaxRows = 12;
						m_nMaxColumns = 8;
						break;
					case 5: // 384 col
						panelInner384.Show();
						m_nMaxRows = 24;
						m_nMaxColumns = 16;
						break;
					case 6: // 1536 col
						panelInner1536.Show();
						m_nMaxRows = 48;
						m_nMaxColumns = 32;
						break;
				}

				//record previous settings to new card if same type
				bool[] setSelect = new bool[0];
				if( m_prevCardType == nPlateType && !bSelectAll && !m_clearAll )
				{
					setSelect = new bool[m_PlateRowsArray.Count];
					for( int i=0; i<setSelect.Length; i++ )
					{
						setSelect[i] = ((PlateRow)m_PlateRowsArray[i]).m_bSelected;
					}
				}

				// empty PlateRowsArray
				for (int i = 0; i < m_PlateRowsArray.Count; i++)
				{
					PlateRow pr = (PlateRow)m_PlateRowsArray[i];
					pr.m_uc.Dispose();	
				}
				m_PlateRowsArray.Clear();

				if ( (m_prevPrevCardType != -1 && m_prevCardType != m_prevPrevCardType && !m_clearAll) || m_newCard || bSelectAll)
				{
					m_newCard = false;
					SelectAllRows();
				}
				else
				{
					DisplaySelectedRows();
				}

				m_clearAll = false;

				// copy previous settings to new card
				if( setSelect.Length > 0 )
				{
					for( int i=0; i<setSelect.Length; i++ )
					{
						PlateRow pr = (PlateRow)m_PlateRowsArray[i];
						if( setSelect[i] )
						{
							pr.m_uc.Show();
							pr.m_bSelected = true;
						}
						else
						{
							pr.m_uc.Hide();
							pr.m_bSelected = false;
						}
					}
				}

				if( RowRb.Checked )
				{
					DrawRowCard();
				}
				else
				{
					DrawColumnCard();
				}

				m_prevPrevCardType = m_prevCardType;
				m_prevCardType = nPlateType;
			}
			if( RowRb.Checked )
			{
				DisplayTextOfSelectedRows();
			}
			else
			{
				DisplayTextOfSelectedColumns();
			}
			this.ResumeLayout();
			this.Show();			
		}

		private void textBoxPlateName_TextChanged(object sender, System.EventArgs e)
		{
		}

		void RefreshCards()
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
//			if (m_strOldFormat == m_PlateProperties.strPlateType)
//			{
//				pf.PlateCardNameChanged(false);
//			}
//			else
			{
				bool restore = false;
				if( !m_update )
				{
					pf.programPanel.Hide();
					if( pf.Minimized )
					{				
						restore = true;
					}
					else
					{
						pf.MinMax(); // minimize all cards first
					}
					pf.MinMax(); // maximize all cards
				}

				int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
				if( m_prevPrevCardType != nPlateType  )
				{
					pf.PlateCardNameChanged(true);
				}
				else
				{
					pf.PlateCardNameChanged(false);
				}

				if( !m_update )
				{
					if( restore )
					{
						pf.MinMax(); // minimize if that was the state
					}
					pf.programPanel.Show();
				}
			}

		}

		private void textBoxRows_TextChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void panelInnerNone_Click(object sender, System.EventArgs e)
		{
			buttonPlateSelector_Click(sender, e);
		}

		private void panelInnerNone_DoubleClick(object sender, System.EventArgs e)
		{
			buttonPlateSelector_Click(sender, e);
		}

		private void label2_Click(object sender, System.EventArgs e)
		{
			buttonPlateSelector_Click(sender, e);
		}

		private void label2_DoubleClick(object sender, System.EventArgs e)
		{
			buttonPlateSelector_Click(sender, e);
		}

		private void buttonSelectAll_Click(object sender, System.EventArgs e)
		{
			RedrawCurrentPlate(true);
		}

		private void buttonClear_Click(object sender, System.EventArgs e)
		{
			Clear();
		}

		private void buttonPlateInfo_Click(object sender, System.EventArgs e)
		{
			PlateInfoForm PIF = new PlateInfoForm();
			PIF._ShowDialog(m_PlateProperties.strPlateOffset, m_PlateProperties.strPlateDbwc, m_PlateProperties.strPlateOffset2, m_PlateProperties.strPlateDbwc2,  m_PlateProperties.strPlateHeight, m_PlateProperties.strPlateDepth, m_PlateProperties.strPlateMaxVolume, m_PlateProperties.loBase, m_PlateProperties.strWellShape, m_PlateProperties.strPlateType, m_PlateProperties.strPlateBottomWellDiameter );
		}

		public void ShowButtons()
		{
			buttonSelectAll.Show();
			buttonClear.Show();
			buttonPlateInfo.Show();
		}

		void SwitchWellType()
		{
			buttonSelectAll.Show();
			buttonClear.Show();
			buttonPlateInfo.Show();


			if( m_update ) return;			
			m_update = true;

			m_newCard = true;

			programForm pf = (programForm)Parent.Parent;
			pf.programPanel.Hide();
			bool restore = false;
			if( pf.Minimized )
			{				
				restore = true;
			}
			else
			{
				pf.MinMax(); // minimize all cards first
			}
			pf.MinMax(); // maximize all cards

			SetRowColumn();
			
			//			pf.ValidateAllCards();
			pf.CardChanged(this);
			pf.PlateCardNameChanged(true);

			int offset = this.panel1.Height - this.panel2.Height;
			if( RowRb.Checked )
			{
				offset = -offset;
			}

			pf.RepositionCards( offset, 0, true );

			if( restore )
			{
				pf.MinMax(); // minimize if that was the state
			}
			m_update = false;
			pf.programPanel.Show();
		}

		void SetRowColumn()
		{
			int plateType = GetPlateType();

			if( ColumnRb.Checked && plateType < 4 )
			{
				plateType += 3;
			}
			if( RowRb.Checked && plateType > 3 )
			{
				plateType -= 3;
			}

			m_PlateProperties.strPlateType = plateType.ToString();
			RedrawCurrentPlate(false);
		}

		private void PlateCard_Load(object sender, System.EventArgs e)
		{
			this.Hide();
			programForm pf = (programForm)Parent.Parent;
			mainForm mf = (mainForm)pf.MdiParent;
			if( !Utilities.SupportColumns )
			{
				RowRb.Visible = false;				
				ColumnRb.Visible = false;
				//DrawRowCard();
				RowRb.Checked = true;
				panelInnerNone.Show();
			}
			this.Show();
		}

		public void AllowedToChangeWellType( bool change )
		{
			m_allowedToChangeWellType = change;
			if( !m_allowedToChangeWellType )
			{
				RowRb.Enabled = false;
				ColumnRb.Enabled = false;
			}
			else
			{
				RowRb.Enabled = true;
				ColumnRb.Enabled = true;
			}
		}

		void DrawRowCard()
		{
			this.panel1.Hide();
			ArrayList list = new ArrayList();
			list.AddRange( this.panel1.Controls );
			this.panel2.Controls.AddRange( (Control[])list.ToArray(typeof(Control)) );
			this.Size = new System.Drawing.Size(400, 190);
			this.panelInnerNone.Size = new System.Drawing.Size(182, 126);
			this.panelInnerNone.Left = 5;
			this.panelInner1536.Size = new System.Drawing.Size(182, 126);
			this.panelInner1536.BackgroundImage = Image.FromFile( "images/1536.bmp" );
			this.panelInner1536.Left = 5;
			this.panelInner384.Size = new System.Drawing.Size(182, 126);
			this.panelInner384.BackgroundImage = Image.FromFile( "images/384.bmp" );
			this.panelInner384.Left = 5;
			this.panelInner96.Size = new System.Drawing.Size(182, 126);
			this.panelInner96.BackgroundImage = Image.FromFile( "images/96.bmp" );
			this.panelInner96.Left = 5;

			this.ColumnRb.Location = new System.Drawing.Point(75, 147);
			this.RowRb.Location = new System.Drawing.Point(22, 147);
			this.label2.Location = new System.Drawing.Point(8, 40);
			this.label2.Size = new System.Drawing.Size(168, 40);

			this.textBoxWells.Size = new System.Drawing.Size(142, 64);
			this.buttonSelectAll.Location = new System.Drawing.Point(240, 137);
			this.buttonClear.Location = new System.Drawing.Point(314, 137);

			this.panel2.Show();
		}

		void DrawColumnCard()
		{
			this.panel2.Hide();
			ArrayList list = new ArrayList();
			list.AddRange( this.panel2.Controls );
			this.panel1.Controls.AddRange( (Control[])list.ToArray(typeof(Control)) );
			this.Size = new System.Drawing.Size(400, 256);
			this.panelInnerNone.Size = new System.Drawing.Size(124, 182);
			this.panelInnerNone.Left = 16;
			this.panelInner1536.Size = new System.Drawing.Size(124, 182);
			this.panelInner1536.BackgroundImage = Image.FromFile( "images/1536-col.bmp" );
			this.panelInner1536.Left = 16;
			this.panelInner384.Size = new System.Drawing.Size(124, 182);
			this.panelInner384.BackgroundImage = Image.FromFile( "images/384-col.bmp" );
			this.panelInner384.Left = 16;
			this.panelInner96.Size = new System.Drawing.Size(124, 182);
			this.panelInner96.BackgroundImage = Image.FromFile( "images/96-col.bmp" );
			this.panelInner96.Left = 16;
			this.ColumnRb.Location = new System.Drawing.Point(75, 203);
			this.RowRb.Location = new System.Drawing.Point(22, 203);
			this.label2.Location = new System.Drawing.Point(6, 40);
			this.label2.Size = new System.Drawing.Size(113, 51);

			this.textBoxWells.Size = new System.Drawing.Size(142, 90);
			this.buttonSelectAll.Location = new System.Drawing.Point(240, 163);
			this.buttonClear.Location = new System.Drawing.Point(314, 163);

			this.panel1.Show();
		}

		private void RowRb_CheckedChanged(object sender, System.EventArgs e)
		{
			if( !RowRb.Checked ) return;
			programForm pf = (programForm)Parent.Parent;
			if( !pf.m_changeAllowed )
			{
				if( !m_prevRowState )
					ColumnRb.Checked = true;
				pf.m_changeAllowed = true;
				return;
			}
			m_prevRowState = true;
			SwitchWellType();
		}

		private void ColumnRb_CheckedChanged(object sender, System.EventArgs e)
		{
			if( !ColumnRb.Checked ) return;
			programForm pf = (programForm)Parent.Parent;
			if( !pf.m_changeAllowed )
			{
				if( m_prevRowState )
					RowRb.Checked = true;
				pf.m_changeAllowed = true;				
				return;
			}
			m_prevRowState = false;
			SwitchWellType();
		}
	}
}
