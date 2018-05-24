using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Runny
{
    public static class Compiler
    {
        public static (bool success, Assembly asm) LoadSource(string source)
        {
            var references = new List<MetadataReference>();
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var compilation = CSharpCompilation.Create("DynamicCode")
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddReferences(references)
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
