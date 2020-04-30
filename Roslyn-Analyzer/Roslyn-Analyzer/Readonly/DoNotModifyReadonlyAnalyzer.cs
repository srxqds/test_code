using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Immutable;
using System;
using RoslynAnalyzer.CLI;

namespace RoslynAnalyzer.Readonly
{
    // 查找所有引用field的syntaxnode
    // 查找所有有修改的syntaxnode
    // 
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotModifyReadonlyAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            context.RegisterCompilationAction(AnalyzeCompilation);
            context.RegisterCompilationStartAction(AnalyzeStartCompilation);
            context.RegisterCodeBlockAction(AnalyzeCodeBlock);
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
            context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
        }

        private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
        {
            if (context.OwningSymbol.Kind != SymbolKind.Method)
            {
                return;
            }
            SyntaxNode syntaxNode = context.CodeBlock;
            ISymbol symbol = context.OwningSymbol;
            SemanticModel semanticModel = context.SemanticModel;
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.ElementAccessExpression);
        }
        

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            
            ElementAccessExpressionSyntax elementAccessExpressionSyntax = context.Node as ElementAccessExpressionSyntax;
            if(elementAccessExpressionSyntax != null)
            {
                ISymbol expressionSymbol = context.SemanticModel.GetSymbolInfo(elementAccessExpressionSyntax.Expression).Symbol;
                if (expressionSymbol.Kind == SymbolKind.Parameter)
                {
                    IParameterSymbol parameter = expressionSymbol as IParameterSymbol;
                }
                else if (expressionSymbol.Kind == SymbolKind.Method)
                {
                    IMethodSymbol method = expressionSymbol as IMethodSymbol;
                }
                else if (expressionSymbol.Kind == SymbolKind.Local)
                {
                    ILocalSymbol local = expressionSymbol as ILocalSymbol;
                }
                else if (expressionSymbol.Kind == SymbolKind.Field && (expressionSymbol as IFieldSymbol).IsReadOnly)
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DoNotModifyReadonlyArray, elementAccessExpressionSyntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
        }


        private static void AnalyzeStartCompilation(CompilationStartAnalysisContext context)
        {
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {

        }

        private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
        {
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {

        }


    }
}
