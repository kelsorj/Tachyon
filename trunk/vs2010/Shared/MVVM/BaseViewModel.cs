using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace BumblebeeBetaGUI
{
    public class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        protected BaseViewModel() {}

        ~BaseViewModel()
        {
            string msg = string.Format( "{0} ({1}) ({2}) Finalized", GetType().Name, DisplayName, GetHashCode());
            Debug.WriteLine( msg);
        }

        public virtual string DisplayName { get; protected set; }
        public virtual bool ThrowOnInvalidPropertyName { get; protected set; }
        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName( string PropertyName)
        {
            if( TypeDescriptor.GetProperties(this)[PropertyName] == null) {
                string msg = "Invalid property name: " + PropertyName;
                if( ThrowOnInvalidPropertyName)
                    throw new Exception( msg);
                Debug.Fail( msg);
            }
        }

        protected virtual void RaisePropertyChanged( string PropertyName)
        {
            VerifyPropertyName( PropertyName);
            PropertyChangedEventHandler handler = PropertyChanged;
            if( handler == null)
                return;
            var e = new PropertyChangedEventArgs( PropertyName);
            handler( this, e);
            AfterPropertyChanged( PropertyName);
        }

        /// <summary>
        /// Derived classes can override this method to do stuff after the property has changed
        /// </summary>
        /// <param name="PropertyName"></param>
        protected virtual void AfterPropertyChanged( string PropertyName)
        {
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            OnDispose();
        }

        protected virtual void OnDispose() {}

        #endregion
    }
}
