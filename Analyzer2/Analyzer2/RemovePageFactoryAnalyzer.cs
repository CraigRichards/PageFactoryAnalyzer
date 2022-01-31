using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer2
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemovePageFactoryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RemovePageFactoryAnalyzer";
        private const string Category = "Naming";

        private static readonly LocalizableString Title = @"PageFactory Pattern should be removed";

        private static readonly LocalizableString MessageFormat = @"Field name '{0}' contains [FindsBy] attribute";

        private static readonly LocalizableString Description = @"Field names should be converted to Get Property.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Error, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            var name = fieldDeclaration.GetText();
            
            if (!fieldDeclaration.AttributeLists.Any()) return;
            var findsByAttribute = fieldDeclaration.AttributeLists
                .SelectMany(x => x.Attributes)
                .FirstOrDefault(x => ((IdentifierNameSyntax)x.Name).Identifier.Value.Equals("FindsBy"));

            if (findsByAttribute == null) return;

            var diagnostic = Diagnostic.Create(Rule, fieldDeclaration.GetLocation(), context.ContainingSymbol.MetadataName);
            context.ReportDiagnostic(diagnostic);
        }

        //private static void AnalyzeSymbol(SymbolAnalysisContext context)
        //{
        //    // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
        //    var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        //    // Find just those named type symbols with names containing lowercase letters.
        //    if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
        //    {
        //        // For all such symbols, produce a diagnostic.
        //        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

        //        context.ReportDiagnostic(diagnostic);
        //    }
        //}
    }
}