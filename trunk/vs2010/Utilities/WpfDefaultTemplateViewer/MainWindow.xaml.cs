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
using System.ComponentModel;
using System.Xml;

namespace WpfDefaultTemplateViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ICollectionView WpfElements { get; set; }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            PopulateWpfElements();
        }

        private void PopulateWpfElements()
        {
            WpfElements = CollectionViewSource.GetDefaultView( new List<string> { "ComboBox" });
            WpfElements.CurrentChanged += new EventHandler(WpfElements_CurrentChanged);
            OnPropertyChanged( "WpfElements");
        }

        void WpfElements_CurrentChanged(object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if( view == null || view.CurrentItem == null)
                return;

            string element = view.CurrentItem as String;
            switch(element) {
                case "ComboBox":
                    LoadDefaultElementStyle( typeof(ComboBox));
                    return;
            }
        }

        void LoadDefaultElementStyle( Type t)
        {
            var resource = Application.Current.FindResource( t);
            string filename = System.IO.Path.GetTempFileName();
            using( XmlTextWriter writer = new XmlTextWriter( filename, System.Text.Encoding.UTF8)) {
                writer.Formatting = Formatting.Indented;
                System.Windows.Markup.XamlWriter.Save( resource, writer);
            }
                        
            XmlDocument doc = new XmlDocument();
            doc.Load( filename);
            _viewer.xmlDocument = doc;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
