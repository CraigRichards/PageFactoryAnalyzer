using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using VerifyCS = Analyzer2.Test.CSharpCodeFixVerifier<
    Analyzer2.ClassNameToUpperCaseAnalyzer,
    Analyzer2.ClassNameToUpperCaseCodeFixProvider>;

namespace Analyzer2.Test
{
    [TestClass]
    public class ClassNameToUpperCaseUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task AnalyserTest()
        {
            var test = @"
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.PageObjects;

    namespace PageFactorySampleApplication
    {
        public class SamplePom
        {
            private readonly IWebDriver _driver;

            public SamplePom(IWebDriver driver) => _driver = driver;

            [FindsBy(How = How.XPath, Using = ""//someXPath"")]
            private IWebElement theWebElement;
        }
    }";

            var referenceAssemblies = ReferenceAssemblies.Default
                .AddPackages(ImmutableArray.Create(
                        new PackageIdentity("Selenium.Support", "3.141.0"),
                        new PackageIdentity("Selenium.WebDriver", "3.141.0")
                    )
                );

            var diagnosticResult = VerifyCS.Diagnostic(ClassNameToUpperCaseAnalyzer.DiagnosticId)
                .WithSpan(7, 22, 7, 31)
                .WithArguments("SamplePom");

            await new VerifyCS.Test
            {
                ReferenceAssemblies = referenceAssemblies,
                TestState =
                {
                    Sources = { test },
                    ExpectedDiagnostics = { diagnosticResult }
                }
            }.RunAsync();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task AnalyzerFixTest()
        {
            var test = @"
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.PageObjects;

    namespace PageFactorySampleApplication
    {
        public class SamplePom
        {
        }
    }";

            var fixTest = @"
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.PageObjects;

    namespace PageFactorySampleApplication
    {
        public class SAMPLEPOM
        {
        }
    }";

            var referenceAssemblies = ReferenceAssemblies.Default
                .AddPackages(ImmutableArray.Create(
                        new PackageIdentity("Selenium.Support", "3.141.0"),
                        new PackageIdentity("Selenium.WebDriver", "3.141.0")
                    )
                );

            var diagnosticResult = VerifyCS.Diagnostic(ClassNameToUpperCaseAnalyzer.DiagnosticId)
                .WithSpan(7, 22, 7, 31)
                .WithArguments("SamplePom");

            await new VerifyCS.Test
            {
                ReferenceAssemblies = referenceAssemblies,
                TestState =
                {
                    Sources = { test },
                    ExpectedDiagnostics = { diagnosticResult }
                },
                FixedCode = fixTest
            }.RunAsync();
        }
    }
}
