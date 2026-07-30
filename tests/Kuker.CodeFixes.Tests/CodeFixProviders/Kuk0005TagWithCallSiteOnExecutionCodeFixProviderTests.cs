// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Kuker.CodeFixes.CodeFixProviders;
using Kuker.Core.Contants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.CodeFixes.Tests.CodeFixProviders;

public class Kuk0005TagWithCallSiteOnExecutionCodeFixProviderTests
{
    private readonly PortableExecutableReference _portableExecutableReference
        = MetadataReference.CreateFromFile(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location);

    [Theory]
    [InlineData(
        "CodeFixAddsTagWithCallSiteBeforeToListAsync",
        "var users = await {|#0:_appDbContext.Users.ToListAsync()|};",
        "var users = await _appDbContext.Users.TagWithCallSite().ToListAsync();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteAfterQueryMethod",
        "var users = await {|#0:_appDbContext.MyQueryMethod().ToListAsync()|};",
        "var users = await _appDbContext.MyQueryMethod().TagWithCallSite().ToListAsync();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteAfterTwoQueryMethods",
        "var users = await {|#0:_appDbContext.MyQueryMethod().MySecondQueryMethod().ToListAsync()|};",
        "var users = await _appDbContext.MyQueryMethod().MySecondQueryMethod().TagWithCallSite().ToListAsync();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteAfterCustomExtensionBoundary",
        "var users = await {|#0:_appDbContext.Users.Where(x => x.Id > 0).MySecondQueryMethod().ToListAsync()|};",
        "var users = await _appDbContext.Users.Where(x => x.Id > 0).MySecondQueryMethod().TagWithCallSite().ToListAsync();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteInsideNestedAwait",
        "var users = await Task.Run(() => {|#0:_appDbContext.Users.ToListAsync()|});",
        "var users = await Task.Run(() => _appDbContext.Users.TagWithCallSite().ToListAsync());"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForInvocationArgument",
        "var count = ProcessUsers(await {|#0:_appDbContext.Users.ToListAsync()|});",
        "var count = ProcessUsers(await _appDbContext.Users.TagWithCallSite().ToListAsync());"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForReturnValueInvocation",
        "return CreateResponse(await {|#0:_appDbContext.Users.ToListAsync()|});",
        "return CreateResponse(await _appDbContext.Users.TagWithCallSite().ToListAsync());"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForWhereSelectFirstOrDefaultAsync",
        "var userId = await {|#0:_appDbContext.Users.Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefaultAsync()|};",
        "var userId = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefaultAsync();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForWhereSelectFirstOrDefault",
        "var userId = {|#0:_appDbContext.Users.Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefault()|};",
        "var userId = _appDbContext.Users.TagWithCallSite().Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefault();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForMultilineWhereSelectFirstOrDefaultAsync",
        """
        var userId = await {|#0:_appDbContext.Users
            .Where(x => x.Id > 0)
            .Select(x => x.Id)
            .FirstOrDefaultAsync()|};
        """,
        """
        var userId = await _appDbContext.Users
            .TagWithCallSite()
            .Where(x => x.Id > 0)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        """
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForMultilineWhereArgument",
        """
        var users = await {|#0:_appDbContext.Users.Where(
            x => x.Id > 0).ToListAsync()|};
        """,
        """
        var users = await _appDbContext.Users
            .TagWithCallSite().Where(
            x => x.Id > 0).ToListAsync();
        """
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteBeforeAsNoTracking",
        "var users = await {|#0:_appDbContext.Users.AsNoTracking().ToListAsync()|};",
        "var users = await _appDbContext.Users.TagWithCallSite().AsNoTracking().ToListAsync();"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteWhenConfigureAwaitUsed",
        "var users = await {|#0:_appDbContext.Users.ToListAsync()|}.ConfigureAwait(false);",
        "var users = await _appDbContext.Users.TagWithCallSite().ToListAsync().ConfigureAwait(false);"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForToListAsyncWithCancellationToken",
        "var users = await {|#0:_appDbContext.Users.ToListAsync(default)|};",
        "var users = await _appDbContext.Users.TagWithCallSite().ToListAsync(default);"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForFirstOrDefaultAsyncWithPredicateAndCancellationToken",
        "var user = await {|#0:_appDbContext.Users.FirstOrDefaultAsync(x => x.Id > 0, default)|};",
        "var user = await _appDbContext.Users.TagWithCallSite().FirstOrDefaultAsync(x => x.Id > 0, default);"
    )]
    [InlineData(
        "CodeFixAddsTagWithCallSiteForConditionalSourceExpression",
        "var users = await {|#0:(true ? _appDbContext.Users : _appDbContext.Users.Where(x => x.Id > 0)).ToListAsync()|};",
        "var users = await (true ? _appDbContext.Users : _appDbContext.Users.Where(x => x.Id > 0)).TagWithCallSite().ToListAsync();"
    )]
    [InlineData(
        "CodeFixPreservesCommentTriviaInMultilineChain",
        """
        var users = await {|#0:_appDbContext.Users
            // keep comment
            .Where(x => x.Id > 0)
            .ToListAsync()|};
        """,
        """
        var users = await _appDbContext.Users
            .TagWithCallSite()
            // keep comment
            .Where(x => x.Id > 0)
            .ToListAsync();
        """
    )]
    public async Task CodeFixAppliesExpectedChangeAsync(string name, string testCode, string fixedCode)
    {
        _ = name;

        CSharpCodeFixTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, Kuk0005TagWithCallSiteOnExecutionCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = WrapCode(testCode),
            FixedCode = WrapCode(fixedCode),
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixFixAllInDocumentAppliesToAllDiagnosticsAsync()
    {
        string testCode = WrapCode(
            """
            var users = await {|#0:_appDbContext.Users.ToListAsync()|};
            var userId = await {|#1:_appDbContext.Users.Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefaultAsync()|};
            """
        );

        string fixedCode = WrapCode(
            """
            var users = await _appDbContext.Users.TagWithCallSite().ToListAsync();
            var userId = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefaultAsync();
            """
        );

        CSharpCodeFixTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, Kuk0005TagWithCallSiteOnExecutionCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            CodeActionEquivalenceKey = "Add .TagWithCallSite()",
            NumberOfFixAllIterations = 1,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(1));

        await test.RunAsync();
    }

