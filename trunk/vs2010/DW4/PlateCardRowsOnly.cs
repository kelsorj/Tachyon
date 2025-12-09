using System;
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
	public class PlateCardRowsOnly : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		public System.Windows.Forms.TextBox textBoxRows;
		private System.Windows.Forms.Label label1;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Panel panelInner96;
		private System.Windows.Forms.Panel panelInner384;
		private System.Windows.Forms.Panel panelInner1536;
		private System.Windows.Forms.Panel panelInnerNone;

		public PlateProperties m_PlateProperties = new PlateProperties();
		public System.Windows.Forms.Label labelProgramStep;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.Button buttonSelectAll;
		ArrayList m_PlateRowsArray = new ArrayList();
		//ArrayList m_PlateColumnsArray = new ArrayList();
		int m_nMaxRows = 0;
		public System.Windows.Forms.TextBox textBoxWells;
		private System.Windows.Forms.Label minimizeLabel;
		int m_nMaxColumns = 0;
		private System.Windows.Forms.PictureBox maximize;
		private System.Windows.Forms.PictureBox minimize;
		bool m_rowMode = true;
		private System.Windows.Forms.PictureBox icon;
		bool m_minimize = true;
		int m_prevCardType = -1;
		bool[] m_setSelect;
		bool m_clearAll = false;

		public PlateCardRowsOnly()
		{
			InitializeComponent();
			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PlateCardRowsOnly));
			this.panel1 = new System.Windows.Forms.Panel();
			this.minimizeLabel = new System.Windows.Forms.Label();
			this.textBoxWells = new System.Windows.Forms.TextBox();
			this.buttonClear = new System.Windows.Forms.Button();
			this.buttonSelectAll = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.panelInnerNone = new System.Windows.Forms.Panel();
			this.panelInner1536 = new System.Windows.Forms.Panel();
			this.panelInner384 = new System.Windows.Forms.Panel();
			this.panelInner96 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxRows = new System.Windows.Forms.TextBox();
			this.maximize = new System.Windows.Forms.PictureBox();
			this.minimize = new System.Windows.Forms.PictureBox();
			this.icon = new System.Windows.Forms.PictureBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.labelProgramStep = new System.Windows.Forms.Label();
			this.deleteButton = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
			this.panel1.Controls.Add(this.minimizeLabel);
			this.panel1.Controls.Add(this.textBoxWells);
			this.panel1.Controls.Add(this.buttonClear);
			this.panel1.Controls.Add(this.buttonSelectAll);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.panelInnerNone);
			this.panel1.Controls.Add(this.panelInner1536);
			this.panel1.Controls.Add(this.panelInner384);
			this.panel1.Controls.Add(this.panelInner96);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.textBoxRows);
			this.panel1.Controls.Add(this.maximize);
			this.panel1.Controls.Add(this.minimize);
			this.panel1.Controls.Add(this.icon);
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(400, 256);
			this.panel1.TabIndex = 0;
			// 
			// minimizeLabel
			// 
			this.minimizeLabel.BackColor = System.Drawing.Color.Transparent;
			this.minimizeLabel.Location = new System.Drawing.Point(48, 24);
			this.minimizeLabel.Name = "minimizeLabel";
			this.minimizeLabel.Size = new System.Drawing.Size(341, 23);
			this.minimizeLabel.TabIndex = 24;
			this.minimizeLabel.Visible = false;
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
			this.textBoxWells.TabIndex = 23;
			this.textBoxWells.TabStop = false;
			this.textBoxWells.Text = "";
			// 
			// buttonClear
			// 
			this.buttonClear.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonClear.Location = new System.Drawing.Point(314, 137);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.Size = new System.Drawing.Size(68, 23);
			this.buttonClear.TabIndex = 22;
			this.buttonClear.Text = "Clear";
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
			// 
			// buttonSelectAll
			// 
			this.buttonSelectAll.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonSelectAll.Location = new System.Drawing.Point(240, 137);
			this.buttonSelectAll.Name = "buttonSelectAll";
			this.buttonSelectAll.Size = new System.Drawing.Size(68, 23);
			this.buttonSelectAll.TabIndex = 21;
			this.buttonSelectAll.Text = "Select All";
			this.buttonSelectAll.Click += new System.EventHandler(this.buttonSelectAll_Click);
			// 
			// label2
			// 
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.Font = new System.Drawing.Font("Verdana", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.Location = new System.Drawing.Point(237, 29);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(142, 27);
			this.label2.TabIndex = 19;
			this.label2.Text = "Select wells";
			// 
			// panelInnerNone
			// 
			this.panelInnerNone.BackColor = System.Drawing.Color.Transparent;
			this.panelInnerNone.Cursor = System.Windows.Forms.Cursors.Hand;
			this.panelInnerNone.Location = new System.Drawing.Point(5, 16);
			this.panelInnerNone.Name = "panelInnerNone";
			this.panelInnerNone.Size = new System.Drawing.Size(182, 126);
			this.panelInnerNone.TabIndex = 18;
			// 
			// panelInner1536
			// 
			this.panelInner1536.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelInner1536.BackgroundImage")));
			this.panelInner1536.Location = new System.Drawing.Point(5, 16);
			this.panelInner1536.Name = "panelInner1536";
			this.panelInner1536.Size = new System.Drawing.Size(182, 126);
			this.panelInner1536.TabIndex = 17;
			this.panelInner1536.Click += new System.EventHandler(this.panelRowClickSenser_Click);
			this.panelInner1536.DoubleClick += new System.EventHandler(this.panelRowClickSenser_Click);
			// 
			// panelInner384
			// 
			this.panelInner384.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelInner384.BackgroundImage")));
			this.panelInner384.Location = new System.Drawing.Point(5, 16);
			this.panelInner384.Name = "panelInner384";
			this.panelInner384.Size = new System.Drawing.Size(182, 126);
			this.panelInner384.TabIndex = 16;
			this.panelInner384.Click += new System.EventHandler(this.panelRowClickSenser_Click);
			this.panelInner384.DoubleClick += new System.EventHandler(this.panelRowClickSenser_Click);
			// 
			// panelInner96
			// 
			this.panelInner96.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelInner96.BackgroundImage")));
			this.panelInner96.Location = new System.Drawing.Point(5, 16);
			this.panelInner96.Name = "panelInner96";
			this.panelInner96.Size = new System.Drawing.Size(182, 126);
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
			// maximize
			// 
			this.maximize.BackColor = System.Drawing.Color.IndianRed;
			this.maximize.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("maximize.BackgroundImage")));
			this.maximize.Location = new System.Drawing.Point(352, 10);
			this.maximize.Name = "maximize";
			this.maximize.Size = new System.Drawing.Size(13, 12);
			this.maximize.TabIndex = 22;
			this.maximize.TabStop = false;
			this.maximize.Visible = false;
			this.maximize.Click += new System.EventHandler(this.maximize_Click);
			// 
			// minimize
			// 
			this.minimize.BackColor = System.Drawing.Color.IndianRed;
			this.minimize.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("minimize.BackgroundImage")));
			this.minimize.Location = new System.Drawing.Point(352, 10);
			this.minimize.Name = "minimize";
			this.minimize.Size = new System.Drawing.Size(13, 12);
			this.minimize.TabIndex = 21;
			this.minimize.TabStop = false;
			this.minimize.Click += new System.EventHandler(this.minimize_Click);
			// 
			// icon
			// 
			this.icon.BackColor = System.Drawing.Color.Transparent;
			this.icon.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("icon.BackgroundImage")));
			this.icon.Location = new System.Drawing.Point(10, 13);
			this.icon.Name = "icon";
			this.icon.Size = new System.Drawing.Size(26, 26);
			this.icon.TabIndex = 25;
			this.icon.TabStop = false;
			this.icon.Visible = false;
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
			// labelProgramStep
			// 
			this.labelProgramStep.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelProgramStep.ForeColor = System.Drawing.Color.White;
			this.labelProgramStep.Location = new System.Drawing.Point(400, 8);
			this.labelProgramStep.Name = "labelProgramStep";
			this.labelProgramStep.Size = new System.Drawing.Size(40, 23);
			this.labelProgramStep.TabIndex = 3;
			this.labelProgramStep.Text = "0";
			this.labelProgramStep.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// deleteButton
			// 
			this.deleteButton.BackColor = System.Drawing.Color.IndianRed;
			this.deleteButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("deleteButton.BackgroundImage")));
			this.deleteButton.Cursor = System.Windows.Forms.Cursors.Default;
			this.deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.deleteButton.Location = new System.Drawing.Point(368, 10);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(13, 12);
			this.deleteButton.TabIndex = 4;
			this.deleteButton.TabStop = false;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// PlateCardRowsOnly
			// 
			this.Controls.Add(this.deleteButton);
			this.Controls.Add(this.labelProgramStep);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panel2);
			this.Name = "PlateCardRowsOnly";
			this.Size = new System.Drawing.Size(440, 256);
			this.Load += new System.EventHandler(this.PlateCard_Load);
			this.panel1.ResumeLayout(false);
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
			if( !loose )
				ValidateAll();
		}

		public int GetPlateType()
		{			
			return Convert.ToInt32(m_PlateProperties.strPlateType);
		}

		public double GetPlateWellDepth()
		{
			return Convert.ToDouble(m_PlateProperties.strPlateDepth);
		}

		private void SelectAllRows()
		{
			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
			int left = 11;
			int width = 168;
			bool row = true;
			if( nPlateType > 3 )
			{
				row = false;
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

			if( row )
			{
				DisplayTextOfSelectedRows();
			}
			else
			{
				DisplayTextOfSelectedColumns();
			}
		}

		public void Clear()
		{
//			textBoxRows.Text = "";
			m_clearAll = true;
			RedrawCurrentPlate(false);
		}

		private void DisplaySelectedRows()
		{
			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);
			int left = 11;
			int width = 168;
			bool row = true;
			if( nPlateType > 3 )
			{
				row = false;
				nPlateType -= 3;
				left = 1;
				width = 112;
				textBoxRows.Text = Utilities.ColumnsToRows( textBoxRows.Text );
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

				PlateRow pr = null;
				if (bFound)
				{
					pr = new PlateRow(true, uc);
				}
				else
				{
					uc.Hide();
					pr = new PlateRow(false, uc);					
				}
				m_PlateRowsArray.Add(pr);

				if( m_setSelect.Length > 0 )
				{
					m_setSelect[i] = pr.m_bSelected;
				}
			}

			if( row )
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

//		private void SelectAllColumns()
//		{
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
			bool row = true;
			if( nPlateType > 3 )
			{
				nPlateType -= 3;
				row = false;
			}
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
				
				if( row )
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

				if( row )
				{
					DisplayTextOfSelectedRows();
				}
				else
				{
					DisplayTextOfSelectedColumns();
				}
			}
		}


		public void RedrawCurrentPlate(bool bSelectAll)
		{
			// get some data from PlateCard (the one and only)
			programForm pf = (programForm)Parent.Parent;
			ProgramGUIElement PGE = (ProgramGUIElement)pf.CardArray[0];
			m_PlateProperties.strPlateType = PGE.platecard_format.ToString();

			// hide all inner panels
			panelInner96.Hide();
			panelInner384.Hide();
			panelInner1536.Hide();
			panelInnerNone.Hide();

			// show proper inner panel
			int nPlateType = Convert.ToInt32(m_PlateProperties.strPlateType);

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

//			//record previous settings to new card if same type
			m_setSelect = new bool[0];
			if( m_prevCardType == nPlateType  && !bSelectAll && !m_clearAll )
			{
				m_setSelect = new bool[m_PlateRowsArray.Count];
				for( int i=0; i<m_setSelect.Length; i++ )
				{
					m_setSelect[i] = ((PlateRow)m_PlateRowsArray[i]).m_bSelected;
				}
			}
			else
			{
				textBoxRows.Text = "";
			}

			m_clearAll = false;

			// empty PlateRowsArray
			for (int i = 0; i < m_PlateRowsArray.Count; i++)
			{
				PlateRow pr = (PlateRow)m_PlateRowsArray[i];
				pr.m_uc.Dispose();	
			}
			m_PlateRowsArray.Clear();

			if (bSelectAll)
			{
				SelectAllRows();
			}
			else
			{
				DisplaySelectedRows();
			}

			// copy previous settings to new card
			if( m_setSelect.Length > 0 )
			{
				for( int i=0; i<m_setSelect.Length; i++ )
				{
					PlateRow pr = (PlateRow)m_PlateRowsArray[i];
					if( m_setSelect[i] )
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

			m_prevCardType = nPlateType;

			if( nPlateType < 4 )
			{
				DrawRowCard();
				DisplayTextOfSelectedRows();
			}
			else
			{
				DrawColumnCard();
				DisplayTextOfSelectedColumns();
			}
		}

		private void textBoxPlateName_TextChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void textBoxRows_TextChanged(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			pf.CardChanged(this);
		}

		private void PlateCard_Load(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;
			ProgramGUIElement PGE = (ProgramGUIElement)pf.CardArray[0];
			PlateCard PC = (PlateCard)PGE.uc;

			int format = PGE.platecard_format;
			if( format > 3 ) format -= 3;

			for (int i = 0; i < PC.m_PlateRowsArray.Count; i++)
			{
				UserControl uc = null;
				switch (format)
				{
					case 1:
						uc = new OneRow96Card();
						uc.Parent = panelInner96;
						break;
					case 2:
						uc = new OneRow384Card();
						uc.Parent = panelInner384;
						break;
					case 3:
						uc = new OneRow1536Card();
						uc.Parent = panelInner1536;
						break;
				}

				PlateRow PR = (PlateRow)PC.m_PlateRowsArray[i];
				uc.Left = PR.m_uc.Left;
				uc.Top =  PR.m_uc.Top;
				PlateRow PRNew = new PlateRow(PR.m_bSelected, uc);
				m_PlateRowsArray.Add(PRNew);
			}
			
			RedrawCurrentPlate(false);
		}

		private void deleteButton_Click(object sender, System.EventArgs e)
		{
			programForm pf = (programForm)Parent.Parent;

			if (pf.IsPartOfRepeat(this))
			{
				MessageBox.Show("Can not delete cards within repeats.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			DialogResult DR = MessageBox.Show("Are you sure you want to delete this step?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (DR == DialogResult.Yes)
			{
				pf.DeleteProgramCard(this);
			}
		}

		private void buttonSelectAll_Click(object sender, System.EventArgs e)
		{
			RedrawCurrentPlate(true);
		}

		private void buttonClear_Click(object sender, System.EventArgs e)
		{
			Clear();
		}

		void DrawRowCard()
		{
			m_rowMode = true;
			this.panel1.Hide();
			ArrayList list = new ArrayList();
			list.AddRange( this.panel1.Controls );
			this.panel2.Controls.AddRange( (Control[])list.ToArray(typeof(Control)) );
			this.Size = new System.Drawing.Size(440, 190);
			this.panelInnerNone.Size = new System.Drawing.Size(182, 126);
			this.panelInner1536.Size = new System.Drawing.Size(182, 126);
			this.panelInner1536.BackgroundImage = Image.FromFile( "images/1536.bmp" );
			this.panelInner1536.Left = 5;
			this.panelInner384.Size = new System.Drawing.Size(182, 126);
			this.panelInner384.BackgroundImage = Image.FromFile( "images/384.bmp" );
			this.panelInner384.Left = 5;
			this.panelInner96.Size = new System.Drawing.Size(182, 126);
			this.panelInner96.BackgroundImage = Image.FromFile( "images/96.bmp" );
			this.panelInner96.Left = 5;

			this.textBoxWells.Size = new System.Drawing.Size(142, 64);
			this.buttonSelectAll.Location = new System.Drawing.Point(240, 137);
			this.buttonClear.Location = new System.Drawing.Point(314, 137);

			this.panel2.Show();
		}

		void DrawColumnCard()
		{
			m_rowMode = false;
			this.panel2.Hide();
			ArrayList list = new ArrayList();
			list.AddRange( this.panel2.Controls );
			this.panel1.Controls.AddRange( (Control[])list.ToArray(typeof(Control)) );
			this.Size = new System.Drawing.Size(440, 256);
			this.panelInnerNone.Size = new System.Drawing.Size(124, 182);
			this.panelInner1536.Size = new System.Drawing.Size(124, 182);
			this.panelInner1536.BackgroundImage = Image.FromFile( "images/1536-col.bmp" );
			this.panelInner1536.Left = 16;
			this.panelInner384.Size = new System.Drawing.Size(124, 182);
			this.panelInner384.BackgroundImage = Image.FromFile( "images/384-col.bmp" );
			this.panelInner384.Left = 16;
			this.panelInner96.Size = new System.Drawing.Size(124, 182);
			this.panelInner96.BackgroundImage = Image.FromFile( "images/96-col.bmp" );
			this.panelInner96.Left = 16;

			this.textBoxWells.Size = new System.Drawing.Size(142, 90);
			this.buttonSelectAll.Location = new System.Drawing.Point(240, 163);
			this.buttonClear.Location = new System.Drawing.Point(314, 163);

			this.panel1.Show();
		}

		void DrawSmallCard()
		{
			if( !m_minimize ) return;
			m_minimize = !m_minimize;
			int offset = 190-66;
			if( !m_rowMode )
			{
				offset +=66;
				this.panel1.BackgroundImage = Image.FromFile( "images/smallcardbg.bmp" );
				this.panel1.Size = new System.Drawing.Size(400, 66);				
			}
			else
			{
				this.panel2.BackgroundImage = Image.FromFile( "images/smallcardbg.bmp" );
				this.panel2.Size = new System.Drawing.Size(400, 66);
			}
			this.panelInnerNone.Hide();
			this.panelInner96.Hide();
			this.panelInner384.Hide();
			this.panelInner1536.Hide();
			this.label1.Hide();
			this.label2.Hide();
			this.textBoxWells.Hide();
			this.buttonClear.Hide();
			this.buttonSelectAll.Hide();
			this.icon.Show();

			this.Size = new System.Drawing.Size(440, 66);
			
			this.minimizeLabel.Text = string.Format( "Wells: {0}", textBoxWells.Text );
			this.minimizeLabel.Visible = true;
			if( this.minimizeLabel.Text.Length > 120 )
			{
				this.minimizeLabel.Top = 11;
				this.minimizeLabel.Left = 45;
				this.minimizeLabel.Height = 36;
				this.minimizeLabel.Width = 300;
			}
			else if( this.minimizeLabel.Text.Length > 60 )
			{
				this.minimizeLabel.Top = 22;
				this.minimizeLabel.Left = 48;
				this.minimizeLabel.Height = 26;
				this.minimizeLabel.Width = 341;
			}			
			else
			{
				this.minimizeLabel.Top = 24;
				this.minimizeLabel.Left = 48;				
				this.minimizeLabel.Height = 23;
				this.minimizeLabel.Width = 341;
			}

			programForm pf = (programForm)Parent.Parent;

			offset = -offset;
			int programStep = int.Parse( labelProgramStep.Text );
			pf.RepositionCards( offset, programStep );
			minimize.Visible = false;
			maximize.Visible = true;
		}

		void DrawBigCard()
		{
			if( m_minimize ) return;
			m_minimize = !m_minimize;

			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PlateCardRowsOnly));
			int offset = 190-66;
			if( !m_rowMode )
			{
				offset += 66;
				this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
				this.panel1.Size = new System.Drawing.Size(400, 256);				
				DrawColumnCard();
			}
			else
			{
				this.panel2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel2.BackgroundImage")));
				this.panel2.Size = new System.Drawing.Size(400, 190);
				DrawRowCard();
			}
			this.textBoxWells.Show();
			
			this.minimizeLabel.Visible = false;
			this.label1.Show();
			this.label2.Show();
			this.icon.Hide();

			this.buttonClear.Show();
			this.buttonSelectAll.Show();

			programForm pf = (programForm)Parent.Parent;			
			int programStep = int.Parse( labelProgramStep.Text );
			pf.RepositionCards( offset, programStep );

			RedrawCurrentPlate( false );
			minimize.Visible = true;
			maximize.Visible = false;
		}

		private void minimize_Click(object sender, System.EventArgs e)
		{
			DrawSmallCard();
		}

		private void maximize_Click(object sender, System.EventArgs e)
		{
			DrawBigCard();
		}
	}
}
