using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;

namespace bettersigntool
{
    class Program
    {
        public static int Main(string[] args)
        {
            return ConsoleCommandDispatcher.DispatchCommand(
                ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof (Program)),
                args,
                Console.Out);
        }
    }
}
