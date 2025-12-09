using System;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;

namespace AQ3
{
	public class XmlData
	{
		public static string PROGRAM_VERSION = "2.3.0";
		public XmlDocument m_xmlData;
		private string m_strXmlFilename = "bnx1536.xml";

		public XmlData()
		{
		}

		public struct CommandStruct
		{
			public byte Command;
			public short CmdValue;
		}

		public struct RepeatCommandStruct
		{
			public byte Repeats;
			public byte From;
			public byte To;
		}

		public int GetNumberOfProgramsInFile(string strFilename, string strUsername)
		{
			int nPrograms = 0;
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");
			
			for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
			{
				XmlNode xmlNodeFile = nlFiles[0].ChildNodes[nFile];
				if (xmlNodeFile.Attributes["name"].Value == strFilename && xmlNodeFile.Attributes["owner"].Value == strUsername)
				{
					nPrograms = xmlNodeFile.ChildNodes.Count;
					
					// no need to iterate further
					break;
				}
			}

			return nPrograms;
		}

		bool FindPlateInfo( double aspOffset, string plateName, out double diameter, out string shape, out double offset, out double spacing, bool rowMode, int programNumber )
		{
			aspOffset = Math.Abs( aspOffset );
			double controlValue = (aspOffset*2) + 0.8;

			diameter = 0;
			shape = "0";
			offset = 0;
			spacing = 0;


			XmlNodeList fileNodes = m_xmlData.GetElementsByTagName("file");
			foreach( XmlNode file in fileNodes )
			{
				// Check programs
				XmlNodeList nodeCards = file.SelectNodes("./program/card");
				int count = -1;
				foreach( XmlNode card in nodeCards )
				{
					if( card.Attributes["name"].Value.Equals( "platecard" ) )
					{
						count++; // check program order against xml
					}

					if( card.Attributes["name"].Value.Equals( "platecard" ) &&
						card.Attributes["plate_name"].Value.Equals( plateName ) && programNumber == count )
					{
						try
						{
							//if( !rowMode )
							if( rowMode )
							{
								offset = double.Parse( card.Attributes["yo"].Value );
								spacing = double.Parse( card.Attributes["dbwc"].Value );
							}
							else
							{
								offset = double.Parse( card.Attributes["yo2"].Value );
								spacing = double.Parse( card.Attributes["dbwc2"].Value );
							}

							if( HaveAttribute( card.Attributes, "diameter" ) )
							{
								diameter = double.Parse( card.Attributes["diameter"].Value );
								shape = card.Attributes["shape"].Value;

								if( diameter < controlValue ) diameter = controlValue;
								return true;
							}
						}
						catch{}
					}
				}
			}

			// Check platelib
			XmlNodeList nodePlates = m_xmlData.GetElementsByTagName("plate");
			foreach( XmlNode plate in nodePlates )
			{
				if( plate.Attributes["name"].Value == plateName )
				{
					try
					{
						//if( !rowMode )
						if( rowMode )
						{
							offset = double.Parse( plate.Attributes["yo"].Value );
							spacing = double.Parse( plate.Attributes["dbwc"].Value );
						}
						else
						{
							offset = double.Parse( plate.Attributes["yo2"].Value );
							spacing = double.Parse( plate.Attributes["dbwc2"].Value );
						}

						if( HaveAttribute( plate.Attributes, "diameter" ) )
						{
							diameter = double.Parse( plate.Attributes["diameter"].Value );
							shape = plate.Attributes["shape"].Value;

							if( diameter < controlValue ) diameter = controlValue;
							return true;
						}
					}
					catch{}
				}
			}
			return false;
		}

		public void LoadFileFromAQ3(mainForm mf)
		{
			ArrayList ProgramNames = new ArrayList();

			Cursor.Current = Cursors.WaitCursor;
			byte[,] file = null;
			RS232 rs232 = null;
			try
			{
				if( !Utilities.IsMachineConnected(GetCommPort()) )
				{
					MessageBox.Show("Could not receive data from BNX1536", "Load File from BNX1536", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}

				if( Utilities.SupportColumns && Utilities.GetDeviceCode(GetCommPort()) < 0x13 )
				{
					MessageBox.Show("Your hardware is not supported with this software version.", "Incompatible hardware", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				if( !Utilities.SupportColumns && Utilities.GetDeviceCode(GetCommPort()) > 0x12 )
				{
					MessageBox.Show("Your hardware is not supported with this software version.", "Incompatible hardware", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}

				rs232 = new RS232(mf.m_xmlData.GetCommPort());
				file = rs232.GetFile();
				rs232.Dispose();
			}
			catch (Exception)
			{
				//MessageBox.Show(e.Message, "Load File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

				// Custom message
				MessageBox.Show("Could not receive data from BNX1536", "Load File from BNX1536", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				if( rs232 != null )
				{
					rs232.Dispose();
				}
				return;
			}
			Cursor.Current = Cursors.Default;

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

			bool bFileFound = false;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFile = null;
			XmlNode xmlNodeProgram = null;

			// find file in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == "____BNX1536_" && xmlNodeFile.Attributes["owner"].Value == "____BNX1536_")
					{
						bFileFound = true;

						// delete all programs
						xmlNodeFile.RemoveAll();

						// repair attributes
						XmlAttribute xmlAttributeFileName = m_xmlData.CreateAttribute("name");
						xmlAttributeFileName.Value = "____BNX1536_";
						xmlNodeFile.Attributes.Append(xmlAttributeFileName);
						XmlAttribute xmlAttributeOwner = m_xmlData.CreateAttribute("owner");
						xmlAttributeOwner.Value = "____BNX1536_";
						xmlNodeFile.Attributes.Append(xmlAttributeOwner);

						// no need to iterate further
						break;
					}
				}

				if (!bFileFound)
				{
					XmlElement xmlFilesElement = null;
					
					if (nlFiles.Count < 1)
					{
						// create files node
						XmlNodeList nlData = m_xmlData.GetElementsByTagName("data");
						if (nlData.Count < 1)
						{
							// create data node
							XmlNode root = m_xmlData.DocumentElement;
							XmlElement xmlDataElement = m_xmlData.CreateElement("data");
							root.AppendChild(xmlDataElement);

							xmlFilesElement = m_xmlData.CreateElement("files");
							xmlDataElement.AppendChild(xmlFilesElement);
						}
						else
						{
							xmlFilesElement = m_xmlData.CreateElement("files");
							nlData[0].AppendChild(xmlFilesElement);
						}

						xmlNodeFile = m_xmlData.CreateElement("file");
						XmlAttribute xmlAttributeFileName = m_xmlData.CreateAttribute("name");
						xmlAttributeFileName.Value = "____BNX1536_";
						xmlNodeFile.Attributes.Append(xmlAttributeFileName);
						XmlAttribute xmlAttributeOwner = m_xmlData.CreateAttribute("owner");
						xmlAttributeOwner.Value = "____BNX1536_";
						xmlNodeFile.Attributes.Append(xmlAttributeOwner);
						xmlFilesElement.AppendChild(xmlNodeFile);
					}
					else
					{
						xmlNodeFile = m_xmlData.CreateElement("file");
						XmlAttribute xmlAttributeFileName = m_xmlData.CreateAttribute("name");
						xmlAttributeFileName.Value = "____BNX1536_";
						xmlNodeFile.Attributes.Append(xmlAttributeFileName);
						XmlAttribute xmlAttributeOwner = m_xmlData.CreateAttribute("owner");
						xmlAttributeOwner.Value = "____BNX1536_";
						xmlNodeFile.Attributes.Append(xmlAttributeOwner);
						nlFiles[0].AppendChild(xmlNodeFile);	
					}
				}
			}

			// program blocks (1-99)
			int nActualPrograms = 0;
			ProgramBlock[] PB = new ProgramBlock[99];
			ArrayList arrayListSubPrgNameLength = new ArrayList();
			ArrayList arrayListPlateNameLength = new ArrayList();
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
					if (file[nBlock + 1, i + 1] == 0)
					{
						int SubPrgNameLength = i;
						arrayListSubPrgNameLength.Add(SubPrgNameLength);
						break;
					}
				}
				
				PB[nBlock].LocalEdit = file[nBlock + 1, 34];
				for (int i = 0; i < 33; i++)
				{
					PB[nBlock].PlateName[i] = file[nBlock + 1, i + 36];
					if (file[nBlock + 1, i + 36] == 0)
					{
						int PlateNameLength = i;
						arrayListPlateNameLength.Add(PlateNameLength);
						break;
					}
				}
				byte plateType = file[nBlock + 1, 69];
				if( file[nBlock + 1, 35] == 1 )
				{
					plateType += 3;
				}
				PB[nBlock].PlateType = plateType;
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
				int Liq1Factor = 0;
				Utilities.PutLoByte(ref Liq1Factor, file[nBlock + 1, 86]);
				Utilities.PutHiByte(ref Liq1Factor, file[nBlock + 1, 87]);
				PB[nBlock].Liq1Factor = (ushort)Liq1Factor;
				int Liq2Factor = 0;
				Utilities.PutLoByte(ref Liq2Factor, file[nBlock + 1, 88]);
				Utilities.PutHiByte(ref Liq2Factor, file[nBlock + 1, 89]);
				PB[nBlock].Liq2Factor = (ushort)Liq2Factor;
				int Liq3Factor = 0;
				Utilities.PutLoByte(ref Liq3Factor, file[nBlock + 1, 90]);
				Utilities.PutHiByte(ref Liq3Factor, file[nBlock + 1, 91]);
				PB[nBlock].Liq3Factor = (ushort)Liq3Factor;
				int Liq4Factor = 0;
				Utilities.PutLoByte(ref Liq4Factor, file[nBlock + 1, 92]);
				Utilities.PutHiByte(ref Liq4Factor, file[nBlock + 1, 93]);
				PB[nBlock].Liq4Factor = (ushort)Liq4Factor;

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

			bool rowMode = true;
			// save to xml data file
			for (int nBlock = 0; nBlock < nActualPrograms; nBlock++)
			{
				// create program node
				xmlNodeProgram = m_xmlData.CreateElement("program");
				XmlAttribute xmlAttributeProgramName = m_xmlData.CreateAttribute("name");
				xmlAttributeProgramName.Value = Encoding.ASCII.GetString(PB[nBlock].SubPrgName, 0, (int)arrayListSubPrgNameLength[nBlock]);
				xmlNodeProgram.Attributes.Append(xmlAttributeProgramName);
				xmlNodeFile.AppendChild(xmlNodeProgram);

				// save name to use later to update tree view
				ProgramNames.Add(xmlAttributeProgramName.Value);

				XmlNode xmlNodeCardPlate = m_xmlData.CreateElement("card");
				XmlAttribute xmlAttributePlateCardName;
				double maxOffset = 0;
			{
				// create card element
				

				// create name attribute
				XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
				xmlAttributeCardName.Value = "platecard";
				xmlNodeCardPlate.Attributes.Append(xmlAttributeCardName);

				// create plate_name attribute
				xmlAttributePlateCardName = m_xmlData.CreateAttribute("plate_name");
				xmlAttributePlateCardName.Value = Encoding.ASCII.GetString(PB[nBlock].PlateName, 0, (int)arrayListPlateNameLength[nBlock]);
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardName);

				// create height attribute
				XmlAttribute xmlAttributePlateCardHeight = m_xmlData.CreateAttribute("height");
				double PlateHeight = (double)PB[nBlock].PlateHeight / 100;
				xmlAttributePlateCardHeight.Value = PlateHeight.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardHeight);

				// create depth attribute
				XmlAttribute xmlAttributePlateCardDepth = m_xmlData.CreateAttribute("depth");
				double PlateDepth = (double)PB[nBlock].PlateDepth / 100;
				xmlAttributePlateCardDepth.Value = PlateDepth.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardDepth);

				XmlAttribute xmlAttributePlateCardLoBase;
				xmlAttributePlateCardLoBase = m_xmlData.CreateAttribute("lobase");
				if( PB[nBlock].PlateType > 10 )
				{					
					xmlAttributePlateCardLoBase.Value = "true";
					PB[nBlock].PlateType -= 10; //reset type
				}
				else
				{
					xmlAttributePlateCardLoBase.Value = "false";
				}
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardLoBase);

				// create format attribute
				XmlAttribute xmlAttributePlateCardFormat = m_xmlData.CreateAttribute("format");
				xmlAttributePlateCardFormat.Value = PB[nBlock].PlateType.ToString();
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardFormat);

				// create offset attribute
				XmlAttribute xmlAttributePlateCardOffset;
				if( PB[nBlock].PlateType < 4 )
				{
					xmlAttributePlateCardOffset = m_xmlData.CreateAttribute("yo");
					rowMode = true;
				}
				else
				{
					xmlAttributePlateCardOffset = m_xmlData.CreateAttribute("yo2");
					rowMode = false;
				}
				double PlateOffset = (double)PB[nBlock].PlateOffset / 100;
				xmlAttributePlateCardOffset.Value = PlateOffset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardOffset);

