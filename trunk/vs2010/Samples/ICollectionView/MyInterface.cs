using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICollectionViewSample
{
    public interface MyInterface
    {
        void DoSomething();
    }

    public class MyInterfaceImpl : MyInterface
    {
        private string _name { get; set; }

        public void DoSomething() {}

        public MyInterfaceImpl( string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
