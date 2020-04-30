using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynAnalyzer.Readonly;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalyzer.CLI
{
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

            var analyzers = this.GetAnalyzers();

            await AnalyzeProject(solution, analyzers, report);

            foreach (Project project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    SemanticModel semanticModel = document.GetSemanticModelAsync().Result;
                    var syntaxRoot = document.GetSyntaxRootAsync().Result;
                    var readonlyNodes = syntaxRoot.DescendantNodes().OfType<FieldDeclarationSyntax>();
                    foreach (var readonlyNode in readonlyNodes)
                    {
                        foreach (var variable in readonlyNode.Declaration.Variables)
                        {
                            var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                            if (fieldSymbol.IsReadOnly && fieldSymbol.IsStatic)
                            {
                                var referencedSymbols = SymbolFinder.FindReferencesAsync(fieldSymbol, solution).Result;
                                foreach (var ref1 in referencedSymbols)
                                {
                                    foreach (var location in ref1.Locations)
                                    {

                                    }
                                }
                            }
                            // Do stuff with the symbol here
                        }
                    }


                }
            }
            
                // var referencedSymbols = SymbolFinder.FindReferencesAsync(fieldSymbol, solution).Result; //the referenceSymbols is empty

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
            foreach (Project project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();

                var diagnosticResults = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

                report.AppendDiagnostics(diagnosticResults);
            }

        }
    }
}
