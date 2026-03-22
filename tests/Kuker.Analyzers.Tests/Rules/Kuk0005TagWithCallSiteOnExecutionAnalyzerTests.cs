// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Kuker.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Kuker.Analyzers.Tests.Rules;

public class Kuk0005TagWithCallSiteOnExecutionAnalyzerTests
{
    private readonly PortableExecutableReference _portableExecutableReference
        = MetadataReference.CreateFromFile(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location);

#pragma warning disable RCS0053, SA1117 // Fix formatting of a list
    [Theory]
    [InlineData("var simpleToListOK = await _appDbContext.Users.TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var simpleToListViolation = await _appDbContext.Users.ToListAsync();", 35, 47, 35, 80)]
    [InlineData("var whereStartToListOK = await _appDbContext.Users.Where(x => x.Id == 5).TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereEndToListOK = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id == 5).ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereToListViolation = await _appDbContext.Users.Where(x => x.Id == 5).ToListAsync();", 35, 46, 35, 101)]
    [InlineData("var whereAndSelectStartToListOK = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id == 5).Select(x => x.Name).ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereAndSelectMiddleToListOK = await _appDbContext.Users.Where(x => x.Id == 5).TagWithCallSite().Select(x => x.Name).ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereAndSelectEndToListOK = await _appDbContext.Users.Where(x => x.Id == 5).Select(x => x.Name).TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereAndSelectToListViolation = await _appDbContext.Users.Where(x => x.Id == 5).Select(x => x.Name).ToListAsync();", 35, 55, 35, 130)]
    [InlineData("/* Concat query (violation) */ var query = _appDbContext.Users.TagWithCallSite(); var callQuery = await query.ToListAsync();", 35, 117, 35, 136)]
    [InlineData("/* Concat query (OK) */ var query = _appDbContext.Users.TagWithCallSite(); var callQuery = await query.TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("/* Concat query without TagWithCallSite (violation) */ var query = _appDbContext.Users; var callQuery = await query.ToListAsync();", 35, 123, 35, 142)]
    [InlineData("/* Concat query with TagWithCallSite (OK) */ var query = _appDbContext.Users; var callQuery = await query.TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var queryMethodViolation = await GetUserQuery().ToListAsync();", 35, 46, 35, 74)]
    [InlineData("var queryMethodOK = await GetUserQuery().TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var tagWithCallSiteAfterExecuteViolation = (await _appDbContext.Users.ToListAsync()).AsQueryable().TagWithCallSite();", 35, 63, 35, 96)]
    [InlineData("var toListNotEFQueryable = new List<User>().AsQueryable().ToList();", 0, 0, 0, 0)]
    [InlineData("var toListNotEF = new List<User>().ToList();", 0, 0, 0, 0)]
    [InlineData("var simpleToListSyncOK = _appDbContext.Users.TagWithCallSite().ToList();", 0, 0, 0, 0)]
    [InlineData("var simpleToListSyncViolation = _appDbContext.Users.ToList();", 35, 45, 35, 73)]
    [InlineData("var firstSyncOK = _appDbContext.Users.TagWithCallSite().First();", 0, 0, 0, 0)]
    [InlineData("var firstSyncViolation = _appDbContext.Users.First();", 35, 38, 35, 65)]
    [InlineData("var firstAsyncOK = await _appDbContext.Users.TagWithCallSite().FirstAsync();", 0, 0, 0, 0)]
    [InlineData("var firstAsyncViolation = await _appDbContext.Users.FirstAsync();", 35, 45, 35, 77)]
    [InlineData("var firstOrDefaultSyncOK = _appDbContext.Users.TagWithCallSite().FirstOrDefault();", 0, 0, 0, 0)]
    [InlineData("var firstOrDefaultSyncViolation = _appDbContext.Users.FirstOrDefault();", 35, 47, 35, 83)]
    [InlineData("var firstOrDefaultAsyncOK = await _appDbContext.Users.TagWithCallSite().FirstOrDefaultAsync();", 0, 0, 0, 0)]
    [InlineData("var firstOrDefaultAsyncViolation = await _appDbContext.Users.FirstOrDefaultAsync();", 35, 54, 35, 95)]
    [InlineData("var singleSyncOK = _appDbContext.Users.TagWithCallSite().Single();", 0, 0, 0, 0)]
    [InlineData("var singleSyncViolation = _appDbContext.Users.Single();", 35, 39, 35, 67)]
    [InlineData("var singleAsyncOK = await _appDbContext.Users.TagWithCallSite().SingleAsync();", 0, 0, 0, 0)]
    [InlineData("var singleAsyncViolation = await _appDbContext.Users.SingleAsync();", 35, 46, 35, 79)]
    [InlineData("var singleOrDefaultSyncOK = _appDbContext.Users.TagWithCallSite().SingleOrDefault();", 0, 0, 0, 0)]
    [InlineData("var singleOrDefaultSyncViolation = _appDbContext.Users.SingleOrDefault();", 35, 48, 35, 85)]
    [InlineData("var singleOrDefaultAsyncOK = await _appDbContext.Users.TagWithCallSite().SingleOrDefaultAsync();", 0, 0, 0, 0)]
    [InlineData("var singleOrDefaultAsyncViolation = await _appDbContext.Users.SingleOrDefaultAsync();", 35, 55, 35, 97)]
    [InlineData("var anySyncOK = _appDbContext.Users.TagWithCallSite().Any();", 0, 0, 0, 0)]
    [InlineData("var anySyncViolation = _appDbContext.Users.Any();", 35, 36, 35, 61)]
    [InlineData("var anyAsyncOK = await _appDbContext.Users.TagWithCallSite().AnyAsync();", 0, 0, 0, 0)]
    [InlineData("var anyAsyncViolation = await _appDbContext.Users.AnyAsync();", 35, 43, 35, 73)]
    [InlineData("var countSyncOK = _appDbContext.Users.TagWithCallSite().Count();", 0, 0, 0, 0)]
    [InlineData("var countSyncViolation = _appDbContext.Users.Count();", 35, 38, 35, 65)]
    [InlineData("var countAsyncOK = await _appDbContext.Users.TagWithCallSite().CountAsync();", 0, 0, 0, 0)]
    [InlineData("var countAsyncViolation = await _appDbContext.Users.CountAsync();", 35, 45, 35, 77)]
    [InlineData("var sumSyncOK = _appDbContext.Users.TagWithCallSite().Sum(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var sumSyncViolation = _appDbContext.Users.Sum(x => x.Age);", 35, 36, 35, 71)]
    [InlineData("var sumAsyncOK = await _appDbContext.Users.TagWithCallSite().SumAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var sumAsyncViolation = await _appDbContext.Users.SumAsync(x => x.Age);", 35, 43, 35, 83)]
    [InlineData("var minSyncOK = _appDbContext.Users.TagWithCallSite().Min(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var minSyncViolation = _appDbContext.Users.Min(x => x.Age);", 35, 36, 35, 71)]
    [InlineData("var minAsyncOK = await _appDbContext.Users.TagWithCallSite().MinAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var minAsyncViolation = await _appDbContext.Users.MinAsync(x => x.Age);", 35, 43, 35, 83)]
    [InlineData("var maxSyncOK = _appDbContext.Users.TagWithCallSite().Max(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var maxSyncViolation = _appDbContext.Users.Max(x => x.Age);", 35, 36, 35, 71)]
    [InlineData("var maxAsyncOK = await _appDbContext.Users.TagWithCallSite().MaxAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var maxAsyncViolation = await _appDbContext.Users.MaxAsync(x => x.Age);", 35, 43, 35, 83)]
    [InlineData("var avgSyncOK = _appDbContext.Users.TagWithCallSite().Average(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var avgSyncViolation = _appDbContext.Users.Average(x => x.Age);", 35, 36, 35, 75)]
    [InlineData("var avgAsyncOK = await _appDbContext.Users.TagWithCallSite().AverageAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var avgAsyncViolation = await _appDbContext.Users.AverageAsync(x => x.Age);", 35, 43, 35, 87)]
    [InlineData("var longCountSyncOK = _appDbContext.Users.TagWithCallSite().LongCount();", 0, 0, 0, 0)]
    [InlineData("var longCountSyncViolation = _appDbContext.Users.LongCount();", 35, 42, 35, 73)]
    [InlineData("var longCountAsyncOK = await _appDbContext.Users.TagWithCallSite().LongCountAsync();", 0, 0, 0, 0)]
    [InlineData("var longCountAsyncViolation = await _appDbContext.Users.LongCountAsync();", 35, 49, 35, 85)]
    [InlineData("var allSyncOK = _appDbContext.Users.TagWithCallSite().All(x => x.Age > 18);", 0, 0, 0, 0)]
    [InlineData("var allSyncViolation = _appDbContext.Users.All(x => x.Age > 18);", 35, 36, 35, 76)]
    [InlineData("var allAsyncOK = await _appDbContext.Users.TagWithCallSite().AllAsync(x => x.Age > 18);", 0, 0, 0, 0)]
    [InlineData("var allAsyncViolation = await _appDbContext.Users.AllAsync(x => x.Age > 18);", 35, 43, 35, 88)]
    [InlineData("var dictSyncOK = _appDbContext.Users.TagWithCallSite().ToDictionary(x => x.Id);", 0, 0, 0, 0)]
    [InlineData("var dictSyncViolation = _appDbContext.Users.ToDictionary(x => x.Id);", 35, 37, 35, 80)]
    [InlineData("var dictAsyncOK = await _appDbContext.Users.TagWithCallSite().ToDictionaryAsync(x => x.Id);", 0, 0, 0, 0)]
    [InlineData("var dictAsyncViolation = await _appDbContext.Users.ToDictionaryAsync(x => x.Id);", 35, 44, 35, 92)]
    [InlineData("var hashSetSyncOK = _appDbContext.Users.TagWithCallSite().ToHashSet();", 0, 0, 0, 0)]
    [InlineData("var hashSetSyncViolation = _appDbContext.Users.ToHashSet();", 35, 40, 35, 71)]
    [InlineData("var hashSetAsyncOK = await _appDbContext.Users.TagWithCallSite().ToHashSetAsync();", 0, 0, 0, 0)]
    [InlineData("var hashSetAsyncViolation = await _appDbContext.Users.ToHashSetAsync();", 35, 47, 35, 83)]
    [InlineData("var lastSyncOK = _appDbContext.Users.TagWithCallSite().Last();", 0, 0, 0, 0)]
    [InlineData("var lastSyncViolation = _appDbContext.Users.Last();", 35, 37, 35, 63)]
    [InlineData("var lastAsyncOK = await _appDbContext.Users.TagWithCallSite().LastAsync();", 0, 0, 0, 0)]
    [InlineData("var lastAsyncViolation = await _appDbContext.Users.LastAsync();", 35, 44, 35, 75)]
    [InlineData("var lastOrDefaultSyncOK = _appDbContext.Users.TagWithCallSite().LastOrDefault();", 0, 0, 0, 0)]
    [InlineData("var lastOrDefaultSyncViolation = _appDbContext.Users.LastOrDefault();", 35, 46, 35, 81)]
    [InlineData("var lastOrDefaultAsyncOK = await _appDbContext.Users.TagWithCallSite().LastOrDefaultAsync();", 0, 0, 0, 0)]
    [InlineData("var lastOrDefaultAsyncViolation = await _appDbContext.Users.LastOrDefaultAsync();", 35, 53, 35, 93)]
    [InlineData("await _appDbContext.Users.TagWithCallSite().ExecuteDeleteAsync();", 0, 0, 0, 0)]
    [InlineData("await _appDbContext.Users.ExecuteDeleteAsync();", 35, 19, 35, 59)]
    [InlineData("await _appDbContext.Users.TagWithCallSite().ExecuteUpdateAsync(s => s.SetProperty(x => x.Age, 30));", 0, 0, 0, 0)]
    [InlineData("await _appDbContext.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.Age, 30));", 35, 19, 35, 93)]
    [InlineData("var asEnumerableSyncOK = _appDbContext.Users.TagWithCallSite().AsEnumerable();", 0, 0, 0, 0)]
    [InlineData("var asEnumerableSyncViolation = _appDbContext.Users.AsEnumerable();", 35, 45, 35, 79)]
    [InlineData("var asAsyncEnumerableOK = _appDbContext.Users.TagWithCallSite().AsAsyncEnumerable();", 0, 0, 0, 0)]
    [InlineData("var asAsyncEnumerableViolation = _appDbContext.Users.AsAsyncEnumerable();", 35, 46, 35, 85)]
    [InlineData("""
       await _appDbContext.Users
          .Where(x => x.Id == 1)
          .Where(x => x.Name == "Test")
          .Where(x => x.Age > 10)
          .ExecuteUpdateAsync(
              s => s
                  .SetProperty(x => x.Name, x => "Test")
                  .SetProperty(x => x.Age, x => 10),
              CancellationToken.None
          );
       """, 35, 19, 44, 5)]
#pragma warning restore RCS0053,SA1117 // Fix formatting of a list
    public async Task RunAsync(string inputQuery, int startLine, int startColumn, int endLine, int endColumn)
    {
        string testCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            using Microsoft.EntityFrameworkCore;

            public class User
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
            }

            public class AppDbContext : DbContext
            {
                public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

                public DbSet<User> Users { get; set; }
            }

            public class TestClass
            {
                private readonly AppDbContext _appDbContext;

                public TestClass(AppDbContext appDbContext)
                {
                    _appDbContext = appDbContext;
                }

                public async Task M1()
                {
                    {%inputQuery%}
                }

                private IQueryable<User> GetUserQuery()
                {
                    return _appDbContext.Users.TagWithCallSite();
                }
            }
        """.Replace("{%inputQuery%}", inputQuery, StringComparison.OrdinalIgnoreCase);

        CSharpAnalyzerTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        if (!(startLine == 0 && startColumn == 0 && endLine == 0 && endColumn == 0))
        {
            DiagnosticResult expected = new DiagnosticResult("KUK0005", DiagnosticSeverity.Warning)
                .WithSpan(startLine, startColumn, endLine, endColumn);

            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }

    [Fact]
    public async Task NoReportForEmptyChainAsync()
    {
        string testCode = """
            using System.Linq;

            public class TestClass
            {
                public void M1()
                {
                    var x = .ToList();
                }
            }
        """;

        CSharpAnalyzerTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
            ExpectedDiagnostics = { new DiagnosticResult("CS1525", DiagnosticSeverity.Error).WithSpan(7, 21, 7, 22) },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState = { AdditionalReferences = { _portableExecutableReference }, },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task TestIQueryableWithoutEFCoreAsync()
    {
        string testCode = """
            using System.Linq;

            class Test
            {
                void M(IQueryable<int> q)
                {
                    q.ToList();
                }
            }
        """;

        CSharpAnalyzerTest<Kuk0005TagWithCallSiteOnExecutionAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = testCode,
        };

        await test.RunAsync();
    }
}
