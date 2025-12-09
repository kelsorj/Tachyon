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
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;

namespace BioNex.Shared.BioNexGuiControls
{
    /// <summary>
    /// Interaction logic for UserInputDialog.xaml
    /// </summary>
    public partial class UserInputDialog : Window, INotifyPropertyChanged
    {
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        
        private string _prompt_text;
        public string PromptText
        {
            get { return _prompt_text; }
            set {
                _prompt_text = value;
                OnPropertyChanged( "PromptText");
            }
        }

        private string _user_text;
        public string UserText
        {
            get { return _user_text; }
            set {
                _user_text = value;
                OnPropertyChanged( "UserText");
            }
        }

        public bool Cancelled { get; private set; }

        public UserInputDialog( string caption, string prompt)
        {
            InitializeComponent();
            DataContext = this;

            Title = caption;
            PromptText = prompt;

            OkCommand = new RelayCommand( () => { Cancelled = false; Close(); });
            CancelCommand = new RelayCommand( () => { Cancelled = true; Close(); });
        }

        public MessageBoxResult PromptUser( out string user_text)
        {
            user_text = "";
            // I am not using the return value, and am instead just querying the window
            // to see what the user ended up clicking
            ShowDialog();

            if( Cancelled)
                return MessageBoxResult.Cancel;
            else {
                user_text = UserText;
                return MessageBoxResult.OK;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }
    }
}
