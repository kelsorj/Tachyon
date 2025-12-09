using System;
using GalaSoft.MvvmLight.Messaging;

namespace BioNex.Shared.ThreadsafeMessenger
{
    /// <summary>
    /// BumblebeeMessenger provides thread safety for message registration
    /// </summary>
    //! \todo this should use CONTAINMENT instead of inheritance to guarantee that public (non-threadsafe) methods
    //!       don't get exposed accidentally.  I just had an issue where Send iterates over messages, but another
    //!       object could be registering / unregistering at the same time.
    public class ThreadsafeMessenger : Messenger
    {
        public override void Register<TMessage>(object recipient, Action<TMessage> action)
        {
            lock( this) {
                base.Register<TMessage>( recipient, action);
            }
        }

        public override void Register<TMessage>(object recipient, bool receiveDerivedMessagesToo, Action<TMessage> action)
        {
            lock( this) {
                base.Register<TMessage>( recipient, receiveDerivedMessagesToo, action);
            }
        }

        public override void Register<TMessage>(object recipient, object token, Action<TMessage> action)
        {
            lock( this) {
                base.Register<TMessage>( recipient, token, action);
            }
        }

        public override void Register<TMessage>(object recipient, object token, bool receiveDerivedMessagesToo, Action<TMessage> action)
        {
            lock( this) {
                base.Register<TMessage>( recipient, token, receiveDerivedMessagesToo, action);
            }
        }

        public override void Unregister<TMessage>(object recipient)
        {
            lock( this) {
                base.Unregister<TMessage>( recipient);
            }
        }

        public override void Unregister(object recipient)
        {
            lock( this) {
                base.Unregister( recipient);
            }
        }

        public override void Unregister<TMessage>(object recipient, Action<TMessage> action)
        {
            lock( this) {
                base.Unregister<TMessage>( recipient, action);
            }
        }

        public override void Send<TMessage, TTarget>(TMessage message)
        {
            lock( this) {
                base.Send<TMessage, TTarget>(message);
            }
        }

        public override void Send<TMessage>(TMessage message)
        {
            lock( this) {
                base.Send<TMessage>(message);
            }
        }

        public override void Send<TMessage>(TMessage message, object token)
        {
            lock( this) {
                base.Send<TMessage>(message, token);
            }
        }
    }
}