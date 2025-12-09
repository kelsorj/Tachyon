using System;
using System.Windows.Forms;
using System.Threading;

namespace AQ3
{
	/// <summary>
	/// Summary description for ErrorHandling.
	/// </summary>
	internal class CustomExceptionHandler 
	{
		public void OnThreadException(object sender, ThreadExceptionEventArgs t) 
		{
			DialogResult result = DialogResult.Cancel;
			try 
			{
				result = this.ShowThreadExceptionDialog(t.Exception);
			}
			catch 
			{
				try 
				{
					MessageBox.Show("Fatal Error",
						"Fatal Error",
						MessageBoxButtons.AbortRetryIgnore,
						MessageBoxIcon.Stop);
				}
				finally 
				{
					Application.Exit();
				}
			}
			if (result == DialogResult.Abort)
				Application.Exit();
		}

		//The simple dialog that is displayed when this class catches and exception
		private DialogResult ShowThreadExceptionDialog(Exception e) 
		{
			string errorMsg = "An error occurred. Please take a screen shot of this box and send " +
				"it to the program administrator.\n\n";
			errorMsg += e.Message + "\n\nStack Trace:\n" + e.StackTrace;			
			return MessageBox.Show(errorMsg,
				"Application Error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Stop);			
		}
	}
}
