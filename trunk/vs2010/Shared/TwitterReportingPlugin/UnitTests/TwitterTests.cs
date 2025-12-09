using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BioNex.Shared.TwitterReportingPlugin
{
    public class UnitTests
    {
        private class TestData
        {
            private string _description;
            public string Description { get { return _description; } }

            public TestData( string desc)
            {
                _description = desc;
            }
        }

        [Test]
        public void TestMoreThan140CharacterTweet()
        {
            BioNex.Shared.TwitterReportingPlugin.TwitterPlugin twitter = new BioNex.Shared.TwitterReportingPlugin.TwitterPlugin();
            twitter.Open( null);
            TestData data = new TestData( "Another test from VS.NET since Twitter apparently recognizes repeated tweets.  Notice how this text flows nicely by reversing the order of tweets >140 characters!");
            twitter.LogMessage( data);
        }
    }
}
