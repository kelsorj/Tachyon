using System;
using System.Linq;
using System.Threading;
using ElevateProcessLauncher;

namespace elevate
{
    class Program
    {
        static void ShowUsage()
        {
            Console.WriteLine("Usage: elevate <command> <args>");
            Console.WriteLine();
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {

            if(args.Length < 1)
                ShowUsage();

            var cmd = args[0];
            var arg = string.Join(" ", args.Skip(1));

            Console.WriteLine("executing '{0}' with args '{1}'", cmd, arg);

            new ElevatedProcessLauncher(cmd, arg);
        }
    }
}
