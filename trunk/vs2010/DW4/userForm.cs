using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace AQ3
{
	/// <summary>
	/// Summary description for userForm.
	/// </summary>
	public class userForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.TextBox textBoxUserName;
		private System.Windows.Forms.PictureBox pictureBox1;
		public System.Windows.Forms.ComboBox comboBoxUserLevel;
		private System.Windows.Forms.Label labelUser;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// helpers
		private bool m_bCreateNew = false;
		public System.Windows.Forms.TextBox textBoxPassword;
		public System.Windows.Forms.TextBox textBoxRetypePassword;
		private System.Windows.Forms.Button saveButton;
		private System.Windows.Forms.Button cancelButton;
		private string m_strUserName;

		public userForm(string strUserName, bool bCreateNew)
		{
			InitializeComponent();

			string strLabel = "User: " + strUserName;
			Text = strLabel;
			labelUser.Text = strLabel;

			m_bCreateNew = bCreateNew;
			m_strUserName = strUserName;
			textBoxUserName.Text = strUserName;

			if (bCreateNew)
			{
			}
			else
			{
				// load data form xml file and populate
				// must be done in userForm_Load()
			}
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(userForm));
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.textBoxRetypePassword = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxUserName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxUserLevel = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.labelUser = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPassword.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPassword.Location = new System.Drawing.Point(152, 96);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new System.Drawing.Size(160, 21);
            this.textBoxPassword.TabIndex = 3;
            this.textBoxPassword.TextChanged += new System.EventHandler(this.textBoxPassword_TextChanged);
            this.textBoxPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxPassword_KeyDown);
            this.textBoxPassword.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPassword_Validating);
            // 
            // textBoxRetypePassword
            // 
            this.textBoxRetypePassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxRetypePassword.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRetypePassword.Location = new System.Drawing.Point(152, 120);
            this.textBoxRetypePassword.Name = "textBoxRetypePassword";
            this.textBoxRetypePassword.PasswordChar = '*';
            this.textBoxRetypePassword.Size = new System.Drawing.Size(160, 21);
            this.textBoxRetypePassword.TabIndex = 4;
            this.textBoxRetypePassword.TextChanged += new System.EventHandler(this.textBoxRetypePassword_TextChanged);
            this.textBoxRetypePassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxRetypePassword_KeyDown);
            this.textBoxRetypePassword.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxRetypePassword_Validating);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(24, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Username:";
            // 
            // textBoxUserName
            // 
            this.textBoxUserName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxUserName.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxUserName.Location = new System.Drawing.Point(152, 72);
            this.textBoxUserName.Name = "textBoxUserName";
            this.textBoxUserName.Size = new System.Drawing.Size(160, 21);
            this.textBoxUserName.TabIndex = 2;
            this.textBoxUserName.TextChanged += new System.EventHandler(this.textBoxUserName_TextChanged);
            this.textBoxUserName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxUserName_KeyDown);
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(24, 120);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Retype Password:";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(24, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 16);
            this.label2.TabIndex = 0;
            this.label2.Text = "Password:";
            // 
            // comboBoxUserLevel
            // 
            this.comboBoxUserLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxUserLevel.DropDownWidth = 121;
            this.comboBoxUserLevel.Items.AddRange(new object[] {
            "User (Level 1)",
            "Supervisor (Level 2)",
            "Administrator (Level 3)"});
            this.comboBoxUserLevel.Location = new System.Drawing.Point(152, 160);
            this.comboBoxUserLevel.Name = "comboBoxUserLevel";
            this.comboBoxUserLevel.Size = new System.Drawing.Size(160, 21);
            this.comboBoxUserLevel.TabIndex = 5;
            this.comboBoxUserLevel.SelectedValueChanged += new System.EventHandler(this.comboBoxUserLevel_SelectedValueChanged);
            this.comboBoxUserLevel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.comboBoxUserLevel_KeyDown);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(24, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 16);
            this.label4.TabIndex = 2;
            this.label4.Text = "User Level:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.Location = new System.Drawing.Point(376, 184);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(176, 152);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // labelUser
            // 
            this.labelUser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelUser.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUser.ForeColor = System.Drawing.Color.White;
            this.labelUser.Location = new System.Drawing.Point(16, 16);
            this.labelUser.Name = "labelUser";
            this.labelUser.Size = new System.Drawing.Size(560, 24);
            this.labelUser.TabIndex = 1;
            this.labelUser.Text = "User:";
            // 
            // saveButton
            // 
            this.saveButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(16, 344);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(72, 23);
            this.saveButton.TabIndex = 22;
            this.saveButton.Text = "&Save";
            this.saveButton.Visible = false;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.ForeColor = System.Drawing.Color.White;
            this.cancelButton.Location = new System.Drawing.Point(496, 344);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 23);
            this.cancelButton.TabIndex = 23;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // userForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(157)))), ((int)(((byte)(227)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(584, 382);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.labelUser);
            this.Controls.Add(this.comboBoxUserLevel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxRetypePassword);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxUserName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "userForm";
            this.Text = "User";
            this.Load += new System.EventHandler(this.userForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.userForm_Closing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private bool bValidate()
		{
			bool bRetValue = false;

			if (textBoxUserName.Text.Length < 1)
			{
				MessageBox.Show("Username must be present...", "Username", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			if (textBoxUserName.Text == "____BNX1536_")
			{
				MessageBox.Show("Username not valid...", "Username", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else if (textBoxPassword.Text.Length < 1)
			{
				MessageBox.Show("Password must be present...", "Password", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else if (textBoxPassword.Text != textBoxRetypePassword.Text)
			{
				MessageBox.Show("The passwords you typed do not match.\nPlease retype the new password in both boxes.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBoxPassword.Text = "";
				textBoxRetypePassword.Text = "";
			}
			else
			{
				bRetValue = true;
			}

			return bRetValue;
		}

		public void Save()
		{
			if (!bValidate())
			{
				return;
			}

			mainForm mf = (mainForm)this.MdiParent;

			if (mf.m_User.UserLevel < 3)
			{
				MessageBox.Show(this, "Only administrators can change user data.\nNew user data NOT saved...", "Save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			if( mf.m_xmlData.UserExist( textBoxUserName.Text ) )
			{
				UserExist ue = new UserExist();
				ue.ShowDialog( this );
				DialogResult res = (DialogResult)ue.Tag;
				ue.Dispose();

				//DialogResult res = MessageBox.Show(this, "Do you want to overwrite(Yes) or update(No)\r\nthe profile" + textBoxUserName.Text + "?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
				if( res == DialogResult.Cancel )
				{
					return;
				}
				else if( res == DialogResult.Yes )
				{
					mf.m_xmlData.DeleteUser(textBoxUserName.Text, mf.treeView, mf);
				}
			}


//			mf.m_xmlData.DeleteUser(textBoxUserName.Text, mf.treeView, mf);
			m_strUserName = textBoxUserName.Text;
			mf.m_xmlData.SaveUser(this, mf.treeView);

			Text = "User: " + textBoxUserName.Text;
			labelUser.Text = Text;
			Tag = false;
			saveButton.Hide();
		}

		private void userForm_Load(object sender, System.EventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;

			if (!m_bCreateNew)
			{
				mf.m_xmlData.LoadUser(m_strUserName, this);
			}
			else
			{
				comboBoxUserLevel.SelectedIndex = 0;
			}

			if( comboBoxUserLevel.SelectedIndex == 2 && m_strUserName == mf.m_User.Username )
			{
				//don't let an admin demote itself
				comboBoxUserLevel.Enabled = false;
			}
			else
			{
				comboBoxUserLevel.Enabled = true;
			}

			Text = "User: " + textBoxUserName.Text;
			labelUser.Text = Text;
			Tag = false;
			saveButton.Hide();

			if (mf.m_User.Username == textBoxUserName.Text)
			{
				textBoxUserName.ReadOnly = true;
			}

			labelUser.Select();
		}

		private void textBoxUserName_TextChanged(object sender, System.EventArgs e)
		{
			Tag = true;
			if (!Text.EndsWith("*"))
			{
				Text += "*";
			}
			labelUser.Text = Text;
			saveButton.Show();
		}

		private void textBoxPassword_TextChanged(object sender, System.EventArgs e)
		{
			textBoxUserName_TextChanged(sender, e);
		}

		private void textBoxRetypePassword_TextChanged(object sender, System.EventArgs e)
		{
			textBoxUserName_TextChanged(sender, e);
		}

		private void comboBoxUserLevel_SelectedValueChanged(object sender, System.EventArgs e)
		{
			textBoxUserName_TextChanged(sender, e);
		}

		private void textBoxPassword_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			
		}

		private void textBoxRetypePassword_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			textBoxPassword_Validating(sender, e);
		}

		private void userForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mainForm mf = (mainForm)this.MdiParent;
			if (mf.m_User.UserLevel < 3)
			{
				return;
			}

			if ((bool)Tag)
			{
				DialogResult DR = MessageBox.Show("Save user " + "\"" + textBoxUserName.Text + "\"" + "?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

				if (DR == DialogResult.Yes)
				{
					if (bValidate())
					{
						Save();
					}
					else
					{
						e.Cancel = true;
					}
				}
				else if (DR == DialogResult.Cancel)
				{
					e.Cancel = true;
				}
			}
		}

		private void textBoxUserName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxPassword.Focus();
			}
		}

		private void textBoxPassword_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxRetypePassword.Focus();
			}
		}

		private void textBoxRetypePassword_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				comboBoxUserLevel.Focus();
			}
		}

		private void comboBoxUserLevel_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter)
			{
				textBoxUserName.Focus();
			}
		}

		private void saveButton_Click(object sender, System.EventArgs e)
		{
			Save();
			this.Close();
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}
