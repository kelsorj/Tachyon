using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICollectionViewSample
{
    public class MyClass
    {
        public string key { get; set; }
        public string value { get; set; }

        public override string ToString()
        {
            return String.Format( "[{0}, {1}]", key, value);
        }
    }
}
