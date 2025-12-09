using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.LiquidProfileLibrary
{
    public class BooleanConverter<T> : IValueConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public virtual object Convert( object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool) value) ? True : False;
        }

        public virtual object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T) value, True);
        }
    }

    public sealed class BooleanToVisibilityConverter : BooleanConverter< Visibility>
    {
        public BooleanToVisibilityConverter() :
            base( Visibility.Visible, Visibility.Collapsed)
        {
        }
    }

    /// <summary>
    /// Interaction logic for LiquidProfileEditor.xaml
    /// </summary>
    public partial class LiquidProfileEditor : Window, INotifyPropertyChanged
    {

        public ObservableCollection< LiquidProfile> LiquidProfiles { get; set; }
        public ObservableCollection< string> MixLiquidProfiles { get; set; }
        public LiquidProfile SelectedLiquidProfile { get; set; }

        public RelayCommand NewLiquidProfileCommand { get; set; }
        public RelayCommand SaveLiquidProfileCommand { get; set; }
        public RelayCommand SaveAsLiquidProfileCommand { get; set; }
        public RelayCommand DeleteLiquidProfileCommand { get; set; }

        public LiquidProfileEditor( LiquidProfileLibrary liquid_profile_library)
        {
            liquid_profile_library_ = liquid_profile_library;

            LiquidProfiles = new ObservableCollection< LiquidProfile>( liquid_profile_library_.LoadLiquidProfiles());
            MixLiquidProfiles = new ObservableCollection< string>();
            MixLiquidProfiles.Add( "");
            foreach( LiquidProfile lp in liquid_profile_library_.LoadLiquidProfiles()){
                if( lp.IsMixingProfile){
                    MixLiquidProfiles.Add( lp.Name);
                }
            }

            InitializeComponent();

            DataContext = this;

            NewLiquidProfileCommand = new RelayCommand( NewLiquidProfile);
            SaveLiquidProfileCommand = new RelayCommand( SaveLiquidProfile);
            SaveAsLiquidProfileCommand = new RelayCommand( SaveAsLiquidProfile);
            DeleteLiquidProfileCommand = new RelayCommand( DeleteLiquidProfile);
        }

        public void NewLiquidProfile()
        {
            NewObjectNameWindow new_name_window = new NewObjectNameWindow( "New Liquid Profile", "Please enter a name for the new liquid profile:", "unnamed liquid");
            new_name_window.ShowDialog();

            string new_name = new_name_window.NewName;

            if( new_name == null){
                return;
            }

            List< string> profile_names = liquid_profile_library_.EnumerateLiquidProfileNames();
            if( profile_names.Contains( new_name)){
                MessageBox.Show( "Sorry, liquid profile '" + new_name + "' already exists.");
                return;
            }

            LiquidProfile new_liquid_profile = new LiquidProfile( new_name_window.NameString);
            liquid_profile_library_.SaveLiquidProfileByName( new_liquid_profile);
            LiquidProfiles.Add( new_liquid_profile);
            if( new_liquid_profile.IsMixingProfile){
                MixLiquidProfiles.Add( new_liquid_profile.Name);
            }
        }

        public void SaveLiquidProfile()
        {
            liquid_profile_library_.SaveLiquidProfileByName( SelectedLiquidProfile);
            if( SelectedLiquidProfile.IsMixingProfile){
                if( ( from mlp_name in MixLiquidProfiles where mlp_name == SelectedLiquidProfile.Name select mlp_name).Count() == 0){
                    MixLiquidProfiles.Add( SelectedLiquidProfile.Name);
                }
            } else{
                if( ( from mlp_name in MixLiquidProfiles where mlp_name == SelectedLiquidProfile.Name select mlp_name).Count() != 0){
                    MixLiquidProfiles.Remove( SelectedLiquidProfile.Name);
                }
            }
        }

        public void SaveAsLiquidProfile()
        {
            NewObjectNameWindow new_name_window = new NewObjectNameWindow( "Save Liquid Profile As", "Please enter another name for this liquid profile:", "");
            new_name_window.ShowDialog();

            string new_name = new_name_window.NewName;

            if( new_name == null){
                return;
            }

            List< string> profile_names = liquid_profile_library_.EnumerateLiquidProfileNames();
            if( profile_names.Contains( new_name)){
                MessageBox.Show( "Sorry, '" + new_name + "' is the name of an existing liquid profile.");
                return;
            }

            LiquidProfile new_liquid_profile = SelectedLiquidProfile;
            new_liquid_profile.Name = new_name;
            liquid_profile_library_.SaveLiquidProfileByName( new_liquid_profile);
            LiquidProfiles.Add( new_liquid_profile);
            if( new_liquid_profile.IsMixingProfile){
                MixLiquidProfiles.Add( new_liquid_profile.Name);
            }
        }

        public void DeleteLiquidProfile()
        {
            string liquid_profile_name = SelectedLiquidProfile.Name;
            if( MessageBox.Show( this, "Are you sure you want to delete liquid profile '" + liquid_profile_name + "'?", "Delete Liquid Profile", MessageBoxButton.YesNo) == MessageBoxResult.Yes){
                liquid_profile_library_.DeleteLiquidProfileByName( liquid_profile_name);
                LiquidProfiles.Remove( SelectedLiquidProfile);
                MixLiquidProfiles.Remove( liquid_profile_name);
            }
        }

        protected LiquidProfileLibrary liquid_profile_library_;

        #region INotifyPropertyChanged Members
#warning FELIX -- maybe this doesn't need to inherit from INotifyPropertyChanged?
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67
        #endregion
    }
}
