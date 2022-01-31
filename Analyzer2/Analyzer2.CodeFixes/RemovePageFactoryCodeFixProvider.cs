using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;

namespace Analyzer2
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemovePageFactoryCodeFixProvider))]
    [Shared]
    public class RemovePageFactoryCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(RemovePageFactoryAnalyzer.DiagnosticId);

        //public string S => "s";
        //private string theWebElement => throw new NotImplementedException();
        //public IWebDriver theWebElement => throw new NotImplementedException();
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            //context.RegisterCodeFix(
            //    CodeAction.Create(
            //        title: CodeFixResources.CodeFixTitle,
            //        createChangedSolution: c => RemovePageFactoryAsync(context.Document, declaration, c),
            //        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
            //    diagnostic);

            var statement = root.FindNode(diagnosticSpan);

            context.RegisterCodeFix(
                CodeAction.Create("Remove PageFactory Pattern",
                    x => UsePropertyAsync(context.Document, statement),
                    RemovePageFactoryAnalyzer.DiagnosticId),
                diagnostic);
        }

        public PropertyDeclarationSyntax ConvertToResourceProperty(string resouceClassIdentifier, string fieldName,
            string resourceKey, CSharpSyntaxNode field)
        {
            var stringType = SyntaxFactory.ParseTypeName("string");

            var resourceClassName = SyntaxFactory.IdentifierName(resouceClassIdentifier);
            var resourceKeyName = SyntaxFactory.IdentifierName(resourceKey);
            var memberaccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                resourceClassName, 
                resourceKeyName);

            var propertyLambda = SyntaxFactory.ArrowExpressionClause(memberaccess);

            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    new SyntaxList<AttributeListSyntax>(),
                    new SyntaxTokenList(),
                    stringType, null, SyntaxFactory.Identifier(fieldName), null,
                    propertyLambda, null, SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)).WithAdditionalAnnotations(Formatter.Annotation)
                .NormalizeWhitespace();

            return propertyDeclaration.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithLeadingTrivia(field.GetLeadingTrivia().ToArray())
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        private ArrowExpressionClauseSyntax CreateReturnExpression(FieldDeclarationSyntax fieldDeclaration)
        {
            var findsByAttribute = fieldDeclaration.AttributeLists
                .SelectMany(x => x.Attributes)
                .FirstOrDefault(x => ((IdentifierNameSyntax)x.Name).Identifier.Value.Equals("FindsBy"));

            var howValue = findsByAttribute.ArgumentList.Arguments
                .Single(x => x.NameEquals.Name.ToString().Equals("How"))
                .Expression.ToString();

            var usingValue = findsByAttribute.ArgumentList.Arguments
                .Single(x => x.NameEquals.Name.ToString().Equals("Using"))
                .Expression.ToString();

            var returnExpression = $"_driver.FindElement(By.{howValue.Split('.')[1]}({usingValue}))";

            return SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ParseExpression(
                    returnExpression));

            //private IWebElement theWebElement => _driver.FindElement(By.XPath("//someXPath"));
        }

        private async Task<Solution> UsePropertyAsync(Document document, SyntaxNode statement)
        {

            var fieldDeclaration = statement.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
            var fieldName = fieldDeclaration.Declaration.Variables
                .First().Identifier.ValueText;

            //CreateReturnExpression(fieldDeclaration);
            //var variableDeclarator = statement.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First();
            //var fieldStatement = variableDeclarator.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
            //var variableDeclaration = variableDeclarator.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().First();

            var blockSyntax = SyntaxFactory.Block(SyntaxFactory.ParseStatement("throw new NotImplementedException();"));
            var expressionStatement =
                SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression("throw new NotImplementedException()"));
            var expressionBody = SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ParseExpression("throw new NotImplementedException()"));

            var expressionSyntax = SyntaxFactory.ParseExpression("public string S => \"s\";");
            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    new SyntaxList<AttributeListSyntax>(),
                    fieldDeclaration.Modifiers,
                    SyntaxFactory.ParseTypeName("IWebDriver"), 
                    null, 
                    SyntaxFactory.Identifier(fieldName), 
                    null,
                    CreateReturnExpression(fieldDeclaration), 
                    null, 
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithAdditionalAnnotations(Formatter.Annotation)
                .NormalizeWhitespace();


            var newProperty = SyntaxFactory.PropertyDeclaration(
                    fieldDeclaration.Declaration.Type,
                    fieldName)

                //.WithExpressionBody(expressionBody)
                //.WithAttributeLists(fieldStatement.AttributeLists)
                .WithModifiers(fieldDeclaration.Modifiers)
                //.WithAdditionalAnnotations(Formatter.Annotation)
                //.WithExpressionBody(eb)
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List(new[]
                        {
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithExpressionBody(expressionBody)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            //SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            //    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        })));
            

            var editor = await DocumentEditor.CreateAsync(document);
            //editor.InsertAfter(statement, newProperty);
            editor.InsertAfter(statement, propertyDeclaration);
            editor.RemoveNode(statement);
            //editor.RemoveNode(fieldDeclaration);
            return editor.GetChangedDocument().Project.Solution;
        }

        private async Task<Solution> RemovePageFactoryAsync(Document document, TypeDeclarationSyntax typeDecl,
            CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer
                .RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken)
                .ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}