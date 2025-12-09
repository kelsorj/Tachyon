using System;

namespace AQ3
{
	/// <summary>
	/// Summary description for CUser.
	/// </summary>

	public class CUser
	{
		private string m_Username;
		private string m_Password;
		private int m_UserLevel;

		public string Username
		{
			get { return m_Username; }
			set { m_Username = value; }
		}

		public string Password
		{
			get { return m_Password; }
			set { m_Password = value; }
		}

		public int UserLevel
		{
			get { return m_UserLevel; }
			set { m_UserLevel = value; }
		}
	}
}
