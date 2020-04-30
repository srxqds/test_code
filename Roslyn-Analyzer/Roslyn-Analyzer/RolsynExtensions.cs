using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynAnalyzer
{
    public static class RolsynExtensions
    {
        public static Solution CurrentSolution;
        public static Solution GetSolution(this SyntaxNodeAnalysisContext context)
        {
            return CurrentSolution;
            var workspace = context.Options.GetPrivatePropertyValue<object>("Workspace");
            return workspace.GetPrivatePropertyValue<Solution>("CurrentSolution");
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
