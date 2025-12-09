using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BioNex.Shared.SimpleWizard;

namespace SimpleWizardTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<Wizard.WizardStep> steps = CreateTestSteps();
            Wizard wiz = new Wizard( "Simple Wizard", steps);
            wiz.ShowDialog();
        }

        private List<Wizard.WizardStep> CreateTestSteps()
        {
            List<Wizard.WizardStep> steps = new List<Wizard.WizardStep>();
            steps.Add( new Wizard.WizardStep( "Step 1", StepFunc, false));
            steps.Add( new Wizard.WizardStep( "Step 2", StepFunc, true));
            steps.Add( new Wizard.WizardStep( "Step 3", StepFunc, false));
            steps.Add( new Wizard.WizardStep( "Step 4", StepFunc, true));
            steps.Add( new Wizard.WizardStep( "Step 5", StepFunc, false));
            steps.Add( new Wizard.WizardStep( "Step 6", StepFunc, true));
            steps.Add( new Wizard.WizardStep( "Done!  You may close this window.", null, false));
            return steps;
        }

        private bool StepFunc()
        {
            return true;
        }
    }
}
