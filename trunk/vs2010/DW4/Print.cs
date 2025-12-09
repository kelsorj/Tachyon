using System;
using System.Drawing;
using System.Drawing.Printing;
//using System.IO;
using System.Resources;
using System.Reflection;

namespace AQ3
{
	public class Print
	{
		// descides the column width in the header of the print
		static string m_leftColumnWidthText = "Bottom well diameter:";
		static string m_rightColumnWidthText = "Catalog number:";

		public Print()
		{
		}

		public static int addLogo(PrintPageEventArgs e)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top / 2;

			//FileStream fs = new FileStream("logo.bmp", FileMode.Open);
			ResourceManager rm = new ResourceManager("AQ3.Printer", Assembly.GetAssembly(typeof(Print))); 
			Bitmap img = (Bitmap)rm.GetObject("printLogo");

			int logoX = ps.PaperSize.Width - ps.Margins.Right - img.Width;
			g.DrawImage(img, logoX, intY, 200, 24);

			return Convert.ToInt32(1);
		}

		public static void addFooter(PrintPageEventArgs e, DateTime date, int pageNum )
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.PaperSize.Height - (ps.Margins.Bottom/2) - 10;

			// Set font
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			System.Globalization.CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentCulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = mainForm.MachineCulture();
			string text = date.ToString() + " - Page " + pageNum.ToString();
			System.Threading.Thread.CurrentThread.CurrentCulture = ci;

			// Find center of page
			SizeF textSize = e.Graphics.MeasureString(text, fontText);
			int x = intX + ((intWidth/2) - (Convert.ToInt32(textSize.Width)/2));			
			
