using System;
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
            var asm = Compiler.LoadSource(Code);
            var hasArgs = asm.EntryPoint.GetParameters().Length > 0;

            var currentOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;
            try
            {
                asm.EntryPoint.Invoke(null, hasArgs ? new string[][] { null } : null);
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
        }
    }
}
