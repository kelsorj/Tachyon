using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.TechnosoftLibrary
{
    /// <summary>
    /// Interaction logic for CycleTester.xaml
    /// </summary>
    public partial class CycleTester
    {
        private ViewModel _vm { get; set; }

        public CycleTester()
        {
            InitializeComponent();
        }

        public void SetTechnosoftConnection( TechnosoftConnection ts)
        {
            if( ts == null)
                return;

            _vm = new ViewModel( ts);
            DataContext = _vm;
        }

        private void UserControl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.Delete)
                _vm.DeletePosition();
        }
    }

    public class PositionData
    {
        public int Index { get; set; }
        public double Position { get; set; }
    }

    public class ViewModel : ViewModelBase
    {    
        private Model _model { get; set; }

        public RelayCommand StartCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        public RelayCommand AddPositionCommand { get; set; }
        public RelayCommand DeletePositionCommand { get; set; }
        public bool ExecutePermutationsChecked { get; set; }
        public bool DelayAfterMoveChecked { get; set; }
        public bool PositionGraphingChecked { get; set; }
        public double DelaySeconds { get; set; }
        public string NewPosition { get; set; }
        public ObservableCollection<PositionData> Positions { get; set; }
        public int SelectedPositionIndex { get; set; }

        // for dealing with displaying of and interacting with axes
        public List<ListBoxItem> AxisIds { get; set; }
        private List<string> SelectedAxes { get; set; }
        private Dictionary<byte,IAxis> _axes { get; set; }

        public ViewModel( TechnosoftConnection ts)
        {
            _model = new Model( ts);

            StartCommand = new RelayCommand( Start);
            StopCommand = new RelayCommand( Stop);
            AddPositionCommand = new RelayCommand( AddPosition);
            DeletePositionCommand = new RelayCommand( DeletePosition);
            NewPosition = "1";
            Positions = new ObservableCollection<PositionData>();
            // add the axes
            AxisIds = new List<ListBoxItem>();
            SelectedAxes = new List<string>();
            _axes = ts.GetAxes();
            // GetAxes() will return null if the technosoft connection wasn't configured beforehand.
            // So please remember to call LoadConfiguration from the application / container.
            if( _axes != null) {
                foreach( KeyValuePair<byte,IAxis> kvp in _axes) {
                    ListBoxItem lbi = new ListBoxItem();
                    lbi.Content = kvp.Key;
                    AxisIds.Add( lbi);//kvp.Key);
                    lbi.Selected += lbi_Selected;
                    lbi.Unselected += lbi_Unselected;
                }
            }
        }

        void lbi_Unselected(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = sender as ListBoxItem;
            if( lbi == null)
                return;
            string axis_id = lbi.Content.ToString();
            SetAxisSelection( axis_id, false);
        }

        void lbi_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = sender as ListBoxItem;
            if( lbi == null)
                return;
            string axis_id = lbi.Content.ToString();
            SetAxisSelection( axis_id, true);
        }

        void SetAxisSelection( string axis_id, bool selected)
        {
            if( selected && !SelectedAxes.Contains( axis_id))
                SelectedAxes.Add( axis_id);
            else if( !selected)
                SelectedAxes.Remove( axis_id);
        }

        private void Start()
        {
            // print summary of cycle test behavior
            Console.WriteLine( String.Format( "permutation {0}enabled", (ExecutePermutationsChecked ? "" : "not ")));
            Console.WriteLine( String.Format( "axis position logging {0}enabled", (PositionGraphingChecked ? "" : "not ")));

            double delay_seconds;
            if( !DelayAfterMoveChecked) {
                Console.WriteLine( "do not delay after each move");
                delay_seconds = 0;
            } else {
                Console.WriteLine( String.Format( "{0}delay {1}s after each move", (DelayAfterMoveChecked ? "" : "do not "), DelaySeconds));
                delay_seconds = DelaySeconds;
            }

            if( SelectedAxes.Count > 0)
                Console.WriteLine( String.Format( "cycling axes: {0}", String.Join( ", ", SelectedAxes.ToArray())));
            else
                Console.WriteLine( "No axes selected for cycling");

            // start the thread that is going to cycle the axes
            // is there a nice .NETish way to copy only the positions from PositionData to the List?
            List<double> positions = new List<double>();
            foreach( PositionData pd in Positions)
                positions.Add( pd.Position);
            _model.Cycle( ExecutePermutationsChecked, delay_seconds, SelectedAxes, positions, PositionGraphingChecked);
        }

        private void Stop()
        {
            _model.Stop();
        }

        private void AddPosition()
        {
            double position = double.Parse( NewPosition);
            Positions.Add( new PositionData { Index=Positions.Count + 1, Position=position} );
        }

        public void DeletePosition()
        {
            if( SelectedPositionIndex == -1)
                return;

            // this is a temporary hack until I can figure out the right way to deal with this
            // make a copy of the list, remove the deleted item(s), then copy over the old list
            ObservableCollection<PositionData> copy = new ObservableCollection<PositionData>( Positions);
            copy.RemoveAt( SelectedPositionIndex);
            // redo the indexes in the list
            for( int i=0; i<copy.Count; i++) {
                copy[i].Index = i + 1;
            }
            Positions.Clear();
            foreach( PositionData pd in copy)
                Positions.Add( pd);
        }
    }

    public class Model
    {
        private TechnosoftConnection _ts { get; set; }
        
        // cycler threading stuff
        private Thread _cycle_thread { get; set; }
        private AutoResetEvent _stop_cycle_event { get; set; }

        // cycler parameters
        private class CyclerParameters
        {
            public bool Permute { get; set; }
            public double DelayBetweenCycles { get; set; }
            public List<IAxis> SelectedAxes { get; set; }
            public List<double> Positions { get; set; }
            public bool DataloggingEnabled { get; set; }
        }
        
        public Model( TechnosoftConnection ts)
        {
            _ts = ts;
            _stop_cycle_event = new AutoResetEvent( false);
        }

        public void Cycle( bool permutation_enabled, double delay_seconds, List<string> selected_axes, List<double> positions,
                           bool datalogging_enabled)
        {
            List<double> new_positions = AdjustPositionsForPermutations( permutation_enabled, positions);
            _cycle_thread = new Thread( CycleAxes);
            _cycle_thread.IsBackground = true;
            _cycle_thread.Name = "Cycle tester";
            // convert selected_axes into a List<IAxis>
            List<IAxis> axes = new List<IAxis>();
            foreach( KeyValuePair<byte,IAxis> kvp in _ts.GetAxes()) {
                byte axis_id = kvp.Key;
                IAxis axis = kvp.Value;
                if( selected_axes.Contains( axis_id.ToString()))
                    axes.Add( axis);
            }
            _cycle_thread.Start( new CyclerParameters { Permute = permutation_enabled, DelayBetweenCycles = delay_seconds,
                                                        SelectedAxes = axes, Positions = new_positions, DataloggingEnabled=datalogging_enabled });
        }

        private static List<double> AdjustPositionsForPermutations( bool permute, IList<double> original_positions)
        {
            List<double> new_positions;
            if( !permute)
                new_positions = new List<double>(original_positions);
            else {
                new_positions = new List<double>();
                // create permuted list -- use two indexes, and advance one all the way through positions
                // before advancing the second one.  When the second one has gone through all of the
                // positions, we're done.
                for( int i=0; i<original_positions.Count; i++) {
                    for( int j=0; j<original_positions.Count; j++) {
                        if( j == i)
                            continue;
                        new_positions.Add( original_positions[i]);
                        new_positions.Add( original_positions[j]);
                    }
                }
            }

            return new_positions;
        }

        public void CycleAxes( object data)
        {
            CyclerParameters cycler_parameters = data as CyclerParameters;
            if( cycler_parameters == null)
                return;
            
            bool local_stop_request = false;

            // create the filename for position logs
            string log_path = Utils.FileSystem.GetAppPath() + "\\logs";

            while( !_stop_cycle_event.WaitOne( 0)) {
                try
                {
                    // move through each position
                    foreach (double position in cycler_parameters.Positions)
                    {
                        // not sure if this is the right way to do it, but I would like to try to run all
                        // four axes simultaneously with datalogging.  It's possible that it won't work
                        // because we could run out of memory before the move starts.  ???  If so, then
                        // we will just have to wrap the moveabsolute calls with StartLogging and 
                        // WaitForLoggingComplete calls.
                        if (cycler_parameters.DataloggingEnabled)
                        {
                            foreach (IAxis axis in cycler_parameters.SelectedAxes)
                                axis.StartLogging();
                        }

                        // move each of the axes through their positions, non-blocking
                        foreach (IAxis axis in cycler_parameters.SelectedAxes)
                        {
                            // check for stop request each time
                            if (local_stop_request = _stop_cycle_event.WaitOne(0))
                            {
                                Console.WriteLine(String.Format("Stopping cycler before moving axis {0} to {1}mm", axis.GetID(), position));
                                break;
                            }
                            Console.WriteLine(String.Format("moving axis {0} to {1}mm", axis.GetID(), position));
                            axis.MoveAbsolute(position, wait_for_move_complete: false);
                            Thread.Sleep((int)(cycler_parameters.DelayBetweenCycles * 1000));
                        }
                        // move again, blocking
                        foreach (IAxis axis in cycler_parameters.SelectedAxes)
                            axis.MoveAbsolute(position);

                        // stop logging
                        if (cycler_parameters.DataloggingEnabled)
                        {
                            foreach (IAxis axis in cycler_parameters.SelectedAxes)
                            {
                                string filename = String.Format("{0}\\axis{1}_positions.log", log_path, axis.GetID());
                                axis.WaitForLoggingComplete(filename);
                            }
                        }

                        if (local_stop_request)
                            break;
                    }

                    if (local_stop_request)
                        break;
                } catch (Exception ex) {
                    // #322: didn't catch exceptions - if position is out of range an exception gets thrown
                    MessageBox.Show( ex.Message);
                    // kill the cycling
                    Stop();
                }
            }

            Console.WriteLine( "Cycler stopped");
        }

        public void Stop()
        {
            _stop_cycle_event.Set();
        }
    }
}
