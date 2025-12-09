using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils.PVT;

namespace BioNex.PlateMover
{
    public class Stage
    {
        private IAxis Y { get; set; }
        private IAxis R { get; set; }

        public bool HasY { get; private set; }
        public bool HasR { get; private set; }

        public Stage( IAxis y, IAxis r)
        {
            // I used the "nonexistentaxis" concept because I didn't want to have to
            // pepper "if( y == null)..." throughout the code
            if( y != null) {
                Y = y;
                HasY = true;
            } else {
                Y = new NonExistentAxis();
                HasY = false;
            }

            if( r != null) {
                R = r;
                HasR = true;
            } else {
                R = new NonExistentAxis();
                HasR = false;
            }
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        public void Home()
        {
            Y.Home( false);
            R.Home( false);
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        public void HomeY()
        {
            Y.Home( false);
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        public void HomeR()
        {
            R.Home( false);
        }

        public bool YHomed
        {
            get { return Y.IsHomed; }
        }

        public bool RHomed
        {
            get { return R.IsHomed; }
        }

        public double GetRPos()
        {
            return R.GetPositionMM();
        }

        public double GetYPos()
        {
            return Y.GetPositionMM();
        }

        /// <summary>
        /// Stage performs a BLOCKING motion.  Must block because of the way this method eventually
        /// gets called by VWorks.  When it was non-blocking, the move would "complete" prematurely
        /// because there wasn't a clean way to make it wait for the motion complete event.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="r"></param>
        public void Move( double y, double r)
        {
            Y.MoveAbsolute( y, wait_for_move_complete: false);
            R.MoveAbsolute( r, wait_for_move_complete: false);
            Y.MoveAbsolute( y);
            R.MoveAbsolute( r);
        }

        private delegate void MoveAmountDelegate( double amount);

        /// <summary>
        /// Non-blocking
        /// </summary>
        /// <param name="amount"></param>
        public void JogNegative( double amount)
        {
            MoveAmountDelegate jog = new MoveAmountDelegate( Y.MoveRelative);
            jog.BeginInvoke( amount, MoveAmountCompleteCallback, null);
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        /// <param name="amount"></param>
        public void JogPositive( double amount)
        {
            MoveAmountDelegate jog = new MoveAmountDelegate( Y.MoveRelative);
            jog.BeginInvoke( -amount, MoveAmountCompleteCallback, null);
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        /// <param name="amount"></param>
        public void RotateCW( double amount)
        {
            MoveAmountDelegate jog = new MoveAmountDelegate( R.MoveRelative);
            jog.BeginInvoke( -amount, MoveAmountCompleteCallback, null);
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        /// <param name="amount"></param>
        public void RotateCCW( double amount)
        {
            MoveAmountDelegate jog = new MoveAmountDelegate( R.MoveRelative);
            jog.BeginInvoke( amount, MoveAmountCompleteCallback, null);
        }
        
        private void MoveAmountCompleteCallback( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                MoveAmountDelegate caller = (MoveAmountDelegate)ar.AsyncDelegate;
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        public void ServoOn()
        {
            R.Enable( true, false);
            Y.Enable( true, false);
        }

        public void ServoOff()
        {
            R.Enable( false, false);
            Y.Enable( false, false);
        }

        public void StopAll()
        {
            R.Stop();
            Y.Stop();
        }
    }
}

