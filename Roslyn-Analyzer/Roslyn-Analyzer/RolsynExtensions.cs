using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace RoslynAnalyzer
{
    public static class RolsynExtensions
    {
        public static DataFlowAnalysis AnalyzeDataFlow(this SemanticModel semanticModel, MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return semanticModel.AnalyzeDataFlow(methodDeclarationSyntax.Body);
        }

        public static void IterateArgumentSyntax(ArgumentListSyntax argumentListSyntax, Action<ArgumentListSyntax, ArgumentSyntax> callback)
        {
            foreach(ArgumentSyntax argumentSyntax in argumentListSyntax.Arguments)
            {
                callback(argumentListSyntax, argumentSyntax);
            }
        }
        public static void IterateReferenceSymbols(IEnumerable<ReferencedSymbol> referencedSymbols, Action<ISymbol, ReferenceLocation> callback)
        {
            if (referencedSymbols != null)
            {
                foreach (ReferencedSymbol referencedSymbol in referencedSymbols)
                {
                    foreach (ReferenceLocation location in referencedSymbol.Locations)
                    {
                        callback(referencedSymbol.Definition, location);
                    }
                }
            }
        }

        public static ISymbol ConvertAnalyzerToWorkspace(ISymbol symbol, Solution solution)
        {
            if (symbol == null)
                return null;
            return null;
        }

        public static SyntaxNode GetSyntaxNodeFromLocation(Project project, Location location)
        {
            SyntaxNode result = null;
            foreach (var document in project.Documents)
            {
                if (document.FilePath == location.SourceTree.FilePath)
                {
                    var syntaxRoot = document.GetSyntaxRootAsync().Result;
                    result = syntaxRoot.FindNode(location.SourceSpan);
                    break;
                }
            }
            return result;
        }

        public static SyntaxNode GetSyntaxNodeFromLocation(Solution solution, Location location)
        {
            SyntaxNode result = null;
            foreach (Project project in solution.Projects)
            {
                result = GetSyntaxNodeFromLocation(project, location);
                if (result != null)
                    break;
            }
            return result;
        }

        public static SemanticModel GetSemanticModelFromLocation(Solution solution, Location location)
        {
            SemanticModel result = null;
            foreach (Project project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == location.SourceTree.FilePath)
                    {
                        result = document.GetSemanticModelAsync().Result;
                        break;
                    }
                }
            }
            return result;
        }

        public static ISymbol GetISymbolFromSyntaxNode(Solution solution, SyntaxNode syntaxNode)
        {
            SemanticModel semanticModel = GetSemanticModelFromLocation(solution, syntaxNode.GetLocation());
            if (semanticModel == null)
                return null;
            return semanticModel.GetSymbolInfo(syntaxNode).Symbol;
        }

        public static T GetPrivatePropertyValue<T>(this object obj, string propName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var pi = obj.GetType().GetRuntimeProperty(propName);

            if (pi == null)
            {
                throw new ArgumentOutOfRangeException(nameof(propName), $"Property {propName} was not found in Type {obj.GetType().FullName}");
            }

            return (T)pi.GetValue(obj, null);
        }

        public static bool TryGetSymbolInfo(this SyntaxNodeAnalysisContext context, SyntaxNode node, out SymbolInfo symbolInfo)
        {
            try
            {
                //NOTE: The Call below fixes many issues where the symbol cannot be found - but there are still cases where an argumentexception is thrown
                // which seems to resemble this issue: https://github.com/dotnet/roslyn/issues/11193

                var semanticModel = SemanticModelFor(context.SemanticModel, node);

                symbolInfo = semanticModel.GetSymbolInfo(node); //context.SemanticModel.GetSymbolInfo(node);
                return true;
            }
            catch (Exception generalException)
            {
                Debug.WriteLine("Unable to find Symbol: " + node);
                Debug.WriteLine(generalException);
            }

            symbolInfo = default(SymbolInfo);
            return false;
        }

        internal static SemanticModel SemanticModelFor(SemanticModel semanticModel, SyntaxNode expression)
        {
            if (ReferenceEquals(semanticModel.SyntaxTree, expression.SyntaxTree))
            {
                return semanticModel;
            }

            //NOTE: there may be a performance boost if we cache some of the semantic models
            return semanticModel.Compilation.GetSemanticModel(expression.SyntaxTree);
        }

        public static bool IsDerived(this ClassDeclarationSyntax classDeclaration)
        {
            return (classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Count > 0);
        }

        public static bool IsSealed(this ClassDeclarationSyntax classDeclaration)
        { 
            return classDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.SealedKeyword);
        }

        public static bool IsSealed(this MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.SealedKeyword);
        }

        public static bool IsOverriden(this MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.OverrideKeyword);
        }
    }
}
