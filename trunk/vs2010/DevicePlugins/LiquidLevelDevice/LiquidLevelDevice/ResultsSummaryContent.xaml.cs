using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using System.Windows.Controls;
using System.Collections.Generic;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for GraphWindow.xaml
    /// </summary>
    public partial class ResultsSummaryContent : UserControl
    {
        public ResultsSummaryContent()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
