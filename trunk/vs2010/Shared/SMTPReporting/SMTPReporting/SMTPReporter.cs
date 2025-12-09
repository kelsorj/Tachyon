using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Net.Mail;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;
using log4net;

namespace SMTPReporting
{
    [Export(typeof(SMTPReporter))]
    [Export(typeof(IErrorNotification))]
    [Export(typeof(ISystemSetupEditor))]
    public class SMTPReporter : ISystemSetupEditor, IErrorNotification
    {
        // UI settable fields
        public string Username { get { return _configuration.Username; } set { _configuration.Username = value; } }
        public string ServerName { get { return _configuration.SMTPServer; } set { _configuration.SMTPServer = value; } }
        public int SMTPPort { get { return _configuration.SMTPPort; } set { _configuration.SMTPPort = value; } }
        public bool SSLEnable { get { return _configuration.UseSSL; } set { _configuration.UseSSL = value; } }
        public bool EmailEnable { get { return _configuration.EnableEmail; } set { _configuration.EnableEmail = value; } }
        public string From { get { return _configuration.From; } set { _configuration.From = value; } }
        public string RecipientList { get { return _configuration.RecipientList; } set { _configuration.RecipientList = value; } }

        public string EmailPassword { get { return _configuration.Password; } set { _configuration.Password = value; } }


        // fields set after construction
        public string HiveName { get; set; }

        // private fields
        private readonly string _configPath;
        private readonly SMTPConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(typeof(SMTPReporter));

        // Test button command
        public RelayCommand TestConnectionCommand { get; set; }

        public SMTPReporter() {
            HiveName = "Synapsis"; // default hive name
            _configPath = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\config\\EmailSettings.xml";
            _configuration = FileSystem.LoadXmlConfiguration<SMTPConfiguration>(_configPath);
            if( _configuration.Password != null)
                _configuration.Password = Encryption.Decrypt(_configuration.Password, "bionex-email-secret");  // our password is disk secure, maybe some day we'll figure out how to make it mem secure
            TestConnectionCommand = new RelayCommand(() => { SendMessage("Test Message", "Synapsis Email is Enabled"); });
        }

        /// <summary>
        /// ISystemSetupEditor uses GetName for menu caption
        /// </summary>
        /// <returns></returns>
        public string Name
        {
            get
            {
                return "Email Settings";
            }
        }

        public void ShowTool()
        {
            var panel = new SMTPDialog(this);
            panel.Show();
        }

        public void SaveConfiguration()
        {
            var password = _configuration.Password;
            if (password != null)
                _configuration.Password = Encryption.Encrypt(password, "bionex-email-secret");
            FileSystem.SaveXmlConfiguration<SMTPConfiguration>(_configuration, _configPath);
            _configuration.Password = password;
        }

        public class TestCreds : ICredentialsByHost
        {
            private readonly SMTPReporter parent;
            public TestCreds(SMTPReporter foo) { parent = foo; }

            #region ICredentialsByHost Members
            public NetworkCredential GetCredential(string host, int port, string authenticationType)
            {
                //user = donotreply@bionexsolutions.com
                //password = bionex;
                //server = mail.bionexsolutions.com;
                //port = 26;
                return new NetworkCredential(parent.Username, parent.EmailPassword);
            }
            #endregion
        }

        public void SendMessage(string subject, string body)
        {
            // don't Send Messages if they've turned off email notification
            if (!EmailEnable)
                return;

            subject = String.Format("{0} - {1}", HiveName, subject);
            
            try
            {
                MailMessage message = new MailMessage(From, RecipientList, subject, body);           
                SmtpClient client = new SmtpClient(ServerName, SMTPPort);
                // Credentials are necessary if the server requires the client 
                // to authenticate before it will send e-mail on the client's behalf.
                client.Credentials = new TestCreds(this);
                client.EnableSsl = SSLEnable;
                client.Send(message);
            }
            catch (Exception ex)
            {
                _log.Error( string.Format( "Failed to send mail, error was '{0}' see log for more details", ex.Message), ex);
            }
        }
        
        public static void Main()
        {
            var test = new SMTPReporter();
            test.ShowTool();
        }

        #region IErrorNotification Members

        public void SendNotification(string text1, string text2)
        {
            SendMessage( text1, text2);
        }

        #endregion
    }
}
