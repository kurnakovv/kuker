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
            }
            """;

        return template.Replace("__CODE__", indentedCode, StringComparison.Ordinal);
    }
}
