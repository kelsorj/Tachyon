using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text;

namespace AQ3
{
	/// <summary>
	/// Summary description for RS232InputOutputForm.
	/// </summary>
	public class RS232InputOutputForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.RichTextBox richTextBoxResponse;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonGetDeviceCodes;
		private System.Windows.Forms.Button buttonGetFirmwareText;
		private System.Windows.Forms.Button buttonGetSerialNumber;
		private System.Windows.Forms.Button buttonGetFile;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private mainForm m_mf = null;

		public RS232InputOutputForm(mainForm mf)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			m_mf = mf;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RS232InputOutputForm));
            this.richTextBoxResponse = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonGetDeviceCodes = new System.Windows.Forms.Button();
            this.buttonGetFirmwareText = new System.Windows.Forms.Button();
            this.buttonGetSerialNumber = new System.Windows.Forms.Button();
            this.buttonGetFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBoxResponse
            // 
            this.richTextBoxResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxResponse.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxResponse.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxResponse.Location = new System.Drawing.Point(16, 32);
            this.richTextBoxResponse.Name = "richTextBoxResponse";
            this.richTextBoxResponse.Size = new System.Drawing.Size(500, 344);
            this.richTextBoxResponse.TabIndex = 10;
            this.richTextBoxResponse.Text = "";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(16, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 18);
            this.label2.TabIndex = 9;
            this.label2.Text = "BNX1536 Response:";
            // 
            // buttonGetDeviceCodes
            // 
            this.buttonGetDeviceCodes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetDeviceCodes.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold);
            this.buttonGetDeviceCodes.ForeColor = System.Drawing.Color.White;
            this.buttonGetDeviceCodes.Location = new System.Drawing.Point(16, 392);
            this.buttonGetDeviceCodes.Name = "buttonGetDeviceCodes";
            this.buttonGetDeviceCodes.Size = new System.Drawing.Size(136, 23);
            this.buttonGetDeviceCodes.TabIndex = 13;
            this.buttonGetDeviceCodes.Text = "Get Device Codes";
            this.buttonGetDeviceCodes.Click += new System.EventHandler(this.buttonGetDeviceCodes_Click);
            // 
            // buttonGetFirmwareText
            // 
            this.buttonGetFirmwareText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetFirmwareText.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold);
            this.buttonGetFirmwareText.ForeColor = System.Drawing.Color.White;
            this.buttonGetFirmwareText.Location = new System.Drawing.Point(168, 392);
            this.buttonGetFirmwareText.Name = "buttonGetFirmwareText";
            this.buttonGetFirmwareText.Size = new System.Drawing.Size(144, 23);
            this.buttonGetFirmwareText.TabIndex = 14;
            this.buttonGetFirmwareText.Text = "Get Firmware Text";
            this.buttonGetFirmwareText.Click += new System.EventHandler(this.buttonGetFirmwareText_Click);
            // 
            // buttonGetSerialNumber
            // 
            this.buttonGetSerialNumber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetSerialNumber.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold);
            this.buttonGetSerialNumber.ForeColor = System.Drawing.Color.White;
            this.buttonGetSerialNumber.Location = new System.Drawing.Point(16, 424);
            this.buttonGetSerialNumber.Name = "buttonGetSerialNumber";
            this.buttonGetSerialNumber.Size = new System.Drawing.Size(136, 23);
            this.buttonGetSerialNumber.TabIndex = 15;
            this.buttonGetSerialNumber.Text = "Get Serial Number";
            this.buttonGetSerialNumber.Click += new System.EventHandler(this.buttonGetSerialNumber_Click);
            // 
            // buttonGetFile
            // 
            this.buttonGetFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetFile.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold);
            this.buttonGetFile.ForeColor = System.Drawing.Color.White;
            this.buttonGetFile.Location = new System.Drawing.Point(168, 424);
            this.buttonGetFile.Name = "buttonGetFile";
            this.buttonGetFile.Size = new System.Drawing.Size(144, 23);
            this.buttonGetFile.TabIndex = 16;
            this.buttonGetFile.Text = "Get File";
            this.buttonGetFile.Click += new System.EventHandler(this.buttonGetFile_Click);
            // 
            // RS232InputOutputForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.ClientSize = new System.Drawing.Size(536, 494);
            this.Controls.Add(this.buttonGetFile);
            this.Controls.Add(this.buttonGetSerialNumber);
            this.Controls.Add(this.buttonGetFirmwareText);
            this.Controls.Add(this.buttonGetDeviceCodes);
            this.Controls.Add(this.richTextBoxResponse);
            this.Controls.Add(this.label2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RS232InputOutputForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RS232";
            this.ResumeLayout(false);

		}
		#endregion

		private void buttonGetDeviceCodes_Click(object sender, System.EventArgs e)
		{
			RS232 rs232 = new RS232(m_mf.m_xmlData.GetCommPort());

			byte DeviceCode1;
			byte DeviceCode2;
			byte DeviceCode3;

			rs232.GetDeviceCodes(out DeviceCode1, out DeviceCode2, out DeviceCode3);
			
			richTextBoxResponse.Text += "DeviceCode1 = ";
			richTextBoxResponse.Text += DeviceCode1.ToString();
			switch(DeviceCode1)
			{
				case 0x10: richTextBoxResponse.Text += " (AquaMax 12389)";	break;
				case 0x11: richTextBoxResponse.Text += " (AquaMax 12392)";	break;
				case 0x12: richTextBoxResponse.Text += " (AquaMax 12394)";	break;
				case 0x13: richTextBoxResponse.Text += " (AquaMax 12395)";	break;
				case 0x20: richTextBoxResponse.Text += " (Embla 12384)";	break;
				case 0x21: richTextBoxResponse.Text += " (Embla 12385)";	break;
				case 0x22: richTextBoxResponse.Text += " (Embla 12386)";	break;
				case 0x23: richTextBoxResponse.Text += " (Embla 12387)";	break;
				case 0x24: richTextBoxResponse.Text += " (Embla 12388)";	break;
				default  : richTextBoxResponse.Text += " (Unknown model)";	break;
			}
			richTextBoxResponse.Text += '\n';

			richTextBoxResponse.Text += "DeviceCode2 = ";
			richTextBoxResponse.Text += DeviceCode2.ToString();
			switch(DeviceCode2) 
			{
				case 0x10: richTextBoxResponse.Text += " (Washer)";			break;
				case 0x11: richTextBoxResponse.Text += " (Washer (Robot))";	break;
				case 0x20: richTextBoxResponse.Text += " (Dispenser)";		break;
				default  : richTextBoxResponse.Text += " (Unknown type)";	break;
			}
			richTextBoxResponse.Text += '\n';
			
			richTextBoxResponse.Text += "DeviceCode3 = ";
			richTextBoxResponse.Text += DeviceCode3.ToString();
			richTextBoxResponse.Text += " (";
			richTextBoxResponse.Text += (DeviceCode3 / 0x10).ToString();
			richTextBoxResponse.Text += ".";
			richTextBoxResponse.Text += (DeviceCode3 % 0x10).ToString();
			richTextBoxResponse.Text += ")";
			richTextBoxResponse.Text += '\n';

			richTextBoxResponse.Text += '\n';
			
			rs232.Dispose();
		}

		private void buttonGetFirmwareText_Click(object sender, System.EventArgs e)
		{
			RS232 rs232 = new RS232(m_mf.m_xmlData.GetCommPort());
			richTextBoxResponse.Text += rs232.GetFirmwareText();
			richTextBoxResponse.Text += '\n';
			richTextBoxResponse.Text += '\n';
			rs232.Dispose();
		}

		private void buttonGetSerialNumber_Click(object sender, System.EventArgs e)
		{
			RS232 rs232 = new RS232(m_mf.m_xmlData.GetCommPort());
			richTextBoxResponse.Text += rs232.GetSerialNumber();
			richTextBoxResponse.Text += '\n';
			richTextBoxResponse.Text += '\n';
			rs232.Dispose();
		}

		private void buttonGetFile_Click(object sender, System.EventArgs e)
		{
			RS232 rs232 = new RS232(m_mf.m_xmlData.GetCommPort());
			byte[,] file = rs232.GetFile();
			rs232.Dispose();

			// info block (0)
			ProgInfoBlock pi = new ProgInfoBlock();
			pi.SubPrgNo = file[0, 0];
			for (int i = 0; i < 33; i++)
			{
				pi.FileName[i] = file[0, i + 1];
			}
			for (int i = 0; i < 9; i++)
			{
				pi.FileDate[i] = file[0, i + 34];
			}
			
			for (int i = 0; i < 256 - 46; i++)
			{
				pi.unused_bytes[i] = file[0, i + 46];
			}

			richTextBoxResponse.Text += "SubPrgNo = ";
			richTextBoxResponse.Text += pi.SubPrgNo.ToString();
			richTextBoxResponse.Text += '\n';
			richTextBoxResponse.Text += "FileName = ";
			richTextBoxResponse.Text += Encoding.ASCII.GetString(pi.FileName);
			richTextBoxResponse.Text += '\n';
			richTextBoxResponse.Text += "FileDate = ";
			richTextBoxResponse.Text += Encoding.ASCII.GetString(pi.FileDate);
			richTextBoxResponse.Text += '\n';
			richTextBoxResponse.Text += '\n';

			// program blocks (1-99)
			int nActualPrograms = 0;
			ProgramBlock[] PB = new ProgramBlock[99];
			for (int nBlock = 0; nBlock < PB.Length; nBlock++)
			{
				PB[nBlock] = new ProgramBlock();

				if (file[nBlock + 1, 1] == 0)
				{
					nActualPrograms = nBlock;
					break;
				}

				PB[nBlock].SubPrgNo = file[nBlock + 1, 0];
				for (int i = 0; i < 33; i++)
				{
					PB[nBlock].SubPrgName[i] = file[nBlock + 1, i + 1];
				}

				PB[nBlock].LocalEdit = file[nBlock + 1, 34];
				for (int i = 0; i < 33; i++)
				{
					PB[nBlock].PlateName[i] = file[nBlock + 1, i + 36];
				}
				PB[nBlock].PlateType = file[nBlock + 1, 69];
				int PlateHeigth = 0;
				Utilities.PutLoByte(ref PlateHeigth, file[nBlock + 1, 70]);
				Utilities.PutHiByte(ref PlateHeigth, file[nBlock + 1, 71]);
				PB[nBlock].PlateHeight = (ushort)PlateHeigth;
				int PlateDepth = 0;
				Utilities.PutLoByte(ref PlateDepth, file[nBlock + 1, 72]);
				Utilities.PutHiByte(ref PlateDepth, file[nBlock + 1, 73]);
				PB[nBlock].PlateDepth = (ushort)PlateDepth;
				int PlateOffset = 0;
				Utilities.PutLoByte(ref PlateOffset, file[nBlock + 1, 74]);
				Utilities.PutHiByte(ref PlateOffset, file[nBlock + 1, 75]);
				PB[nBlock].PlateOffset = (ushort)PlateOffset;
				int PlateVolume = 0;
				Utilities.PutLoByte(ref PlateVolume, file[nBlock + 1, 76]);
				Utilities.PutHiByte(ref PlateVolume, file[nBlock + 1, 77]);
				PB[nBlock].PlateVolume = (ushort)PlateVolume;
				int PlateDbwc = 0;
				Utilities.PutLoByte(ref PlateDbwc, file[nBlock + 1, 78]);
				Utilities.PutHiByte(ref PlateDbwc, file[nBlock + 1, 79]);
				PB[nBlock].PlateDbwc = (ushort)PlateDbwc;
				int PlateRows0 = 0;
				Utilities.PutLoByte(ref PlateRows0, file[nBlock + 1, 80]);
				Utilities.PutHiByte(ref PlateRows0, file[nBlock + 1, 81]);
				PB[nBlock].PlateRows0 = (ushort)PlateRows0;
				int PlateRows1 = 0;
				Utilities.PutLoByte(ref PlateRows1, file[nBlock + 1, 82]);
				Utilities.PutHiByte(ref PlateRows1, file[nBlock + 1, 83]);
				PB[nBlock].PlateRows1 = (ushort)PlateRows1;
				int AspOffset = 0;
				Utilities.PutLoByte(ref AspOffset, file[nBlock + 1, 84]);
				Utilities.PutHiByte(ref AspOffset, file[nBlock + 1, 85]);
				PB[nBlock].AspOffset = (ushort)AspOffset;
				int Liq1Pressure = 0;
				Utilities.PutLoByte(ref Liq1Pressure, file[nBlock + 1, 86]);
				Utilities.PutHiByte(ref Liq1Pressure, file[nBlock + 1, 87]);
				PB[nBlock].Liq1Factor = (ushort)Liq1Pressure;
				int Liq2Pressure = 0;
				Utilities.PutLoByte(ref Liq2Pressure, file[nBlock + 1, 88]);
				Utilities.PutHiByte(ref Liq2Pressure, file[nBlock + 1, 89]);
				PB[nBlock].Liq2Factor = (ushort)Liq2Pressure;
				int Liq3Pressure = 0;
				Utilities.PutLoByte(ref Liq3Pressure, file[nBlock + 1, 90]);
				Utilities.PutHiByte(ref Liq3Pressure, file[nBlock + 1, 91]);
				PB[nBlock].Liq3Factor = (ushort)Liq3Pressure;
				int Liq4Pressure = 0;
				Utilities.PutLoByte(ref Liq4Pressure, file[nBlock + 1, 92]);
				Utilities.PutHiByte(ref Liq4Pressure, file[nBlock + 1, 93]);
				PB[nBlock].Liq4Factor = (ushort)Liq4Pressure;

				int DispLow1 = 0;
				Utilities.PutLoByte(ref DispLow1, file[nBlock + 1, 94]);
				Utilities.PutHiByte(ref DispLow1, file[nBlock + 1, 95]);
				PB[nBlock].DispLowPr1 = (ushort)DispLow1;
				int DispLow2 = 0;
				Utilities.PutLoByte(ref DispLow2, file[nBlock + 1, 96]);
				Utilities.PutHiByte(ref DispLow2, file[nBlock + 1, 97]);
				PB[nBlock].DispLowPr2 = (ushort)DispLow2;
				int DispLow3 = 0;
				Utilities.PutLoByte(ref DispLow3, file[nBlock + 1, 98]);
				Utilities.PutHiByte(ref DispLow3, file[nBlock + 1, 99]);
				PB[nBlock].DispLowPr3 = (ushort)DispLow3;
				int DispLow4 = 0;
				Utilities.PutLoByte(ref DispLow4, file[nBlock + 1, 100]);
				Utilities.PutHiByte(ref DispLow4, file[nBlock + 1, 101]);
				PB[nBlock].DispLowPr4 = (ushort)DispLow4;
				
				for (int i = 0; i < 50; i++)
				{
					PB[nBlock].Command[i] = file[nBlock + 1, i + 106];
					int CmdValue = 0;
					Utilities.PutLoByte(ref CmdValue, file[nBlock + 1, (i * 2) + 156]);
					Utilities.PutHiByte(ref CmdValue, file[nBlock + 1, (i * 2) + 157]);
					PB[nBlock].CmdValue[i] = (ushort)CmdValue;
				}
			}

			for (int nBlock = 0; nBlock < nActualPrograms; nBlock++)
			{
				richTextBoxResponse.Text += "SubPrgNo = ";
				richTextBoxResponse.Text += PB[nBlock].SubPrgNo.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "SubPrgName = ";
				richTextBoxResponse.Text += Encoding.ASCII.GetString(PB[nBlock].SubPrgName);
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "LocalEdit = ";
				richTextBoxResponse.Text += PB[nBlock].LocalEdit.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateName = ";
				richTextBoxResponse.Text += Encoding.ASCII.GetString(PB[nBlock].PlateName);
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateType = ";
				richTextBoxResponse.Text += PB[nBlock].PlateType.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateHeight = ";
				richTextBoxResponse.Text += PB[nBlock].PlateHeight.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateDepth = ";
				richTextBoxResponse.Text += PB[nBlock].PlateDepth.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateOffset = ";
				richTextBoxResponse.Text += PB[nBlock].PlateOffset.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateVolume = ";
				richTextBoxResponse.Text += PB[nBlock].PlateVolume.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateDbwc = ";
				richTextBoxResponse.Text += PB[nBlock].PlateDbwc.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateRows0 = ";
				richTextBoxResponse.Text += PB[nBlock].PlateRows0.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "PlateRows1 = ";
				richTextBoxResponse.Text += PB[nBlock].PlateRows1.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "AspOffset = ";
				richTextBoxResponse.Text += PB[nBlock].AspOffset.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "Liq1Pressure = ";
				richTextBoxResponse.Text += PB[nBlock].Liq1Factor.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "Liq2Pressure = ";
				richTextBoxResponse.Text += PB[nBlock].Liq2Factor.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "Liq3Pressure = ";
				richTextBoxResponse.Text += PB[nBlock].Liq3Factor.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "Liq4Pressure = ";
				richTextBoxResponse.Text += PB[nBlock].Liq4Factor.ToString();
				richTextBoxResponse.Text += '\n';

				richTextBoxResponse.Text += "DispLow1 = ";
				richTextBoxResponse.Text += PB[nBlock].DispLowPr1.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "DispLow2 = ";
				richTextBoxResponse.Text += PB[nBlock].DispLowPr2.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "DispLow3 = ";
				richTextBoxResponse.Text += PB[nBlock].DispLowPr3.ToString();
				richTextBoxResponse.Text += '\n';
				richTextBoxResponse.Text += "DispLow4 = ";
				richTextBoxResponse.Text += PB[nBlock].DispLowPr4.ToString();
				richTextBoxResponse.Text += '\n';
				bool bEndReached = false;
				for (int i = 0; i < 50; i++)
				{
					if (bEndReached)
					{
						break;
					}

					richTextBoxResponse.Text += "Command ";
					richTextBoxResponse.Text += i.ToString();
					richTextBoxResponse.Text += " = ";
					switch (PB[nBlock].Command[i])
					{
						case 0:
							richTextBoxResponse.Text += "END";
							bEndReached = true;
							break;
						case 1:
							richTextBoxResponse.Text += "ASP1 (LowSpeed)";
							break;
						case 2:
							richTextBoxResponse.Text += "ASP2 (MediumSpeed)";
							break;
						case 3:
							richTextBoxResponse.Text += "ASP3 (HighSpeed)";
							break;
						case 20:
							richTextBoxResponse.Text += "DISP1 (Liquid1)";
							break;
						case 21:
							richTextBoxResponse.Text += "DISP2 (Liquid2)";
							break;
						case 22:
							richTextBoxResponse.Text += "DISP3 (Liquid3)";
							break;
						case 23:
							richTextBoxResponse.Text += "DISP4 (Liquid4)";
							break;
						case 30:
							richTextBoxResponse.Text += "SOAK";
							break;
						case 40:
							richTextBoxResponse.Text += "REP1";
							break;
						case 41:
							richTextBoxResponse.Text += "REP2";
							break;
						case 42:
							richTextBoxResponse.Text += "REP3";
							break;
						case 43:
							richTextBoxResponse.Text += "REP4";
							break;
						case 44:
							richTextBoxResponse.Text += "REP5";
							break;
						case 45:
							richTextBoxResponse.Text += "REP6";
							break;
						case 46:
							richTextBoxResponse.Text += "REP7";
							break;
						case 47:
							richTextBoxResponse.Text += "REP8";
							break;
						case 48:
							richTextBoxResponse.Text += "REP9";
							break;
						case 49:
							richTextBoxResponse.Text += "REP10";
							break;
						case 50:
							richTextBoxResponse.Text += "DISPL1";
							break;
						case 51:
							richTextBoxResponse.Text += "DISPL2";
							break;
						case 52:
							richTextBoxResponse.Text += "DISPL3";
							break;
						case 53:
							richTextBoxResponse.Text += "DISPL4";
							break;
						case 60:
							richTextBoxResponse.Text += "ROW0";
							break;
						case 61:
							richTextBoxResponse.Text += "ROW1";
							break;
					}
					richTextBoxResponse.Text += ", CmdValue = ";
					richTextBoxResponse.Text += PB[nBlock].CmdValue[i].ToString();
					
					richTextBoxResponse.Text += '\n';
				}
				
				richTextBoxResponse.Text += '\n';
			}
		}

		private void buttonPutFile_Click(object sender, System.EventArgs e)
		{
			
		}
	}
}