			g.DrawString(text, fontText, Brushes.Black, x, intY, new StringFormat());			
		}

		public static int addTitle(PrintPageEventArgs e, int fromY, string title)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 16, FontStyle.Bold);

			// Find center of page
			SizeF textSize = e.Graphics.MeasureString(title, fontTitle);
			int x = intX + ((intWidth/2) - (Convert.ToInt32(textSize.Width)/2));

			g.DrawString(title, fontTitle, Brushes.Black, x, intY, new StringFormat());
			return Convert.ToInt32(textSize.Height) + fromY;
		}

		public static int addSubTitle(PrintPageEventArgs e, int fromY, string title)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 12, FontStyle.Bold);

			// Find center of page
			SizeF textSize = e.Graphics.MeasureString(title, fontTitle);
			int x = intX;

			g.DrawString(title, fontTitle, Brushes.Black, x, intY, new StringFormat());

			return Convert.ToInt32(textSize.Height) + fromY;
		}

		public static int addProgramHeader(PrintPageEventArgs e, int fromY,  string programname, string filename, string owner, string user, string version)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeProgramNameTitle = e.Graphics.MeasureString(m_leftColumnWidthText, fontTitle);
			SizeF sizeVersionTitle = e.Graphics.MeasureString(m_rightColumnWidthText, fontTitle);
			
			int lineSpacing = 2;
			int padding = 3;
			int titleSpacing = 5;
			int frameHeight = (lineHeight * 3) + (lineSpacing * 2) + (padding * 2);
			
			//Begin drawing
			int centerX = intX + (intWidth/2);
			Pen pen = new Pen(Brushes.Black, 1);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, centerX, intY, centerX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = intX + Convert.ToInt32(pen.Width) + padding;
			int valueLeftX = titleLeftX + Convert.ToInt32(sizeProgramNameTitle.Width) + titleSpacing;
			int titleRightX = centerX + Convert.ToInt32(pen.Width) + padding;
			int valueRightX = titleRightX + Convert.ToInt32(sizeVersionTitle.Width) + titleSpacing;

			float leftColWidth = (intWidth/2)-sizeProgramNameTitle.Width-20;
			float rightColWidth = (intWidth/2)-sizeVersionTitle.Width-20;
			RectangleF rect;
			
			// Program name title
			g.DrawString("Program name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Program name
			x = valueLeftX;
//			string name = (string)programname.Clone();
//			if(programname.Length > 35)
//			{
//				name = name.Substring(0, 35);
//			}
//			g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, leftColWidth, lineHeight );
			g.DrawString(programname, fontText, Brushes.Black, rect, new StringFormat());

			// File name title
			x = titleRightX;
			g.DrawString("File name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// File name
			x = valueRightX;
//			if(filename.Length > 28)
//			{
//				filename = filename.Substring(0, 28);
//			}
//			g.DrawString(filename, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, rightColWidth, lineHeight );
			g.DrawString(filename, fontText, Brushes.Black, rect, new StringFormat());

			// Next line
			y = y + lineHeight + lineSpacing;

			// Owner title
			x = titleLeftX;
			g.DrawString("Owner:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Owner
			x = valueLeftX;
//			if(owner.Length > 28)
//			{
//				owner = owner.Substring(0, 28);
//			}
//			g.DrawString(owner, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, leftColWidth, lineHeight );
			g.DrawString(owner, fontText, Brushes.Black, rect, new StringFormat());

			// Username title
			x = titleRightX;
			g.DrawString("User name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Username
			x = valueRightX;
//			if(user.Length > 28)
//			{
//				user = user.Substring(0, 28);
//			}
//			g.DrawString(user, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, rightColWidth, lineHeight );
			g.DrawString(user, fontText, Brushes.Black, rect, new StringFormat());
			
			// Next line
			y = y + lineHeight + lineSpacing;

			// Version title
			x = titleRightX;
			g.DrawString("SW version:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Version
			x = valueRightX;
			g.DrawString(version, fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}
		
//		public static int addCurrentProgramHeader(PrintPageEventArgs e, int fromY,  string programname, string owner, string user, string version)
//		{
//			Graphics g = e.Graphics;
//			
//			PageSettings ps = e.PageSettings;
//
//			// Set internal work area
//			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
//			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
//			int intX = ps.Margins.Left;
//			int intY = ps.Margins.Top + fromY;
//
//			// Set font
//			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
//			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);
//
//			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
//			SizeF sizeProgramNameTitle = e.Graphics.MeasureString(m_leftColumnWidthText, fontTitle);
//			SizeF sizeVersionTitle = e.Graphics.MeasureString(m_rightColumnWidthText, fontTitle);
//			
//			int lineSpacing = 2;
//			int padding = 3;
//			int titleSpacing = 5;
//			int frameHeight = (lineHeight * 2) + (lineSpacing * 1) + (padding * 2);
//			
//			//Begin drawing
//			int centerX = intX + (intWidth/2);
//			Pen pen = new Pen(Brushes.Black, 1);
//			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
//			g.DrawLine(pen, centerX, intY, centerX, intY + frameHeight);
//
//			int x = intX + Convert.ToInt32(pen.Width) + padding;
//			int y = intY + Convert.ToInt32(pen.Width) + padding;
//			
//			int titleLeftX = intX + Convert.ToInt32(pen.Width) + padding;
//			int valueLeftX = titleLeftX + Convert.ToInt32(sizeProgramNameTitle.Width) + titleSpacing;
//			int titleRightX = centerX + Convert.ToInt32(pen.Width) + padding;
//			int valueRightX = titleRightX + Convert.ToInt32(sizeVersionTitle.Width) + titleSpacing;
//			
//			// Program name title
//			g.DrawString("Program name:", fontTitle, Brushes.Black, x, y, new StringFormat());
//
//			// Program name
//			x = valueLeftX;
//			string name = (string)programname.Clone();
//			if(programname.Length > 35)
//			{
//				name = name.Substring(0, 35);
//			}
//			g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());
//
//			// Username title
//			x = titleRightX;
//			g.DrawString("User name:", fontTitle, Brushes.Black, x, y, new StringFormat());
//
//			// Username
//			x = valueRightX;
//			if(user.Length > 28)
//			{
//				user = user.Substring(0, 28);
//			}
//			g.DrawString(user, fontText, Brushes.Black, x, y, new StringFormat());
//
//			// Next line
//			y = y + lineHeight + lineSpacing;
//
//			// Owner title
//			x = titleLeftX;
//			g.DrawString("Owner:", fontTitle, Brushes.Black, x, y, new StringFormat());
//
//			// Owner
//			x = valueLeftX;
//			if(owner.Length > 28)
//			{
//				owner = owner.Substring(0, 28);
//			}
//			g.DrawString(owner, fontText, Brushes.Black, x, y, new StringFormat());
//
//			// Version title
//			x = titleRightX;
//			g.DrawString("SW version:", fontTitle, Brushes.Black, x, y, new StringFormat());
//
//			// Version
//			x = valueRightX;
//			g.DrawString(version, fontText, Brushes.Black, x, y, new StringFormat());
//
//			return frameHeight + fromY;
//		}

		//public static int addPlateInfo(PrintPageEventArgs e, int fromY,  string format, string offset, string rowspacing, string height, string welldepth, string maxvolume, string aspoffset)
		public static int addPlateInfo(PrintPageEventArgs e, int fromY,
					string format,
					string rowOffset,
					string rowSpacing,
					string columnOffset,
					string columnSpacing,
					string height,
					string welldepth,
					string maxvolume,
					//string aspoffset,
					string plate,
					string catalog,
					string wellType,
					string loBase,
					string wellDiameter,
					string wellShape
			)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeRowSpacingTitle = e.Graphics.MeasureString(m_leftColumnWidthText, fontTitle);
			SizeF sizeMaxVolumeTitle = e.Graphics.MeasureString(m_rightColumnWidthText, fontTitle);

			float leftColWidth = (intWidth/2)-sizeRowSpacingTitle.Width-20;
			float rightColWidth = (intWidth/2)-sizeMaxVolumeTitle.Width-20;
			
			int lineSpacing = 3;
			int padding = 3;
			int titleSpacing = 5;
			int frameHeight = (lineHeight * 7) + (lineSpacing * 5) + (padding * 2); // 7 is number of lines
			
			//Begin drawing

			RectangleF rect;

			int centerX = intX + (intWidth/2);
			Pen pen = new Pen(Brushes.Black, 1);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, centerX, intY, centerX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = intX + Convert.ToInt32(pen.Width) + padding;
			int valueLeftX = titleLeftX + Convert.ToInt32(sizeRowSpacingTitle.Width) + titleSpacing;
			int titleRightX = centerX + Convert.ToInt32(pen.Width) + padding;
			int valueRightX = titleRightX + Convert.ToInt32(sizeMaxVolumeTitle.Width) + titleSpacing;

			// Line 1
			// Format title
			g.DrawString("Format:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Format
			x = valueLeftX;
			//g.DrawString(format, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, leftColWidth, lineHeight );
			g.DrawString(format, fontText, Brushes.Black, rect, new StringFormat());

			// Well type title
			x = titleRightX;
			g.DrawString("Well orientation:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Well type
			x = valueRightX;
			rect = new RectangleF( x, y, rightColWidth, lineHeight );
			g.DrawString(wellType, fontText, Brushes.Black, rect, new StringFormat());

			//Line 2
			// New line
			int y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Plate title
			x = titleLeftX;
			g.DrawString("Name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Plate name
			x = valueLeftX;
			//g.DrawString(plate, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, leftColWidth, lineHeight );
			g.DrawString(plate, fontText, Brushes.Black, rect, new StringFormat());

			// Catalog title
			x = titleRightX;
			g.DrawString("Catalog number:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Catalog number
			x = valueRightX;
			//g.DrawString(catalog, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, rightColWidth, lineHeight );
			g.DrawString(catalog, fontText, Brushes.Black, rect, new StringFormat());

			//Line 3
			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Row spacing title
			x = titleLeftX;
			g.DrawString("Row spacing:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Row spacing
			x = valueLeftX;
			g.DrawString(rowSpacing, fontText, Brushes.Black, x, y, new StringFormat());

			// Offset title
			x = titleRightX;
			g.DrawString("Row offset:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Offset
			x = valueRightX;
			g.DrawString(rowOffset, fontText, Brushes.Black, x, y, new StringFormat());

			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Column spacing title
			x = titleLeftX;
			g.DrawString("Column spacing:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Column spacing
			x = valueLeftX;
			g.DrawString(columnSpacing, fontText, Brushes.Black, x, y, new StringFormat());

			// Offset title
			x = titleRightX;
			g.DrawString("Column offset:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Offset
			x = valueRightX;
			g.DrawString(columnOffset, fontText, Brushes.Black, x, y, new StringFormat());


			//Line 5
			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Well depth title
			x = titleLeftX;
			g.DrawString("Well depth:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Well depth
			x = valueLeftX;
			g.DrawString(welldepth, fontText, Brushes.Black, x, y, new StringFormat());

			// Height title
			x = titleRightX;
			g.DrawString("Height:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Height
			x = valueRightX;
			g.DrawString(height, fontText, Brushes.Black, x, y, new StringFormat());

			//Line 6
			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Max volume title
			x = titleLeftX;
			g.DrawString("Max volume:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Max volume
			x = valueLeftX;
			g.DrawString(maxvolume, fontText, Brushes.Black, x, y, new StringFormat());

			// LoBase title
			x = titleRightX;
			g.DrawString("Extended rim:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Height
			x = valueRightX;
			g.DrawString(loBase, fontText, Brushes.Black, x, y, new StringFormat());

			//Line 7
			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			x = titleLeftX;
			g.DrawString("Bottom well diameter:", fontTitle, Brushes.Black, x, y, new StringFormat());

			x = valueLeftX;
			g.DrawString(wellDiameter, fontText, Brushes.Black, x, y, new StringFormat());

			x = titleRightX;
			g.DrawString("Well shape:", fontTitle, Brushes.Black, x, y, new StringFormat());

			x = valueRightX;
			g.DrawString(wellShape, fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}
	
		public static int addStepHeader(PrintPageEventArgs e, int fromY, string plate, string rows, int format)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeStepTitle = e.Graphics.MeasureString("Step", fontTitle);
			SizeF sizePlateTitle = e.Graphics.MeasureString("Plate:", fontTitle);
			SizeF sizeRowsTitle = e.Graphics.MeasureString("Columns:", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int titleSpacing = 5;
			int frameHeight;

			SizeF textSize = e.Graphics.MeasureString(rows, fontText);
			if( textSize.Width > 570 )
			{
				frameHeight = (lineHeight * 2) + (padding * 2);
			}
			else
			{
				frameHeight = lineHeight + (padding * 2);
			}			


			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int centerX = intX + (intWidth/2) - 47;
			int stepX = intX + Convert.ToInt32(sizeStepTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
//			g.DrawLine(pen, centerX, intY, centerX, intY + frameHeight);
			g.DrawLine(pen, stepX, intY, stepX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = stepX + padding + Convert.ToInt32(pen.Width);
			int valueLeftX;
			if( format < 4 )
				valueLeftX = titleLeftX + Convert.ToInt32(sizePlateTitle.Width) + titleSpacing;
			else
				valueLeftX = titleLeftX + Convert.ToInt32(sizeRowsTitle.Width) + titleSpacing;
			int titleRightX = centerX + Convert.ToInt32(pen.Width) + padding;
			int valueRightX = titleRightX + Convert.ToInt32(sizeRowsTitle.Width) + titleSpacing;
			
			// Step title
			g.DrawString("Step", fontTitle, Brushes.Black, x, y, new StringFormat());

//			// Plate title
//			x = titleLeftX;
//			g.DrawString("Plate:", fontTitle, Brushes.Black, x, y, new StringFormat());
//
//			// Plate
//			x = valueLeftX;
//			string name = (string)plate.Clone();
//			if(name.Length > 28)
//			{
//				name = name.Substring(0, 28);
//			}
//			g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());

			// Rows title
			x = titleLeftX;
//			if( format < 4 )
//				g.DrawString("Rows:", fontTitle, Brushes.Black, x, y, new StringFormat());
//			else
//				g.DrawString("Columns:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Rows
//			x = valueLeftX;

			if( textSize.Width > 570 )
			{
				string[] parts = rows.Split( ';' );
				string r1 = string.Join( "; ", parts, 0, parts.Length-2 );
				string r2 = string.Join( "; ", parts, parts.Length-2, 2 );
				g.DrawString(r1, fontText, Brushes.Black, x, y, new StringFormat());
				y += Convert.ToInt32( textSize.Height );
				x -= 3;
				g.DrawString(r2, fontText, Brushes.Black, x, y, new StringFormat());
			}
			else
			{
				g.DrawString(rows, fontText, Brushes.Black, x, y, new StringFormat());
			}

			return frameHeight + fromY;
		}
	
		public static int addStepAspirate(PrintPageEventArgs e, int fromY, string step, string name, string time, string height, string aspOffset, bool sweep)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeStepTitle = e.Graphics.MeasureString("Step", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int frameHeight = lineHeight + (padding * 2);
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int col1X = intX + padding + Convert.ToInt32(pen.Width);
			int col2X = intX + Convert.ToInt32(sizeStepTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int col3X = col2X + 70 + (padding*2) + Convert.ToInt32(pen.Width);
			int col4X = col3X + 150 + (padding*2) + Convert.ToInt32(pen.Width);
			int col5X = col4X + 80 + (padding*2) + Convert.ToInt32(pen.Width);
			int col6X = col5X + 80 + (padding*2) + Convert.ToInt32(pen.Width);

			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, col2X, intY, col2X, intY + frameHeight);
			g.DrawLine(pen, col3X, intY, col3X, intY + frameHeight);
			g.DrawLine(pen, col4X, intY, col4X, intY + frameHeight);
			g.DrawLine(pen, col5X, intY, col5X, intY + frameHeight);
			g.DrawLine(pen, col6X, intY, col6X, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			
			// Step
			x = col1X + Convert.ToInt32(pen.Width);
			g.DrawString(step, fontText, Brushes.Black, x, y, new StringFormat());

			// Type
			x = col2X + padding + Convert.ToInt32(pen.Width);
			string lead = sweep ? "Sweep Asp" : "Aspirate";
			g.DrawString(lead, fontText, Brushes.Black, x, y, new StringFormat());

			x = col3X + padding + Convert.ToInt32(pen.Width);
			g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());

			x = col4X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Time: " + time, fontText, Brushes.Black, x, y, new StringFormat());

			x = col5X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Height: " + height, fontText, Brushes.Black, x, y, new StringFormat());

			x = col6X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("ASP Offset: " + aspOffset, fontText, Brushes.Black, x, y, new StringFormat());


			return frameHeight + fromY;
		}

		public static int addStepDispense(PrintPageEventArgs e, int fromY,  string step, string name, string inlet, string v, string lf, string pressure)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeStepTitle = e.Graphics.MeasureString("Step", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int frameHeight = lineHeight + (padding * 2);
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int col1X = intX + padding + Convert.ToInt32(pen.Width);
			int col2X = intX + Convert.ToInt32(sizeStepTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int col3X = col2X + 70 + (padding*2) + Convert.ToInt32(pen.Width);
			int col4X = col3X + 150 + (padding*2) + Convert.ToInt32(pen.Width);
			int col5X = col4X + 80 + (padding*2) + Convert.ToInt32(pen.Width);
			int col6X = col5X + 80 + (padding*2) + Convert.ToInt32(pen.Width);
			int col7X = col6X + 90 + (padding*2) + Convert.ToInt32(pen.Width);

			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, col2X, intY, col2X, intY + frameHeight);
			g.DrawLine(pen, col3X, intY, col3X, intY + frameHeight);
			g.DrawLine(pen, col4X, intY, col4X, intY + frameHeight);
			g.DrawLine(pen, col5X, intY, col5X, intY + frameHeight);
			g.DrawLine(pen, col6X, intY, col6X, intY + frameHeight);
			g.DrawLine(pen, col7X, intY, col7X, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			
			// Step
			x = col1X + Convert.ToInt32(pen.Width);
			g.DrawString(step, fontText, Brushes.Black, x, y, new StringFormat());

			// Type
			x = col2X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Dispense", fontText, Brushes.Black, x, y, new StringFormat());

			x = col3X + padding + Convert.ToInt32(pen.Width);

			SizeF sizeLiquid = e.Graphics.MeasureString(name, fontTitle);
			while( sizeLiquid.Width > col4X - col3X )
			{
				name = name.Substring( 0, name.Length-1 );
				sizeLiquid = e.Graphics.MeasureString(name, fontTitle);
			}
			g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());

			x = col4X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Inlet: " + inlet, fontText, Brushes.Black, x, y, new StringFormat());

			x = col5X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Volume: " + v, fontText, Brushes.Black, x, y, new StringFormat());

			x = col6X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Liquid factor: " + lf, fontText, Brushes.Black, x, y, new StringFormat());

			x = col7X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Press.: " + pressure + " mBar", fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}
	
		public static int addStepRepeat(PrintPageEventArgs e, int fromY,  string step, string start, string end, string repeats)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeStepTitle = e.Graphics.MeasureString("Step", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int frameHeight = lineHeight + (padding * 2);
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int col1X = intX + padding + Convert.ToInt32(pen.Width);
			int col2X = intX + Convert.ToInt32(sizeStepTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int col3X = col2X + 70 + (padding*2) + Convert.ToInt32(pen.Width);
			int col4X = col3X + 73 + (padding*2) + Convert.ToInt32(pen.Width);
			int col5X = col4X + 70 + (padding*2) + Convert.ToInt32(pen.Width);
			int col6X = col5X + 80 + (padding*2) + Convert.ToInt32(pen.Width);

			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, col2X, intY, col2X, intY + frameHeight);
			g.DrawLine(pen, col3X, intY, col3X, intY + frameHeight);
			g.DrawLine(pen, col4X, intY, col4X, intY + frameHeight);
			g.DrawLine(pen, col5X, intY, col5X, intY + frameHeight);
			g.DrawLine(pen, col6X, intY, col6X, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			
			// Step
			x = col1X + Convert.ToInt32(pen.Width);
			g.DrawString(step, fontText, Brushes.Black, x, y, new StringFormat());

			// Type
			x = col2X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Repeat", fontText, Brushes.Black, x, y, new StringFormat());

			x = col3X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Start: " + start, fontText, Brushes.Black, x, y, new StringFormat());

			x = col4X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("End: " + end, fontText, Brushes.Black, x, y, new StringFormat());

			x = col5X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Repeat(s): " + repeats, fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}

		public static int addStepSoak(PrintPageEventArgs e, int fromY,  string step, string time)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeStepTitle = e.Graphics.MeasureString("Step", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int frameHeight = lineHeight + (padding * 2);
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int col1X = intX + padding + Convert.ToInt32(pen.Width);
			int col2X = intX + Convert.ToInt32(sizeStepTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int col3X = col2X + 70 + (padding*2) + Convert.ToInt32(pen.Width);

			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, col2X, intY, col2X, intY + frameHeight);
			g.DrawLine(pen, col3X, intY, col3X, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			
			// Step
			x = col1X + Convert.ToInt32(pen.Width);
			g.DrawString(step, fontText, Brushes.Black, x, y, new StringFormat());

			// Type
			x = col2X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Soak", fontText, Brushes.Black, x, y, new StringFormat());

			x = col3X + padding + Convert.ToInt32(pen.Width);
			g.DrawString("Time: " + time, fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}
	
		public static int addStepRowSelector(PrintPageEventArgs e, int fromY,  string step, string rows)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeStepTitle = e.Graphics.MeasureString("Step", fontTitle);

			int padding = 3;
			int frameHeight;
			SizeF textSize = e.Graphics.MeasureString(rows, fontText);
			if( textSize.Width > 570 )
			{
				frameHeight = (lineHeight * 2) + (padding * 2);
			}
			else
			{
				frameHeight = lineHeight + (padding * 2);
			}			
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int col1X = intX + padding + Convert.ToInt32(pen.Width);
			int col2X = intX + Convert.ToInt32(sizeStepTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int col3X = col2X + 70 + (padding*2) + Convert.ToInt32(pen.Width);

			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, col2X, intY, col2X, intY + frameHeight);
			g.DrawLine(pen, col3X, intY, col3X, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			
			// Step
			x = col1X + Convert.ToInt32(pen.Width);
			g.DrawString(step, fontText, Brushes.Black, x, y, new StringFormat());

			// Type
			x = col2X + padding + Convert.ToInt32(pen.Width);
			//g.DrawString("Rows selector", fontText, Brushes.Black, x, y, new StringFormat());

			g.DrawString("Wells", fontText, Brushes.Black, x, y, new StringFormat());

			x = col3X + padding + Convert.ToInt32(pen.Width);

			if( textSize.Width > 570 )
			{
				string[] parts = rows.Split( ';' );
				string r1 = string.Join( "; ", parts, 0, parts.Length-3 );
				string r2 = string.Join( "; ", parts, parts.Length-3, 3 );
				g.DrawString(r1, fontText, Brushes.Black, x, y, new StringFormat());
				y += Convert.ToInt32( textSize.Height );
				x -= 3;
				g.DrawString(r2, fontText, Brushes.Black, x, y, new StringFormat());
			}
			else
			{
				g.DrawString(rows, fontText, Brushes.Black, x, y, new StringFormat());
			}

			return frameHeight + fromY;
		}

		public static int addLiquidHeader(PrintPageEventArgs e, int fromY)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeBlankTitle = e.Graphics.MeasureString("___", fontTitle);
			SizeF sizeLFTitle = e.Graphics.MeasureString("LF:__", fontTitle);
			SizeF sizeNameTitle = e.Graphics.MeasureString("Name:", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int frameHeight = lineHeight + (padding * 2);
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int X1 = intX + padding;
			int blankX = intX + Convert.ToInt32(sizeBlankTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int X2 = blankX + Convert.ToInt32(sizeLFTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, X2, intY, X2, intY + frameHeight);
			g.DrawLine(pen, blankX, intY, blankX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = X1 + padding + Convert.ToInt32(pen.Width);
			int titleMidX = titleLeftX + Convert.ToInt32(sizeBlankTitle.Width)+ Convert.ToInt32(pen.Width) + padding;
			int titleRightX = titleMidX + Convert.ToInt32(sizeLFTitle.Width) + Convert.ToInt32(pen.Width) + (padding*2);
			
			// LF
			x = titleMidX;
			g.DrawString("LF:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Name title
			x = titleRightX;
			g.DrawString("Name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}

		public static int addLiquidItem(PrintPageEventArgs e, int fromY, string itemNo, string lf, string name)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeBlankTitle = e.Graphics.MeasureString("___", fontTitle);
			SizeF sizeLFTitle = e.Graphics.MeasureString("LF:__", fontTitle);
			SizeF sizeNameTitle = e.Graphics.MeasureString("Name:", fontTitle);
			
			//int lineSpacing = 2;
			int padding = 3;
			int frameHeight = lineHeight + (padding * 2);
			
			//Begin drawing
			Pen pen = new Pen(Brushes.Black, 1);
			int X1 = intX + padding;
			int blankX = intX + Convert.ToInt32(sizeBlankTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			int X2 = blankX + Convert.ToInt32(sizeLFTitle.Width) + (padding*2) + Convert.ToInt32(pen.Width);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, X2, intY, X2, intY + frameHeight);
			g.DrawLine(pen, blankX, intY, blankX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = X1 + padding + Convert.ToInt32(pen.Width);
			int titleMidX = titleLeftX + Convert.ToInt32(sizeBlankTitle.Width)+ Convert.ToInt32(pen.Width) + padding;
			int titleRightX = titleMidX + Convert.ToInt32(sizeLFTitle.Width) + Convert.ToInt32(pen.Width) + (padding*2);
			
			// LF
			x = titleLeftX;
			g.DrawString(itemNo, fontText, Brushes.Black, x, y, new StringFormat());

			// LF
			x = titleMidX;
			g.DrawString(lf, fontText, Brushes.Black, x, y, new StringFormat());

			// Name
			x = titleRightX;
			g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}

		public static int addSimpleTitleHeader(PrintPageEventArgs e, int fromY,  string user, string version)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeUserTitle = e.Graphics.MeasureString("User name:", fontTitle);
			
			int lineSpacing = 2;
			int padding = 3;
			int titleSpacing = 20;
			int frameHeight = (lineHeight * 2) + (lineSpacing * 1) + (padding * 2);
			
			//Begin drawing
			int centerX = intX + (intWidth/2);
			Pen pen = new Pen(Brushes.Black, 1);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			//g.DrawLine(pen, centerX, intY, centerX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = intX + Convert.ToInt32(pen.Width) + padding;
			int valueLeftX = titleLeftX + Convert.ToInt32(sizeUserTitle.Width) + titleSpacing;
			
			// Username title
			g.DrawString("User name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Username
			x = valueLeftX;
			g.DrawString(user, fontText, Brushes.Black, x, y, new StringFormat());


			// Next line
			y = y + lineHeight + lineSpacing;

			// Version title
			x = titleLeftX;
			g.DrawString("SW Version:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Version
			x = valueLeftX;
			g.DrawString(version, fontText, Brushes.Black, x, y, new StringFormat());

			return frameHeight + fromY;
		}
	
		public static int addPlateInfo2(
				PrintPageEventArgs e,
				int fromY,
				string name,
				string rowOffset,
				string rowSpacing,
				string columnOffset,
				string columnSpacing,
				string height,
				string welldepth,
				string maxvolume,
				//string aspoffset,
				string catalog,
				string loBase,
				string wellDiameter,
				string wellShape
			)
		{
			Graphics g = e.Graphics;
			
			PageSettings ps = e.PageSettings;

			// Set internal work area
			int intWidth = ps.PaperSize.Width - ps.Margins.Left - ps.Margins.Right;
			int intHeight = ps.PaperSize.Height - ps.Margins.Top - ps.Margins.Bottom;
			int intX = ps.Margins.Left;
			int intY = ps.Margins.Top + fromY;

			// Set font
			Font fontTitle = new Font("Times New Roman", 8, FontStyle.Bold);
			Font fontText = new Font("Times New Roman", 8, FontStyle.Regular);

			int lineHeight = Convert.ToInt32(e.Graphics.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", fontTitle).Height);
			SizeF sizeRowSpacingTitle = e.Graphics.MeasureString("Bottom well diameter:", fontTitle);
			SizeF sizeMaxVolumeTitle = e.Graphics.MeasureString("Catalog number:", fontTitle);

			float leftColWidth = (intWidth/2)-sizeRowSpacingTitle.Width-20;
			float rightColWidth = (intWidth/2)-sizeMaxVolumeTitle.Width-20;
			
			int lineSpacing = 3;
			int padding = 3;
			int titleSpacing = 20;
			int frameHeight = (lineHeight * 6) + (lineSpacing * 4) + (padding * 2);
			
			//Begin drawing
			RectangleF rect;

			int centerX = intX + (intWidth/2);
			Pen pen = new Pen(Brushes.Black, 1);
			g.DrawRectangle(pen, intX, intY, intWidth, frameHeight);
			g.DrawLine(pen, centerX, intY, centerX, intY + frameHeight);

			int x = intX + Convert.ToInt32(pen.Width) + padding;
			int y = intY + Convert.ToInt32(pen.Width) + padding;
			
			int titleLeftX = intX + Convert.ToInt32(pen.Width) + padding;
			int valueLeftX = titleLeftX + Convert.ToInt32(sizeRowSpacingTitle.Width) + titleSpacing;
			int titleRightX = centerX + Convert.ToInt32(pen.Width) + padding;
			int valueRightX = titleRightX + Convert.ToInt32(sizeMaxVolumeTitle.Width) + titleSpacing;
			
			// Format title
			g.DrawString("Name:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Format
			x = valueLeftX;
			//g.DrawString(name, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, leftColWidth, lineHeight );
			g.DrawString( name, fontText, Brushes.Black, rect, new StringFormat() );


			// Catalog title
			x = titleRightX;
			g.DrawString("Catalog number:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Catalog numer
			x = valueRightX;
//			g.DrawString(catalog, fontText, Brushes.Black, x, y, new StringFormat());
			rect = new RectangleF( x, y, rightColWidth, lineHeight );
			g.DrawString( catalog, fontText, Brushes.Black, rect, new StringFormat() );

			// New line
			int y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Row spacing title
			x = titleLeftX;
			g.DrawString("Row spacing:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Row spacing
			x = valueLeftX;
			g.DrawString(rowSpacing, fontText, Brushes.Black, x, y, new StringFormat());

			// Offset title
			x = titleRightX;
			g.DrawString("Row offset:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Offset
			x = valueRightX;
			g.DrawString(rowOffset, fontText, Brushes.Black, x, y, new StringFormat());

			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Row spacing title
			x = titleLeftX;
			g.DrawString("Column spacing:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Row spacing
			x = valueLeftX;
			g.DrawString(columnSpacing, fontText, Brushes.Black, x, y, new StringFormat());

			// Offset title
			x = titleRightX;
			g.DrawString("Column offset:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Offset
			x = valueRightX;
			g.DrawString(columnOffset, fontText, Brushes.Black, x, y, new StringFormat());

			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			// Well depth title
			x = titleLeftX;
			g.DrawString("Well depth:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Well depth
			x = valueLeftX;
			g.DrawString(welldepth, fontText, Brushes.Black, x, y, new StringFormat());

			// Height title
			x = titleRightX;
			g.DrawString("Height:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Height
			x = valueRightX;
			g.DrawString(height, fontText, Brushes.Black, x, y, new StringFormat());

			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

//			// ASP offset title
//			x = titleLeftX;
//			g.DrawString("ASP offset:", fontTitle, Brushes.Black, x, y, new StringFormat());
//
//			// ASP offset
//			x = valueLeftX;
//			g.DrawString(aspoffset, fontText, Brushes.Black, x, y, new StringFormat());

			// Max volume title
			x = titleLeftX;
			g.DrawString("Max volume:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Max volume
			x = valueLeftX;
			g.DrawString(maxvolume, fontText, Brushes.Black, x, y, new StringFormat());

			// LoBase title
			x = titleRightX;
			g.DrawString("Extended rim:", fontTitle, Brushes.Black, x, y, new StringFormat());

			// Height
			x = valueRightX;
			g.DrawString(loBase, fontText, Brushes.Black, x, y, new StringFormat());

			//Line 7
			// New line
			y2 = y + lineHeight + (lineSpacing/2);
			g.DrawLine(pen, intX, y2, intWidth + ps.Margins.Right, y2);

			// Next line
			y = y + lineHeight + lineSpacing;

			x = titleLeftX;
			g.DrawString("Bottom well diameter:", fontTitle, Brushes.Black, x, y, new StringFormat());

			x = valueLeftX;
			g.DrawString(wellDiameter, fontText, Brushes.Black, x, y, new StringFormat());

			x = titleRightX;
			g.DrawString("Well shape:", fontTitle, Brushes.Black, x, y, new StringFormat());

			x = valueRightX;
			g.DrawString(wellShape, fontText, Brushes.Black, x, y, new StringFormat());


			return frameHeight + fromY;
		}
	}
}
