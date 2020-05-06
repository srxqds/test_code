using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Immutable;
using System;
using RoslynAnalyzer.CLI;
using System.Linq;
using System.Collections.Generic;

namespace RoslynAnalyzer.Readonly
{
    // 查找所有引用field的syntaxnode
    // 查找所有有修改的syntaxnode
    // 
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotModifyReadonlyArrayAnalyzer : DiagnosticAnalyzerWithSolution
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray);
        public List<IEnumerable<ReferencedSymbol>> ReadonlyArrayReferences = new List<IEnumerable<ReferencedSymbol>>();
        public DoNotModifyReadonlyArrayAnalyzer(Solution solution): base(solution)
        {
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
                            if (fieldSymbol.IsReadOnly && fieldSymbol.Type.TypeKind == TypeKind.Array)
                            {
                                var referencedSymbols = SymbolFinder.FindReferencesAsync(fieldSymbol, solution).Result;
                                if (referencedSymbols != null)
                                {
                                    foreach(ReferencedSymbol referencedSymbol in referencedSymbols)
                                    {
                                        foreach(ReferenceLocation location in referencedSymbol.Locations)
                                        {
                                            SyntaxNode syntaxNode = RolsynExtensions.GetSyntaxNodeFromLocation(solution, location.Location);
                                            
                                            if(syntaxNode is ElementAccessExpressionSyntax)
                                            {
                                                int b = 1;
                                            }
                                            int a = 1;
                                        }
                                    }
                                    ReadonlyArrayReferences.Add(referencedSymbols);
                                }
                            }
                        }
                    }
                }
            }

        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.ElementAccessExpression);
        }
        
        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            ElementAccessExpressionSyntax elementAccessExpressionSyntax = context.Node as ElementAccessExpressionSyntax;
            Location contextLocation = elementAccessExpressionSyntax.GetLocation();
            if(elementAccessExpressionSyntax != null)
            {
                ISymbol expressionSymbol = context.SemanticModel.GetSymbolInfo(elementAccessExpressionSyntax.Expression).Symbol;
                if (expressionSymbol.Kind == SymbolKind.Parameter)
                {
                    IParameterSymbol parameter = expressionSymbol as IParameterSymbol;
                    if(CheckParameterSymbol(parameter))
                    {
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray, elementAccessExpressionSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else if (expressionSymbol.Kind == SymbolKind.Method)
                {
                    IMethodSymbol methodSymbol = expressionSymbol as IMethodSymbol;
                    if (CheckMethod(methodSymbol))
                    {
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray, elementAccessExpressionSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else if (expressionSymbol.Kind == SymbolKind.Local)
                {
                    ILocalSymbol localSymbol = expressionSymbol as ILocalSymbol;
                    if(CheckLocalSymbol(localSymbol))
                    {
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray, elementAccessExpressionSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else if (expressionSymbol.Kind == SymbolKind.Field)
                {
                    IFieldSymbol fieldSymbol = expressionSymbol as IFieldSymbol;
                    if (CheckFieldSymbol(fieldSymbol))
                    {
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray, elementAccessExpressionSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private bool CheckParameterSymbol(IParameterSymbol parameterSymbol)
        {
            bool result = false;
            Location location = parameterSymbol.Locations[0];
            SyntaxNode syntaxNode = RolsynExtensions.GetSyntaxNodeFromLocation(solution, location);
            SemanticModel semanticModel = RolsynExtensions.GetSemanticModelFromLocation(solution, location);
            ISymbol workspaceSymbol = semanticModel.GetDeclaredSymbol(syntaxNode);

            ISymbol containerSymbol = workspaceSymbol.ContainingSymbol;
            IEnumerable<ReferencedSymbol> referenceSymbols = SymbolFinder.FindReferencesAsync(containerSymbol, solution).Result;
            RolsynExtensions.IterateReferenceSymbols(referenceSymbols, (ISymbol referenceSymbol, ReferenceLocation referenceLocation) =>
            {
                SyntaxNode referenceNode = RolsynExtensions.GetSyntaxNodeFromLocation(solution, referenceLocation.Location);
                InvocationExpressionSyntax invocationExpressionSyntax = referenceNode.Parent as InvocationExpressionSyntax;
                if(invocationExpressionSyntax != null)
                {
                    RolsynExtensions.IterateArgumentSyntax(invocationExpressionSyntax.ArgumentList, (ArgumentListSyntax argumentListSyntax, ArgumentSyntax argumentSyntax) =>
                    {
                        if (result)
                            return;
                        ISymbol isymbol = semanticModel.GetSymbolInfo(argumentSyntax.Expression).Symbol;
                        if (isymbol is IFieldSymbol )
                            result = CheckFieldSymbol(isymbol as IFieldSymbol);
                    });
                }
            });
            return result;
        }

        private bool CheckFieldSymbol(IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol == null)
                return false;
            return fieldSymbol.IsReadOnly && fieldSymbol.Type.TypeKind == TypeKind.Array;
        }

        private bool CheckMethod(IMethodSymbol methodSymbol)
        {
            Location location = methodSymbol.Locations[0];
            SyntaxNode syntaxNode = RolsynExtensions.GetSyntaxNodeFromLocation(solution, location);

            SemanticModel semanticModel = RolsynExtensions.GetSemanticModelFromLocation(solution, location);
            bool result = false;
            if (syntaxNode is MethodDeclarationSyntax)
            {
                MethodDeclarationSyntax methodDeclarationSyntax = syntaxNode as MethodDeclarationSyntax;
                SyntaxList<StatementSyntax> statements = methodDeclarationSyntax.Body.Statements;
                IEnumerable<ReturnStatementSyntax> returns = statements.OfType<ReturnStatementSyntax>();
                foreach(ReturnStatementSyntax returnSyntax in returns)
                {
                    ISymbol returnSymbol = semanticModel.GetSymbolInfo(returnSyntax.Expression).Symbol;
                    if(returnSymbol is IFieldSymbol)
                    {
                        return CheckFieldSymbol(returnSymbol as IFieldSymbol);
                    }
                }

            }
            ISymbol isymbol = semanticModel.GetDeclaredSymbol(syntaxNode);
            return result;
        }

        private bool CheckLocalSymbol(ILocalSymbol localSymbol)
        {
            Location location = localSymbol.Locations[0];
            SyntaxNode syntaxNode = RolsynExtensions.GetSyntaxNodeFromLocation(solution, location);
            SemanticModel semanticModel = RolsynExtensions.GetSemanticModelFromLocation(solution, location);
            ISymbol isymbol = semanticModel.GetDeclaredSymbol(syntaxNode);
            bool result = false;
            IEnumerable<ReferencedSymbol> referenceSymbols = SymbolFinder.FindReferencesAsync(isymbol, solution).Result;
            RolsynExtensions.IterateReferenceSymbols(referenceSymbols, (ISymbol symbol, ReferenceLocation referenceLocation) =>
            {
                SyntaxNode referenceNode = RolsynExtensions.GetSyntaxNodeFromLocation(solution, referenceLocation.Location);
                SyntaxNode parentNode = referenceNode.Parent;
                if(parentNode is AssignmentExpressionSyntax)
                {
                    AssignmentExpressionSyntax assignmentExpressSyntax = parentNode as AssignmentExpressionSyntax;
                    SyntaxNode rightNode = assignmentExpressSyntax.Right;
                    ISymbol rightSymbol = RolsynExtensions.GetISymbolFromSyntaxNode(solution, rightNode);
                    if (rightSymbol is IFieldSymbol)
                    {
                        result = this.CheckFieldSymbol(rightSymbol as IFieldSymbol);
                        return;
                    }
                }
            });
            return result;
        }

    }
}
