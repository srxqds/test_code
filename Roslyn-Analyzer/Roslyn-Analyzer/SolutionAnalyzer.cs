using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynAnalyzer.Readonly;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace RoslynAnalyzer.CLI
{
    public class SolutionAnalyzer
    {

        public static ISymbol fieldSymbol; // store the filed by AnalyzeProject
        public async Task LoadAnadAnalyzeProject(FileInfo projectFile, AnalyzerReport report) //TODO: Add async suffix
        {
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (o, e) =>
            {
                int a = 1;
                Console.WriteLine(e.Diagnostic.Message);
            };
            var solution = await workspace.OpenSolutionAsync(projectFile.FullName);

            var analyzers = this.GetAnalyzers();

            await AnalyzeProject(solution, analyzers, report);
            var referencedSymbols = SymbolFinder.FindReferencesAsync(fieldSymbol, solution).Result; //the referenceSymbols is empty
            int b = 1;
        }

        private ImmutableArray<DiagnosticAnalyzer> GetAnalyzers()
        {
            var listBuilder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            var assembly = typeof(DoNotModifyReadonlyAnalyzer).Assembly;
            var allTypes = assembly.DefinedTypes;

            foreach (var type in allTypes)
            {
                if (type.BaseType == typeof(DiagnosticAnalyzer))
                {
                    var instance = Activator.CreateInstance(type) as DiagnosticAnalyzer;
                    listBuilder.Add(instance);
                }
            }


            var analyzers = listBuilder.ToImmutable();
            return analyzers;
        }

        private async Task AnalyzeProject(Solution solution, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerReport report)
        {
            RolsynExtensions.CurrentSolution = solution;
            foreach (Project project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();

                var diagnosticResults = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

                report.AppendDiagnostics(diagnosticResults);
            }

        }
    }
}
