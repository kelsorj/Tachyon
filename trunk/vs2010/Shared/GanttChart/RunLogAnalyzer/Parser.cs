using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RunLogAnalyzer
{
    public class ParserException : ApplicationException
    {
        public ParserException( string msg) : base(msg)
        {
        }
    }

    public enum TaskTypes { Undefined, ChangeTips, Aspirate, Dispense, ShuckTips, MoveStage, Unload, Load }

    public class Task
    {
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public string TipPairID { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        public Task( DateTime start, DateTime end, string id, string task_name, string description)
        {
            StartTime = start;
            EndTime = end;
            TipPairID = id;
            Name = task_name;
            Description = description;
        }
    }

    public class Parser
    {
        public List<Task> Tasks { get; private set; }

        public Parser( string filepath)
        {
            // open the file
            TextReader reader = new StreamReader( filepath);
            Tasks = new List<Task>();
            while( reader.Peek() >= 0) {
                string line = reader.ReadLine();
                string[] fields = line.Split( new char[] { ',' });
                // map the data in the fields to a structure
                string time = fields[1];
                TimeSpan ts = TimeSpan.Parse( time);
                DateTime now = DateTime.Now;
                DateTime end_time = new DateTime( now.Year, now.Month, now.Day, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                double task_time_s = double.Parse( fields[5]);
                // here, I tried to force better precision in the time, but it didn't help at all
                DateTime start_time = new DateTime( end_time.Ticks - TimeSpan.FromSeconds( task_time_s).Ticks);
                string description = String.Format( "start: {0}, duration: {1}", start_time.TimeOfDay, task_time_s);
                string pair_id = String.Empty;

                // here, we need to be a bit smarter about how we add tasks.  I originally looked for one
                // value in column 2, and from that inferred the tip pair.  However, 1&2 aren't always paired,
                // nor are 3&4.  It could be 1&3, 2&4, or just 1, just 2, etc etc.

                // look for aspirate and dispense strings first
                bool found_asp_disp = false;
                for( int i=2; i<=3; i++) {
                    if( fields[i] != String.Empty) {
                        pair_id = "Tip " + fields[i];
                        description = fields[4] + description;
                        TaskTypes task_type = GetTaskType( fields[4]);
                        Tasks.Add( new Task( start_time, end_time, pair_id, task_type.ToString(), description));
                        found_asp_disp = true;
                    }
                }
                // start over again if we just did aspirate or dispense
                if( found_asp_disp)
                    continue;

                // now look for changing tips
                if( fields[4].Contains( "changing tips")) {
                    // regex for getting tips that changed
                    Regex r = new Regex( @"changing tips\s((\d)\s\s)((\d)\s\s)*\stook");
                    MatchCollection m = r.Matches( fields[4]);
                    if( m.Count == 1) {
                        for( int i=1; i<=m[0].Groups.Count; i++) {
                            Group group = m[0].Groups[i];
                            if( !group.Success || group.ToString().Contains( "  "))
                                continue;
                            string tip_id = group.ToString();
                            pair_id = "Tip " + int.Parse( tip_id).ToString();
                            description = fields[4] + description;
                            TaskTypes task_type = GetTaskType( fields[4]);
                            Tasks.Add( new Task( start_time, end_time, pair_id, task_type.ToString(), description));
                        }
                    }
                } else {
                    // handle the stage and robot moves here
                    if( fields[4].Contains( "robot unloading") || fields[4].Contains( "robot loading"))
                        pair_id = "Robot";
                    else
                        pair_id = "Stages";
                    description = fields[4] + description;
                    TaskTypes task_type = GetTaskType( fields[4]);
                    Tasks.Add( new Task( start_time, end_time, pair_id, task_type.ToString(), description));
                }
            }
            reader.Close();
        }

        private TaskTypes GetTaskType( string description)
        {
            if( description.Contains( "changing tips"))
                return TaskTypes.ChangeTips;
            if( description.Contains( "aspirate"))
                return TaskTypes.Aspirate;
            if( description.Contains( "dispense"))
                return TaskTypes.Dispense;
            if( description.Contains( "tip shucking"))
                return TaskTypes.ShuckTips;
            if( description.Contains( "moving stage"))
                return TaskTypes.MoveStage;
            if( description.Contains( "robot unloading"))
                return TaskTypes.Unload;
            if( description.Contains( "robot loading"))
                return TaskTypes.Load;
            return TaskTypes.Undefined;
        }
    }
}