				// create max volume attribute
				XmlAttribute xmlAttributePlateCardMaxVolume = m_xmlData.CreateAttribute("max_volume");
				double PlateVolume = (double)PB[nBlock].PlateVolume / 10;
				xmlAttributePlateCardMaxVolume.Value = PlateVolume.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardMaxVolume);

				// create dbwc attribute
				XmlAttribute xmlAttributePlateCardDbwc;
				if( PB[nBlock].PlateType < 4 )
				{
					xmlAttributePlateCardDbwc = m_xmlData.CreateAttribute("dbwc");
				}
				else
				{
					xmlAttributePlateCardDbwc = m_xmlData.CreateAttribute("dbwc2");
				}
				double PlateDbwc = (double)PB[nBlock].PlateDbwc / 1000;
				xmlAttributePlateCardDbwc.Value = PlateDbwc.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardDbwc);

				// create plate row selections attribute
				XmlAttribute xmlAttributePlateCardRows = m_xmlData.CreateAttribute("rows");
				uint nRows = PB[nBlock].PlateRows1;
				nRows <<= 16;
				nRows |= PB[nBlock].PlateRows0;
				int stepFactor = 1;
				//byte deviceCode = mf.DeviceCode;
				if( Utilities.SupportColumns && (PB[nBlock].PlateType != 1 && PB[nBlock].PlateType != 4) )
				{
					// new machine with double steps
					stepFactor = 2;
				}
				
				string strRows = SetPlateRows( nRows, PB[nBlock].PlateType, stepFactor );
				xmlAttributePlateCardRows.Value = strRows.Trim();
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardRows);

				// create ASP offset attribute
				XmlAttribute xmlAttributePlateCardASPOffset = m_xmlData.CreateAttribute("asp_offset");
				double AspOffset = ((short)PB[nBlock].AspOffset);
				AspOffset /= 10;

				maxOffset = Math.Abs( AspOffset );

				xmlAttributePlateCardASPOffset.Value = AspOffset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
				xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardASPOffset);

				// insert card
				xmlNodeProgram.AppendChild(xmlNodeCardPlate);
			}
				// write cards
				ushort nRows0 = 0;
				//int nRowChangeCommands = 0;
				bool bEndReached = false;

				// counter for the actual number of program steps
				// repeats don't count as steps in the XML
				bool aspOffSetChanged = false;
				bool firstAspOffset = true;
				int actualCommandCount = 0;
				double AspirateASPOffset = 0;
				int doubleCommandCount = 0;
				int[] offsetChangeCount = new int[51];
				for (int i = 0; i < 50; i++)
				{
					actualCommandCount++; // add one step

					if (bEndReached)
					{
						break;
					}

					bool sweep = false;
					byte cmd = PB[nBlock].Command[i];

					switch (cmd)
					{
						case 0:
							bEndReached = true;
							break;
						case 10:
							//asp change
							aspOffSetChanged = true;
							doubleCommandCount++;
							actualCommandCount--; // subtract since this is not a command in the program per say
							break;
						case 11:
						case 12:
						case 13:
						{
							sweep = true;
							cmd -= 10;
							goto case 1;
						}
						case 1:
						case 2:
						case 3:
						{
							byte Command = cmd;
							ushort CmdValue = PB[nBlock].CmdValue[i];

							// create card element
							XmlNode xmlNodeCardAsp = m_xmlData.CreateElement("card");

							// create name attribute
							XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
							xmlAttributeCardName.Value = "aspiratecard";
							xmlNodeCardAsp.Attributes.Append(xmlAttributeCardName);

							// create sweep attribute
							XmlAttribute xmlAttributeSweep = m_xmlData.CreateAttribute("sweep");
							xmlAttributeSweep.Value = sweep.ToString();
							xmlNodeCardAsp.Attributes.Append(xmlAttributeSweep);

							// create velocity attribute
							byte ASPCommand = (byte)(Command - 1);
							XmlAttribute xmlAttributeCardAspirateVelocity = m_xmlData.CreateAttribute("velocity");
							xmlAttributeCardAspirateVelocity.Value = ASPCommand.ToString();
							xmlNodeCardAsp.Attributes.Append(xmlAttributeCardAspirateVelocity);

							// create time attribute
							byte MSB = Utilities.HiByte(CmdValue);
							XmlAttribute xmlAttributeCardAspirateTime = m_xmlData.CreateAttribute("time");
							double AspirateTime = (double)MSB / 10;
							xmlAttributeCardAspirateTime.Value = AspirateTime.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							xmlNodeCardAsp.Attributes.Append(xmlAttributeCardAspirateTime);

							// create probe_height attribute
							byte LSB = Utilities.LoByte(CmdValue);
							XmlAttribute xmlAttributeCardAspirateProbeHeight = m_xmlData.CreateAttribute("probe_height");
							double AspirateProbeHeight = (double)LSB / 10;
							xmlAttributeCardAspirateProbeHeight.Value = AspirateProbeHeight.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							xmlNodeCardAsp.Attributes.Append(xmlAttributeCardAspirateProbeHeight);

							// set asp_offset. It's stored on the program, so we copy it to the cards
							XmlAttribute xmlAttributeCardAspirateASPOffset = m_xmlData.CreateAttribute("asp_offset");							
							if( !aspOffSetChanged && firstAspOffset )
							{
								firstAspOffset = false;
								AspirateASPOffset = (short)PB[nBlock].AspOffset;
								AspirateASPOffset /= 10;

								double max = Math.Abs( AspirateASPOffset );
								if( max > maxOffset ) maxOffset = max;
							}
							else if( aspOffSetChanged )
							{
								// read the value from the previous set offset command
								aspOffSetChanged = false;
								AspirateASPOffset = (short)PB[nBlock].CmdValue[i-1];
								AspirateASPOffset /= 10;

								double max = Math.Abs( AspirateASPOffset );
								if( max > maxOffset ) maxOffset = max;
							}
							
							xmlAttributeCardAspirateASPOffset.Value = AspirateASPOffset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							xmlNodeCardAsp.Attributes.Append(xmlAttributeCardAspirateASPOffset);

							// insert card
							xmlNodeProgram.AppendChild(xmlNodeCardAsp);
						}
							break;
						case 20:
						case 21:
						case 22:
						case 23:
						case 50:
						case 51:
						case 52:
						case 53:
						{
							byte Command = PB[nBlock].Command[i];
							ushort CmdValue = PB[nBlock].CmdValue[i];

							// create card element
							XmlNode xmlNodeCardCmd = m_xmlData.CreateElement("card");

							// create name attribute
							XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
							xmlAttributeCardName.Value = "dispensecard";
							xmlNodeCardCmd.Attributes.Append(xmlAttributeCardName);

							// create liquid name attribute
							XmlAttribute xmlAttributeLiquidName = m_xmlData.CreateAttribute("liquid_name");
							xmlAttributeLiquidName.Value = ""; // no liquid name stored on AQ3
							xmlNodeCardCmd.Attributes.Append(xmlAttributeLiquidName);

							// create inlet attribute
							byte inlet;
							if(Command > 23)
								inlet = (byte)(Command - 49);
							else
								inlet = (byte)(Command - 19);

							XmlAttribute xmlAttributeDispenseCardInlet = m_xmlData.CreateAttribute("inlet");
							xmlAttributeDispenseCardInlet.Value = inlet.ToString();
							xmlNodeCardCmd.Attributes.Append(xmlAttributeDispenseCardInlet);

							// create volume attribute
							XmlAttribute xmlAttributeDispenseCardVolume = m_xmlData.CreateAttribute("volume");
							double DispenseCardVolume = (double)CmdValue / 10;
							xmlAttributeDispenseCardVolume.Value = DispenseCardVolume.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							xmlNodeCardCmd.Attributes.Append(xmlAttributeDispenseCardVolume);

							// create liquid_factor attribute
							XmlAttribute xmlAttributeDispenseCardLiquidFactor = m_xmlData.CreateAttribute("liquid_factor");
							switch (inlet)
							{
								case 1:
									double dLiq1Factor = (double)PB[nBlock].Liq1Factor / 100;
									xmlAttributeDispenseCardLiquidFactor.Value = dLiq1Factor.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
								case 2:
									double dLiq2Factor = (double)PB[nBlock].Liq2Factor / 100;
									xmlAttributeDispenseCardLiquidFactor.Value = dLiq2Factor.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
								case 3:
									double dLiq3Factor = (double)PB[nBlock].Liq3Factor / 100;
									xmlAttributeDispenseCardLiquidFactor.Value = dLiq3Factor.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
								case 4:
									double dLiq4Factor = (double)PB[nBlock].Liq4Factor / 100;
									xmlAttributeDispenseCardLiquidFactor.Value = dLiq4Factor.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
							}
							xmlNodeCardCmd.Attributes.Append(xmlAttributeDispenseCardLiquidFactor);

							// create disp_low attribute
							XmlAttribute xmlAttributeDispenseCardDispLow = m_xmlData.CreateAttribute("disp_low");
							switch (inlet)
							{
								case 1:
									//double dDisp1Low = (double)PB[nBlock].DispLowPr1 / 100;
									double dDisp1Low = (double)PB[nBlock].DispLowPr1;
									xmlAttributeDispenseCardDispLow.Value = dDisp1Low.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
								case 2:
									double dDisp2Low = (double)PB[nBlock].DispLowPr2;
									xmlAttributeDispenseCardDispLow.Value = dDisp2Low.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
								case 3:
									double dDisp3Low = (double)PB[nBlock].DispLowPr3;
									xmlAttributeDispenseCardDispLow.Value = dDisp3Low.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
								case 4:
									double dDisp4Low = (double)PB[nBlock].DispLowPr4;
									xmlAttributeDispenseCardDispLow.Value = dDisp4Low.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
									break;
							}
							xmlNodeCardCmd.Attributes.Append(xmlAttributeDispenseCardDispLow);

							// insert card
							xmlNodeProgram.AppendChild(xmlNodeCardCmd);
						}

							break;
						case 30:
						{
							ushort CmdValue = PB[nBlock].CmdValue[i];

							// create card element
							XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

							// create name attribute
							XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
							xmlAttributeCardName.Value = "soakcard";
							xmlNodeCard.Attributes.Append(xmlAttributeCardName);

							// create time attribute
							XmlAttribute xmlAttributeSoakCardTime = m_xmlData.CreateAttribute("time");
							xmlAttributeSoakCardTime.Value = CmdValue.ToString();
							xmlNodeCard.Attributes.Append(xmlAttributeSoakCardTime);

							// insert card
							xmlNodeProgram.AppendChild(xmlNodeCard);
						}
							break;
						case 40:
						case 41:
						case 42:
						case 43:
						case 44:
						case 45:
						case 46:
						case 47:
						case 48:
						case 49:
						{
							// repeat card 
							actualCommandCount--; //subtract one because this command don't count
							byte Command = PB[nBlock].Command[i];
							ushort CmdValue = PB[nBlock].CmdValue[i];

							// create card element
							XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

							// create name attribute
							XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
							xmlAttributeCardName.Value = "repeatcard";
							xmlNodeCard.Attributes.Append(xmlAttributeCardName);


							int fromValue = actualCommandCount; // loop ends at this point

							// create from attribute
							XmlAttribute xmlAttributeRepeatCardFrom = m_xmlData.CreateAttribute("from");
							xmlAttributeRepeatCardFrom.Value = fromValue.ToString(); //correct end value
							xmlNodeCard.Attributes.Append(xmlAttributeRepeatCardFrom);

							// create to attribute
							XmlAttribute xmlAttributeRepeatCardTo = m_xmlData.CreateAttribute("to");
							int toValue = CmdValue;
							// adjust value for double commands
							toValue -= offsetChangeCount[CmdValue-1];

							xmlAttributeRepeatCardTo.Value = toValue.ToString();

							xmlNodeCard.Attributes.Append(xmlAttributeRepeatCardTo);

							// create repeats attribute
							byte nRepeats = (byte)(Command - 39);
							XmlAttribute xmlAttributeRepeatCardRepeats = m_xmlData.CreateAttribute("repeats");
							xmlAttributeRepeatCardRepeats.Value = nRepeats.ToString();
							xmlNodeCard.Attributes.Append(xmlAttributeRepeatCardRepeats);

							// insert card
							xmlNodeProgram.AppendChild(xmlNodeCard);
							//repeatCount++;
							doubleCommandCount++;
						}
							break;
						case 60:
						{
							nRows0 = PB[nBlock].CmdValue[i];
							doubleCommandCount++;
							actualCommandCount--; // this command uses two rows
						}
							break;
						case 61:
						{							
							byte Command = PB[nBlock].Command[i];
							ushort CmdValue = PB[nBlock].CmdValue[i];

							// create card element
							XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

							// create name attribute
							XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
							xmlAttributeCardName.Value = "platecardrowsonly";
							xmlNodeCard.Attributes.Append(xmlAttributeCardName);

							// create plate row selections attribute
							uint nRows = CmdValue;
							nRows <<= 16;
							nRows |= nRows0;
							int stepFactor = 1;
							//byte deviceCode = mf.DeviceCode;
							if( Utilities.SupportColumns && (PB[nBlock].PlateType != 1 && PB[nBlock].PlateType != 4) )
							{
								// new machine with double steps
								stepFactor = 2;
							}
							string strRows = SetPlateRows( nRows, PB[nBlock].PlateType, stepFactor );

							XmlAttribute xmlAttributePlateCardRows = m_xmlData.CreateAttribute("rows");
							xmlAttributePlateCardRows.Value = strRows;
							xmlNodeCard.Attributes.Append(xmlAttributePlateCardRows);

							// insert card
							xmlNodeProgram.AppendChild(xmlNodeCard);

							nRows0 = 0;
						}
							break;
					}

					offsetChangeCount[i+1] = doubleCommandCount;
				}

				// copy diameter, shape from either prog lib or card lib
				double diameter;
				string shape;
				double offset;
				double spacing;

				if( FindPlateInfo( maxOffset, xmlAttributePlateCardName.Value, out diameter, out shape, out offset, out spacing, rowMode, nBlock ) )
				{
					XmlAttribute xmlAttributePlateCardDiameter = m_xmlData.CreateAttribute("diameter");
					xmlAttributePlateCardDiameter.Value = diameter.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardDiameter);

					XmlAttribute xmlAttributePlateCardShape = m_xmlData.CreateAttribute("shape");
					xmlAttributePlateCardShape.Value = shape;
					xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardShape);

					XmlAttribute xmlAttrCardOffset;
					if( rowMode ) xmlAttrCardOffset = m_xmlData.CreateAttribute("yo2");						
					else xmlAttrCardOffset = m_xmlData.CreateAttribute("yo");

					xmlAttrCardOffset.Value = offset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCardPlate.Attributes.Append( xmlAttrCardOffset );

					XmlAttribute xmlAttrCardSpace;
					if( rowMode ) xmlAttrCardSpace = m_xmlData.CreateAttribute("dbwc2");						
					else xmlAttrCardSpace = m_xmlData.CreateAttribute("dbwc");

					xmlAttrCardSpace.Value = spacing.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCardPlate.Attributes.Append( xmlAttrCardSpace );
				}
				else if( maxOffset > 0 )
				{
					maxOffset = Math.Abs( maxOffset );
					diameter = (maxOffset*2) + 0.8;						
					XmlAttribute xmlAttributePlateCardDiameter = m_xmlData.CreateAttribute("diameter");
					xmlAttributePlateCardDiameter.Value = diameter.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardDiameter);

					XmlAttribute xmlAttributePlateCardShape = m_xmlData.CreateAttribute("shape");
					xmlAttributePlateCardShape.Value = shape;
					xmlNodeCardPlate.Attributes.Append(xmlAttributePlateCardShape);
				}
			}
			m_xmlData.Save(m_strXmlFilename);

			// update tree
			TreeView tw = mf.treeView;
			for (int i = 0; i < tw.Nodes.Count; i++)
			{
				if (tw.Nodes[i].Text.ToLower() == "current bnx1536 programs")
				{
					// remove all old programs
					int nOldPrograms = tw.Nodes[i].Nodes.Count;
					for (int j = 0; j < nOldPrograms; j++)
					{
						tw.Nodes[i].Nodes[0].Remove();
					}

					// add new programs
					for (int nProgram = 0; nProgram < ProgramNames.Count; nProgram++)
					{
						TreeNode treeNode = new TreeNode((string)ProgramNames[nProgram], 5, 5);
						treeNode.ForeColor = Color.Blue;
						tw.Nodes[i].Nodes.Add(treeNode);
					}

					break;
				}
			}
		}

		public void SaveFileToAQ3(string strFileName, string strOwner, mainForm mf)
		{
			// find file in xml data
			XmlNodeList nlFile = m_xmlData.GetElementsByTagName("file");
			for (int nFileNode = 0; nFileNode < nlFile.Count; nFileNode++)
			{
				if (nlFile[nFileNode].Attributes["name"].Value == strFileName && nlFile[nFileNode].Attributes["owner"].Value == strOwner)
				{
					// found file to upload to AQ3 hardware
					XmlNode xmlNodeFile = nlFile[nFileNode];
					
					int nProgramCount = xmlNodeFile.ChildNodes.Count;
					if (nProgramCount > 99)
					{
						nProgramCount = 99;
					}
					
					int nBlockSize = 256;
					byte[,] file = new byte[nProgramCount + 1, nBlockSize]; // max 99 programs + 1 info block

					// info block (0)
					file[0, 0] = 0;
					for (int i = 1; i < 33; i++)
					{
						if (nlFile[nFileNode].Attributes["name"].Value.Length > (i - 1))
						{
							file[0, i] = (byte)nlFile[nFileNode].Attributes["name"].Value[i - 1];
						}
						else
						{
							file[0, i] = 0;
						}

					}
					file[0, 33] = 0;
					for (int i = 34; i < 42; i++)
					{
						file[0, i] = Convert.ToByte(DateTime.Now.ToString("ddMMyyyy")[i - 34]);
					}
					file[0, 42] = 0;
					
					for (int i = 43; i < 256; i++)
					{
						file[0, i] = 0;
					}
										
					// fill programs 1 - 99 (99 is max, but could be less here)
					for (int nProgram = 1; nProgram <= nProgramCount; nProgram++)
					{
						// values that must be saved for later writes (after all the commands are processed)
						double dLiq1Factor = 0;
						double dLiq2Factor = 0;
						double dLiq3Factor = 0;
						double dLiq4Factor = 0;

						double dDispLow1 = 0;
						double dDispLow2 = 0;
						double dDispLow3 = 0;
						double dDispLow4 = 0;

						// begin
						file[nProgram, 0] = (byte)nProgram;
						for (int i = 1; i < 33; i++)
						{
							if (xmlNodeFile.ChildNodes[nProgram - 1].Attributes["name"].Value.Length > (i - 1))
							{
								file[nProgram, i] = (byte)xmlNodeFile.ChildNodes[nProgram - 1].Attributes["name"].Value[i - 1];
							}
							else
							{
								file[nProgram, i] = 0;
							}

						}
						file[nProgram, 33] = 0;
						file[nProgram, 34] = 0;
						
						// Unused bytes
						for (int i = 102; i <= 105; i++)
						{
							file[nProgram, i] = 0;
						}

						XmlNode xmlNodeProgram = xmlNodeFile.ChildNodes[nProgram - 1];
						int nMaxCommands = xmlNodeProgram.ChildNodes.Count;
						if (nMaxCommands > 50)
						{
							nMaxCommands = 50;
						}
						ArrayList CommandArray = new ArrayList();
						ArrayList RepeatCommandArray = new ArrayList();
						
						bool aspOffsetSet = false;
						bool aspOffsetChanged = false;
						double lastAspOffset = 0;
						CommandStruct aspOffsetCS = new CommandStruct();
						byte origFormat = 0;
						for (int nCommand = 0; nCommand < nMaxCommands; nCommand++)
						{
							XmlNode xmlNodeCard = xmlNodeProgram.ChildNodes[nCommand];
							string strCardName = xmlNodeCard.Attributes["name"].Value;
							byte Command = 0;
							short CmdValue = 0;
							byte Command2 = 0;
							short CmdValue2 = 0;
							bool bDoubleCommand = false;

							// note:
							// index 35 (linked) is handled after all commands
							// index 84-85 (asp height) is handeled for every command -> the last asp command decides...
							// IMPORTANT: No longer in use. Is part of the command, now...
							// index 86-93 (liquid pressures) is handled after all commands
							
							if (strCardName == "platecard")
							{
								for (int i = 36; i < 68; i++)
								{
									if (xmlNodeCard.Attributes["plate_name"].Value.Length > (i - 36))
									{
										file[nProgram, i] = (byte)xmlNodeCard.Attributes["plate_name"].Value[i - 36];
									}
									else
									{
										file[nProgram, i] = 0;
									}
								}
								file[nProgram, 68] = 0;
								byte format = Convert.ToByte(xmlNodeCard.Attributes["format"].Value, 10);
								origFormat = format;
								if( format > 3 )
								{
									format -=  3;
									file[nProgram, 35] = 1; // Set column byte
								}

								try
								{
									bool loBase = bool.Parse( xmlNodeCard.Attributes["lobase"].Value );
									if( loBase ) format += 10;
								}
								catch( Exception ){}

								file[nProgram, 69] = format;
								double dPlateHeight = Convert.ToDouble(xmlNodeCard.Attributes["height"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								dPlateHeight *= 100;
								int nPlateHeight = Convert.ToInt32(dPlateHeight);
								file[nProgram, 70] = Utilities.LoByte(nPlateHeight);
								file[nProgram, 71] = Utilities.HiByte(nPlateHeight);
								double dPlateDepth = Convert.ToDouble(xmlNodeCard.Attributes["depth"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								dPlateDepth *= 100;
								int nPlateDepth = Convert.ToInt32(dPlateDepth);
								file[nProgram, 72] = Utilities.LoByte(nPlateDepth);
								file[nProgram, 73] = Utilities.HiByte(nPlateDepth);
								double dPlateOffset;
								if( file[nProgram, 35] == 0 )
								{
									//row
									dPlateOffset = Convert.ToDouble(xmlNodeCard.Attributes["yo"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								}
								else
								{
									//column
									dPlateOffset = Convert.ToDouble(xmlNodeCard.Attributes["yo2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));								
								}
								dPlateOffset *= 100;
								int nPlateOffset = Convert.ToInt32(dPlateOffset);
								file[nProgram, 74] = Utilities.LoByte(nPlateOffset);
								file[nProgram, 75] = Utilities.HiByte(nPlateOffset);
								double dPlateVolume = Convert.ToDouble(xmlNodeCard.Attributes["max_volume"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								dPlateVolume *= 10;
								int nPlateVolume = Convert.ToInt32(dPlateVolume);
								file[nProgram, 76] = Utilities.LoByte(nPlateVolume);
								file[nProgram, 77] = Utilities.HiByte(nPlateVolume);
								double dPlateDbwc;
								if( file[nProgram, 35] == 0 )
								{
									dPlateDbwc = Convert.ToDouble(xmlNodeCard.Attributes["dbwc"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								}
								else
								{
									dPlateDbwc = Convert.ToDouble(xmlNodeCard.Attributes["dbwc2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								}
								dPlateDbwc *= 1000;
								int nPlateDbwc = Convert.ToInt32(dPlateDbwc);
								file[nProgram, 78] = Utilities.LoByte(nPlateDbwc);
								file[nProgram, 79] = Utilities.HiByte(nPlateDbwc);

								string strPlateRows = xmlNodeCard.Attributes["rows"].Value;
								uint nPlateRows = 0;
								if( strPlateRows.Length > 0 )
								{
									//byte deviceCode = mf.DeviceCode;
									if( Utilities.SupportColumns )
									{									
										nPlateRows = GetPlateRowNew( strPlateRows, origFormat );
									}
									else
									{
										nPlateRows = GetPlateRowOld( strPlateRows );
									}
								}

								file[nProgram, 80] = Utilities.LoByte((int)nPlateRows);
								nPlateRows >>= 8;
								file[nProgram, 81] = Utilities.LoByte((int)nPlateRows);
								nPlateRows >>= 8;
								file[nProgram, 82] = Utilities.LoByte((int)nPlateRows);
								nPlateRows >>= 8;
								file[nProgram, 83] = Utilities.LoByte((int)nPlateRows);
//retrieve asp_offset from first dispense card
//or just set this byte further down when we enounter
//the offset
//								double dPlateASPOffset = Convert.ToDouble(xmlNodeCard.Attributes["asp_offset"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
//								dPlateASPOffset *= 10;
//								int nPlateASPOffset = Convert.ToInt32(dPlateASPOffset);
//								file[nProgram, 84] = Utilities.LoByte(nPlateASPOffset);
//								file[nProgram, 85] = Utilities.HiByte(nPlateASPOffset);
							}
							else if (strCardName == "platecardrowsonly")
							{
								string strPlateRows = xmlNodeCard.Attributes["rows"].Value;
								uint nPlateRows = 0;

								if( strPlateRows.Length > 0 )
								{
									//byte deviceCode = mf.DeviceCode;
									if( Utilities.SupportColumns )
									{	
										nPlateRows = GetPlateRowNew( strPlateRows, origFormat );										
									}
									else
									{
										nPlateRows = GetPlateRowOld( strPlateRows );
									}
								}
								
								bDoubleCommand = true;
								Command = 60;
								CmdValue = (short)nPlateRows;
								nPlateRows >>= 16;
								Command2 = 61;
								CmdValue2 = (short)nPlateRows;
							}
							else if (strCardName == "aspiratecard")
							{
								byte b = Convert.ToByte(xmlNodeCard.Attributes["velocity"].Value, 10);
								
								bool sweep = false;
								if( xmlNodeCard.Attributes["sweep"] != null )
								{
									sweep = Convert.ToBoolean(xmlNodeCard.Attributes["sweep"].Value );
								}
								if( sweep )
									b+= 10;

								if (b == 0)
								{
									Command = 1;
								}
								else if (b == 1)
								{
									Command = 2;
								}
								else if (b == 2)
								{
									Command = 3;
								}

								else if (b == 10)
								{
									Command = 11;
								}
								else if (b == 11)
								{
									Command = 12;
								}
								else if (b == 12)
								{
									Command = 13;
								}

								byte MSB = (byte)(10 * Convert.ToDouble(xmlNodeCard.Attributes["time"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US")));
								byte LSB = (byte)(10 * Convert.ToDouble(xmlNodeCard.Attributes["probe_height"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US")));
								CmdValue = MSB;
								CmdValue <<= 8;
								ushort _CmdValue = (ushort)CmdValue;
								_CmdValue |= LSB;
								CmdValue = (short)_CmdValue;

								double dPlateASPOffset = Convert.ToDouble(xmlNodeCard.Attributes["asp_offset"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								//set ASPOffset for the program from the first asp card
								if( !aspOffsetSet )
								{
									aspOffsetSet = true;									
									lastAspOffset = dPlateASPOffset;
									dPlateASPOffset *= 10;
									int nPlateASPOffset = Convert.ToInt32(dPlateASPOffset);
									file[nProgram, 84] = Utilities.LoByte(nPlateASPOffset);
									file[nProgram, 85] = Utilities.HiByte(nPlateASPOffset);
								}
								else
								{
									aspOffsetChanged = false;
									if( dPlateASPOffset != lastAspOffset )
									{
										//offset changed and we need to insert an ASP offset change command
										lastAspOffset = dPlateASPOffset;
										aspOffsetChanged = true;
										aspOffsetCS = new CommandStruct();
										aspOffsetCS.Command = 10;
										dPlateASPOffset *=10;
										int nPlateASPOffset = Convert.ToInt32(dPlateASPOffset);
										ushort newOff = Utilities.LoByte(nPlateASPOffset);
										newOff <<= 8;
										newOff |= Utilities.HiByte(nPlateASPOffset);
										aspOffsetCS.CmdValue = (short)dPlateASPOffset;
									}
								}
							}
							else if (strCardName == "dispensecard")
							{
								double dLiquidFactor = Convert.ToDouble(xmlNodeCard.Attributes["liquid_factor"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								switch (Convert.ToByte(xmlNodeCard.Attributes["inlet"].Value, 10))
								{
									case 1:
										dLiq1Factor = dLiquidFactor;
										break;
									case 2:
										dLiq2Factor = dLiquidFactor;
										break;
									case 3:
										dLiq3Factor = dLiquidFactor;
										break;
									case 4:
										dLiq4Factor = dLiquidFactor;
										break;
								}
								
								double dDispLow = Convert.ToDouble(xmlNodeCard.Attributes["disp_low"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								switch (Convert.ToByte(xmlNodeCard.Attributes["inlet"].Value, 10))
								{
									case 1:
										dDispLow1 = dDispLow;
										break;
									case 2:
										dDispLow2 = dDispLow;
										break;
									case 3:
										dDispLow3 = dDispLow;
										break;
									case 4:
										dDispLow4 = dDispLow;
										break;
								}

								// command
								if(dDispLow == 550)
								{
									Command = Convert.ToByte((19 + Convert.ToByte(xmlNodeCard.Attributes["inlet"].Value, 10)));
								}
								else
								{
									Command = Convert.ToByte((49 + Convert.ToByte(xmlNodeCard.Attributes["inlet"].Value, 10)));
								}
								
								// volume (CmdValue)
								double dDispVolume = Convert.ToDouble(xmlNodeCard.Attributes["volume"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
								dDispVolume *= 10;
								CmdValue = Convert.ToInt16(dDispVolume);
							}
							else if (strCardName == "soakcard")
							{
								Command = 30;
								CmdValue = Convert.ToInt16(xmlNodeCard.Attributes["time"].Value);
							}
							else if (strCardName == "repeatcard")
							{
								// update repeat command array
								RepeatCommandStruct RCS = new RepeatCommandStruct();
								RCS.Repeats = Convert.ToByte(xmlNodeCard.Attributes["repeats"].Value, 10);
								RCS.From = Convert.ToByte(xmlNodeCard.Attributes["from"].Value, 10);
								RCS.To = Convert.ToByte(xmlNodeCard.Attributes["to"].Value, 10);
								
								// insert sorted (reverse)
								bool bInserted = false;
								for (int i = 0; i < RepeatCommandArray.Count; i++)
								{
									if (RCS.To >= ((RepeatCommandStruct)RepeatCommandArray[i]).To)
									{
										RepeatCommandArray.Insert(i, RCS);
										bInserted = true;
										break;
									}
								}
								if (!bInserted)
								{
									RepeatCommandArray.Add(RCS);
								}
							}

							// update command array
							if (strCardName != "platecard" && strCardName != "repeatcard")
							{
								if( aspOffsetChanged )
								{
									aspOffsetChanged = false;
									CommandArray.Add( aspOffsetCS );
								}

								CommandStruct CS = new CommandStruct();
								CS.Command = Command;
								CS.CmdValue = CmdValue;
								CommandArray.Add(CS);

								if (bDoubleCommand)
								{
									CommandStruct CS2 = new CommandStruct();
									CS2.Command = Command2;
									CS2.CmdValue = CmdValue2;
									CommandArray.Add(CS2);
									bDoubleCommand = false;
								}
							}
						}
						// values that can not be written before all commands is processed
						short nLiq1Factor = (short)(dLiq1Factor * 100);
						short nLiq2Factor = (short)(dLiq2Factor * 100);
						short nLiq3Factor = (short)(dLiq3Factor * 100);
						short nLiq4Factor = (short)(dLiq4Factor * 100);
						file[nProgram, 86] = Utilities.LoByte(nLiq1Factor);
						file[nProgram, 87] = Utilities.HiByte(nLiq1Factor);
						file[nProgram, 88] = Utilities.LoByte(nLiq2Factor);
						file[nProgram, 89] = Utilities.HiByte(nLiq2Factor);
						file[nProgram, 90] = Utilities.LoByte(nLiq3Factor);
						file[nProgram, 91] = Utilities.HiByte(nLiq3Factor);
						file[nProgram, 92] = Utilities.LoByte(nLiq4Factor);
						file[nProgram, 93] = Utilities.HiByte(nLiq4Factor);

						//short nDispLow1 = (short)(dDispLow1 * 100);
						short nDispLow1 = (short)(dDispLow1);
						short nDispLow2 = (short)(dDispLow2);
						short nDispLow3 = (short)(dDispLow3);
						short nDispLow4 = (short)(dDispLow4);
						file[nProgram, 94] = Utilities.LoByte(nDispLow1);
						file[nProgram, 95] = Utilities.HiByte(nDispLow1);
						file[nProgram, 96] = Utilities.LoByte(nDispLow2);
						file[nProgram, 97] = Utilities.HiByte(nDispLow2);
						file[nProgram, 98] = Utilities.LoByte(nDispLow3);
						file[nProgram, 99] = Utilities.HiByte(nDispLow3);
						file[nProgram, 100] = Utilities.LoByte(nDispLow4);
						file[nProgram, 101] = Utilities.HiByte(nDispLow4);

						// merge repeat commands into the other commands
						for (int i = 0; i < RepeatCommandArray.Count; i++)
						{
							RepeatCommandStruct RCS = (RepeatCommandStruct)RepeatCommandArray[i];
							
							// row selection cards messes it up...
							int nDoubleCommandsBefore = 0;
							int nDoubleCommandsWithin = 0;
							for (int n = 0; n < RCS.From + nDoubleCommandsBefore; n++)
							{
								CommandStruct CSTemp = (CommandStruct)CommandArray[n];
								if (CSTemp.Command == 60) // row select
								{
									if (n >= RCS.To - 1 + nDoubleCommandsBefore)
									{
										nDoubleCommandsWithin++;
									}
									nDoubleCommandsBefore++;
								}
							}

							CommandStruct CS = new CommandStruct();
							CS.Command = (byte)(39 + RCS.Repeats);
							CS.CmdValue = (short)((RCS.From - RCS.To)+1); // set to distance of jump in actual commands
							int pos = RCS.From + nDoubleCommandsBefore;
							CommandArray.Insert(pos, CS);

							// compensate in repeats below
							for (int nRestOfArray = RCS.To; nRestOfArray < CommandArray.Count; nRestOfArray++)
							{
								CommandStruct CSCurrent = (CommandStruct)CommandArray[nRestOfArray];
								if (CSCurrent.Command >= 40 && CSCurrent.Command <= 49)
								{
									CommandArray.RemoveAt(nRestOfArray);
									CommandArray.Insert(nRestOfArray, CSCurrent);
								}
							}
						}


						// move repeats to the correct position due to double commands
						ArrayList reOrderRepeatCommandArray = new ArrayList(CommandArray);
						ArrayList repPos = new ArrayList();						
						for( int i=0; i < reOrderRepeatCommandArray.Count; i++ )
						{
							CommandStruct CS = (CommandStruct)reOrderRepeatCommandArray[i];
							if (CS.Command == 10)
							{
								for( int j=i; j<reOrderRepeatCommandArray.Count; j++ )
								{
									CommandStruct CSTemp = (CommandStruct)reOrderRepeatCommandArray[j];
									if (CSTemp.Command >= 40 && CSTemp.Command <= 49 )
									{
										reOrderRepeatCommandArray.RemoveAt(j);
										j++;
										reOrderRepeatCommandArray.Insert( j, CSTemp );
										if( ((CommandStruct)reOrderRepeatCommandArray[j-1]).Command == 60 )
										{
											//we're in the middle of a row-select and need to swap down
											CommandStruct CSSwap = (CommandStruct)reOrderRepeatCommandArray[j+1];
											reOrderRepeatCommandArray[j+1] = CSTemp;
											reOrderRepeatCommandArray[j] = CSSwap;
											j++;
										}
									}
								}
							}
						}

						// repeats have the correct position at this point
						for( int i=0; i < reOrderRepeatCommandArray.Count; i++ )
						{
							CommandStruct CS = (CommandStruct)reOrderRepeatCommandArray[i];

							if (CS.Command >= 40 && CS.Command <= 49)
							{
								// retrieve actual number of steps
								int visualSteps = CS.CmdValue;
								// count backwards in the program
								for( int j=i-1; visualSteps > 0; j-- )
								{
									CommandStruct CSJump = (CommandStruct)reOrderRepeatCommandArray[j];
									if( j>0 && (((CommandStruct)reOrderRepeatCommandArray[j-1]).Command == 10 ||
										((CommandStruct)reOrderRepeatCommandArray[j-1]).Command == 60 ))
									{
										continue;
									}
									visualSteps--;
									// set the jump point
									CS.CmdValue = (short)j;
								}
								// adjust for j starting with 0
								CS.CmdValue++;
								reOrderRepeatCommandArray[i] = CS;
							}
						}

						CommandArray = reOrderRepeatCommandArray;

						System.Diagnostics.Trace.WriteLine( "-------------" );

						// write commands into blocks
						for (int i = 0; i < CommandArray.Count; i++)
						{
							System.Diagnostics.Trace.WriteLine( i+1 + ":" + ((CommandStruct)CommandArray[i]).Command + ":" + ((CommandStruct)CommandArray[i]).CmdValue  );
							int cc = ((CommandStruct)CommandArray[i]).Command;
							byte dd = Utilities.LoByte(((CommandStruct)CommandArray[i]).CmdValue);
							byte ee = Utilities.HiByte(((CommandStruct)CommandArray[i]).CmdValue);

							file[nProgram, 106 + i] = ((CommandStruct)CommandArray[i]).Command;
							file[nProgram, 156 + (i * 2)] = Utilities.LoByte(((CommandStruct)CommandArray[i]).CmdValue);
							file[nProgram, 157 + (i * 2)] = Utilities.HiByte(((CommandStruct)CommandArray[i]).CmdValue);
						}

						// terminate
						// no need - c# fills with zeroes
						/*
						file[nProgram, 106 + CommandArray.Count + 1] = 0;
						file[nProgram, 156 + ((CommandArray.Count + 1) * 2)] = 0;
						file[nProgram, 157 + ((CommandArray.Count + 1) * 2)] = 0;
						*/
					}

					// serial put to AQ3
					Cursor.Current = Cursors.WaitCursor;
					RS232 rs232 = null;
					try 
					{
						if( !Utilities.IsMachineConnected(GetCommPort()) )
						{
							MessageBox.Show("Could not transmit data to BNX1536", "Upload File to BNX1536", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							break;
						}

						if( Utilities.SupportColumns && Utilities.GetDeviceCode(GetCommPort()) < 0x13 )
						{
							MessageBox.Show("Your hardware is not supported with this software version.", "Incompatible hardware", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							break;
						}
						if( !Utilities.SupportColumns && Utilities.GetDeviceCode(GetCommPort()) > 0x12 )
						{
							MessageBox.Show("Your hardware is not supported with this software version.", "Incompatible hardware", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							break;
						}

						rs232 = new RS232(GetCommPort());
						rs232.CheckTimeout = true;
						rs232.PutFile(file);
						rs232.Dispose();
					}
					catch (Exception)
					{
						//MessageBox.Show(e.Message, "Upload", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						
						// Custom message
						MessageBox.Show("Could not transmit data to BNX1536", "Upload File to BNX1536", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						if( rs232 != null )
						{
							rs232.Dispose();
						}
					}
					Cursor.Current = Cursors.Default;

					break;
				}
			}
		}

		string SetPlateRows( uint nRows, int plateType, int stepFactor  )
		{
			string strRows = "";
			for (int nBit= 0; nBit < 32; nBit++)
			{
				uint BitSet = (uint)Math.Pow(2, nBit);
				if (0 != (nRows & BitSet))
				{
					if( stepFactor == 2 )
					{
						int num = (nBit+1)*2;
						strRows += string.Format( " {0} {1}", num-1, num );
					}
					else
					{
						strRows += " " + (nBit+1).ToString();
					}
				}
			}
			if( plateType < 4 )
			{
				strRows = Utilities.ColumnsToRows( strRows );
			}
			return strRows;
		}
//		string SetPlateRows( uint nRows, int plateType, int stepFactor  )
//		{
//			string strRows = "";
//			for (int nBit= 0; nBit < 32; nBit++)
//			{
//				uint BitSet = (uint)Math.Pow(2, nBit);
//				if (0 != (nRows & BitSet))
//				{
//					if( plateType < 4 ) //rows
//					{
//						if (nBit < 26)
//						{
//							strRows += (char)(nBit + 65);
//						}
//						else
//						{
//							strRows += (char)(nBit + 65 - 26);
//							strRows += (char)(nBit + 65 - 26);
//						}
//					}
//					else //cols
//					{
//						if( stepFactor == 2 )
//						{
//							int num = (nBit+1)*2;
//							strRows += string.Format( " {0} {1}", num-1, num );
//						}
//						else
//						{
//							strRows += " " + (nBit+1).ToString();
//						}
//					}
//				}
//			}
//			return strRows;
//		}

		uint GetPlateRowOld( string strPlateRows )
		{
			uint nPlateRows = 0;
			for (int nRowSelected = 0; nRowSelected < strPlateRows.Length; nRowSelected++)
			{
				char cCurrent = strPlateRows[nRowSelected];
				char cNext = (char)0;
				if (strPlateRows.Length > nRowSelected + 1)
				{
					cNext = strPlateRows[nRowSelected + 1];
				}

				int nRow = 0;
				if (cCurrent == cNext)
				{
					// special row AA, BB, CC, DD or EE 
					nRow = cCurrent - 'A' + 26;
					nRowSelected++;
				}
				else
				{
					nRow = cCurrent - 'A';
				}
				uint nBit = 1;
				nBit = nBit << nRow;
				nPlateRows |= nBit;
			}
			return nPlateRows;
		}

		uint GetPlateRowNew( string strPlateRows, byte format )
		{
			// 96 card uses every row
			// 384, 1536 uses every other row only since the head has double rows
			int stepFactor = 1;
			switch( format )
			{
				case 2:
				case 3:
				case 5:
				case 6:
					stepFactor = 2;
					break;
			}

			if( format > 3 )
			{
				//convert column numbers to letters so we can use the same code below
				//instead of rewriting it
				//i just want this to work :) - mikael (hack and conquer)
				strPlateRows = Utilities.ColumnsToRows( strPlateRows );
			}

			uint nPlateRows = 0;
			for (int nRowSelected = 0; nRowSelected < strPlateRows.Length; nRowSelected += stepFactor)
			{
				char cCurrent = strPlateRows[nRowSelected];
				char cNext = (char)0;
				if (strPlateRows.Length > nRowSelected + 1)
				{
					cNext = strPlateRows[nRowSelected + 1];
				}

				int nRow = 0;
				if (cCurrent == cNext)
				{
					// special row AA, BB, CC, DD or EE 
					nRow = cCurrent - 'A' + 26;
					nRowSelected += stepFactor;
				}
				else
				{
					nRow = cCurrent - 'A';
				}
				nRow = nRow / stepFactor;
				uint nBit = 1;
				nBit = nBit << nRow;
				nPlateRows |= nBit;
			}
			return nPlateRows;
		}

		public void SavePlate(plateForm PF, TreeView tw)
		{
			// get all parametres to save
			string strPlateName = PF.textBoxName.Text;
			string strPlateTypeNo = PF.textBoxTypeNo.Text;
			int nPlateFormat = PF.comboBoxFormat.SelectedIndex + 1;
			string strPlateFormat = nPlateFormat.ToString();
			string strPlateYo = PF.textBoxYo.Text;
			string strPlateDbwc = PF.textBoxdbwc.Text;
			string strPlateYo2 = PF.textBoxYo2.Text;
			string strPlateDbwc2 = PF.textBoxdbwc2.Text;
			string strPlateHeight = PF.textBoxHeight.Text;
			string strPlateDepth = PF.textBoxDepth.Text;
			string strPlateMaxVolume = PF.textBoxMaxVolume.Text;
			//string strPlateASPOffset = PF.textBoxASPOffset.Text;
			string strPlateBottomWellDiameter = PF.textBoxWellDiameter.Text;
			int nPlateWellShape = PF.comboBoxShape.SelectedIndex;
			string strPlateWellShape = nPlateWellShape.ToString();

			// convert parametres of type "double" to en-US culture
			double dPlateYo = Convert.ToDouble(PF.textBoxYo.Text);
			double dPlateDbwc = Convert.ToDouble(PF.textBoxdbwc.Text);
			double dPlateYo2 = Convert.ToDouble(PF.textBoxYo2.Text);
			double dPlateDbwc2 = Convert.ToDouble(PF.textBoxdbwc2.Text);
			double dPlateHeight = Convert.ToDouble(PF.textBoxHeight.Text);
			double dPlateDepth = Convert.ToDouble(PF.textBoxDepth.Text);
			double dPlateMaxVolume = Convert.ToDouble(PF.textBoxMaxVolume.Text);
			double dPlateBottomWellDiameter = Convert.ToDouble(PF.textBoxWellDiameter.Text);

			bool bLoBase = PF.LoBaseCb.Checked;

			//double dPlateASPOffset = Convert.ToDouble(PF.textBoxASPOffset.Text);
			strPlateYo = dPlateYo.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateDbwc = dPlateDbwc.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateYo2 = dPlateYo2.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateDbwc2 = dPlateDbwc2.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateHeight = dPlateHeight.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateDepth = dPlateDepth.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateMaxVolume = dPlateMaxVolume.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
//			strPlateASPOffset = dPlateASPOffset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
			strPlateBottomWellDiameter = dPlateBottomWellDiameter.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						
			if (strPlateName.Length == 0)
			{
				return;
			}

			bool bPlateFound = false;
			bool bGroupFound = false;
			XmlNodeList nlPlates = null;
			XmlNode xmlNodePlates = null;
			XmlNode xmlNodeGroup = null;
			XmlNode xmlNodePlate = null;
			XmlAttribute xmlAttributePlateName;

			// find plate in xml data
			nlPlates = m_xmlData.GetElementsByTagName("plates");
			for (int nPlatesNode = 0; nPlatesNode < nlPlates.Count; nPlatesNode++)
			{
				xmlNodePlates = nlPlates[nPlatesNode];
				
				for (int nGroup = 0; nGroup < xmlNodePlates.ChildNodes.Count; nGroup++)
				{
					xmlNodeGroup = nlPlates[nPlatesNode].ChildNodes[nGroup];

					if (xmlNodeGroup.Attributes["name"].Value == strPlateFormat)
					{
						bGroupFound = true;

						// find all plates within group collection
						for (int nPlate = 0; nPlate < xmlNodeGroup.ChildNodes.Count; nPlate++)
						{
							xmlNodePlate = xmlNodeGroup.ChildNodes[nPlate];

							if (xmlNodePlate.Attributes["name"].Value == strPlateName)
							{
								bPlateFound = true;

								// delete plate attributes, but preserve name attribute
								xmlNodePlate.RemoveAll();
								xmlAttributePlateName = m_xmlData.CreateAttribute("name");
								xmlAttributePlateName.Value = strPlateName;
								xmlNodePlate.Attributes.Append(xmlAttributePlateName);

								// no need to iterate further
								break;
							}
						}

						// no need to iterate further
						break;
					}					
				}				
			}

			// plate node found? create it if not
			if (!bPlateFound)
			{
				// ...but first check if group node was found. create it if not
				if (!bGroupFound)
				{
					// ...but first check if plates node was found. create it if not
					if (nlPlates.Count < 1)
					{
						// create plates node
						XmlNodeList nlData = m_xmlData.GetElementsByTagName("data");
						if (nlData.Count < 1)
						{
							// create data node
							XmlNode root = m_xmlData.DocumentElement;
							XmlElement xmlDataElement = m_xmlData.CreateElement("data");
							root.AppendChild(xmlDataElement);

							// create plates node
							xmlNodePlates = m_xmlData.CreateElement("plates");
							xmlDataElement.AppendChild(xmlNodePlates);
						}
						else
						{
							// create plates node
							xmlNodePlates = m_xmlData.CreateElement("plates");
							nlData[0].AppendChild(xmlNodePlates);
						}					
					}
		
					// create group node
					xmlNodeGroup = m_xmlData.CreateElement("group");
					XmlAttribute xmlAttributeGroupFormat = m_xmlData.CreateAttribute("name");
					xmlAttributeGroupFormat.Value = strPlateFormat;
					xmlNodeGroup.Attributes.Append(xmlAttributeGroupFormat);
					xmlNodePlates.AppendChild(xmlNodeGroup);
				}

				// create plate node
				xmlNodePlate = m_xmlData.CreateElement("plate");
				xmlNodeGroup.AppendChild(xmlNodePlate);
			}

			// new attributes in plate node
			xmlAttributePlateName = m_xmlData.CreateAttribute("name");
			xmlAttributePlateName.Value = strPlateName;
			xmlNodePlate.Attributes.Append(xmlAttributePlateName);

			XmlAttribute xmlAttributePlateTypeNo = m_xmlData.CreateAttribute("type_no");
			xmlAttributePlateTypeNo.Value = strPlateTypeNo;
			xmlNodePlate.Attributes.Append(xmlAttributePlateTypeNo);
			
			XmlAttribute xmlAttributePlateFormat = m_xmlData.CreateAttribute("format");
			xmlAttributePlateFormat.Value = strPlateFormat;
			xmlNodePlate.Attributes.Append(xmlAttributePlateFormat);

			XmlAttribute xmlAttributePlateYo = m_xmlData.CreateAttribute("yo");
			xmlAttributePlateYo.Value = strPlateYo;
			xmlNodePlate.Attributes.Append(xmlAttributePlateYo);

			XmlAttribute xmlAttributePlateDbwc = m_xmlData.CreateAttribute("dbwc");
			xmlAttributePlateDbwc.Value = strPlateDbwc;
			xmlNodePlate.Attributes.Append(xmlAttributePlateDbwc);

			XmlAttribute xmlAttributePlateYo2 = m_xmlData.CreateAttribute("yo2");
			xmlAttributePlateYo2.Value = strPlateYo2;
			xmlNodePlate.Attributes.Append(xmlAttributePlateYo2);

			XmlAttribute xmlAttributePlateDbwc2 = m_xmlData.CreateAttribute("dbwc2");
			xmlAttributePlateDbwc2.Value = strPlateDbwc2;
			xmlNodePlate.Attributes.Append(xmlAttributePlateDbwc2);

			XmlAttribute xmlAttributePlateHeight = m_xmlData.CreateAttribute("height");
			xmlAttributePlateHeight.Value = strPlateHeight;
			xmlNodePlate.Attributes.Append(xmlAttributePlateHeight);

			XmlAttribute xmlAttributePlateDepth = m_xmlData.CreateAttribute("depth");
			xmlAttributePlateDepth.Value = strPlateDepth;
			xmlNodePlate.Attributes.Append(xmlAttributePlateDepth);

			XmlAttribute xmlAttributePlateMaxVolume = m_xmlData.CreateAttribute("max_volume");
			xmlAttributePlateMaxVolume.Value = strPlateMaxVolume;
			xmlNodePlate.Attributes.Append(xmlAttributePlateMaxVolume);

			XmlAttribute xmlAttributePlateLoBase = m_xmlData.CreateAttribute("lobase");
			xmlAttributePlateLoBase.Value = bLoBase.ToString();
			xmlNodePlate.Attributes.Append(xmlAttributePlateLoBase);

			XmlAttribute xmlAttributePlateDiameter = m_xmlData.CreateAttribute("diameter");
			xmlAttributePlateDiameter.Value = strPlateBottomWellDiameter;
			xmlNodePlate.Attributes.Append(xmlAttributePlateDiameter);

			XmlAttribute xmlAttributePlateWellShape = m_xmlData.CreateAttribute("shape");
			xmlAttributePlateWellShape.Value = strPlateWellShape;
			xmlNodePlate.Attributes.Append(xmlAttributePlateWellShape);

//			XmlAttribute xmlAttributePlateASPOffset = m_xmlData.CreateAttribute("asp_offset");
//			xmlAttributePlateASPOffset.Value = strPlateASPOffset;
//			xmlNodePlate.Attributes.Append(xmlAttributePlateASPOffset);

			m_xmlData.Save(m_strXmlFilename);

			// update tree
			string strVisibleTreeFormat = "";
			switch (nPlateFormat)
			{
				case 1:
					strVisibleTreeFormat = "96";
					break;
				case 2:
					strVisibleTreeFormat = "384";
					break;
				case 3:
					strVisibleTreeFormat = "1536";
					break;
			}

			for (int i = 0; i < tw.Nodes.Count; i++)
			{
				if (tw.Nodes[i].Text.ToLower() == "plates")
				{
					for (int j = 0; j < tw.Nodes[i].Nodes.Count; j++)
					{
						if (tw.Nodes[i].Nodes[j].Text.ToLower() == strVisibleTreeFormat)
						{
							TreeNode treeNode = new TreeNode(strPlateName, 1, 1);
							tw.Nodes[i].Nodes[j].Nodes.Add(treeNode);
							break;
						}
					}
				}
			}
		}

		public void LoadPlate(string strPlateName, string strPlateType, plateForm PF)
		{
			bool bPlateFound = false;
			XmlNode xmlNodePlate = null;

			// find group
			XmlNodeList nlGroup = m_xmlData.GetElementsByTagName("group");
			for (int nGroup = 0; nGroup < nlGroup.Count; nGroup++)
			{
				XmlNode xmlNodeGroup = nlGroup[nGroup];
				if (xmlNodeGroup.Attributes["name"].Value == strPlateType)
				{
					// find plate
					for (int nPlate = 0; nPlate < xmlNodeGroup.ChildNodes.Count; nPlate++)
					{
						xmlNodePlate = xmlNodeGroup.ChildNodes[nPlate];
						if (xmlNodePlate.Attributes["name"].Value == strPlateName)
						{
							bPlateFound = true;

							// no need to iterate further
							break;
						}
					}

					// no need to iterate further
					break;
				}
			}

			if (bPlateFound)
			{
				try
				{
					// convert to current culture
					double dPlateYo = Convert.ToDouble(xmlNodePlate.Attributes["yo"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					double dPlateDbwc = Convert.ToDouble(xmlNodePlate.Attributes["dbwc"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					double dPlateYo2 = 0;
					double dPlateDbwc2 = 0;
					bool bLoBase = false;
					//backwards compability check
					if( xmlNodePlate.Attributes["yo2"] != null )
					{
						dPlateYo2 = Convert.ToDouble(xmlNodePlate.Attributes["yo2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						dPlateDbwc2 = Convert.ToDouble(xmlNodePlate.Attributes["dbwc2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						bLoBase = bool.Parse( xmlNodePlate.Attributes["lobase"].Value );
					}
					double dPlateHeight = Convert.ToDouble(xmlNodePlate.Attributes["height"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					double dPlateDepth = Convert.ToDouble(xmlNodePlate.Attributes["depth"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					double dPlateMaxVolume = Convert.ToDouble(xmlNodePlate.Attributes["max_volume"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					double dPlateBottomWellDiameter = 0;
					int nPlateWellShape = 0;
					if( xmlNodePlate.Attributes["diameter"] != null )
					{
						dPlateBottomWellDiameter = Convert.ToDouble(xmlNodePlate.Attributes["diameter"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						nPlateWellShape = Convert.ToInt32(xmlNodePlate.Attributes["shape"].Value);
					}
//					double dPlateASPOffset = Convert.ToDouble(xmlNodePlate.Attributes["asp_offset"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));

					PF.comboBoxFormat.SelectedIndex = Convert.ToInt32(xmlNodePlate.Attributes["format"].Value) - 1;
					PF.textBoxName.Text = xmlNodePlate.Attributes["name"].Value;
					PF.textBoxTypeNo.Text = xmlNodePlate.Attributes["type_no"].Value;
					PF.textBoxYo.Text = dPlateYo.ToString("F2");
					PF.textBoxdbwc.Text = dPlateDbwc.ToString("F3");
					PF.textBoxYo2.Text = dPlateYo2.ToString("F2");
					PF.textBoxdbwc2.Text = dPlateDbwc2.ToString("F3");
					PF.textBoxHeight.Text = dPlateHeight.ToString("F2");
					PF.textBoxDepth.Text = dPlateDepth.ToString("F2");
					PF.textBoxMaxVolume.Text = dPlateMaxVolume.ToString("F1");
					PF.LoBaseCb.Checked = bLoBase;
					PF.textBoxWellDiameter.Text = dPlateBottomWellDiameter.ToString("F2");
					PF.comboBoxShape.SelectedIndex = nPlateWellShape;
//					PF.textBoxASPOffset.Text = dPlateASPOffset.ToString("F1");
					
				}
				catch (Exception e)
				{
					e = e;
				}
			}
		}

		public void SaveLiquid(liquidForm LF, TreeView tw)
		{
			// get all parametres to save
			string strLiquidName = LF.textBoxName.Text;
			string strLiquidFactor = LF.textBoxLiquidFactor.Text;

			// convert parametres of type "double" to en-US culture
			double dLiquidFactor = Convert.ToDouble(LF.textBoxLiquidFactor.Text);
			strLiquidFactor = dLiquidFactor.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));

			if (strLiquidName.Length == 0)
			{
				return;
			}

			bool bLiquidFound = false;
			XmlNodeList nlLiquids = null;
			XmlNode xmlNodeLiquids = null;
			XmlNode xmlNodeLiquid = null;
			XmlAttribute xmlAttributeLiquidName;
			
			// find liquid in xml data
			nlLiquids = m_xmlData.GetElementsByTagName("liquids");
			for (int nLiquidsNode = 0; nLiquidsNode < nlLiquids.Count; nLiquidsNode++)
			{
				// find all liquids within collection
				for (int nLiquid = 0; nLiquid < nlLiquids[nLiquidsNode].ChildNodes.Count; nLiquid++)
				{
					xmlNodeLiquid = nlLiquids[nLiquidsNode].ChildNodes[nLiquid];
					if (xmlNodeLiquid.Attributes["name"].Value == strLiquidName)
					{
						bLiquidFound = true;

						// delete liquid attributes, but preserve name attribute
						xmlNodeLiquid.RemoveAll();
						xmlAttributeLiquidName = m_xmlData.CreateAttribute("name");
						xmlAttributeLiquidName.Value = strLiquidName;
						xmlNodeLiquid.Attributes.Append(xmlAttributeLiquidName);

						// no need to iterate further
						break;
					}
				}
			}

			if (!bLiquidFound)
			{
				if (nlLiquids.Count < 1)
				{
					// create liquids node
					XmlNodeList nlData = m_xmlData.GetElementsByTagName("data");
					if (nlData.Count < 1)
					{
						// create data node
						XmlNode root = m_xmlData.DocumentElement;
						XmlElement xmlDataElement = m_xmlData.CreateElement("data");
						root.AppendChild(xmlDataElement);

						// create liquids node
						xmlNodeLiquids = m_xmlData.CreateElement("liquids");
						xmlDataElement.AppendChild(xmlNodeLiquids);
					}
					else
					{
						// create liquids node
						xmlNodeLiquids = m_xmlData.CreateElement("liquids");
						nlData[0].AppendChild(xmlNodeLiquids);
					}					
				}
				else
				{
					xmlNodeLiquids = nlLiquids[0];
				}

				// create liquid node
				xmlNodeLiquid = m_xmlData.CreateElement("liquid");
				xmlNodeLiquids.AppendChild(xmlNodeLiquid);
			}

			// new attributes in liquid node
			xmlAttributeLiquidName = m_xmlData.CreateAttribute("name");
			xmlAttributeLiquidName.Value = strLiquidName;
			xmlNodeLiquid.Attributes.Append(xmlAttributeLiquidName);

			XmlAttribute xmlAttributeLiquidFactor = m_xmlData.CreateAttribute("liquid_factor");
			xmlAttributeLiquidFactor.Value = strLiquidFactor;
			xmlNodeLiquid.Attributes.Append(xmlAttributeLiquidFactor);

			m_xmlData.Save(m_strXmlFilename);

			// update tree
			for (int i = 0; i < tw.Nodes.Count; i++)
			{
				if (tw.Nodes[i].Text.ToLower() == "liquids")
				{
					TreeNode treeNode = new TreeNode(strLiquidName, 6, 6);
					tw.Nodes[i].Nodes.Add(treeNode);
					break;
				}
			}
		}

		public void LoadLiquid(string strLiquidName, liquidForm LF)
		{
			bool bLiquidFound = false;
			XmlNode xmlNodeLiquid = null;
			
			// find liquid in xml data
			XmlNodeList nlLiquids = m_xmlData.GetElementsByTagName("liquids");
			for (int nLiquidsNode = 0; nLiquidsNode < nlLiquids.Count; nLiquidsNode++)
			{
				// find all liquids within collection
				for (int nLiquid = 0; nLiquid < nlLiquids[nLiquidsNode].ChildNodes.Count; nLiquid++)
				{
					xmlNodeLiquid = nlLiquids[nLiquidsNode].ChildNodes[nLiquid];
					if (xmlNodeLiquid.Attributes["name"].Value == strLiquidName)
					{
						bLiquidFound = true;
						
						// no need to iterate further
						break;
					}
				}
			}

			if (bLiquidFound)
			{
				try
				{
					// convert to current culture
					double dLiquidFactor = Convert.ToDouble(xmlNodeLiquid.Attributes["liquid_factor"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));

					LF.textBoxName.Text = xmlNodeLiquid.Attributes["name"].Value;					
					LF.textBoxLiquidFactor.Text = dLiquidFactor.ToString("F2");
				}
				catch (Exception e)
				{
					e = e;
				}
			}	
		}

		public void SaveUser(userForm UF, TreeView tw)
		{
			string strUserName = UF.textBoxUserName.Text;
			string strPassword = UF.textBoxPassword.Text;
			string strLevel = (UF.comboBoxUserLevel.SelectedIndex + 1).ToString();

			if (strUserName.Length == 0)
			{
				return;
			}

			bool bUserFound = false;
			XmlNodeList nlUsers = null;
			XmlNode xmlNodeUsers = null;
			XmlNode xmlNodeUser = null;
			XmlAttribute xmlAttributeUserName;
			
			// find user in xml data
			nlUsers = m_xmlData.GetElementsByTagName("users");
			for (int nUsersNode = 0; nUsersNode < nlUsers.Count; nUsersNode++)
			{
				// find all users within collection
				for (int nUser = 0; nUser < nlUsers[nUsersNode].ChildNodes.Count; nUser++)
				{
					xmlNodeUser = nlUsers[nUsersNode].ChildNodes[nUser];
					if (xmlNodeUser.Attributes["name"].Value == strUserName)
					{
						bUserFound = true;

						// delete user attributes... but preserve user name
						xmlNodeUser.RemoveAll();
						xmlAttributeUserName = m_xmlData.CreateAttribute("name");
						xmlAttributeUserName.Value = strUserName;
						xmlNodeUser.Attributes.Append(xmlAttributeUserName);

						// no need to iterate further
						break;
					}
				}
			}

			if (!bUserFound)
			{
				if (nlUsers.Count < 1)
				{
					// create users node
					XmlNodeList nlData = m_xmlData.GetElementsByTagName("data");
					if (nlData.Count < 1)
					{
						// create data node
						XmlNode root = m_xmlData.DocumentElement;
						XmlElement xmlDataElement = m_xmlData.CreateElement("data");
						root.AppendChild(xmlDataElement);

						// create users node
						xmlNodeUsers = m_xmlData.CreateElement("users");
						xmlDataElement.AppendChild(xmlNodeUsers);
					}
					else
					{
						// create users node
						xmlNodeUsers = m_xmlData.CreateElement("users");
						nlData[0].AppendChild(xmlNodeUsers);
					}					
				}
				else
				{
					xmlNodeUsers = nlUsers[0];
				}

				// create user node
				xmlNodeUser = m_xmlData.CreateElement("user");
				xmlNodeUsers.AppendChild(xmlNodeUser);
			}

			// new attributes in user node
			xmlAttributeUserName = m_xmlData.CreateAttribute("name");
			xmlAttributeUserName.Value = strUserName;
			xmlNodeUser.Attributes.Append(xmlAttributeUserName);

			XmlAttribute xmlAttributePassword = m_xmlData.CreateAttribute("password");
			xmlAttributePassword.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(strPassword));
			xmlNodeUser.Attributes.Append(xmlAttributePassword);

			XmlAttribute xmlAttributeLevel = m_xmlData.CreateAttribute("level");
			xmlAttributeLevel.Value = strLevel;
			xmlNodeUser.Attributes.Append(xmlAttributeLevel);

			m_xmlData.Save(m_strXmlFilename);

			// update tree
			for (int i = 0; i < tw.Nodes.Count; i++)
			{
				if (tw.Nodes[i].Text.ToLower() == "users" )
				{
					bool add = true;
					foreach( TreeNode name in tw.Nodes[i].Nodes )
					{
						if( name.Text == strUserName )
						{
							add = false;
							break;
						}
					}
					// don't add if we overwrite
					if( add )
					{
						TreeNode treeNode = new TreeNode(strUserName, 3, 3);
						tw.Nodes[i].Nodes.Add(treeNode);
					}
					break;
				}
			}
		}

		public void LoadUser(string strUserName, userForm UF)
		{
			bool bUserFound = false;
			XmlNode xmlNodeUser = null;
			
			// find user in xml data
			XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");
			for (int nUsersNode = 0; nUsersNode < nlUsers.Count; nUsersNode++)
			{
				// find all users within collection
				for (int nUser = 0; nUser < nlUsers[nUsersNode].ChildNodes.Count; nUser++)
				{
					xmlNodeUser = nlUsers[nUsersNode].ChildNodes[nUser];
					if (xmlNodeUser.Attributes["name"].Value == strUserName)
					{
						bUserFound = true;
						
						// no need to iterate further
						break;
					}
				}
			}

			if (bUserFound)
			{
				try
				{
					UF.textBoxUserName.Text = xmlNodeUser.Attributes["name"].Value;					
					UF.textBoxPassword.Text = Encoding.UTF8.GetString(Convert.FromBase64String(xmlNodeUser.Attributes["password"].Value));
					UF.textBoxRetypePassword.Text = UF.textBoxPassword.Text;
					UF.comboBoxUserLevel.SelectedIndex = Convert.ToInt32(xmlNodeUser.Attributes["level"].Value) - 1;
				}
				catch (Exception e)
				{
					e = e;
				}
			}	
		}

		public void LoadUsersForLogin(Login login)
		{
			XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");

			if (nlUsers.Count > 0)
			{
				for (int nUser = 0; nUser < nlUsers[0].ChildNodes.Count; nUser++)
				{
					XmlNode xmlNodeUser = nlUsers[0].ChildNodes[nUser];
					login.comboBoxUsername.Items.Add(xmlNodeUser.Attributes["name"].Value);
				}
			}
		}

		public int VerifyUser(string strUserName, string strPassword)
		{
			bool bUserFound = false;
			XmlNode xmlNodeUser = null;
			int nRetVal = 0;
			
			// find user in xml data
			XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");
			for (int nUsersNode = 0; nUsersNode < nlUsers.Count; nUsersNode++)
			{
				// find all users within collection
				for (int nUser = 0; nUser < nlUsers[nUsersNode].ChildNodes.Count; nUser++)
				{
					xmlNodeUser = nlUsers[nUsersNode].ChildNodes[nUser];
					if (xmlNodeUser.Attributes["name"].Value == strUserName)
					{
						bUserFound = true;
						
						// no need to iterate further
						break;
					}
				}
			}

			if (bUserFound)
			{
				try
				{
					if (strPassword == Encoding.UTF8.GetString(Convert.FromBase64String(xmlNodeUser.Attributes["password"].Value)))
					{
						nRetVal = Convert.ToInt32(xmlNodeUser.Attributes["level"].Value);
					}					
				}
				catch (Exception e)
				{
					e = e;

					if (strPassword.Length == 0)
					{
						nRetVal = Convert.ToInt32(xmlNodeUser.Attributes["level"].Value);
					}
				}
			}

			return nRetVal;
		}

		public void SaveConfig(configForm CF)
		{			
			// find config in xml data
			XmlNodeList nlConfig = m_xmlData.GetElementsByTagName("config");

			if (nlConfig.Count > 0)
			{
				nlConfig[0].RemoveAll();

				XmlAttribute xmlAttributeSerialPort = m_xmlData.CreateAttribute("serialport");
				xmlAttributeSerialPort.Value = CF.comPort.Text;
				nlConfig[0].Attributes.Append(xmlAttributeSerialPort);

				m_xmlData.Save(m_strXmlFilename);
			}
		}

		public void LoadConfig(configForm CF)
		{			
			// find config in xml data
			XmlNodeList nlConfig = m_xmlData.GetElementsByTagName("config");

			if (nlConfig.Count > 0)
			{
				try
				{
					int nPort = Convert.ToInt32(nlConfig[0].Attributes["serialport"].Value);
					CF.comPort.SelectedIndex = nPort - 1;
				}
				catch (Exception e)
				{
					e = e;
				}
			}
		}

		public bool CreateFile(string strFileName, string strOwner)
		{
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");

			if (nlFiles.Count > 0)
			{
				for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFileSearh = nlFiles[0].ChildNodes[nFile];
					if (xmlNodeFileSearh.Attributes["name"].Value == strFileName)
					{
						MessageBox.Show("File already exists", "Create file", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}

				XmlElement xmlNodeFile = m_xmlData.CreateElement("file");

				XmlAttribute xmlAttributeFileName = m_xmlData.CreateAttribute("name");
				xmlAttributeFileName.Value = strFileName;
				xmlNodeFile.Attributes.Append(xmlAttributeFileName);

				XmlAttribute xmlAttributeOwner = m_xmlData.CreateAttribute("owner");
				xmlAttributeOwner.Value = strOwner;
				xmlNodeFile.Attributes.Append(xmlAttributeOwner);
				
				nlFiles[0].AppendChild(xmlNodeFile);
				m_xmlData.Save(m_strXmlFilename);
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Create File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}

			return true;
		}

		public bool FileExist(string strFileName, string programName, mainForm mf)
		{
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");

			if (nlFiles.Count > 0)
			{				
				for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFile = nlFiles[0].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strFileName )
					{
						return true;
					}
				}
			}
			return false;
		}

		public void DeleteFile(string strFileName, string strOwner, mainForm mf, TreeView tw)
		{
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");

			if (nlFiles.Count > 0)
			{				
				for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFile = nlFiles[0].ChildNodes[nFile];
					if ((xmlNodeFile.Attributes["name"].Value == strFileName || strFileName == "*") && xmlNodeFile.Attributes["owner"].Value == strOwner)
					{
						nlFiles[0].RemoveChild(xmlNodeFile);
						nFile--; // pay attention here... must have this for multiple file deletes
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Delete File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

			// update tree
			if (tw != null && mf != null)
			{
				for (int i = 0; i < tw.Nodes.Count; i++)
				{
					string strTreeNodeLabel = "";
					if (mf.m_User.Username == strOwner)
					{
						strTreeNodeLabel = "My Files";
					}
					else
					{
						strTreeNodeLabel = strOwner + "'s files";
					}
					if (tw.Nodes[i].Text == strTreeNodeLabel)
					{
						tw.Nodes[i].Remove();
					}
				}
			}

			m_xmlData.Save(m_strXmlFilename);
		}

		public void DeleteProgram(string strFileName, string strProgramName, string strOwner)
		{
			// find file
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");

			if (nlFiles.Count > 0)
			{
				for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFile = nlFiles[0].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strFileName && xmlNodeFile.Attributes["owner"].Value == strOwner)
					{
						for (int nProgram = 0; nProgram < xmlNodeFile.ChildNodes.Count; nProgram++)
						{
							XmlNode xmlNodeProgram = xmlNodeFile.ChildNodes[nProgram];
							if (xmlNodeProgram.Attributes["name"].Value == strProgramName)
							{
								xmlNodeFile.RemoveChild(xmlNodeProgram);
								m_xmlData.Save(m_strXmlFilename);

								// no need to iterate further
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Delete Program", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public bool PlateExist(string strGroupName, string strPlateName)
		{
			XmlNodeList nlPlates = m_xmlData.GetElementsByTagName("plates");

			if (nlPlates.Count > 0)
			{
				for (int nGroup = 0; nGroup < nlPlates[0].ChildNodes.Count; nGroup++)
				{
					XmlNode xmlNodeGroup = nlPlates[0].ChildNodes[nGroup];
					if (xmlNodeGroup.Attributes["name"].Value == strGroupName)
					{
						for (int nPlate = 0; nPlate < xmlNodeGroup.ChildNodes.Count; nPlate++)
						{
							XmlNode xmlNodePlate = xmlNodeGroup.ChildNodes[nPlate];
							if (xmlNodePlate.Attributes["name"].Value == strPlateName)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public void DeletePlate(string strGroupName, string strPlateName, TreeView tw)
		{
			if (strGroupName == "96")
			{
				strGroupName = "1";
			}
			else if (strGroupName == "384")
			{
				strGroupName = "2";
			}
			else if (strGroupName == "1536")
			{
				strGroupName = "3";
			}
			
			string strTreeGroupName = "";
			if (strGroupName == "1")
			{
				strTreeGroupName = "96";
			}
			else if (strGroupName == "2")
			{
				strTreeGroupName = "384";
			}
			else if (strGroupName == "3")
			{
				strTreeGroupName = "1536";
			}

			// find group
			XmlNodeList nlPlates = m_xmlData.GetElementsByTagName("plates");

			if (nlPlates.Count > 0)
			{
				for (int nGroup = 0; nGroup < nlPlates[0].ChildNodes.Count; nGroup++)
				{
					XmlNode xmlNodeGroup = nlPlates[0].ChildNodes[nGroup];
					if (xmlNodeGroup.Attributes["name"].Value == strGroupName)
					{
						for (int nPlate = 0; nPlate < xmlNodeGroup.ChildNodes.Count; nPlate++)
						{
							XmlNode xmlNodePlate = xmlNodeGroup.ChildNodes[nPlate];
							if (xmlNodePlate.Attributes["name"].Value == strPlateName)
							{
								xmlNodeGroup.RemoveChild(xmlNodePlate);
								m_xmlData.Save(m_strXmlFilename);

								// update tree
								for (int i = 0; i < tw.Nodes.Count; i++)
								{
									if (tw.Nodes[i].Text.ToLower() == "plates")
									{
										for (int j = 0; j < tw.Nodes[i].Nodes.Count; j++)
										{
											if (tw.Nodes[i].Nodes[j].Text == strTreeGroupName)
											{
												for (int k = 0; k < tw.Nodes[i].Nodes[j].Nodes.Count; k++)
												{
													if (tw.Nodes[i].Nodes[j].Nodes[k].Text == strPlateName)
													{
														tw.Nodes[i].Nodes[j].Nodes[k].Remove();
														break;
													}
												}
											}
										}
										break;
									}
								}
								break;
							}
						}
						break;
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Delete Plate", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public bool LiquidExist(string strLiquidName)
		{
			XmlNodeList nlLiquids = m_xmlData.GetElementsByTagName("liquids");

			if (nlLiquids.Count > 0)
			{
				for (int nLiquid = 0; nLiquid < nlLiquids[0].ChildNodes.Count; nLiquid++)
				{
					XmlNode xmlNodeLiquid = nlLiquids[0].ChildNodes[nLiquid];
					if (xmlNodeLiquid.Attributes["name"].Value == strLiquidName)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void DeleteLiquid(string strLiquidName, TreeView tw)
		{
			XmlNodeList nlLiquids = m_xmlData.GetElementsByTagName("liquids");

			if (nlLiquids.Count > 0)
			{
				for (int nLiquid = 0; nLiquid < nlLiquids[0].ChildNodes.Count; nLiquid++)
				{
					XmlNode xmlNodeLiquid = nlLiquids[0].ChildNodes[nLiquid];
					if (xmlNodeLiquid.Attributes["name"].Value == strLiquidName)
					{
						nlLiquids[0].RemoveChild(xmlNodeLiquid);
						m_xmlData.Save(m_strXmlFilename);

						// update tree
						for (int i = 0; i < tw.Nodes.Count; i++)
						{
							if (tw.Nodes[i].Text.ToLower() == "liquids")
							{
								for (int j = 0; j < tw.Nodes[i].Nodes.Count; j++)
								{
									if (tw.Nodes[i].Nodes[j].Text == strLiquidName)
									{
										tw.Nodes[i].Nodes[j].Remove();
										break;
									}
								}
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Delete Liquid", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public int CountAdministrators()
		{
			int nRetVal = 0;

			XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");

			if (nlUsers.Count > 0)
			{
				for (int nUser = 0; nUser < nlUsers[0].ChildNodes.Count; nUser++)
				{
					XmlNode xmlNodeUser = nlUsers[0].ChildNodes[nUser];
					if (xmlNodeUser.Attributes["level"].Value == "3")
					{
						nRetVal++;
					}
				}
			}
			
			return nRetVal;
		}
		
		public bool UserExist(string strUserName)
		{
			XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");

			if (nlUsers.Count > 0)
			{
				for (int nUser = 0; nUser < nlUsers[0].ChildNodes.Count; nUser++)
				{
					XmlNode xmlNodeUser = nlUsers[0].ChildNodes[nUser];
					if (xmlNodeUser.Attributes["name"].Value == strUserName)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void DeleteUser(string strUserName, TreeView tw, mainForm mf)
		{
			XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");

			if (nlUsers.Count > 0)
			{
				for (int nUser = 0; nUser < nlUsers[0].ChildNodes.Count; nUser++)
				{
					XmlNode xmlNodeUser = nlUsers[0].ChildNodes[nUser];
					if (xmlNodeUser.Attributes["name"].Value == strUserName)
					{
						nlUsers[0].RemoveChild(xmlNodeUser);
						m_xmlData.Save(m_strXmlFilename);

						// update tree
						for (int i = 0; i < tw.Nodes.Count; i++)
						{
							if (tw.Nodes[i].Text.ToLower() == "users")
							{
								for (int j = 0; j < tw.Nodes[i].Nodes.Count; j++)
								{
									if (tw.Nodes[i].Nodes[j].Text == strUserName)
									{
										tw.Nodes[i].Nodes[j].Remove();
										break;
									}
								}
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Delete User", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

			// delete all files of this user
			DeleteFile("*", strUserName, mf, tw);
		}

		public void RenameFile(string strOldFileName, string strNewFileName, string strUsername)
		{
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");

			if (nlFiles.Count > 0)
			{
				for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFile = nlFiles[0].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strOldFileName && xmlNodeFile.Attributes["owner"].Value == strUsername)
					{
						xmlNodeFile.Attributes["name"].Value = strNewFileName;
						m_xmlData.Save(m_strXmlFilename);

						// no need to iterate further
						break;
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Create File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void RenameProgram(string strFileName, string strOldProgramName, string strNewProgramName, string strUsername)
		{
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");

			if (nlFiles.Count > 0)
			{
				for (int nFile = 0; nFile < nlFiles[0].ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFile = nlFiles[0].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strFileName && xmlNodeFile.Attributes["owner"].Value == strUsername)
					{
						// find program
						for (int nProgram = 0; nProgram < nlFiles[0].ChildNodes[nFile].ChildNodes.Count; nProgram++)
						{
							XmlNode xmlNodeProgram = nlFiles[0].ChildNodes[nFile].ChildNodes[nProgram];
							if (xmlNodeProgram.Attributes["name"].Value == strOldProgramName)
							{
								xmlNodeProgram.Attributes["name"].Value = strNewProgramName;
								m_xmlData.Save(m_strXmlFilename);
							
								// no need to iterate further
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}
			}
			else
			{
				MessageBox.Show("Data file corrupt or not present", "BNX1536 Create File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void SaveProgram(ArrayList CardArray, ArrayList RepeatCardArray, string strFileNameInternal, string strProgramName, string strUsername)
		{
			bool bFileFound = false;
			bool bProgramFound = false;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFile = null;
			XmlNode xmlNodeProgram = null;

			// find file in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strFileNameInternal && xmlNodeFile.Attributes["owner"].Value == strUsername)
					{
						bFileFound = true;

						// find program
						for (int nProgram = 0; nProgram < nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes.Count; nProgram++)
						{
							xmlNodeProgram = nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes[nProgram];
							if (xmlNodeProgram.Attributes["name"].Value == strProgramName)
							{
								bProgramFound = true;

								// delete old program data
								xmlNodeProgram.RemoveAll();
								XmlAttribute xmlAttributeProgramName = m_xmlData.CreateAttribute("name");
								xmlAttributeProgramName.Value = strProgramName;
								xmlNodeProgram.Attributes.Append(xmlAttributeProgramName);

								// no need to iterate further
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}

				if (!bProgramFound)
				{
					XmlElement xmlFilesElement = null;

					if (!bFileFound)
					{
						if (nlFiles.Count < 1)
						{
							// create files node
							XmlNodeList nlData = m_xmlData.GetElementsByTagName("data");
							if (nlData.Count < 1)
							{
								// create data node
								XmlNode root = m_xmlData.DocumentElement;
								XmlElement xmlDataElement = m_xmlData.CreateElement("data");
								root.AppendChild(xmlDataElement);

								xmlFilesElement = m_xmlData.CreateElement("files");
								xmlDataElement.AppendChild(xmlFilesElement);
							}
							else
							{
								xmlFilesElement = m_xmlData.CreateElement("files");
								nlData[0].AppendChild(xmlFilesElement);
							}

							xmlNodeFile = m_xmlData.CreateElement("file");
							XmlAttribute xmlAttributeFileName = m_xmlData.CreateAttribute("name");
							xmlAttributeFileName.Value = strFileNameInternal;
							xmlNodeFile.Attributes.Append(xmlAttributeFileName);
							XmlAttribute xmlAttributeOwner = m_xmlData.CreateAttribute("owner");
							xmlAttributeOwner.Value = strUsername;
							xmlNodeFile.Attributes.Append(xmlAttributeOwner);
							xmlFilesElement.AppendChild(xmlNodeFile);
						}
						else
						{
							xmlNodeFile = m_xmlData.CreateElement("file");
							XmlAttribute xmlAttributeFileName = m_xmlData.CreateAttribute("name");
							xmlAttributeFileName.Value = strFileNameInternal;
							xmlNodeFile.Attributes.Append(xmlAttributeFileName);
							XmlAttribute xmlAttributeOwner = m_xmlData.CreateAttribute("owner");
							xmlAttributeOwner.Value = strUsername;
							xmlNodeFile.Attributes.Append(xmlAttributeOwner);
							nlFiles[0].AppendChild(xmlNodeFile);	
						}
					}

					// create program node
					xmlNodeProgram = m_xmlData.CreateElement("program");
					XmlAttribute xmlAttributeProgramName = m_xmlData.CreateAttribute("name");
					xmlAttributeProgramName.Value = strProgramName;
					xmlNodeProgram.Attributes.Append(xmlAttributeProgramName);
					xmlNodeFile.AppendChild(xmlNodeProgram);
				}
			}

			// write cards
			for (int i = 0; i < CardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)CardArray[i];
				
				if (PGE.strCardName == "platecard")
				{
					// create card element
					XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

					// create name attribute
					XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
					xmlAttributeCardName.Value = "platecard";
					xmlNodeCard.Attributes.Append(xmlAttributeCardName);

					// create plate_name attribute
					XmlAttribute xmlAttributePlateCardName = m_xmlData.CreateAttribute("plate_name");
					xmlAttributePlateCardName.Value = PGE.platecard_name.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardName);

					// create format attribute
					XmlAttribute xmlAttributePlateCardFormat = m_xmlData.CreateAttribute("format");
					xmlAttributePlateCardFormat.Value = PGE.platecard_format.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardFormat);

					// create height attribute
					XmlAttribute xmlAttributePlateCardHeight = m_xmlData.CreateAttribute("height");
					xmlAttributePlateCardHeight.Value = PGE.platecard_height.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardHeight);

					// create depth attribute
					XmlAttribute xmlAttributePlateCardDepth = m_xmlData.CreateAttribute("depth");
					xmlAttributePlateCardDepth.Value = PGE.platecard_depth.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardDepth);

					// create offset attribute
					XmlAttribute xmlAttributePlateCardOffset = m_xmlData.CreateAttribute("yo");
					xmlAttributePlateCardOffset.Value = PGE.platecard_offset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardOffset);

					// create offset attribute 2
					XmlAttribute xmlAttributePlateCardOffset2 = m_xmlData.CreateAttribute("yo2");
					xmlAttributePlateCardOffset2.Value = PGE.platecard_offset2.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardOffset2);

					// create max volume attribute
					XmlAttribute xmlAttributePlateCardMaxVolume = m_xmlData.CreateAttribute("max_volume");
					xmlAttributePlateCardMaxVolume.Value = PGE.platecard_max_volume.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardMaxVolume);

					// create dbwc attribute
					XmlAttribute xmlAttributePlateCardDbwc = m_xmlData.CreateAttribute("dbwc");
					xmlAttributePlateCardDbwc.Value = PGE.platecard_dbwc.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardDbwc);

					// create dbwc2 attribute
					XmlAttribute xmlAttributePlateCardDbwc2 = m_xmlData.CreateAttribute("dbwc2");
					xmlAttributePlateCardDbwc2.Value = PGE.platecard_dbwc2.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardDbwc2);

					// create plate row selections attribute
					XmlAttribute xmlAttributePlateCardRows = m_xmlData.CreateAttribute("rows");
					xmlAttributePlateCardRows.Value = PGE.platecard_rows.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardRows);

					// create ASP offset attribute
					XmlAttribute xmlAttributePlateCardASPOffset = m_xmlData.CreateAttribute("asp_offset");
					xmlAttributePlateCardASPOffset.Value = PGE.platecard_asp_offset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardASPOffset);

					// create LoBase offset attribute
					XmlAttribute xmlAttributePlateCardLoBase = m_xmlData.CreateAttribute("lobase");
					xmlAttributePlateCardLoBase.Value = PGE.platecard_loBase.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardLoBase);

					// create LoBase offset attribute
					XmlAttribute xmlAttributePlateCardDiameter = m_xmlData.CreateAttribute("diameter");
					xmlAttributePlateCardDiameter.Value = PGE.platecard_diameter.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardDiameter);

					// create LoBase offset attribute
					XmlAttribute xmlAttributePlateWellShape = m_xmlData.CreateAttribute("shape");
					xmlAttributePlateWellShape.Value = PGE.platecard_well_shape;
					xmlNodeCard.Attributes.Append(xmlAttributePlateWellShape);
						
					// insert card
					xmlNodeProgram.AppendChild(xmlNodeCard);
				}
				else if (PGE.strCardName == "platecardrowsonly")
				{
					// create card element
					XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

					// create name attribute
					XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
					xmlAttributeCardName.Value = "platecardrowsonly";
					xmlNodeCard.Attributes.Append(xmlAttributeCardName);

					// create plate row selections attribute
					XmlAttribute xmlAttributePlateCardRows = m_xmlData.CreateAttribute("rows");
					xmlAttributePlateCardRows.Value = PGE.platecardrowsonly_rows.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributePlateCardRows);

					// insert card
					xmlNodeProgram.AppendChild(xmlNodeCard);
				}
				else if (PGE.strCardName == "aspiratecard")
				{
					// create card element
					XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

					// create name attribute
					XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
					xmlAttributeCardName.Value = "aspiratecard";
					xmlNodeCard.Attributes.Append(xmlAttributeCardName);

					// create velocity attribute
					XmlAttribute xmlAttributeCardAspirateVelocity = m_xmlData.CreateAttribute("velocity");
					xmlAttributeCardAspirateVelocity.Value = PGE.aspiratecard_velocity.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributeCardAspirateVelocity);

					// create sweep attribute
					XmlAttribute xmlAttributeCardAspirateSweep = m_xmlData.CreateAttribute("sweep");
					xmlAttributeCardAspirateSweep.Value = PGE.aspiratecard_sweep.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributeCardAspirateSweep);

					// create time attribute
					XmlAttribute xmlAttributeCardAspirateTime = m_xmlData.CreateAttribute("time");
					xmlAttributeCardAspirateTime.Value = PGE.aspiratecard_time.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributeCardAspirateTime);

					// create probe_height attribute
					XmlAttribute xmlAttributeCardAspirateProbeHeight = m_xmlData.CreateAttribute("probe_height");
					xmlAttributeCardAspirateProbeHeight.Value = PGE.aspiratecard_probe_height.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributeCardAspirateProbeHeight);

					// create asp_offset attribute
					XmlAttribute xmlAttributeCardAspirateASPOffset = m_xmlData.CreateAttribute("asp_offset");
					xmlAttributeCardAspirateASPOffset.Value = PGE.aspiratecard_asp_offset.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributeCardAspirateASPOffset);

					// insert card
					xmlNodeProgram.AppendChild(xmlNodeCard);
				}
				else if (PGE.strCardName == "dispensecard")
				{	
					// create card element
					XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

					// create name attribute
					XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
					xmlAttributeCardName.Value = "dispensecard";
					xmlNodeCard.Attributes.Append(xmlAttributeCardName);

					// create liquid name attribute
					XmlAttribute xmlAttributeLiquidName = m_xmlData.CreateAttribute("liquid_name");
					xmlAttributeLiquidName.Value = PGE.dispensecard_liquid_name;
					xmlNodeCard.Attributes.Append(xmlAttributeLiquidName);

					// create inlet attribute
					XmlAttribute xmlAttributeDispenseCardInlet = m_xmlData.CreateAttribute("inlet");
					xmlAttributeDispenseCardInlet.Value = PGE.dispensecard_inlet.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributeDispenseCardInlet);

					// create volume attribute
					XmlAttribute xmlAttributeDispenseCardVolume = m_xmlData.CreateAttribute("volume");
					xmlAttributeDispenseCardVolume.Value = PGE.dispensecard_volume.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributeDispenseCardVolume);

					// create liquid_factor attribute
					XmlAttribute xmlAttributeDispenseCardPressure = m_xmlData.CreateAttribute("liquid_factor");
					xmlAttributeDispenseCardPressure.Value = PGE.dispensecard_liquid_factor.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributeDispenseCardPressure);

					// create disp_low attribute
					XmlAttribute xmlAttributeDispenseCardDispLow = m_xmlData.CreateAttribute("disp_low");
					xmlAttributeDispenseCardDispLow.Value = PGE.dispensecard_disp_low.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					xmlNodeCard.Attributes.Append(xmlAttributeDispenseCardDispLow);

					// insert card
					xmlNodeProgram.AppendChild(xmlNodeCard);
				}
				else if (PGE.strCardName == "soakcard")
				{
					// create card element
					XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

					// create name attribute
					XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
					xmlAttributeCardName.Value = "soakcard";
					xmlNodeCard.Attributes.Append(xmlAttributeCardName);

					// create time attribute
					XmlAttribute xmlAttributeSoakCardTime = m_xmlData.CreateAttribute("time");
					xmlAttributeSoakCardTime.Value = PGE.soakcard_time.ToString();
					xmlNodeCard.Attributes.Append(xmlAttributeSoakCardTime);

					// insert card
					xmlNodeProgram.AppendChild(xmlNodeCard);
				}
			}

			for (int i = 0; i < RepeatCardArray.Count; i++)
			{
				ProgramGUIElement PGE = (ProgramGUIElement)RepeatCardArray[i];
			
				// create card element
				XmlNode xmlNodeCard = m_xmlData.CreateElement("card");

				// create name attribute
				XmlAttribute xmlAttributeCardName = m_xmlData.CreateAttribute("name");
				xmlAttributeCardName.Value = "repeatcard";
				xmlNodeCard.Attributes.Append(xmlAttributeCardName);

				// create from attribute
				XmlAttribute xmlAttributeRepeatCardFrom = m_xmlData.CreateAttribute("from");
				xmlAttributeRepeatCardFrom.Value = PGE.repeatcard_from.ToString();
				xmlNodeCard.Attributes.Append(xmlAttributeRepeatCardFrom);

				// create to attribute
				XmlAttribute xmlAttributeRepeatCardTo = m_xmlData.CreateAttribute("to");
				xmlAttributeRepeatCardTo.Value = PGE.repeatcard_to.ToString();
				xmlNodeCard.Attributes.Append(xmlAttributeRepeatCardTo);

				// create repeats attribute
				XmlAttribute xmlAttributeRepeatCardRepeats = m_xmlData.CreateAttribute("repeats");
				xmlAttributeRepeatCardRepeats.Value = PGE.repeatcard_repeats.ToString();
				xmlNodeCard.Attributes.Append(xmlAttributeRepeatCardRepeats);

				// insert card
				xmlNodeProgram.AppendChild(xmlNodeCard);
			}
			
			m_xmlData.Save(m_strXmlFilename);
		}

		// loads program into structures
		public void LoadProgram(ArrayList CardArray, ArrayList RepeatCardArray, string strFileNameInternal, string strProgramName, string strUsername)
		{
			bool bProgramFound = false;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFile = null;
			XmlNode xmlNodeProgram = null;

			// find file in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strFileNameInternal && xmlNodeFile.Attributes["owner"].Value == strUsername)
					{
						// find program
						for (int nProgram = 0; nProgram < nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes.Count; nProgram++)
						{
							xmlNodeProgram = nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes[nProgram];
							string str3 = xmlNodeProgram.Attributes["name"].Value;
							if (xmlNodeProgram.Attributes["name"].Value == strProgramName)
							{
								bProgramFound = true;

								// no need to iterate further
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}
			}

			if (bProgramFound)
			{
				bool bRepeatCard = false;
				for (int nCard = 0; nCard < xmlNodeProgram.ChildNodes.Count; nCard++)
				{
					ProgramGUIElement PGE = new ProgramGUIElement();
					PGE.uc = null;
					
					XmlNode xmlNodeCard = xmlNodeProgram.ChildNodes[nCard];

					if (xmlNodeCard.Attributes["name"].Value == "platecard")
					{
						PGE.strCardName = "platecard";
						
						if( HaveAttribute( xmlNodeCard.Attributes, "dbwc" ) && HaveAttribute( xmlNodeCard.Attributes, "yo" ) )
						//try
						{
							PGE.platecard_dbwc = Convert.ToDouble(xmlNodeCard.Attributes["dbwc"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							PGE.platecard_offset = Convert.ToDouble(xmlNodeCard.Attributes["yo"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						}
						//catch
						else
						{
							PGE.allowedToChangeWellType = false;
						}
						PGE.platecard_depth = Convert.ToDouble(xmlNodeCard.Attributes["depth"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						PGE.platecard_format  = Convert.ToInt32(xmlNodeCard.Attributes["format"].Value);
						PGE.platecard_height = Convert.ToDouble(xmlNodeCard.Attributes["height"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						PGE.platecard_max_volume = Convert.ToDouble(xmlNodeCard.Attributes["max_volume"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						PGE.platecard_name = xmlNodeCard.Attributes["plate_name"].Value;
						if( HaveAttribute( xmlNodeCard.Attributes, "yo2" ) && HaveAttribute( xmlNodeCard.Attributes, "dbwc2" ) )
						//try
						{
							PGE.platecard_offset2 = Convert.ToDouble(xmlNodeCard.Attributes["yo2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							PGE.platecard_dbwc2 = Convert.ToDouble(xmlNodeCard.Attributes["dbwc2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						}
						//catch
						else
						{
							PGE.allowedToChangeWellType = false;
						}
						if( HaveAttribute( xmlNodeCard.Attributes, "lobase" ) )
						//try
						{
							PGE.platecard_loBase = bool.Parse( xmlNodeCard.Attributes["lobase"].Value );
						}
						//catch
						else
						{
							PGE.allowedToChangeWellType = false;
						}
						PGE.platecard_rows = xmlNodeCard.Attributes["rows"].Value;
						PGE.platecard_asp_offset = Convert.ToDouble(xmlNodeCard.Attributes["asp_offset"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						if( HaveAttribute( xmlNodeCard.Attributes, "diameter" ) && HaveAttribute( xmlNodeCard.Attributes, "shape" ) )
						//try
						{
							PGE.platecard_diameter = Convert.ToDouble(xmlNodeCard.Attributes["diameter"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							PGE.platecard_well_shape = xmlNodeCard.Attributes["shape"].Value;
						}
						//catch{}
					}
					else if (xmlNodeCard.Attributes["name"].Value == "platecardrowsonly")
					{
						PGE.strCardName = "platecardrowsonly";
						PGE.platecardrowsonly_rows = xmlNodeCard.Attributes["rows"].Value;
					}
					else if (xmlNodeCard.Attributes["name"].Value == "aspiratecard")
					{
						PGE.strCardName = "aspiratecard";
						PGE.aspiratecard_probe_height = Convert.ToDouble(xmlNodeCard.Attributes["probe_height"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						if( xmlNodeCard.Attributes["asp_offset"] != null )
						{
							PGE.aspiratecard_asp_offset = Convert.ToDouble(xmlNodeCard.Attributes["asp_offset"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						}
						PGE.aspiratecard_time = Convert.ToDouble(xmlNodeCard.Attributes["time"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						PGE.aspiratecard_velocity = Convert.ToInt32(xmlNodeCard.Attributes["velocity"].Value);
						if( xmlNodeCard.Attributes["sweep"] != null )
						{
							PGE.aspiratecard_sweep = Convert.ToBoolean(xmlNodeCard.Attributes["sweep"].Value);
						}
					}
					else if (xmlNodeCard.Attributes["name"].Value == "dispensecard")
					{
						PGE.strCardName = "dispensecard";
						PGE.dispensecard_inlet = Convert.ToInt32(xmlNodeCard.Attributes["inlet"].Value);
						PGE.dispensecard_volume = Convert.ToDouble(xmlNodeCard.Attributes["volume"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						PGE.dispensecard_liquid_name = xmlNodeCard.Attributes["liquid_name"].Value;
						PGE.dispensecard_liquid_factor = Convert.ToDouble(xmlNodeCard.Attributes["liquid_factor"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						PGE.dispensecard_disp_low = Convert.ToDouble(xmlNodeCard.Attributes["disp_low"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					}
					else if (xmlNodeCard.Attributes["name"].Value == "soakcard")
					{
						PGE.strCardName = "soakcard";
						PGE.soakcard_time = Convert.ToInt32(xmlNodeCard.Attributes["time"].Value);
					}
					else if (xmlNodeCard.Attributes["name"].Value == "repeatcard")
					{
						bRepeatCard = true;

						PGE.strCardName = "repeatcard";
						PGE.repeatcard_from = Convert.ToInt32(xmlNodeCard.Attributes["from"].Value, 10);
						PGE.repeatcard_to = Convert.ToInt32(xmlNodeCard.Attributes["to"].Value, 10);
						PGE.repeatcard_repeats = Convert.ToInt32(xmlNodeCard.Attributes["repeats"].Value, 10);
					}
					if (bRepeatCard)
					{
						RepeatCardArray.Add(PGE);
						bRepeatCard = false;
					}
					else
					{
						CardArray.Add(PGE);
					}
				}
			}
		}

		public void PopulateLiquidTree(TreeView tw)
		{
			// clear all
			tw.Nodes.Clear();

			// find all liquids collections
			XmlNodeList nlLiquids = m_xmlData.GetElementsByTagName("liquids");
			for (int nLiquidsNode = 0; nLiquidsNode < nlLiquids.Count; nLiquidsNode++)
			{
				// find all liquids within collection
				for (int nLiquid = 0; nLiquid < nlLiquids[nLiquidsNode].ChildNodes.Count; nLiquid++)
				{
					// add liquid
					XmlNode xmlNodeLiquid = nlLiquids[nLiquidsNode].ChildNodes[nLiquid];
					TreeNode treeNodeLiquid = new TreeNode(xmlNodeLiquid.Attributes["name"].Value, 6, 6);
					double dLiquidFactor = Convert.ToDouble(xmlNodeLiquid.Attributes["liquid_factor"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
					treeNodeLiquid.Tag = dLiquidFactor.ToString();
					tw.Nodes.Add(treeNodeLiquid);
				}
			}

			tw.ExpandAll();
		}

		public void PopulatePlateTree(TreeView tw)
		{
			// clear all
			tw.Nodes.Clear();

			// add plate group folders
			TreeNode treeNodePlates1536 = new TreeNode("1536", 0, 0);
			tw.Nodes.Add(treeNodePlates1536);
			TreeNode treeNodePlates384 = new TreeNode("384", 0, 0);
			tw.Nodes.Add(treeNodePlates384);
			TreeNode treeNodePlates96 = new TreeNode("96", 0, 0);
			tw.Nodes.Add(treeNodePlates96);

			// find all plates collections
			XmlNodeList nlPlates = m_xmlData.GetElementsByTagName("plates");
			for (int nPlatesNode = 0; nPlatesNode < nlPlates.Count; nPlatesNode++)
			{
				// find all plate groups within plates collection
				for (int nGroup = 0; nGroup < nlPlates[nPlatesNode].ChildNodes.Count; nGroup++)
				{
					XmlNode xmlNodeGroup = nlPlates[nPlatesNode].ChildNodes[nGroup];

					// find all plates within group
					for (int nPlate = 0; nPlate < nlPlates[nPlatesNode].ChildNodes[nGroup].ChildNodes.Count; nPlate++)
					{
						XmlNode xmlNodePlate = nlPlates[nPlatesNode].ChildNodes[nGroup].ChildNodes[nPlate];
						TreeNode treeNodePlate = new TreeNode(xmlNodePlate.Attributes["name"].Value, 1, 1);

						// create property bag
						PlateProperties pp = new PlateProperties();

						//double dPlateASPOffset = Convert.ToDouble(xmlNodePlate.Attributes["asp_offset"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						double dPlateDbwc = Convert.ToDouble(xmlNodePlate.Attributes["dbwc"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						double dPlateDepth = Convert.ToDouble(xmlNodePlate.Attributes["depth"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						double dPlateHeight = Convert.ToDouble(xmlNodePlate.Attributes["height"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						double dPlateMaxVolume = Convert.ToDouble(xmlNodePlate.Attributes["max_volume"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						double dPlateOffset = Convert.ToDouble(xmlNodePlate.Attributes["yo"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
						
						double dPlateBottomWellDiameter = 0;
						string sPlateWellShape = "0";
						if( HaveAttribute( xmlNodePlate.Attributes, "diameter" ) && HaveAttribute( xmlNodePlate.Attributes, "shape" ) )
						//try
						{
							dPlateBottomWellDiameter = Convert.ToDouble(xmlNodePlate.Attributes["diameter"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							sPlateWellShape = xmlNodePlate.Attributes["shape"].Value;
						}
						//catch{}

						double dPlateOffset2 = 0;
						double dPlateDbwc2 = 0;
						bool bPlateLoBase = false;
						if( HaveAttribute( xmlNodePlate.Attributes, "yo2" ) && HaveAttribute( xmlNodePlate.Attributes, "dbwc2" ) && HaveAttribute( xmlNodePlate.Attributes, "lobase" ) )
						//try
						{
							dPlateOffset2 = Convert.ToDouble(xmlNodePlate.Attributes["yo2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));						
							dPlateDbwc2 = Convert.ToDouble(xmlNodePlate.Attributes["dbwc2"].Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
							bPlateLoBase = bool.Parse( xmlNodePlate.Attributes["lobase"].Value );
						}
						//catch{}

						//pp.strPlateASPOffset = dPlateASPOffset.ToString("F1");
						pp.strPlateDbwc = dPlateDbwc.ToString("F3");
						pp.strPlateDbwc2 = dPlateDbwc2.ToString("F3");
						pp.strPlateDepth = dPlateDepth.ToString("F2");
						pp.strPlateHeight = dPlateHeight.ToString("F2");
						pp.strPlateMaxVolume = dPlateMaxVolume.ToString("F1");
						pp.strPlateOffset = dPlateOffset.ToString("F2");
						pp.strPlateOffset2 = dPlateOffset2.ToString("F2");
						pp.strPlateBottomWellDiameter = dPlateBottomWellDiameter.ToString("F2");
						pp.strWellShape = sPlateWellShape;

						pp.strPlateName = xmlNodePlate.Attributes["name"].Value;
						pp.strPlateType = xmlNodePlate.Attributes["format"].Value;
						pp.loBase = bPlateLoBase;

						treeNodePlate.Tag = pp;

						// add plate
						if (xmlNodeGroup.Attributes["name"].Value == "3")
						{
							treeNodePlates1536.Nodes.Add(treeNodePlate);
						}
						else if (xmlNodeGroup.Attributes["name"].Value == "2")
						{
							treeNodePlates384.Nodes.Add(treeNodePlate);
						}
						else if (xmlNodeGroup.Attributes["name"].Value == "1")
						{
							treeNodePlates96.Nodes.Add(treeNodePlate);
						}
					}
				}
			}
		}

		public void PopulateFileTree(TreeView tw, mainForm mf, bool bPrivate)
		{
			// clear all
			tw.Nodes.Clear();

			// find all files collections
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					// add file
					XmlNode xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					if (bPrivate)
					{
						if (xmlNodeFile.Attributes["owner"].Value == mf.m_User.Username)
						{
							TreeNode treeNodeFile = new TreeNode(xmlNodeFile.Attributes["name"].Value, 7, 7);
							tw.Nodes.Add(treeNodeFile);
						}
					}
					else
					{
						string name = "";
						Color color;

						if (xmlNodeFile.Attributes["owner"].Value != mf.m_User.Username)
						{
							name = xmlNodeFile.Attributes["owner"].Value + "'s files";
							color = Color.Gray;
						}
						else
						{
							name = "My files";
							color = Color.Black;
						}

						if (xmlNodeFile.Attributes["owner"].Value == "____BNX1536_")
						{
							continue;
						}
						
						bool bExists = false;
						TreeNode treeNodeUser = null;
						for (int i = 0; i < tw.Nodes.Count; i++)
						{
							if (tw.Nodes[i].Text == name)
							{
								treeNodeUser = tw.Nodes[i];
								bExists = true;
								break;
							}
						}
						if (!bExists)
						{
							treeNodeUser = new TreeNode(name, 0, 0);
							treeNodeUser.ForeColor = color;
							if (xmlNodeFile.Attributes["owner"].Value == mf.m_User.Username)
							{
								tw.Nodes.Insert(0, treeNodeUser);
							}
							else
							{
								tw.Nodes.Add(treeNodeUser);
							}
						}
						
						TreeNode treeNodeFile = new TreeNode(xmlNodeFile.Attributes["name"].Value, 7, 7);
						treeNodeFile.ForeColor = color;
						treeNodeUser.Nodes.Add(treeNodeFile);
					}
				}
			}

			tw.ExpandAll();
		}

		public void PopulateProgramComboBox(ComboBox cb, string strFileName)
		{
			// find all files collections
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					// add file
					XmlNode xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					
					if (xmlNodeFile.Attributes["name"].Value == strFileName)
					{
						// find all programs within collection
						for (int nProgram = 0; nProgram < nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes.Count; nProgram++)
						{
							// add program
							XmlNode xmlNodeProgram = nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes[nProgram];
							cb.Items.Add(xmlNodeProgram.Attributes["name"].Value);
						}

						break;
					}
				}
			}	
		}

		public void PopulateTree(TreeView tw, mainForm mf, bool forPrint)
		{	
			// clear all nodes in tree
			tw.Nodes.Clear();

			int nUserLevel = 0;

			// add special file folder
		{
			TreeNode treeNodeSpecialFile = new TreeNode("Current BNX1536 programs", 0, 0);
			treeNodeSpecialFile.ForeColor = Color.Blue;
			tw.Nodes.Add(treeNodeSpecialFile);

			// add user's files (with programs)
			XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all "file" within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					// add file
					XmlNode xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["owner"].Value == "____BNX1536_")
					{
						// find all programs within collection
						for (int nProgram = 0; nProgram < nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes.Count; nProgram++)
						{
							// add program
							XmlNode xmlNodeProgram = nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes[nProgram];
							TreeNode treeNodeProgram = new TreeNode(xmlNodeProgram.Attributes["name"].Value, 5, 5);
							treeNodeProgram.ForeColor = Color.Blue;
							treeNodeSpecialFile.Nodes.Add(treeNodeProgram);
						}
					}
				}
			}
		}

			// add file folders
			XmlNodeList nlUsersForFileFolders = m_xmlData.GetElementsByTagName("users");
			for (int nUsersNode = 0; nUsersNode < nlUsersForFileFolders.Count; nUsersNode++)
			{
				// find all users within collection
				for (int nUser = 0; nUser < nlUsersForFileFolders[nUsersNode].ChildNodes.Count; nUser++)
				{
					// add user's file folder
					XmlNode xmlNodeUser = nlUsersForFileFolders[nUsersNode].ChildNodes[nUser];					
					string name = "";
					Color color;

					if (xmlNodeUser.Attributes["name"].Value != mf.m_User.Username)
					{
						name = xmlNodeUser.Attributes["name"].Value + "'s files";
						color = Color.Gray;
					}
					else
					{
						nUserLevel = Convert.ToInt32(xmlNodeUser.Attributes["level"].Value);
						name = "My files";
						color = Color.Black;
					}
						
					TreeNode treeNodeUserFileFolder = new TreeNode(name, 0, 0);
					treeNodeUserFileFolder.ForeColor = color;
					if (xmlNodeUser.Attributes["name"].Value == mf.m_User.Username)
					{
						tw.Nodes.Insert(0, treeNodeUserFileFolder);
					}
					else
					{
						tw.Nodes.Add(treeNodeUserFileFolder);
					}

					// add user's files (with programs)
					XmlNodeList nlFiles = m_xmlData.GetElementsByTagName("files");
					for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
					{
						// find all "file" within collection
						for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
						{
							// add file
							XmlNode xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
							if (xmlNodeFile.Attributes["owner"].Value == xmlNodeUser.Attributes["name"].Value)
							{
								TreeNode treeNodeFile = new TreeNode(xmlNodeFile.Attributes["name"].Value, 7, 7);
								treeNodeFile.ForeColor = color;
								treeNodeUserFileFolder.Nodes.Add(treeNodeFile);

								// find all programs within collection
								for (int nProgram = 0; nProgram < nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes.Count; nProgram++)
								{
									// add program
									XmlNode xmlNodeProgram = nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes[nProgram];
									TreeNode treeNodeProgram = new TreeNode(xmlNodeProgram.Attributes["name"].Value, 5, 5);
									treeNodeProgram.ForeColor = color;
									treeNodeFile.Nodes.Add(treeNodeProgram);
								}
							}
						}
					}
				}
			}
	
			//if (nUserLevel == 3)
			if (true)
			{
				// add plates collection folder
				TreeNode treeNodePlates = new TreeNode("Plates", 0, 0);
				tw.Nodes.Add(treeNodePlates);

				// add plate group folders
				TreeNode treeNodePlates1536 = new TreeNode("1536", 0, 0);
				treeNodePlates.Nodes.Add(treeNodePlates1536);
				TreeNode treeNodePlates384 = new TreeNode("384", 0, 0);
				treeNodePlates.Nodes.Add(treeNodePlates384);
				TreeNode treeNodePlates96 = new TreeNode("96", 0, 0);
				treeNodePlates.Nodes.Add(treeNodePlates96);

				// find all plates collections
				XmlNodeList nlPlates = m_xmlData.GetElementsByTagName("plates");
				for (int nPlatesNode = 0; nPlatesNode < nlPlates.Count; nPlatesNode++)
				{
					// find all plate groups within plates collection
					for (int nGroup = 0; nGroup < nlPlates[nPlatesNode].ChildNodes.Count; nGroup++)
					{
						XmlNode xmlNodeGroup = nlPlates[nPlatesNode].ChildNodes[nGroup];

						// find all plates within group
						for (int nPlate = 0; nPlate < nlPlates[nPlatesNode].ChildNodes[nGroup].ChildNodes.Count; nPlate++)
						{
							// add plate
							if (xmlNodeGroup.Attributes["name"].Value == "3")
							{
								XmlNode xmlNodePlate = nlPlates[nPlatesNode].ChildNodes[nGroup].ChildNodes[nPlate];
								TreeNode treeNodePlate = new TreeNode(xmlNodePlate.Attributes["name"].Value, 1, 1);
								treeNodePlates1536.Nodes.Add(treeNodePlate);
							}
							else if (xmlNodeGroup.Attributes["name"].Value == "2")
							{
								XmlNode xmlNodePlate = nlPlates[nPlatesNode].ChildNodes[nGroup].ChildNodes[nPlate];
								TreeNode treeNodePlate = new TreeNode(xmlNodePlate.Attributes["name"].Value, 1, 1);
								treeNodePlates384.Nodes.Add(treeNodePlate);
							}
							else if (xmlNodeGroup.Attributes["name"].Value == "1")
							{
								XmlNode xmlNodePlate = nlPlates[nPlatesNode].ChildNodes[nGroup].ChildNodes[nPlate];
								TreeNode treeNodePlate = new TreeNode(xmlNodePlate.Attributes["name"].Value, 1, 1);
								treeNodePlates96.Nodes.Add(treeNodePlate);
							}
						}
					}
				}

				// add liquids folder
				TreeNode treeNodeLiquids = new TreeNode("Liquids", 0, 0);
				tw.Nodes.Add(treeNodeLiquids);

				// find all liquids collections
				XmlNodeList nlLiquids = m_xmlData.GetElementsByTagName("liquids");
				for (int nLiquidsNode = 0; nLiquidsNode < nlLiquids.Count; nLiquidsNode++)
				{
					// find all liquids within collection
					for (int nLiquid = 0; nLiquid < nlLiquids[nLiquidsNode].ChildNodes.Count; nLiquid++)
					{
						// add liquid
						XmlNode xmlNodeLiquid = nlLiquids[nLiquidsNode].ChildNodes[nLiquid];
						TreeNode treeNodeLiquid = new TreeNode(xmlNodeLiquid.Attributes["name"].Value, 6, 6);
						treeNodeLiquids.Nodes.Add(treeNodeLiquid);
					}
				}

				if(!forPrint)
				{
					// add users folder
					TreeNode treeNodeUsers = new TreeNode("Users", 2, 2);
					tw.Nodes.Add(treeNodeUsers);

					// find all users collections
					XmlNodeList nlUsers = m_xmlData.GetElementsByTagName("users");
					for (int nUsersNode = 0; nUsersNode < nlUsers.Count; nUsersNode++)
					{
						// find all users within collection
						for (int nUser = 0; nUser < nlUsers[nUsersNode].ChildNodes.Count; nUser++)
						{
							// add user
							XmlNode xmlNodeUser = nlUsers[nUsersNode].ChildNodes[nUser];
							TreeNode treeNodeUser = new TreeNode(xmlNodeUser.Attributes["name"].Value, 3, 3);
							treeNodeUsers.Nodes.Add(treeNodeUser);
						}
					}
				}

				// add config folder
				if(!forPrint)
				{
					TreeNode treeNodeConfig = new TreeNode("Communication", 4, 4);
					tw.Nodes.Add(treeNodeConfig);
				}
			}
		}

		public void LoadAll()
		{
			m_xmlData = new XmlDocument();
			m_xmlData.Load(m_strXmlFilename);
		}

		int GetProgramNumber(string strFileName, string strProgramName)
		{
			int nProgramNumber = 0;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFile = null;
			XmlNode xmlNodeProgram = null;

			// find file in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFile = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFile.Attributes["name"].Value == strFileName)
					{
						// find program
						for (int nProgram = 0; nProgram < nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes.Count; nProgram++)
						{
							xmlNodeProgram = nlFiles[nFilesNode].ChildNodes[nFile].ChildNodes[nProgram];
							if (xmlNodeProgram.Attributes["name"].Value == strProgramName)
							{
								nProgramNumber = nProgram + 1;

								// no need to iterate further
								break;
							}
						}

						// no need to iterate further
						break;
					}
				}
			}
			return nProgramNumber;
		}

		public void CopyFile(string strFileNameInternal, string strUsernameFrom, string strUsernameTo, TreeNode treeNodeParent)
		{
			// any file to paste?
			if (strFileNameInternal == "")
			{
				MessageBox.Show("You have to Copy before you Paste.", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// check if file exists
			for (int nFile = 0; nFile < treeNodeParent.Nodes.Count; nFile++)
			{
				if (treeNodeParent.Nodes[nFile].Text.ToLower() == strFileNameInternal.ToLower())
				{
					MessageBox.Show("File already exists", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			bool bFileFound = false;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFileFrom = null;

			// find file in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find all files within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFileFrom = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFileFrom.Attributes["name"].Value == strFileNameInternal && xmlNodeFileFrom.Attributes["owner"].Value == strUsernameFrom)
					{
						bFileFound = true;
						break;
					}
				}
			}

			if (!bFileFound)
			{
				MessageBox.Show("You have to Copy before you Paste.", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// copy it
			XmlNode xmlNodeCopy = null;
			if (bFileFound)
			{
				nlFiles = m_xmlData.GetElementsByTagName("files");
				XmlNode xmlNodeFiles = nlFiles[0];
				xmlNodeCopy = xmlNodeFileFrom.Clone();
				xmlNodeCopy.Attributes["owner"].Value = strUsernameTo;
				xmlNodeFiles.AppendChild(xmlNodeCopy);
			}

			m_xmlData.Save(m_strXmlFilename);

			// update tree
			TreeNode treeNodeFileToAdd = new TreeNode(strFileNameInternal, 7, 7);
			treeNodeParent.Nodes.Add(treeNodeFileToAdd);
			for (int i = 0; i < xmlNodeCopy.ChildNodes.Count; i++)
			{
				TreeNode treeNodeProgram = new TreeNode(xmlNodeCopy.ChildNodes[i].Attributes["name"].Value, 5, 5);
				treeNodeFileToAdd.Nodes.Add(treeNodeProgram);
			}
		}

		public void CopyProgram(string strProgramName, string strFileNameFrom, string strUsernameFrom, string strUsernameTo, string strFileNameTo, TreeNode treeNodeParent)
		{
			// any program to paste?
			if (strProgramName == "")
			{
				MessageBox.Show("You have to Copy before you Paste.", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// check if program exists
			for (int nProgram = 0; nProgram < treeNodeParent.Nodes.Count; nProgram++)
			{
				if (treeNodeParent.Nodes[nProgram].Text.ToLower() == strProgramName.ToLower())
				{
					MessageBox.Show("Program already exists", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			bool bProgramFound = false;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFileFrom = null;
			XmlNode xmlNodeProgramFrom = null;
			

			// find files in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find file within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFileFrom = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFileFrom.Attributes["name"].Value == strFileNameFrom && xmlNodeFileFrom.Attributes["owner"].Value == strUsernameFrom)
					{
						// find program within file
						for (int nProgram = 0; nProgram < xmlNodeFileFrom.ChildNodes.Count; nProgram++)
						{
							xmlNodeProgramFrom = xmlNodeFileFrom.ChildNodes[nProgram];
							if (xmlNodeProgramFrom.Attributes["name"].Value == strProgramName)
							{
								bProgramFound = true;
								break;
							}
						}
						break;
					}
				}
			}

			if (!bProgramFound)
			{
				MessageBox.Show("You have to Copy before you Paste.", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// copy it
			XmlNode xmlNodeCopy = null;
			if (bProgramFound)
			{
				nlFiles = m_xmlData.GetElementsByTagName("files");
				XmlNode xmlNodeFiles = nlFiles[0];
				xmlNodeCopy = xmlNodeProgramFrom.Clone();
				
				// find file within collection
				for (int nFile = 0; nFile < xmlNodeFiles.ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFileTo = xmlNodeFiles.ChildNodes[nFile];
					if (xmlNodeFileTo.Attributes["name"].Value == strFileNameTo && xmlNodeFileTo.Attributes["owner"].Value == strUsernameTo)
					{
						// file found!
						xmlNodeFileTo.AppendChild(xmlNodeCopy);
						break;
					}
				}
			}

			m_xmlData.Save(m_strXmlFilename);

			// update tree
			TreeNode treeNodeFileToAdd = new TreeNode(strProgramName, 5, 5);
			treeNodeParent.Nodes.Add(treeNodeFileToAdd);
		}

		public void CopyProgramDW4(string strProgramName, string strUsernameTo, string strFileNameTo, TreeNode treeNodeParent)
		{
			// any program to paste?
			if (strProgramName == "")
			{
				MessageBox.Show("You have to Copy before you Paste.", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// check if program exists
			for (int nProgram = 0; nProgram < treeNodeParent.Nodes.Count; nProgram++)
			{
				if (treeNodeParent.Nodes[nProgram].Text.ToLower() == strProgramName.ToLower())
				{
					MessageBox.Show("Program already exists", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			bool bProgramFound = false;
			XmlNodeList nlFiles = null;
			XmlNode xmlNodeFileFrom = null;
			XmlNode xmlNodeProgramFrom = null;
			

			// find files in xml data
			nlFiles = m_xmlData.GetElementsByTagName("files");
			for (int nFilesNode = 0; nFilesNode < nlFiles.Count; nFilesNode++)
			{
				// find file within collection
				for (int nFile = 0; nFile < nlFiles[nFilesNode].ChildNodes.Count; nFile++)
				{
					xmlNodeFileFrom = nlFiles[nFilesNode].ChildNodes[nFile];
					if (xmlNodeFileFrom.Attributes["owner"].Value == "____BNX1536_")
					{
						// find program within file
						for (int nProgram = 0; nProgram < xmlNodeFileFrom.ChildNodes.Count; nProgram++)
						{
							xmlNodeProgramFrom = xmlNodeFileFrom.ChildNodes[nProgram];
							if (xmlNodeProgramFrom.Attributes["name"].Value == strProgramName)
							{
								bProgramFound = true;
								break;
							}
						}
						break;
					}
				}
			}

			if (!bProgramFound)
			{
				MessageBox.Show("You have to Copy before you Paste.", "Copy and Paste", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// copy it
			XmlNode xmlNodeCopy = null;
			if (bProgramFound)
			{
				nlFiles = m_xmlData.GetElementsByTagName("files");
				XmlNode xmlNodeFiles = nlFiles[0];
				xmlNodeCopy = xmlNodeProgramFrom.Clone();
				
				// find file within collection
				for (int nFile = 0; nFile < xmlNodeFiles.ChildNodes.Count; nFile++)
				{
					XmlNode xmlNodeFileTo = xmlNodeFiles.ChildNodes[nFile];
					if (xmlNodeFileTo.Attributes["name"].Value == strFileNameTo && xmlNodeFileTo.Attributes["owner"].Value == strUsernameTo)
					{
						// file found!
						xmlNodeFileTo.AppendChild(xmlNodeCopy);
						break;
					}
				}
			}

			m_xmlData.Save(m_strXmlFilename);

			// update tree
			TreeNode treeNodeFileToAdd = new TreeNode(strProgramName, 5, 5);
			treeNodeParent.Nodes.Add(treeNodeFileToAdd);
		}

		public int GetCommPort()
		{			
			XmlNodeList nlConfig = m_xmlData.GetElementsByTagName("config");

			int nPort = 0;

			if (nlConfig.Count > 0)
			{
				try
				{
					nPort = Convert.ToInt32(nlConfig[0].Attributes["serialport"].Value);
				}
				catch (Exception e)
				{
					e = e;
				}
			}

			return nPort;
		}

		static bool HaveAttribute( XmlAttributeCollection attributes, string name )
		{
			foreach( XmlAttribute attr in attributes )
			{
				if( attr.Name.Equals(name) ) return true;
			}
			return false;
		}
	}
}