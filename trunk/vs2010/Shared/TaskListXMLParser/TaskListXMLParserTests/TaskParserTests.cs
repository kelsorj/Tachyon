using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using BioNex.Shared.LibraryInterfaces;
using System.Xml.Linq;
using BioNex.Shared.TaskListXMLParser;

namespace TaskListXMLParserTests
{
    [TestFixture]
    public class TaskParserTests
    {
        [SetUp]
        public void Init()
        {

        }

        [Test]
        public void TestSchema()
        {

        }

        [Test]
        public void TestParseHitpickDefaultTasksSourcePreOnly()
        {
            var xml = XElement.Load( @"..\..\sample_sourcepre_only.xml");
            TaskListParser parser = new TaskListParser();
            TaskListParser.DefaultTaskLists default_tasks = parser.ParseDefaultTaskLists( xml.Element( "tasks"));
            Assert.IsTrue( default_tasks.source_prehitpick_tasks.Count() == 1);
            Assert.IsTrue( default_tasks.source_posthitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_posthitpick_tasks.Count() == 0);
            var task = default_tasks.source_prehitpick_tasks.First();
            Assert.AreEqual( "PlatePierce", task.DeviceInstance);
            Assert.AreEqual( "Pierce", task.Command);
            Assert.IsTrue( task.ParametersAndVariables.Count() == 1);
            Task.Parameter p = task["pierce_time_seconds"];
            Assert.AreEqual( "2", p.Value);
            Assert.AreEqual( "", p.Variable);
        }

        [Test]
        public void TestParseHitpickDefaultTasksSourcePostOnly()
        {
            var xml = XElement.Load( @"..\..\sample_sourcepost_only.xml");
            TaskListParser parser = new TaskListParser();
            TaskListParser.DefaultTaskLists default_tasks = parser.ParseDefaultTaskLists( xml.Element( "tasks"));
            Assert.IsTrue( default_tasks.source_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.source_posthitpick_tasks.Count() == 1);
            Assert.IsTrue( default_tasks.dest_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_posthitpick_tasks.Count() == 0);
            var task = default_tasks.source_posthitpick_tasks.First();
            Assert.AreEqual( "PlateLoc", task.DeviceInstance);
            Assert.AreEqual( "Seal", task.Command);
            Assert.IsTrue( task.ParametersAndVariables.Count() == 1);
            Task.Parameter p = task["seal_time_seconds"];
            Assert.AreEqual( "5", p.Value);
            Assert.AreEqual( "", p.Variable);
        }
    
        [Test]
        public void TestParseHitpickDefaultTasksDestPreOnly()
        {
            var xml = XElement.Load( @"..\..\sample_destpre_only.xml");
            TaskListParser parser = new TaskListParser();
            TaskListParser.DefaultTaskLists default_tasks = parser.ParseDefaultTaskLists( xml.Element( "tasks"));
            Assert.IsTrue( default_tasks.source_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.source_posthitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_prehitpick_tasks.Count() == 1);
            Assert.IsTrue( default_tasks.dest_posthitpick_tasks.Count() == 0);
            var task = default_tasks.dest_prehitpick_tasks.First();
            Assert.AreEqual( "HiG", task.DeviceInstance);
            Assert.AreEqual( "Spin", task.Command);
            Assert.IsTrue( task.ParametersAndVariables.Count() == 3);
            // validate multiple parameters
            Task.Parameter p = task["acceleration_percent"];
            Assert.AreEqual( "100", p.Value);
            Assert.AreEqual( "", p.Variable);
            p = task["velocity_percent"];
            Assert.AreEqual( "100", p.Value);
            Assert.AreEqual( "", p.Variable);
            p = task["spin_time_seconds"];
            Assert.AreEqual( "5", p.Value);
            Assert.AreEqual( "", p.Variable);
        }

        [Test]
        public void TestParseHitpickDefaultTasksDestPostOnly()
        {
            var xml = XElement.Load( @"..\..\sample_destpost_only.xml");
            TaskListParser parser = new TaskListParser();
            TaskListParser.DefaultTaskLists default_tasks = parser.ParseDefaultTaskLists( xml.Element( "tasks"));
            Assert.IsTrue( default_tasks.source_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.source_posthitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_posthitpick_tasks.Count() == 1);
            var task = default_tasks.dest_posthitpick_tasks.First();
            Assert.AreEqual( "PlateLoc", task.DeviceInstance);
            Assert.AreEqual( "Seal", task.Command);
            Assert.IsTrue( task.ParametersAndVariables.Count() == 1);
            // validate multiple parameters
            Task.Parameter p = task["seal_time_seconds"];
            Assert.AreEqual( "5", p.Value);
            Assert.AreEqual( "", p.Variable);
        }

        [Test]
        public void TestParseHitpickDefaultTasksOnly()
        {
            var xml = XElement.Load( @"..\..\sample_defaulttasks_only.xml");
            TaskListParser parser = new TaskListParser();
            TaskListParser.DefaultTaskLists default_tasks = parser.ParseDefaultTaskLists( xml.Element( "tasks"));
            Assert.IsTrue( default_tasks.source_prehitpick_tasks.Count() == 2);
            Assert.IsTrue( default_tasks.source_posthitpick_tasks.Count() == 1);
            Assert.IsTrue( default_tasks.dest_prehitpick_tasks.Count() == 0);
            Assert.IsTrue( default_tasks.dest_posthitpick_tasks.Count() == 2);
            // check source pre
            // task #1
            var task = default_tasks.source_prehitpick_tasks[0];
            Assert.AreEqual( "PlatePierce", task.DeviceInstance);
            Assert.AreEqual( "Pierce", task.Command);
            Assert.AreEqual( 1, task.ParametersAndVariables.Count());
            Task.Parameter p = task["pierce_time_seconds"];
            Assert.AreEqual( "3", p.Value);
            Assert.AreEqual( "", p.Variable);
            // task #2
            task = default_tasks.source_prehitpick_tasks[1];
            Assert.AreEqual( "HiG", task.DeviceInstance);
            Assert.AreEqual( "Spin", task.Command);
            Assert.AreEqual( 3, task.ParametersAndVariables.Count());
            p = task["acceleration_percentage"];
            Assert.AreEqual( "100", p.Value);
            Assert.AreEqual( "", p.Variable);
            p = task["velocity_percentage"];
            Assert.AreEqual( "100", p.Value);
            Assert.AreEqual( "", p.Variable);
            p = task["spin_time_seconds"];
            Assert.AreEqual( "5.5", p.Value);
            Assert.AreEqual( "", p.Variable);
            // check source post
            task = default_tasks.source_posthitpick_tasks[0];
            Assert.AreEqual( "PlateLoc", task.DeviceInstance);
            Assert.AreEqual( "Seal", task.Command);
            Assert.AreEqual( 1, task.ParametersAndVariables.Count());
            p = task["seal_time_seconds"];
            Assert.AreEqual( "5", p.Value);
            Assert.AreEqual( "", p.Variable);
            // check dest post
            task = default_tasks.dest_posthitpick_tasks[0];
            Assert.AreEqual( "WellMate", task.DeviceInstance);
            Assert.AreEqual( "Fill", task.Command);
            Assert.AreEqual( 1, task.ParametersAndVariables.Count());
            p = task["seal_time_seconds"];
            Assert.AreEqual( "5", p.Value);
            Assert.AreEqual( "", p.Variable);
        }

        // NOTE: I used to have tests for overrides here, but that's actually not necessary from a TaskParser
        //       perspective because the underlying code is the same.  Testing task parameters when overriding
        //       tasks in the source and dest plate sections should really be done from HitpickXMLReader.
    }
}
