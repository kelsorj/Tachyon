using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BioNex.Shared.TechnosoftLibrary
{
#if TML_SINGLETHREADED
    public class ITMLChannel {}
#endif

    public class Group
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private int GroupNumber { get; set; }
#if !TML_SINGLETHREADED
        private ITMLChannel Channel { get; set; }
#else
        private class Channel : TML.TMLLib {}
#endif
        private Object TSLock { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public Group( int group_number, ITMLChannel channel, Object ts_lock)
        {
            GroupNumber = group_number;
#if !TML_SINGLETHREADED
            Channel = channel;
#endif
            TSLock = ts_lock;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void AcquireAxes( IEnumerable< IAxis> axes)
        {
            ReleaseAxes();
            foreach( IAxis axis in axes){
                axis.AddToGroup(( byte)GroupNumber);
            }
        }
        // ----------------------------------------------------------------------
        public void ReleaseAxes()
        {
            lock( TSLock){
                Channel.TS_SelectBroadcast();
                Channel.TS_Execute( String.Format( "REMGRID({0});", GroupNumber));
            }
        }
        // ----------------------------------------------------------------------
        public void EnableGroup()
        {
            lock( TSLock){
                Channel.TS_SelectGroup(( byte)GroupNumber);
                // SendPositionLimitAndSettingTime();
                const string exe_cmd_str = "SRB UPGRADE, 0xF7FF, 0x0; POSOKLIM = 0U; TONPOSOK = 65535U;";
                Channel.TS_Execute( exe_cmd_str);
                Channel.TS_SetLongVariable( "func_done", TechnosoftConnection.InitialFuncDone);
                Channel.TS_CALL_Label( "func_my_axison");
            }
        }
    }

    public class GroupManager
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private static IDictionary< Group, bool> GroupAvailabilities { get; set; }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        private static readonly Object CreationaryLock = new Object();
        private static GroupManager Instance = null;

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        private GroupManager( ITMLChannel ts_channel, Object ts_lock)
        {
            GroupAvailabilities = Enumerable.Range( 1, 8).ToDictionary( n => new Group( n, ts_channel, ts_lock), n => true);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public static GroupManager GetInstance( ITMLChannel ts_channel, Object ts_lock)
        {
            lock( CreationaryLock){
                if( Instance == null){
                    Instance = new GroupManager( ts_channel, ts_lock);
                }
            }
            return Instance;
        }
        // ----------------------------------------------------------------------
        public Group AcquireGroup( IEnumerable< IAxis> axes, int timeout_ms = 0)
        {
            DateTime acquire_group_start = DateTime.Now;
            do{
                lock( GroupAvailabilities){
                    Group available_group = ( from ga in GroupAvailabilities
                                              where ga.Value
                                              select ga.Key).FirstOrDefault();
                    if( available_group != null){
                        GroupAvailabilities[ available_group] = false;
                        available_group.AcquireAxes( axes);
                        return available_group;
                    }
                }
                Thread.Sleep( 10);
            } while(( DateTime.Now - acquire_group_start).TotalMilliseconds >= timeout_ms);
            throw new Exception( "out of groups");
        }
        // ----------------------------------------------------------------------
        public void ReleaseGroup( Group group)
        {
            group.ReleaseAxes();
            lock( GroupAvailabilities){
                GroupAvailabilities[ group] = true;
            }
        }
        // ----------------------------------------------------------------------
        public void ReleaseAllGroups()
        {
            foreach( Group group in GroupAvailabilities.Keys){
                group.ReleaseAxes();
            }
            lock( GroupAvailabilities){
                foreach( Group group in GroupAvailabilities.Keys){
                    GroupAvailabilities[ group] = true;
                }
            }
        }
    }
}
