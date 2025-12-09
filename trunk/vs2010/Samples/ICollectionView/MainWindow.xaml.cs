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
using System.Collections.ObjectModel;

namespace ICollectionViewSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// you cannot make this member private because that would prevent databinding from working
        /// </summary>
        public ICollectionView ComboBoxView1 { get; set; }
        /// <summary>
        /// stuff does not need to be a member for the CollectionChanged event to get fired when GetDefaultView is called.
        /// but it could be public or private, field or property if you want to do it this way.
        /// </summary>
        //private List<MyClass> stuff;// { get; set; }

        public ICollectionView ComboBoxView3 { get; set; }
        //private List<MyClass> stuff2;
        //private List<MyClass> stuff4;

        public ICollectionView ComboBoxView2 { get; set; }
        //private List<MyInterface> stuff3;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            List<MyClass> stuff = new List<MyClass> { new MyClass { key="a", value="b" }, new MyClass { key="c", value="d" } };
            ComboBoxView1 = CollectionViewSource.GetDefaultView( stuff);
            stuff.Add( new MyClass { key="e", value="f" });
            ComboBoxView1.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComboBoxView1_CollectionChanged);
            ComboBoxView1.CurrentChanged += new EventHandler(ComboBoxView1_CurrentChanged);
            ComboBoxView1.CurrentChanging += new CurrentChangingEventHandler(ComboBoxView1_CurrentChanging);

            List<MyInterface> stuff3 = new List<MyInterface> { new MyInterfaceImpl("test1"), new MyInterfaceImpl("test2") };
            ComboBoxView2 = CollectionViewSource.GetDefaultView( stuff3);
            ComboBoxView2.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComboBoxView2_CollectionChanged);
            ComboBoxView2.CurrentChanged += new EventHandler(ComboBoxView2_CurrentChanged);
            ComboBoxView2.CurrentChanging += new CurrentChangingEventHandler(ComboBoxView2_CurrentChanging);

            List<MyClass> stuff2 = new List<MyClass> { new MyClass { key="a", value="b" }, new MyClass { key="c", value="d" } };
            ComboBoxView3 = CollectionViewSource.GetDefaultView( stuff2);
            stuff2.Add( new MyClass { key="e", value="f" });
            ComboBoxView3.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComboBoxView3_CollectionChanged);
            ComboBoxView3.CurrentChanged += new EventHandler(ComboBoxView3_CurrentChanged);
            ComboBoxView3.CurrentChanging += new CurrentChangingEventHandler(ComboBoxView3_CurrentChanging);
            // changing what the ICollectionView's source points to doesn't affect the contents.  You have to reassign the ICollectionView.
            List<MyClass> stuff4 = new List<MyClass> { new MyClass { key="1", value="2" }, new MyClass { key="3", value="4" } };
            // calling Refresh() will refire CollectionChanged, but will NOT use the new data
            ComboBoxView3.Refresh();
            // reassigning ICollectionView is necessary to load the new data
            // WHY ISN'T THIS WORKING???
            ComboBoxView3 = CollectionViewSource.GetDefaultView( stuff4);
            stuff4.Add( new MyClass { key="5", value="6" } );
            ComboBoxView3.Refresh();
            // refresh doesn't do the GUI update -- you have to use OnPropertyChanged!!!
        }

        // ComboBoxView1

        /// <summary>
        /// Looks like this event handler gets fired when the collection itself is set with GetDefaultView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ComboBoxView1_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if( ComboBoxView1.CurrentItem == null)
                return;

            MessageBox.Show( String.Format( "ComboBoxView1_CollectionChanged: CurrentItem is now '{0}'.", ComboBoxView1.CurrentItem.ToString()));
        }

        void ComboBoxView1_CurrentChanging(object sender, CurrentChangingEventArgs e)
        {
        }

        void ComboBoxView1_CurrentChanged(object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            MessageBox.Show( String.Format( "ComboBoxView1_CurrentChanged: CurrentItem is now '{0}'", view.CurrentItem.ToString()));
        }

        // ComboBoxView2 
        void ComboBoxView2_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if( ComboBoxView2.CurrentItem == null)
                return;

            MessageBox.Show( String.Format( "ComboBoxView2_CollectionChanged: CurrentItem is now '{0}'.", ComboBoxView2.CurrentItem.ToString()));
        }

        void ComboBoxView2_CurrentChanging(object sender, CurrentChangingEventArgs e)
        {
        }

        void ComboBoxView2_CurrentChanged(object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            MessageBox.Show( String.Format( "ComboBoxView2_CurrentChanged: CurrentItem is now '{0}'", view.CurrentItem.ToString()));
        }

        // ComboBoxView3
        void ComboBoxView3_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if( ComboBoxView3.CurrentItem == null)
                return;

            MessageBox.Show( String.Format( "ComboBoxView3_CollectionChanged: CurrentItem is now '{0}'.", ComboBoxView3.CurrentItem.ToString()));
        }

        void ComboBoxView3_CurrentChanging(object sender, CurrentChangingEventArgs e)
        {
        }

        void ComboBoxView3_CurrentChanged(object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            MessageBox.Show( String.Format( "ComboBoxView3_CurrentChanged: CurrentItem is now '{0}'", view.CurrentItem.ToString()));
        }

    }
}
