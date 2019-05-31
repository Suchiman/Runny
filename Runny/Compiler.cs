using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Runny
{
    public static class Compiler
    {
        class BlazorBoot
        {
            public string main { get; set; }
            public string entryPoint { get; set; }
            public string[] assemblyReferences { get; set; }
            public string[] cssReferences { get; set; }
            public string[] jsReferences { get; set; }
            public bool linkerEnabled { get; set; }
        }

        private static Task InitializationTask;
        private static List<MetadataReference> References;

        public static void InitializeMetadataReferences(HttpClient client)
        {
            async Task InitializeInternal()
            {
                var response = await client.GetJsonAsync<BlazorBoot>("_framework/blazor.boot.json");
                var assemblies = await Task.WhenAll(response.assemblyReferences.Where(x => x.EndsWith(".dll")).Select(x => client.GetAsync("_framework/_bin/" + x)));

                var references = new List<MetadataReference>(assemblies.Length);
                foreach (var asm in assemblies)
                {
                    using (var task = await asm.Content.ReadAsStreamAsync())
                    {
                        references.Add(MetadataReference.CreateFromStream(task));
                    }
                }

                References = references;
            }
            InitializationTask = InitializeInternal();
        }

        public static void WhenReady(Func<Task> action)
        {
            if (InitializationTask.Status != TaskStatus.RanToCompletion)
            {
                InitializationTask.ContinueWith(x => action());
            }
            else
            {
                action();
            }
        }

        public static (bool success, Assembly asm) LoadSource(string source)
        {
            var compilation = CSharpCompilation.Create("DynamicCode")
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddReferences(References)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

            bool error = false;
            foreach (Diagnostic diag in diagnostics)
            {
                switch (diag.Severity)
                {
                    case DiagnosticSeverity.Info:
                        Console.WriteLine(diag.ToString());
                        break;
                    case DiagnosticSeverity.Warning:
                        Console.WriteLine(diag.ToString());
                        break;
                    case DiagnosticSeverity.Error:
                        error = true;
                        Console.WriteLine(diag.ToString());
                        break;
                }
            }

            if (error)
            {
                return (false, null);
            }

            using (var outputAssembly = new MemoryStream())
            {
                compilation.Emit(outputAssembly);

                return (true, Assembly.Load(outputAssembly.ToArray()));
            }
        }

        public static string Format(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();
            var normalized = root.NormalizeWhitespace();
            return normalized.ToString();
        }
    }
}
