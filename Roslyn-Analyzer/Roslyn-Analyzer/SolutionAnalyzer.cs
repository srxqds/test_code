using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace RoslynAnalyzer.CLI
{

    public abstract class DiagnosticAnalyzerWithSolution : DiagnosticAnalyzer
    {
        public Solution solution { get; private set; }

        public DiagnosticAnalyzerWithSolution(Solution solution) : base()
        {
            this.solution = solution;
        }

        public SyntaxNode GetSyntaxNodeFromLocation(Location location)
        {
            return RolsynExtensions.GetSyntaxNodeFromLocation(this.solution, location);
        }
        
        
    }
    public class SolutionAnalyzer
    {
        
        public async Task LoadAnadAnalyzeProject(FileInfo projectFile, AnalyzerReport report) //TODO: Add async suffix
        {
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (o, e) =>
            {
                Console.WriteLine(e.Diagnostic.Message);
            };
            var solution = await workspace.OpenSolutionAsync(projectFile.FullName);

            var analyzers = this.GetAnalyzers(solution);

            await AnalyzeProject(solution, analyzers, report);
        }

        private ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(Solution solution)
        {
            var listBuilder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            var assembly = typeof(DiagnosticAnalyzerWithSolution).Assembly;
            var allTypes = assembly.DefinedTypes;

            foreach (var type in allTypes)
            {
                if (type == typeof(DiagnosticAnalyzerWithSolution))
                    continue;
                else if (type.BaseType == typeof(DiagnosticAnalyzerWithSolution))
                {
                    listBuilder.Add(Activator.CreateInstance(type, solution) as DiagnosticAnalyzer);
                }
                else if (type.BaseType == typeof(DiagnosticAnalyzer))
                {
                    listBuilder.Add(Activator.CreateInstance(type) as DiagnosticAnalyzer);
                }
            }
            
            var analyzers = listBuilder.ToImmutable();
            return analyzers;
        }

        private async Task AnalyzeProject(Solution solution, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerReport report)
        {
            foreach (Project project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();

                var diagnosticResults = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

                report.AppendDiagnostics(diagnosticResults);
            }

        }
    }
}
