using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMTPReporting
{
    public class SMTPConfiguration
    {
        /// <summary>
        /// the user name that will be used for login to the email server
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// the encrypted password that will be used for login to the email server
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// the name of the email server that will be used to send messages from synapsis
        /// </summary>
        public string SMTPServer { get; set; }
        /// <summary>
        /// the port for the email server
        /// </summary>
        public int SMTPPort { get; set; }
        /// <summary>
        /// true/false does the email server use SSL for the password transmission
        /// </summary>
        public bool UseSSL { get; set; }
        /// <summary>
        /// true/false should synapsis use the emailing function to send messages
        /// </summary>
        public bool EnableEmail { get; set; }
        /// <summary>
        /// the from address in the email to be used for filtering, etc.
        /// </summary>
        public string From { get; set; }
        /// <summary>
        /// a comma delimited list of recipients that will all get the messages from the system
        /// </summary>
        public string RecipientList { get; set; }
    }
}
