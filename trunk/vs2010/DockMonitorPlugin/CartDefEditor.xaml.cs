using System;
using System.Windows;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using log4net;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.BioNexGuiControls;

namespace BioNex.Plugins.Dock
{
    /// <summary>
    /// Interaction logic for DockGUI.xaml
    /// </summary>
    [Export(typeof(ISystemSetupEditor))]
    public partial class CartDefEditor : Window, INotifyPropertyChanged, ISystemSetupEditor
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DockGUI));

        CartDefinitionFile _cart_def;
        bool _backed_up;
        bool _internal_change;
        string _human_readable;
        string _file_prefix;
        string _num_racks;
        string _num_slots;

        public ObservableCollection<string> CartBarcodes { get; private set; }
        public string HumanReadableText { get { return _human_readable; } set { _human_readable = value; UpdateFile(); OnPropertyChanged("HumanReadableText"); } }
        public string FilePrefixText { get { return _file_prefix; } set { _file_prefix = value; UpdateFile(); OnPropertyChanged("FilePrefixText"); } }
        public string NumberOfRacksText { get { return _num_racks; } set { _num_racks = value; UpdateFile(); OnPropertyChanged("NumberOfRacksText"); } }
        public string NumberOfSlotsText { get { return _num_slots; } set { _num_slots = value; UpdateFile(); OnPropertyChanged("NumberOfSlotsText"); } }

        public RelayCommand AddCartDefCommand { get; set; }
        public RelayCommand RemoveCartDefCommand { get; set; }

        public CartDefEditor()
        {
            InitializeComponent();
            DataContext = this;

            AddCartDefCommand = new RelayCommand(AddCartDef);
            RemoveCartDefCommand = new RelayCommand(RemoveCartDef, () => CartBarcodeListBox.Items.Count > 0);

            _internal_change = false;
            _backed_up = false;
            _cart_def = new CartDefinitionFile();
            CartBarcodes = new ObservableCollection<string>(_cart_def.GetCartBarcodes());
            if (CartBarcodes.Count > 0)
                CartBarcodeListBox.SelectedIndex = 0;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged == null)
                return;
            PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion

        private void CartBarcodeListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _internal_change = true;

            // update the Cart Details section to reflect the new selection
            try
            {
                if (CartBarcodeListBox.SelectedIndex >= 0)
                {
                    var barcode = CartBarcodeListBox.SelectedValue.ToString();
                    string hr;
                    string fp;
                    int nr;
                    int ns;
                    _cart_def.GetCartIdentifiersFromBarcode(barcode, out hr, out fp);
                    _cart_def.GetNumberOfRacksAndSlotsFromBarcode(barcode, out nr, out ns);
                    HumanReadableText = hr;
                    FilePrefixText = fp;
                    NumberOfRacksText = nr.ToString();
                    NumberOfSlotsText = ns.ToString();
                    return;
                }
            }
            catch (Exception)
            { }
            finally
            {
                _internal_change = false;
            }

            string error_text = "Error or no barcode selected";
            HumanReadableText = error_text;
            FilePrefixText = error_text;
            NumberOfRacksText = error_text;
            NumberOfSlotsText = error_text;

            _internal_change = false;
        }

        // backup if it hasn't been backed up during this editor session
        private void Backup()
        {
            if (_backed_up)
                return;

            _cart_def.MakeBackup();
            _backed_up = true;      // no further backup until user reopens the editor
        }

        private void AddCartDef()
        {
            // prompt for barcode
            string barcode;
            var prompt = new UserInputDialog("Add Cart Definition", "Please enter the new cart barcode:");
            var response = prompt.PromptUser(out barcode);
            if (response == MessageBoxResult.Cancel)
                return;

            // backup exisiting cart definition file if necessary
            Backup();

            // add a new cart definition to file
            bool ok = _cart_def.AddCartDefinition(barcode);
            if (!ok)
                return;

            // TODO -- create teachpoint file for cart ?

            // add new barcode to list box, select it
            CartBarcodes.Add(barcode);
            CartBarcodeListBox.SelectedItem = barcode;
        }

        private void RemoveCartDef()
        {
            var barcode = CartBarcodeListBox.SelectedValue.ToString();

            // prompt for confirmation
            var response = MessageBox.Show(string.Format("Are you sure you want to remove the definition for cart '{0}'?", barcode), "Remove Cart Definition", MessageBoxButton.OKCancel);
            if (response == MessageBoxResult.Cancel)
                return;

            // backup exisiting cart definition file if necessary
            Backup();

            // remove cart definition from file
            _cart_def.RemoveCartDefinition(barcode);

            // remove item from listbox
            var selected_index = Math.Max(CartBarcodeListBox.SelectedIndex - 1, 0);
            CartBarcodes.Remove(barcode);

            // select item above removed item
            if (CartBarcodes.Count == 0)
                return;
            CartBarcodeListBox.SelectedIndex = selected_index;
        }

        private void UpdateFile()
        {
            if (_internal_change)
                return;

            var barcode = CartBarcodeListBox.SelectedValue.ToString();
            if (string.IsNullOrWhiteSpace(HumanReadableText))
                return;
            if (string.IsNullOrWhiteSpace(FilePrefixText))
                return;
            if (string.IsNullOrWhiteSpace(NumberOfRacksText))
                return;
            if (string.IsNullOrWhiteSpace(NumberOfSlotsText))
                return;

            int nr = 0;
            int ns = 0;

            try
            {
                nr = int.Parse(NumberOfRacksText);
                ns = int.Parse(NumberOfSlotsText);
            }
            catch (Exception)
            {
                return;
            }
            if (nr <= 0 || ns <= 0)
                return;

            // backup exisiting cart definition file if necessary
            Backup();

            // update cart definition
            _cart_def.UpdateCartDefinition(barcode, HumanReadableText, FilePrefixText, nr, ns);
        }

        #region ISystemSetupEditor
        public string Name
        {
            get
            {
                return "Cart Definition Manager";
            }
        }

        public void ShowTool()
        {
            var editor = new CartDefEditor();
            editor.ShowDialog();
            editor.Close();
        }
        #endregion

    }
}
