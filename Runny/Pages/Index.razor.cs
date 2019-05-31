using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Runny.Pages
{
    public class IndexModel : ComponentBase
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

        [Inject] private HttpClient Client { get; set; }

        protected override Task OnInitAsync()
        {
            Compiler.InitializeMetadataReferences(Client);
            return base.OnInitAsync();
        }

        public void Run()
        {
            Compiler.WhenReady(RunInternal);
        }

        async Task RunInternal()
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
                    var entry = asm.EntryPoint;
                    if (entry.Name == "<Main>") // sync wrapper over async Task Main
                    {
                        entry = entry.DeclaringType.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for the async Task Main
                    }
                    var hasArgs = entry.GetParameters().Length > 0;
                    var result = entry.Invoke(null, hasArgs ? new object[] { new string[0] } : null);
                    if (result is Task t)
                    {
                        await t;
                    }
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

            StateHasChanged();
        }
    }
}
