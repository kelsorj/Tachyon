using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using PCComm;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace SonicDataCollection
{
    public partial class Form1 : Form
    {
        CommunicationManager comm1 = new CommunicationManager();
        CommunicationManager comm2 = new CommunicationManager();
        CommunicationManager comm3 = new CommunicationManager();
        CommunicationManager comm4 = new CommunicationManager();
        CommunicationManager comm5 = new CommunicationManager();
        CommunicationManager comm6 = new CommunicationManager();
        CommunicationManager comm7 = new CommunicationManager();
        CommunicationManager comm8 = new CommunicationManager();

        SerialPort port;

        string transType = string.Empty;
        public static String logLocation = "C:\\Logs\\" + System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") +
            "-logfile.txt"; // create a log file path for the current instance

        public Form1()
        {
            InitializeComponent();
            if (enable1.Checked)
            {
                comm1.FirstOpenPort("115200", "8", "1", "0", sencom1.Text);
            }
            if (enable2.Checked)
            {
                comm2.FirstOpenPort("115200", "8", "1", "0", sencom2.Text);
            }
            if (enable3.Checked)
            {
                comm3.FirstOpenPort("115200", "8", "1", "0", sencom3.Text);
            }
            if (enable4.Checked)
            {
                comm4.FirstOpenPort("115200", "8", "1", "0", sencom4.Text);
            }
            if (enable5.Checked)
            {
                comm5.FirstOpenPort("115200", "8", "1", "0", sencom5.Text);
            }
            if (enable6.Checked)
            {
                comm6.FirstOpenPort("115200", "8", "1", "0", sencom6.Text);
            }
            if (enable7.Checked)
            {
                comm7.FirstOpenPort("115200", "8", "1", "0", sencom7.Text);
            }
            if (enable8.Checked)
            {
                comm8.FirstOpenPort("115200", "8", "1", "0", sencom8.Text);
            }
            comm1.DisplayWindow = richTextBox1;
            comm2.DisplayWindow = richTextBox2;
            comm3.DisplayWindow = richTextBox3;
            comm4.DisplayWindow = richTextBox4;
            comm5.DisplayWindow = richTextBox5;
            comm6.DisplayWindow = richTextBox6;
            comm7.DisplayWindow = richTextBox7;
            comm8.DisplayWindow = richTextBox8;
        }

        private void setSensors_Click(object sender, EventArgs e)
        {
            CheckBox[] enables = new CheckBox[] { enable1, enable2, enable3, enable4, enable5, enable6, enable7, enable8 };
            TextBox[] comms = new TextBox[] { sencom1, sencom2, sencom3, sencom4, sencom5, sencom6, sencom7, sencom8 };
            CheckBox[] temps = new CheckBox[] { temp1, temp2, temp3, temp4, temp5, temp6, temp7, temp8 };
            TextBox[] senses = new TextBox[] { sense2, sense2, sense3, sense4, sense5, sense6, sense7, sense8 };
            TextBox[] rels = new TextBox[] { textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7, textBox8 };
            TextBox[] avgs = new TextBox[] { average1, average2, average3, average4, average5, average6, average7, average8 };
            CommunicationManager[] comport = new CommunicationManager[] { comm1,comm2,comm3,comm4,comm5,comm6,comm7,comm8};

            for (var i=0;i<8;++i){
                if (enables[i].Checked){
                     comport[i].FirstOpenPort("115200","8","1","0",comms[i].Text);
                    /*
                    comm1.WriteData("{0AB}");
                    comm1.WriteData("{0B"+sense1.Text+"}");
                    comm1.WriteData("{0C" + average1.Text + "}");
                    comm1.WriteData("{0FA}");
                     */
                    if (temps[i].Checked){
                        comport[i].WriteData("{0U" + rels[i].Text + "A" + senses[i].Text + avgs[i].Text + "1}");
                    }else{
                        comport[i].WriteData("{0U" + rels[i].Text + "A" + senses[i].Text + avgs[i].Text + "0}");
                    }
                }
            }
        }

        private void setTop_Click(object sender, EventArgs e)
        {
            CheckBox[] enables = new CheckBox[] { enable1, enable2, enable3, enable4, enable5, enable6, enable7, enable8 };
            TextBox[] comms = new TextBox[] { sencom1, sencom2, sencom3, sencom4, sencom5, sencom6, sencom7, sencom8 };
            CommunicationManager[] comport = new CommunicationManager[] { comm1,comm2,comm3,comm4,comm5,comm6,comm7,comm8};
            for (var i=0;i<8;++i){
                if (enables[i].Checked){
                    comport[i].FirstOpenPort("115200","8","1","0",comms[i].Text);
                    comport[i].WriteData("{0X}");
                }
            }
        }

        private void setBottom_Click(object sender, EventArgs e)
        {
            CheckBox[] enables = new CheckBox[] { enable1, enable2, enable3, enable4, enable5, enable6, enable7, enable8 };
            TextBox[] comms = new TextBox[] { sencom1, sencom2, sencom3, sencom4, sencom5, sencom6, sencom7, sencom8 };
            CommunicationManager[] comport = new CommunicationManager[] { comm1,comm2,comm3,comm4,comm5,comm6,comm7,comm8};
            for (var i=0;i<8;++i){
                if (enables[i].Checked){
                    comport[i].FirstOpenPort("115200","8","1","0",comms[i].Text);
                    comport[i].WriteData("{0Y}");
                }
            }
        }

        private void logData_Click(object sender, EventArgs e)
        {   
            string easy_text = "";
            CheckBox[] enables = new CheckBox[] { enable1, enable2, enable3, enable4, enable5, enable6, enable7, enable8 };
            CommunicationManager[] comport = new CommunicationManager[] { comm1,comm2,comm3,comm4,comm5,comm6,comm7,comm8};
            TextBox[] comms = new TextBox[] { sencom1, sencom2, sencom3, sencom4, sencom5, sencom6, sencom7, sencom8 };
            TextBox[] measured = new TextBox[] { textBox9, textBox10, textBox11, textBox12, textBox13, textBox14, textBox15, textBox16};
            for (var i=0;i<8;++i){
                if (enables[i].Checked){
                    comport[i].FirstOpenPort("115200","8","1","0",comms[i].Text);
                    comport[i].WriteData("{0M}");
                    comport[i].CloseOpenPort();
                    port = new SerialPort( comms[i].Text, 115200, Parity.None, 8, StopBits.One);
                    port.Open();
                    port.DiscardInBuffer();
                    port.Write("{0M}");
                    var buffer = port.ReadTo("}") + "}";
                    int start_of_message = buffer.IndexOf('{');
                    var parsed_buffer = buffer.Substring(start_of_message);
                    // get measurement
                    measured[i].Text = parsed_buffer.Substring(5, 4);
                    easy_text += measured[i].Text + "\r\n";
                    port.Close();
                }
            }
            easy_paste.Text = easy_text; 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (enable1.Checked)
            {
                comm1.WriteData("{0P}");
            }
            if (enable2.Checked)
            {
                comm2.WriteData("{0P}");
            }
            if (enable3.Checked)
            {
                comm3.WriteData("{0P}");
            }
            if (enable4.Checked)
            {
                comm4.WriteData("{0P}");
            }
            if (enable5.Checked)
            {
                comm5.WriteData("{0P}");
            }
            if (enable6.Checked)
            {
                comm6.WriteData("{0P}");
            }
            if (enable7.Checked)
            {
                comm7.WriteData("{0P}");
            }
            if (enable8.Checked)
            {
                comm8.WriteData("{0P}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (enable1.Checked)
            {
                comm1.WriteData("{0R}");
            }
            if (enable2.Checked)
            {
                comm2.WriteData("{0R}");
            }
            if (enable3.Checked)
            {
                comm3.WriteData("{0R}");
            }
            if (enable4.Checked)
            {
                comm4.WriteData("{0R}");
            }
            if (enable5.Checked)
            {
                comm5.WriteData("{0R}");
            }
            if (enable6.Checked)
            {
                comm6.WriteData("{0R}");
            }
            if (enable7.Checked)
            {
                comm7.WriteData("{0R}");
            }
            if (enable8.Checked)
            {
                comm8.WriteData("{0R}");
            }
        }

        private void saveData_Click(object sender, EventArgs e)
        {
            if (enable1.Checked)
            {
                comm1.WriteData("{0M}");
            }
            if (enable2.Checked)
            {
                comm2.WriteData("{0M}");
            }
            if (enable3.Checked)
            {
                comm3.WriteData("{0M}");
            }
            if (enable4.Checked)
            {
                comm4.WriteData("{0M}");
            }
            if (enable5.Checked)
            {
                comm5.WriteData("{0M}");
            }
            if (enable6.Checked)
            {
                comm6.WriteData("{0M}");
            }
            if (enable7.Checked)
            {
                comm7.WriteData("{0M}");
            }
            if (enable8.Checked)
            {
                comm8.WriteData("{0M}");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string easy_text = "";
            CheckBox[] enables = new CheckBox[] { enable1, enable2, enable3, enable4, enable5, enable6, enable7, enable8 };
            CommunicationManager[] comport = new CommunicationManager[] { comm1,comm2,comm3,comm4,comm5,comm6,comm7,comm8};
            TextBox[] comms = new TextBox[] { sencom1, sencom2, sencom3, sencom4, sencom5, sencom6, sencom7, sencom8 };
            TextBox[] measured = new TextBox[] { textBox9, textBox10, textBox11, textBox12, textBox13, textBox14, textBox15, textBox16};
            Label[] values = new Label[] {label12,label13,label14,label15,label16,label17,label18,label19,label20,label21,label22,label23,label24,label25,label26,label27,label28,label29,label30,label31,label32,label33,label34,label35};
            var j=0;
            for (var i=0;i<8;++i){
                if (enables[i].Checked){
                    comport[i].FirstOpenPort("115200","8","1","0",comms[i].Text);
                    //comport[i].WriteData("{0V}");
                    comport[i].CloseOpenPort();
                    port = new SerialPort( comms[i].Text, 115200, Parity.None, 8, StopBits.One);
                    port.Open();
                    port.DiscardInBuffer();
                    port.Write("{0V}");
                    var buffer = port.ReadTo("}") + "}";
                    int start_of_message = buffer.IndexOf('{');
                    var parsed_buffer = buffer.Substring(start_of_message);
                    // get p-code
                    values[j].Text = parsed_buffer.Substring(8, 4);
                    // get sw doc no
                    values[j+1].Text = parsed_buffer.Substring(12, 6);
                    // get sw version
                    values[j+2].Text = parsed_buffer.Substring(18, 6);
                    port.Close();
                    j=j+3;
                }
            }
        }
    }
}
