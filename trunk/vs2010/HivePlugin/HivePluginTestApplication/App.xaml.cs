using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using BioNex.Shared.LabwareDatabase;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;

namespace HivePluginTestApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private CompositionContainer _container;

        public App()
        {
            Compose();
        }

        private bool Compose()
        {
            /*
            var catalog = new DirectoryCatalog( ".");
            _container = new CompositionContainer( catalog);
            _container.ComposeParts( this);
             */
            return true;
        }
    }

}
