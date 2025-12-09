using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BioNex.Shared.Utils
{
    public static class Windows
    {
        /// <summary>
        /// http://sachabarber.net/?p=162
        /// </summary>
        /// <param name="uc"></param>
        /// <returns></returns>
        /// <remarks>
        /// I could have also used System.Windows.Window.GetWindow( UserControl)
        /// </remarks>
        public static Window GetParentWindow( this UserControl uc)
        {
            DependencyObject dp = uc.Parent;
            do {
                dp = LogicalTreeHelper.GetParent( dp);
            } while( dp.GetType() != typeof( Window));

            return dp as Window;
        }
    }
}