    [Fact]
    public async Task CodeFixFixAllInDocumentAppliesToMixedChainShapesAsync()
    {
        string testCode = WrapCode(
            """
            var users = await {|#0:_appDbContext.Users.ToListAsync()|};
            var trackedUsers = await {|#1:_appDbContext.Users.AsNoTracking().ToListAsync()|};
            var conditionalUsers = await {|#2:(true ? _appDbContext.Users : _appDbContext.Users.Where(x => x.Id > 0)).ToListAsync()|};
            var branchUsers = true ? await {|#3:_appDbContext.Users.ToListAsync()|} : await {|#4:_appDbContext.Users.Where(x => x.Id > 0).ToListAsync()|};
            """
        );

        string fixedCode = WrapCode(
            """
            var users = await _appDbContext.Users.TagWithCallSite().ToListAsync();
            var trackedUsers = await _appDbContext.Users.TagWithCallSite().AsNoTracking().ToListAsync();
            var conditionalUsers = await (true ? _appDbContext.Users : _appDbContext.Users.Where(x => x.Id > 0)).TagWithCallSite().ToListAsync();
            var branchUsers = true ? await _appDbContext.Users.TagWithCallSite().ToListAsync() : await _appDbContext.Users.TagWithCallSite().Where(x => x.Id > 0).ToListAsync();
            """
        );

        CSharpCodeFixTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, Kuk0005TagWithCallSiteOnExecutionCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            CodeActionEquivalenceKey = "Add .TagWithCallSite()",
            NumberOfFixAllIterations = 1,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(1));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(2));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(3));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(4));

        await test.RunAsync();
    }

    [Theory]
    [InlineData(
        "CodeFixAppliesInlineStyleFromEditorConfig",
        "inline",
        """
        var userId = await {|#0:_appDbContext.Users
            .Where(x => x.Id > 0)
            .Select(x => x.Id)
            .FirstOrDefaultAsync()|};
        """,
        """
        var userId = await _appDbContext.Users.TagWithCallSite()
            .Where(x => x.Id > 0)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        """
    )]
    [InlineData(
        "CodeFixAppliesInlineStyleFromEditorConfigForSingleLineQuery",
        "inline",
        "var userId = await {|#0:_appDbContext.Users.Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefaultAsync()|};",
        "var userId = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefaultAsync();"
    )]
    [InlineData(
        "CodeFixAppliesNewlineStyleFromEditorConfig",
        "newline",
        """
        var userId = await {|#0:_appDbContext.Users
            .Where(x => x.Id > 0)
            .Select(x => x.Id)
            .FirstOrDefaultAsync()|};
        """,
        """
        var userId = await _appDbContext.Users
            .TagWithCallSite()
            .Where(x => x.Id > 0)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        """
    )]
    [InlineData(
        "CodeFixAppliesNewlineStyleForSplitSourceAndExecution",
        "newline",
        """
        var user = await {|#0:_appDbContext.Users
            .FirstOrDefaultAsync(x => x.Id > 0)|};
        """,
        """
        var user = await _appDbContext.Users
            .TagWithCallSite()
            .FirstOrDefaultAsync(x => x.Id > 0);
        """
    )]
    public async Task CodeFixAppliesConfiguredStyleFromEditorConfigAsync(string name, string codeFixStyle, string testCode, string fixedCode)
    {
        _ = name;

        CSharpCodeFixTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, Kuk0005TagWithCallSiteOnExecutionCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = WrapCode(testCode),
            FixedCode = WrapCode(fixedCode),
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $"""
            root = true

            [*.cs]
            dotnet_diagnostic.KUK0005.code_fix_style = {codeFixStyle}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Theory]
    [InlineData("foobar")]
    [InlineData("INVALID")]
    public async Task CodeFixDoesNotApplyWhenStyleOptionIsInvalidAsync(string invalidStyle)
    {
        string testCode = WrapCode("var users = await {|#0:_appDbContext.Users.ToListAsync()|};");

        CSharpCodeFixTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, Kuk0005TagWithCallSiteOnExecutionCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            FixedCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            $"""
            root = true

            [*.cs]
            dotnet_diagnostic.KUK0005.code_fix_style = {invalidStyle}
            """
        ));

        test.ExpectedDiagnostics.Add(new DiagnosticResult(DiagnosticIdContant.KUK0005, DiagnosticSeverity.Warning).WithLocation(0).WithArguments(invalidStyle));

        await test.RunAsync();
    }

    private static string WrapCode(string code)
    {
        string normalizedCode = code.ReplaceLineEndings(Environment.NewLine).Trim('\r', '\n');
        string indentedCode = normalizedCode.Replace(Environment.NewLine, $"{Environment.NewLine}        ", StringComparison.Ordinal);

        string template = """
            using System.Linq;
            using System.Threading.Tasks;
            using Microsoft.EntityFrameworkCore;

            public class TestClass
            {
                private readonly AppDbContext _appDbContext;

                public TestClass(AppDbContext appDbContext)
                {
                    _appDbContext = appDbContext;
                }

                public async Task<object> M1()
                {
                    __CODE__
                    return null;
                }

                private static int ProcessUsers(object users)
                {
                    return 0;
                }

                private static object CreateResponse(object users)
                {
                    return users;
                }
            }

            public class User
            {
                public long Id { get; set; }
            }

            public class AppDbContext : DbContext
            {
                public AppDbContext(DbContextOptions<AppDbContext> options)
                    : base(options)
                {
                }

                public DbSet<User> Users { get; set; }

                public IQueryable<User> MyQueryMethod()
                {
                    return Users.Where(x => x.Id > 0);
                }
            }

            public static class UserQueryExtensions
            {
                public static IQueryable<User> MySecondQueryMethod(this IQueryable<User> query)
                {
                    return query.Where(x => x.Id > 1);
                }
            }
            """;

        return template.Replace("__CODE__", indentedCode, StringComparison.Ordinal);
    }
}
