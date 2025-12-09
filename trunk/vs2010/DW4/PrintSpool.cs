//using System;
//using System.Collections;
//using System.Xml;
//using System.Drawing;
//using System.Windows.Forms;
//using System.Drawing.Printing;
//
//namespace AQ3
//{
//	/// <summary>
//	/// Summary description for PrintSpool.
//	/// </summary>
//	public class MyPrintDocument : PrintDocument
//	{
//		bool m_more = false;
//		private System.Windows.Forms.ListView listViewPrintJobs;
//		XmlDocument m_xmlData;
//		private ArrayList m_jobItems = new ArrayList();
//		string m_username;
//		private ListView m_lv;
//
//		public bool RestOfProgram
//		{
//			set{ m_more = value; }
//		}
//
//		public System.Windows.Forms.ListView PrintJobs
//		{
//			set{ listViewPrintJobs = value; }
//		}
//
//		public MyPrintDocument()
//		{
//			//
//			// TODO: Add constructor logic here
//			//
//		}
//
////		private enum JobType {Program, CurrentProgram, Plate, Liquid, User};
////		private struct PrintJob
////		{
////			public string owner;
////			public string name;
////			public string path;
////			public TreeNode sourceNode;
////			public Color sourceColor;
////			public JobType type;
////		}
//
//		protected override void OnPrintPage(PrintPageEventArgs e) 
//		{
//			PageSettings ps = e.PageSettings;
//			ps.Margins.Top = 100;
//			ps.Margins.Bottom = 100;
//
//			int nextY = 0;
//
//			// Get first item
//			if(listViewPrintJobs.Items.Count > 0)
//			{
//				ListViewItem item = listViewPrintJobs.Items[0];
//				string name = item.SubItems[1].Text;
//				string owner = item.SubItems[2].Text;
//				PrintForm.PrintJob job = (PrintForm.PrintJob) item.Tag;
//			
//				// Program
//				if(job.type == PrintForm.JobType.Program)
//				{
//					// Progams
//					XmlNodeList programList = m_xmlData.SelectNodes("//file[@owner='" + owner + "']/program[@name='" + name + "']");
//					XmlNode program = programList[0];
//
//					string fileName = program.ParentNode.Attributes["name"].Value;
//						
//					string programName = program.Attributes["name"].Value;
//
//					// Cards
//					XmlNodeList cardList = program.SelectNodes("./card[@name != 'platecard']");
//
//					//string fileName = program.ParentNode.Attributes["name"].Value;
//					//string owner = program.ParentNode.Attributes["owner"].Value;
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
//					XmlNode node = m_xmlData.SelectSingleNode( string.Format( "//plates/group/plate[@name='{0}' and @format='{1}']", plateName, format ) );
//					string catalog = "";
//					if( node != null )
//					{
//						catalog = node.Attributes["type_no"].Value;
//					}
//					
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
//					nextY = AQ3.Print.addLogo(e);
//					nextY = AQ3.Print.addTitle(e, nextY, "BNX1536 Program Sheet");
//					nextY = AQ3.Print.addProgramHeader(e, nextY+10, programName, fileName, owner, m_username, XmlData.PROGRAM_VERSION);
//					nextY = AQ3.Print.addSubTitle(e, nextY+10, "Plate info:");
//					//nextY = Print.addPlateInfo(e, nextY+2, formatType, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm");
//					nextY = AQ3.Print.addPlateInfo(e, nextY+2, formatType, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm", plateName, catalog );
//					nextY = AQ3.Print.addSubTitle(e, nextY+10, "Steps:");
//					nextY = AQ3.Print.addStepHeader(e, nextY+2, plateName, pcRows);
//
//					AQ3.Print.addFooter(e, DateTime.Now );
//
//					int step = 0;
//					
//					// Steps
//					try
//					{
//						//find all repeats and sort them
//						//not a very nice hack, but quicker than fix how
//						//program is saved in the first place
//						Hashtable repeatPos = new Hashtable();
//						foreach( XmlNode card in cardList )
//						{
//							string cardType = card.Attributes["name"].Value;
//							if( cardType == "repeatcard" )
//							{
//								int end = int.Parse( card.Attributes["from"].Value );
//								repeatPos[end] = card;
//							}
//						}
//
//						int[] pos = (int[])new ArrayList( repeatPos.Keys ).ToArray(typeof(int));
//						XmlNode[] nodes = (XmlNode[])new ArrayList( repeatPos.Values ).ToArray(typeof(XmlNode));
//						Array.Sort( pos, nodes );
//						//repeat sort end
//
//						//sort the repeats into the list for the printout
//						ArrayList sortedList = new ArrayList( cardList.Count );						
//						int count = 0;
//						foreach( XmlNode card in cardList )
//						{
//							string cardType = card.Attributes["name"].Value;
//
//							if( cardType == "repeatcard" )
//							{
//								//use the sorted list, instead of the unsorted from cardList
//								//repeats are always at the end so this works
//								XmlNode sortedCard = nodes[count];
//								int end = int.Parse( sortedCard.Attributes["from"].Value );
//								sortedList.Insert( end+count, sortedCard );
//								
//								//int end = int.Parse( card.Attributes["from"].Value );
//								//sortedList.Insert( end+count, card );
//								count++;
//							}
//							else
//							{
//								sortedList.Add( card );
//							}
//						}
//
//						//foreach(XmlNode card in cardList)
//						foreach(XmlNode card in sortedList)
//						{
//							string cardType = card.Attributes["name"].Value;
//
//							step++;
//
//							switch(cardType.ToLower())
//							{
//								case "platecardrowsonly":
//									string pcoRows = card.Attributes["rows"].Value;
//									nextY = AQ3.Print.addStepRowSelector(e, nextY, Convert.ToString(step), pcoRows);
//									break;
//								case "soakcard":
//									string soakTime = card.Attributes["time"].Value;
//									nextY = AQ3.Print.addStepSoak(e, nextY, Convert.ToString(step), soakTime + " sec");
//									break;
//								case "dispensecard":
//									string liquid = card.Attributes["liquid_name"].Value;
//									string inlet = card.Attributes["inlet"].Value;
//									string volume = card.Attributes["volume"].Value;
//									string lf = card.Attributes["liquid_factor"].Value;
//									string pressure = card.Attributes["disp_low"].Value;
//									nextY = AQ3.Print.addStepDispense(e, nextY, Convert.ToString(step), liquid, inlet, volume + " µl", lf, pressure);
//									break;
//								case "repeatcard":
//									step--;
//									string start = card.Attributes["to"].Value;
//									string end = card.Attributes["from"].Value;
//									string repeats = card.Attributes["repeats"].Value;
//									nextY = AQ3.Print.addStepRepeat(e, nextY, "", start, end, repeats);
//									break;
//								case "aspiratecard":
//									string velocity = card.Attributes["velocity"].Value;
//									string aspTime = card.Attributes["time"].Value;
//									string probeHeight = card.Attributes["probe_height"].Value;
//								
//									string velocityType = "";
//								
//								switch (velocity)
//								{
//									case "0":
//										velocityType = "Low Speed";
//										break;
//									case "1":
//										velocityType = "Medium Speed";
//										break;
//									case "2":
//										velocityType = "High Speed";
//										break;
//								}
//									nextY = AQ3.Print.addStepAspirate(e, nextY, Convert.ToString(step), velocityType, aspTime + " sec", probeHeight + " mm");
//									if( nextY > 900 )
//									{
//										//this.document.RestOfProgram = true;
//										//e.HasMorePages = true;										
//									}
//									break;
//							}
//
//						}
//					}
//					catch (Exception ex)
//					{
//						MessageBox.Show(ex.Message);
//					}
//					removeJob(job, item);
//				}
//				else if(job.type == PrintForm.JobType.CurrentProgram)
//				{
//					// Progams
//					XmlNodeList programList = m_xmlData.SelectNodes("//file[@owner='" + owner + "']/program[@name='" + name + "']");
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
//					XmlNode node = m_xmlData.SelectSingleNode( string.Format( "//plates/group/plate[@name='{0}' and @format='{1}']", plateName, format ) );
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
//					nextY = AQ3.Print.addLogo(e);
//					nextY = AQ3.Print.addTitle(e, nextY, "BNX1536 Current Program Sheet");
//					nextY = AQ3.Print.addCurrentProgramHeader(e, nextY+10, programName, owner, m_username, XmlData.PROGRAM_VERSION);
//					nextY = AQ3.Print.addSubTitle(e, nextY+10, "Plate info:");
//					nextY = AQ3.Print.addPlateInfo(e, nextY+2, formatType, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm", plateName, catalog );
//					nextY = AQ3.Print.addSubTitle(e, nextY+10, "Steps:");
//					nextY = AQ3.Print.addStepHeader(e, nextY+2, plateName, pcRows);
//					AQ3.Print.addFooter(e, DateTime.Now);
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
//										nextY = AQ3.Print.addStepRowSelector(e, nextY, Convert.ToString(step), pcoRows);
//										break;
//									case "soakcard":
//										string soakTime = card.Attributes["time"].Value;
//										nextY = AQ3.Print.addStepSoak(e, nextY, Convert.ToString(step), soakTime + " sec");
//										break;
//									case "dispensecard":
//										string liquid = card.Attributes["liquid_name"].Value;
//										string inlet = card.Attributes["inlet"].Value;
//										string volume = card.Attributes["volume"].Value;
//										string lf = card.Attributes["liquid_factor"].Value;
//										string pressure = card.Attributes["disp_low"].Value;
//										nextY = AQ3.Print.addStepDispense(e, nextY, Convert.ToString(step), liquid, inlet, volume + " µl", lf, pressure);
//										break;
//									case "repeatcard":
//										string start = card.Attributes["from"].Value;
//										string end = card.Attributes["to"].Value;
//										string repeats = card.Attributes["repeats"].Value;
//										nextY = AQ3.Print.addStepRepeat(e, nextY, "", start, end, repeats);
//										break;
//									case "aspiratecard":
//										string velocity = card.Attributes["velocity"].Value;
//										string aspTime = card.Attributes["time"].Value;
//										string probeHeight = card.Attributes["probe_height"].Value;
//								
//										string velocityType = "";
//								
//									switch (velocity)
//									{
//										case "1":
//											velocityType = "Low Speed";
//											break;
//										case "3":
//											velocityType = "Medium Speed";
//											break;
//										case "2":
//											velocityType = "High Speed";
//											break;
//									}
//										nextY = AQ3.Print.addStepAspirate(e, nextY, Convert.ToString(step), velocityType, aspTime + " sec", probeHeight + " mm");
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
//				}
//				else if(job.type == PrintForm.JobType.Plate)
//				{
//					nextY = AQ3.Print.addLogo(e);
//					nextY = AQ3.Print.addTitle(e, nextY, "BNX1536 Plate Sheet");
//					nextY = AQ3.Print.addSimpleTitleHeader(e, nextY, m_username, XmlData.PROGRAM_VERSION);
//					AQ3.Print.addFooter(e, DateTime.Now);
//
//					ListViewItem plateItem = listViewPrintJobs.Items[0];
//
//					// Plates
//					XmlNodeList plateList = m_xmlData.SelectNodes("//plates/group/plate[@name='" + name + "']");
//					
//					foreach(XmlNode plate in plateList)
//					{
//						string format = plate.Attributes["format"].Value;
//
//						switch(format)
//						{
//							case "3":
//								string formatType = "1536 Wells Plate";
//								nextY = AQ3.Print.addSubTitle(e, nextY+10, formatType + "s:");
//									
//								foreach(ListViewItem plateItem2 in listViewPrintJobs.Items)
//								{
//									string itemName = plateItem2.SubItems[1].Text;
//									PrintForm.PrintJob thisJob = (PrintForm.PrintJob) plateItem2.Tag;
//										
//									XmlNodeList plateList2 = m_xmlData.SelectNodes("//plates/group/plate[@name='" + itemName + "' and @format='3']");
//
//									XmlNode node = m_xmlData.SelectSingleNode("//plates/group/plate[@name='" + itemName + "' and @format='3']");
//										
//									foreach(XmlNode plate2 in plateList2)
//									{
//										string plateName = plate2.Attributes["name"].Value;
//										string height = plate2.Attributes["height"].Value;
//										string depth = plate2.Attributes["depth"].Value;
//										string yo = plate2.Attributes["yo"].Value;
//										string maxVolume = plate2.Attributes["max_volume"].Value;
//										string dbwc = plate2.Attributes["dbwc"].Value;
//										string aspOffset = plate2.Attributes["asp_offset"].Value;
//										string catalog = "";
//										if( node != null )
//										{
//											catalog = node.Attributes["type_no"].Value;
//										}
//
//										int maxY = e.MarginBounds.Height;
//										if((nextY + 50) < maxY)
//										{
//											nextY = AQ3.Print.addPlateInfo2(e, nextY+5, plateName, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm", catalog );
//											removeJob(thisJob, plateItem2);
//										}
//									}
//								}
//								break;
//							case "2":
//								formatType = "384 Wells Plate";
//								nextY = AQ3.Print.addSubTitle(e, nextY+10, formatType + "s:");
//									
//								foreach(ListViewItem plateItem2 in listViewPrintJobs.Items)
//								{
//									string itemName = plateItem2.SubItems[1].Text;
//									PrintForm.PrintJob thisJob = (PrintForm.PrintJob) plateItem2.Tag;
//										
//									XmlNodeList plateList2 = m_xmlData.SelectNodes("//plates/group/plate[@name='" + itemName + "' and @format='2']");
//
//									XmlNode node = m_xmlData.SelectSingleNode("//plates/group/plate[@name='" + itemName + "' and @format='2']");
//										
//									foreach(XmlNode plate2 in plateList2)
//									{
//										string plateName = plate2.Attributes["name"].Value;
//										string height = plate2.Attributes["height"].Value;
//										string depth = plate2.Attributes["depth"].Value;
//										string yo = plate2.Attributes["yo"].Value;
//										string maxVolume = plate2.Attributes["max_volume"].Value;
//										string dbwc = plate2.Attributes["dbwc"].Value;
//										string aspOffset = plate2.Attributes["asp_offset"].Value;
//										string catalog = "";
//										if( node != null )
//										{
//											catalog = node.Attributes["type_no"].Value;
//										}
//
//										int maxY = e.MarginBounds.Height;
//										if((nextY + 50) < maxY)
//										{
//											nextY = AQ3.Print.addPlateInfo2(e, nextY+5, plateName, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm", catalog);
//											removeJob(thisJob, plateItem2);
//										}
//									}
//								}
//								break;
//							case "1":
//								formatType = "96 Wells Plate";
//								nextY = AQ3.Print.addSubTitle(e, nextY+10, formatType + "s:");
//									
//								foreach(ListViewItem plateItem2 in listViewPrintJobs.Items)
//								{
//									string itemName = plateItem2.SubItems[1].Text;
//									PrintForm.PrintJob thisJob = (PrintForm.PrintJob) plateItem2.Tag;
//										
//									XmlNodeList plateList2 = m_xmlData.SelectNodes("//plates/group/plate[@name='" + itemName + "' and @format='1']");
//									
//									XmlNode node = m_xmlData.SelectSingleNode("//plates/group/plate[@name='" + itemName + "' and @format='1']");
//
//									foreach(XmlNode plate2 in plateList2)
//									{
//										string plateName = plate2.Attributes["name"].Value;
//										string height = plate2.Attributes["height"].Value;
//										string depth = plate2.Attributes["depth"].Value;
//										string yo = plate2.Attributes["yo"].Value;
//										string maxVolume = plate2.Attributes["max_volume"].Value;
//										string dbwc = plate2.Attributes["dbwc"].Value;
//										string aspOffset = plate2.Attributes["asp_offset"].Value;
//										string catalog = "";
//										if( node != null )
//										{
//											catalog = node.Attributes["type_no"].Value;
//										}
//
//										int maxY = e.MarginBounds.Height;
//										if((nextY + 50) < maxY)
//										{
//											nextY = AQ3.Print.addPlateInfo2(e, nextY+5, plateName, yo + " mm", dbwc + " mm", height + " mm", depth + " mm", maxVolume + " µl", aspOffset + " mm", catalog);
//											removeJob(thisJob, plateItem2);
//										}
//
//									}
//								}
//								break;
//						}
//					}
//					
//				}
//				else if(job.type == PrintForm.JobType.Liquid)
//				{
//					nextY = AQ3.Print.addLogo(e);
//					nextY = AQ3.Print.addTitle(e, nextY, "BNX1536 Liquid Sheet");
//					nextY = AQ3.Print.addSimpleTitleHeader(e, nextY, m_username, XmlData.PROGRAM_VERSION);
//					nextY = AQ3.Print.addSubTitle(e, nextY+10, "Liquids:");
//					nextY = AQ3.Print.addLiquidHeader(e, nextY);
//					AQ3.Print.addFooter(e, DateTime.Now);
//
//					int i = 1;
//					foreach(ListViewItem liquidItem in listViewPrintJobs.Items)
//					{
//						string itemName = liquidItem.SubItems[1].Text;
//						PrintForm.PrintJob thisJob = (PrintForm.PrintJob) liquidItem.Tag;
//
//						// Liquids
//						XmlNodeList liquidList = m_xmlData.SelectNodes("//liquids/liquid[@name='" + itemName + "']");
//					
//						foreach(XmlNode liquid in liquidList)
//						{
//							string liquidName = liquid.Attributes["name"].Value;
//							string lf = liquid.Attributes["liquid_factor"].Value;
//							nextY = AQ3.Print.addLiquidItem(e, nextY, Convert.ToString(i++), lf, liquidName);
//							removeJob(thisJob, liquidItem);
//						}
//							
//					}
//					//removeJob(job, item);
//				}
//				
//				if(listViewPrintJobs.Items.Count > 0)
//				{
//					e.HasMorePages = true;
//				}
//				else
//				{
//					e.HasMorePages = false;
//					foreach(ListViewItem item2 in m_lv.Items)
//					{
//						listViewPrintJobs.Items.Add((ListViewItem)item2.Clone());
//					}
//				}
//			}
//
//			//nextY = Print.addCurrentProgramHeader(e, nextY, "New Program 1", "Administrator", m_mf.m_User.Username, XmlData.PROGRAM_VERSION);
//
//			//e.HasMorePages = false;
//		}
//
//		private void removeJob( PrintForm.PrintJob job, ListViewItem item)
//		{
//			job.sourceNode.Text = job.sourceNode.Text.Replace(" (Print)", "");
//			job.sourceNode.ForeColor = job.sourceColor;
//			m_jobItems.Remove(job.path);
//			listViewPrintJobs.Items.Remove(item);
//		}
//
//	}
//}
