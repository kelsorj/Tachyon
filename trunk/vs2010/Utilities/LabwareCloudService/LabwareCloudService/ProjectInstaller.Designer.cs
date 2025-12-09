namespace LabwareCloudService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labwareCloudProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.labwareCloudServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // labwareCloudProcessInstaller
            // 
            this.labwareCloudProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.labwareCloudProcessInstaller.Password = null;
            this.labwareCloudProcessInstaller.Username = null;
            // 
            // labwareCloudServiceInstaller
            // 
            this.labwareCloudServiceInstaller.Description = "BioNex Labware Stand-Alone Cloud Service";
            this.labwareCloudServiceInstaller.ServiceName = "BioNexLabwareService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.labwareCloudProcessInstaller,
            this.labwareCloudServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller labwareCloudProcessInstaller;
        private System.ServiceProcess.ServiceInstaller labwareCloudServiceInstaller;
    }
}