using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for Login.
	/// </summary>
	public class Login : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button loginButton;
		private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxPassword;
		public System.Windows.Forms.ComboBox comboBoxUsername;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private mainForm m_mf = null;

		public Login()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.loginButton = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.comboBoxUsername = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPassword.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPassword.Location = new System.Drawing.Point(104, 86);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new System.Drawing.Size(168, 21);
            this.textBoxPassword.TabIndex = 2;
            this.textBoxPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxPassword_KeyPress);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(24, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 16);
            this.label2.TabIndex = 1;
            this.label2.Text = "Username";
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(24, 86);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "Password";
            // 
            // loginButton
            // 
            this.loginButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loginButton.ForeColor = System.Drawing.Color.White;
            this.loginButton.Location = new System.Drawing.Point(200, 118);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(72, 23);
            this.loginButton.TabIndex = 3;
            this.loginButton.Text = "&Login";
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(16, 16);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(264, 40);
            this.label10.TabIndex = 6;
            this.label10.Text = "Login";
            // 
            // comboBoxUsername
            // 
            this.comboBoxUsername.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxUsername.DropDownWidth = 72;
            this.comboBoxUsername.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxUsername.Location = new System.Drawing.Point(104, 60);
            this.comboBoxUsername.Name = "comboBoxUsername";
            this.comboBoxUsername.Size = new System.Drawing.Size(168, 21);
            this.comboBoxUsername.TabIndex = 8;
            // 
            // Login
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(292, 189);
            this.Controls.Add(this.comboBoxUsername);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.loginButton);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Login";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.Load += new System.EventHandler(this.Login_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void loginButton_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void textBoxUsername_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (13 == e.KeyChar)
			{
				loginButton_Click(sender, e);
			}
		}

		private void textBoxPassword_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (13 == e.KeyChar)
			{
				loginButton_Click(sender, e);
			}
		}

		public DialogResult _ShowDialog(CUser user, mainForm mf)
		{
			m_mf = mf;

			ShowDialog();
			user.Password = textBoxPassword.Text;
			user.Username = comboBoxUsername.Text;
			return DialogResult;
		}

		private void Login_Load(object sender, System.EventArgs e)
		{
			m_mf.m_xmlData.LoadUsersForLogin(this);

			comboBoxUsername.SelectedIndex = 0;
		}
	}
}
