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
    public partial class ResultsSummary : Window
    {
        static string NOREAD = "--";

        public ResultsSummary(int run_id, int rows, int columns, IList<Averages> averages, IList<double> volumes)
        {
            InitializeComponent();
            DataContext = this;

            PopulateGrid(run_id, rows, columns, averages, volumes);
        }

        private void PopulateGrid(int run_id, int rows, int columns, IList<Averages> averages, IList<double> volumes)
        {
            // step 1. convert list of values to row/column array -- use NegativeInfinity to mark NO DATA
            bool use_volumes = volumes.Count == averages.Count;
            double[][] values = new double[rows][];
            for (int i = 0; i < rows; ++i)
            {
                values[i] = new double[columns];
                for (int j = 0; j < columns; ++j)
                    values[i][j] = double.NegativeInfinity;
            }
            for (int i = 0; i < averages.Count; ++i)
            {
                var value = use_volumes ? volumes[i] : averages[i].Average;
                var column = averages[i].Column;
                var row = (int)(averages[i].Channel * (rows / 8.0) + averages[i].Row); // row is either 0 or 1
                values[row][column] = value;
            }

            // step 2. calculate column averages
            double[] column_avg = new double[columns];
            int[] column_count = new int[columns];
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < columns; ++j)
                {
                    var value = values[i][j];
                    if (double.IsInfinity(value) || double.IsNaN(value))
                        continue;
                    column_avg[j] += value;
                    column_count[j] += 1;
                }
            for (int j = 0; j < columns; ++j)
                column_avg[j] = column_count[j] == 0 ? 0.0 : column_avg[j] / column_count[j];

            // step 3. calculate column stdev
            double[] column_stdev = new double[columns];
            for (int j = 0; j < columns; ++j)
            {
                column_stdev[j] = 0.0;
                if (column_count[j] == 0)
                    continue;
                for (int i = 0; i < rows; ++i)
                {
                    var value = values[i][j];
                    if (double.IsInfinity(value) || double.IsNaN(value))
                        continue;
                    column_stdev[j] += Math.Pow(value - column_avg[j], 2);
                }
                column_stdev[j] = Math.Sqrt(column_stdev[j] / column_count[j]);
            }


            // populate ResultsGrid 
            ResultsGrid.Rows = rows + 3;
            ResultsGrid.Columns = columns;
            // set font size and padding based on plate size
            var well_count = rows * columns;
            int font_size = well_count > 96 ? 12 : 18;
            
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < columns; ++j)
                {
                    var content = new ResultsSummaryContent();
                    content.Coordinate.Header = string.Format("({0},{1})", i + 1, j + 1);
                    content.Measurement.Text = double.IsNegativeInfinity(values[i][j]) ? NOREAD : string.Format("{0:0.00}", values[i][j]);
                    content.Measurement.FontSize = font_size;
                    ResultsGrid.Children.Add(content);
                }

            // for Ben -- add a column avg and column std_dev row along the bottom
            for (int j = 0; j < columns; ++j)
                ResultsGrid.Children.Add(new TextBlock()); // dummy row -- margin would make more sense, but then i can't use uniform grid
            for (int j = 0; j < columns; ++j)
            {
                var content = new ResultsSummaryContent();
                content.Coordinate.Header = string.Format("(average {0})", j + 1);
                content.Measurement.Text = string.Format("{0:0.00}", column_avg[j]);
                content.Measurement.FontSize = font_size;
                ResultsGrid.Children.Add(content);
            }
            for (int j = 0; j < columns; ++j)
            {
                var content = new ResultsSummaryContent();
                content.Coordinate.Header = string.Format("(stdev {0})", j + 1);
                content.Measurement.Text = string.Format("{0:0.00}", column_stdev[j]);
                content.Measurement.FontSize = font_size;
                ResultsGrid.Children.Add(content);
            }
            Title = string.Format("Run {0} Results Summary - units are {1}", run_id, (use_volumes ? "microliters (ul)" : "millimeters (mm)"));
        }
    }
}
