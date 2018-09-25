using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Blazor.Components;

namespace Runny.Pages
{
    public class IndexModel : BlazorComponent
    {
        public string Output = "";
        public string Code = @"using System;

class Program
{
    public static void Main()
    {
        Console.WriteLine(""Hello World"");
    }
}";

        public void Run()
        {
            Output = "";

            Console.WriteLine("Compiling and Running code");
            var sw = Stopwatch.StartNew();

            var currentOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;
            try
            {
                var (success, asm) = Compiler.LoadSource(Code);
                if (success)
                {
                    var hasArgs = asm.EntryPoint.GetParameters().Length > 0;
                    asm.EntryPoint.Invoke(null, hasArgs ? new string[][] { null } : null);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Output = writer.ToString();
            if (exception != null)
            {
                Output += "\r\n" + exception.ToString();
            }
            Console.SetOut(currentOut);

            sw.Stop();
            Console.WriteLine("Done in " + sw.ElapsedMilliseconds + "ms");
        }
    }
}
