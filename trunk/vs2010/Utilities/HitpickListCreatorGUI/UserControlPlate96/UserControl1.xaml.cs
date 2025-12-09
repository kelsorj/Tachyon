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
using System.Windows.Media.Animation;
using BioNex.Shared.LabwareDatabase;

namespace BioNex.HitpickListCreatorGUI.UserControlPlate96
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public Plate plate = new Plate();
        private LabwareDatabase _labware_database = new LabwareDatabase();

        public string PlateName
        { 
            get { return plate.PlateName; }
            set { plate.PlateName = value; }
        }

        public string Barcode {
            get { return plate.Barcode; }
            set { plate.Barcode = value; }
        }

        public string Labware {
            get { return plate.Labware; }
            set { plate.Labware = value; }
        }

        public UserControl1()
        {
            InitializeComponent();
            int num_rows = 8;
            int num_columns = 12;
            for( int i=0; i<num_rows; i++)
                stackpanel_main.Children.Add( CreateRow( i * num_columns, i * num_columns + num_columns - 1));
            PopulateLabwareDatabase();
            PopulateLabwareDroplist();
        }

        //! \todo this is lame, because each usercontrol hosts a labware database.  Need to pass it
        //!       labware information from an external source
        private void PopulateLabwareDatabase()
        {
            /*
            _labware_database.AddLabware( new Labware( "NUNC 96 clear round well flat bottom", 96, 14.47, 11.25));
            _labware_database.AddLabware( new Labware( "Griener 96 clear round well flat bottom", 96, 14.4, 11.17));
            _labware_database.AddLabware( new Labware( "NUNC 96 clear round well conical bottom", 96, 14.4, 9.46));
             */
        }

        private void PopulateLabwareDroplist()
        {
            List<string> labware = _labware_database.GetLabwareNames();
            foreach( string s in labware)
                combo_labware.Items.Add( s);
            combo_labware.SelectedIndex = 0;
        }

        private StackPanel CreateRow( int start_index, int end_index)
        {
            StackPanel p = new StackPanel();
            p.Orientation = Orientation.Horizontal;
            for( int i=start_index; i<=end_index; i++) {
                double diameter = 18;
                Well well = new Well( i, diameter, plate);
                well.ClearClick += new RoutedEventHandler(well_ClearClick);
                p.Children.Add( well);
            }
            return p;
        }

        public static readonly RoutedEvent WellClearClickEvent =
            EventManager.RegisterRoutedEvent( "WellClearClick", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( Well));

        public event RoutedEventHandler WellClearClick
        {
            add { AddHandler( WellClearClickEvent, value); }
            remove { RemoveHandler( WellClearClickEvent, value); }
        }

        void well_ClearClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent( new RoutedEventArgs( WellClearClickEvent));
        }

        private void button_apply_click( object sender, RoutedEventArgs e)
        {
            plate.Barcode = text_barcode.Text;
        }

        public static readonly RoutedEvent DeleteButtonClickEvent =
            EventManager.RegisterRoutedEvent( "DeleteButtonClick", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( UserControl1));

        public event RoutedEventHandler DeleteButtonClick
        {
            add { AddHandler( DeleteButtonClickEvent, value); }
            remove { RemoveHandler( DeleteButtonClickEvent, value); }
        }

        private void button_delete_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent( new RoutedEventArgs( DeleteButtonClickEvent));
        }

        public static readonly RoutedEvent ColorChangedEvent =
            EventManager.RegisterRoutedEvent( "ColorChanged", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( UserControl1));

        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler( ColorChangedEvent, value); }
            remove { RemoveHandler( ColorChangedEvent, value); }
        }

        private void UserControl1_ColorChanged(object sender, RoutedEventArgs e)
        {
            SolidColorBrush brush = (SolidColorBrush)e.OriginalSource;
            plate.Color = brush;
            RaiseEvent( new RoutedEventArgs( ColorChangedEvent));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            colorpicker.SetColorRGBString( "11CC11");
        }

        private void combo_labware_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Labware = ((ComboBox)sender).SelectedValue.ToString();
        }
    }

    public class WellProperties
    {
        public Plate MyPlate { get; set; }
        public int Index { get; set; }

        public WellProperties()
        {

        }

        public WellProperties( int index, Plate plate)
        {
            Index = index;
            MyPlate = plate;
        }
    }

    public class Well : FrameworkElement
    {
        public WellProperties Properties { get; set; }
        private double Diameter { get; set; }
        private Brush MyBrush { get; set; }
        private Pen MyPen { get; set; }
        private string MyText { get; set; }
        private bool Marked { get; set; }
        private Well Mate { get; set; }

        /*
        public Well()
        {
            Properties = new WellProperties();
            MyBrush = Brushes.White;
            MyPen = new Pen( Brushes.Black, 1);
            MyText = "";
            Height = Width = Diameter;
            Marked = false;
            Mate = null;
        }
        */

        public Well( int index, double diameter, Plate plate)
        {
            Diameter = diameter;
            Properties = new WellProperties( index, plate);
            MyBrush = Brushes.White;
            MyPen = new Pen( Brushes.Black, 1);
            MyText = "";
            Height = Width = Diameter;
            Marked = false;
            Mate = null;
            // set up context menu
            ContextMenu menu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "Clear";
            item.Click += new RoutedEventHandler(item_ClearClick);
            menu.Items.Add( item);
            this.ContextMenu = menu;
        }

        public static readonly RoutedEvent ClearClickEvent =
            EventManager.RegisterRoutedEvent( "ClearClick", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( Well));

        public event RoutedEventHandler ClearClick
        {
            add { AddHandler( ClearClickEvent, value); }
            remove { RemoveHandler( ClearClickEvent, value); }
        }

        void item_ClearClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent( new RoutedEventArgs( ClearClickEvent));
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if( !Marked) {
                MyBrush = Brushes.Yellow;
                MyPen = new Pen( Brushes.Black, 2);
            }
            base.OnMouseEnter(e);
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if( !Marked) {
                MyBrush = Brushes.White;
                MyPen = new Pen( Brushes.Black, 1);
            }
            base.OnMouseLeave(e);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Console.WriteLine( MyBrush.ToString());
            drawingContext.DrawEllipse( MyBrush, MyPen, new Point( 9, 9), 9, 9);
            drawingContext.DrawText( new FormattedText( MyText, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface( "Arial"), 12.0, Brushes.Black), new Point( 2.5, 2.5));
            base.OnRender( drawingContext);
        }

        public void Mark( Plate plate_to_use_fill_from, string text, ref Well mate)
        {
            MyBrush = plate_to_use_fill_from.Color;
            MyText = text;
            Marked = true;
            Mate = mate;
            InvalidateVisual();
        }

        public void Unmark()
        {
            MyBrush = Brushes.White;
            MyText = "";
            Marked = false;
            Mate = null;
            MyPen = new Pen( Brushes.Black, 1);
            InvalidateVisual();
        }

        public void Animate()
        {
            // transparency animation
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = 1;
            anim.To = .2;
            anim.AutoReverse = true;
            anim.Duration = new Duration( new TimeSpan( 0, 0, 0, 0, 250));
            anim.RepeatBehavior = new RepeatBehavior( 3);
            // need a storyboard
            Storyboard sb = new Storyboard();
            sb.Children.Add( anim);
            Storyboard.SetTargetProperty( anim, new PropertyPath( OpacityProperty));
            sb.Begin( this);
        }
    }

    public class Plate
    {
        public string PlateName { get; set; }
        public string Barcode { get; set; }
        public string Labware { get; set; }
        public Brush Color { get; set; }

        public Plate()
        {
            Barcode = "";
            PlateName = "";
            Labware = "";
            Color = Brushes.Gray;
        }
    }
}
