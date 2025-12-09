using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PathFinding;

namespace TestPathFinding
{
    [TestClass]
    public class Test_Djikstra
    {
        public Test_Djikstra()
        {
        }

        private TestContext testContextInstance;
        private static Dictionary<string, Node<string>> graph = new Dictionary<string, Node<string>>();
        private static Djikstra<string> pathing = new Djikstra<string>();

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext) 
        {
            // See attached spreadsheet "test_path_visualization.xlsx" for a graphic view of this
            graph["a"] = new Node<string>() { key = "a" };
            graph["b"] = new Node<string>() { key = "b" };
            graph["c"] = new Node<string>() { key = "c" };
            graph["d"] = new Node<string>() { key = "d" };
            graph["e"] = new Node<string>() { key = "e" };
            graph["f"] = new Node<string>() { key = "f" };
            graph["g"] = new Node<string>() { key = "g" };

            // "forward" connections
            graph["a"].connections.Add(new Connection<string>() { node = graph["b"], cost = 1 });
            graph["b"].connections.Add(new Connection<string>() { node = graph["b"], cost = 1 });
            graph["b"].connections.Add(new Connection<string>() { node = graph["c"], cost = 1 });
            graph["b"].connections.Add(new Connection<string>() { node = graph["d"], cost = 1 });
            graph["b"].connections.Add(new Connection<string>() { node = graph["f"], cost = 1 });
            graph["c"].connections.Add(new Connection<string>() { node = graph["d"], cost = 1 });
            graph["c"].connections.Add(new Connection<string>() { node = graph["g"], cost = 1 });
            graph["d"].connections.Add(new Connection<string>() { node = graph["e"], cost = 1 });
            graph["d"].connections.Add(new Connection<string>() { node = graph["g"], cost = 1 });
            graph["f"].connections.Add(new Connection<string>() { node = graph["a"], cost = 1 });

            // "backward" connections -- Added 12/2/11 to make all the graph edges "double" arrows instead of singly directed
            // graph["a"].connections.Add(new Connection<string>() { node = graph["f"], cost = 1 }); --> A has no path back to F for "TestPathWithCycleTerminates"
            graph["b"].connections.Add(new Connection<string>() { node = graph["a"], cost = 1 });
            graph["c"].connections.Add(new Connection<string>() { node = graph["b"], cost = 1 });
            graph["d"].connections.Add(new Connection<string>() { node = graph["b"], cost = 1 });
            graph["d"].connections.Add(new Connection<string>() { node = graph["c"], cost = 1 });
            // graph["e"].connections.Add(new Connection<string>() { node = graph["d"], cost = 1 }); --> E has no path back for "TestNoPathPossible"
            // graph["f"].connections.Add(new Connection<string>() { node = graph["b"], cost = 1 }); --> F has no path back to B for "TestPathWithCycleTerminates"
            graph["g"].connections.Add(new Connection<string>() { node = graph["c"], cost = 1 });
            graph["g"].connections.Add(new Connection<string>() { node = graph["d"], cost = 1 });
        }

        [TestMethod]
        public void TestTrivialCase()
        {
            var a = graph["a"];
            var result = pathing.FindShortestPath(a, a);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count == 1, "one item in result");
            Assert.IsTrue(result[0] == a, "path is 'a'");
        }

        [TestMethod]
        public void TestShortPath()
        {
            var a = graph["a"];
            var b = graph["b"];
            var result = pathing.FindShortestPath(a, b);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 2, "two items in result");
            Assert.IsTrue(result[0] == a, "1st path is 'a'");
            Assert.IsTrue(result[1] == b, "2nd path is 'b'");
        }

        [TestMethod]
        public void TestSecondShortPath()
        {
            var a = graph["a"];
            var b = graph["b"];
            var c = graph["c"];
            var result = pathing.FindShortestPath(a, c);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 3, "three items in result");
            Assert.IsTrue(result[0] == a, "1st path is 'a'");
            Assert.IsTrue(result[1] == b, "2nd path is 'b'");
            Assert.IsTrue(result[2] == c, "3rd path is 'c'");
        }

        [TestMethod]
        public void TestPathWithSingleAlternative()
        {
            var a = graph["a"];
            var b = graph["b"];
            var d = graph["d"];
            var e = graph["e"];
            var result = pathing.FindShortestPath(a, e);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 4, "four items in result");
            Assert.IsTrue(result[0] == a, "1st path is 'a'");
            Assert.IsTrue(result[1] == b, "2nd path is 'b'");
            Assert.IsTrue(result[2] == d, "3rd path is 'd'");
            Assert.IsTrue(result[3] == e, "4th path is 'e'");
        }

        [TestMethod]
        public void TestPathWithTwoAlternativesPicksFirstDefined()
        {
            var a = graph["a"];
            var b = graph["b"];
            var c = graph["c"];
            var g = graph["g"];
            var result = pathing.FindShortestPath(a, g);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 4, "four items in result");
            Assert.IsTrue(result[0] == a, "1st path is 'b'");
            Assert.IsTrue(result[1] == b, "2nd path is 'b'");
            Assert.IsTrue(result[2] == c, "3rd path is 'c'");
            Assert.IsTrue(result[3] == g, "4th path is 'g'");
        }

        [TestMethod]
        public void TestPathWithCycleTerminates()
        {
            var a = graph["a"];
            var b = graph["b"];
            var f = graph["f"];
            var result = pathing.FindShortestPath(a, f);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 3, "three items in result");
            Assert.IsTrue(result[0] == a, "1st path is 'a'");
            Assert.IsTrue(result[1] == b, "2nd path is 'b'");
            Assert.IsTrue(result[2] == f, "3rd path is 'f'");
        }

        [TestMethod]
        public void TestPathNotStartingFromA()
        {
            var a = graph["a"];
            var b = graph["b"];
            var c = graph["c"];
            var f = graph["f"];
            var result = pathing.FindShortestPath(f, c);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 4, "four items in result");
            Assert.IsTrue(result[0] == f, "1st path is 'f'");
            Assert.IsTrue(result[1] == a, "2nd path is 'a'");
            Assert.IsTrue(result[2] == b, "3rd path is 'b'");
            Assert.IsTrue(result[3] == c, "4th path is 'c'");
        }

        [TestMethod]
        public void TestNoPathPossible()
        {
            var a = graph["a"];
            var e = graph["e"];
            var result = pathing.FindShortestPath(e, a);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 0, "no items in result");
        }

        [TestMethod]
        public void TestBadArgs_StartNotConnected()
        {
            var bad = new Node<string>() { key = "bad" };
            var b = graph["b"];
            var result = pathing.FindShortestPath(bad, b);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 0, "no items in result");
        }

        [TestMethod]
        public void TestBadArgs_EndNotConnected()
        {
            var bad = new Node<string>() { key = "bad" };
            var b = graph["b"];
            var result = pathing.FindShortestPath(b, bad);
            Assert.IsNotNull(result);
            foreach (var node in result)
                System.Console.WriteLine("node: {0}", node.key);
            Assert.IsTrue(result.Count == 0, "no items in result");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestBadArgs_NullStart()
        {
            var b = graph["b"];
            var result = pathing.FindShortestPath(null, b);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestBadArgs_NullEnd()
        {
            var a = graph["a"];
            var result = pathing.FindShortestPath(a, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadArgs_NegativeConnectionCost()
        {
            var a = graph["a"];
            var d = graph["d"];
            var e = graph["e"];
            d.connections[0].cost = -1;
            var result = pathing.FindShortestPath(a, e);
        }
    }
}
