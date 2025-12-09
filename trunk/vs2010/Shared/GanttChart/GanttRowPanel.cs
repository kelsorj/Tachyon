using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Diagnostics;

namespace BioNex.Shared.GanttChart
{
    public class GanttRowPanel : Panel
    {
        // this property is attached to tasks so the panel knows how to space them out
        public static readonly DependencyProperty StartDateProperty = 
            DependencyProperty.RegisterAttached( "StartDate", typeof(DateTime), typeof(GanttRowPanel), new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsParentArrange));  
        public static readonly DependencyProperty EndDateProperty = 
            DependencyProperty.RegisterAttached( "EndDate", typeof(DateTime), typeof(GanttRowPanel), new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.AffectsParentArrange));  
        // this property allows us to size task parent as desired.  all of them should end up with the same min and max times
        public static readonly DependencyProperty MinDateProperty =
           DependencyProperty.Register("MinDate", typeof(DateTime), typeof(GanttRowPanel), new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register("MaxDate", typeof(DateTime), typeof(GanttRowPanel),  new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static DateTime GetStartDate(DependencyObject obj)
        {
            return (DateTime)obj.GetValue(StartDateProperty);
        }

        public static void SetStartDate(DependencyObject obj, DateTime value)
        {
            obj.SetValue(StartDateProperty, value);
        }

        public static DateTime GetEndDate(DependencyObject obj)
        {
            return (DateTime)obj.GetValue(EndDateProperty);
        }

        public static void SetEndDate(DependencyObject obj, DateTime value)
        {
            obj.SetValue(EndDateProperty, value);
        }

        public DateTime MinDate  
        {  
            get { return (DateTime)GetValue(MinDateProperty); }  
            set { SetValue(MinDateProperty, value); }  
        }  

        public DateTime MaxDate
        {  
            get { return (DateTime)GetValue(MaxDateProperty); }  
            set { SetValue(MaxDateProperty, value); }  
        }  

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach( UIElement child in Children)
                child.Measure(availableSize);  
            return new Size( ActualWidth, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double timespan = (MaxDate - MinDate).TotalSeconds;
            double pixels_per_second = UserControl1.PixelsPerSecond;//finalSize.Width / timespan;

            for( int i=0; i<Children.Count; i++) {
                UIElement child = Children[i] as UIElement;
                if( child == null)
                    continue;
                ArrangeChild( child, pixels_per_second, finalSize.Height);
            }

            return finalSize;
        }

        private void ArrangeChild(UIElement child, double pixels_per_second, double elementHeight)
        {
            DateTime childStartDate = GetStartDate(child);
            DateTime childEndDate = GetEndDate(child);//childStartDate + new TimeSpan( 0, 0, 5);//GetEndTime(child);
            TimeSpan childDuration = childEndDate - childStartDate;

            double total_seconds = (childStartDate - MinDate).TotalSeconds;
            double offset = total_seconds * pixels_per_second;
            double width = childDuration.TotalSeconds * pixels_per_second;
            
            child.Arrange(new Rect(offset, 0, width, elementHeight));
        }
    }
}
    