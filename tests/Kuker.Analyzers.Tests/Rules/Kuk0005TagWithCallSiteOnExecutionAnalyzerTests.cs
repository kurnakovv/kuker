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
    [InlineData("var simpleToListViolation = await _appDbContext.Users.ToListAsync();", 13, 47, 13, 80)]
    [InlineData("var whereStartToListOK = await _appDbContext.Users.Where(x => x.Id == 5).TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereEndToListOK = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id == 5).ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereToListViolation = await _appDbContext.Users.Where(x => x.Id == 5).ToListAsync();", 13, 46, 13, 101)]
    [InlineData("var whereAndSelectStartToListOK = await _appDbContext.Users.TagWithCallSite().Where(x => x.Id == 5).Select(x => x.Name).ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereAndSelectMiddleToListOK = await _appDbContext.Users.Where(x => x.Id == 5).TagWithCallSite().Select(x => x.Name).ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereAndSelectEndToListOK = await _appDbContext.Users.Where(x => x.Id == 5).Select(x => x.Name).TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var whereAndSelectToListViolation = await _appDbContext.Users.Where(x => x.Id == 5).Select(x => x.Name).ToListAsync();", 13, 55, 13, 130)]
    [InlineData("/* Concat query (violation) */ var query = _appDbContext.Users.TagWithCallSite(); var callQuery = await query.ToListAsync();", 13, 117, 13, 136)]
    [InlineData("/* Concat query (OK) */ var query = _appDbContext.Users.TagWithCallSite(); var callQuery = await query.TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("/* Concat query without TagWithCallSite (violation) */ var query = _appDbContext.Users; var callQuery = await query.ToListAsync();", 13, 123, 13, 142)]
    [InlineData("/* Concat query with TagWithCallSite (OK) */ var query = _appDbContext.Users; var callQuery = await query.TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var queryMethodViolation = await GetUserQuery().ToListAsync();", 13, 46, 13, 74)]
    [InlineData("var queryMethodOK = await GetUserQuery().TagWithCallSite().ToListAsync();", 0, 0, 0, 0)]
    [InlineData("var tagWithCallSiteAfterExecuteViolation = (await _appDbContext.Users.ToListAsync()).AsQueryable().TagWithCallSite();", 13, 63, 13, 96)]
    [InlineData("var toListNotEFQueryable = new List<User>().AsQueryable().ToList();", 0, 0, 0, 0)]
    [InlineData("var toListNotEF = new List<User>().ToList();", 0, 0, 0, 0)]
    [InlineData("var simpleToListSyncOK = _appDbContext.Users.TagWithCallSite().ToList();", 0, 0, 0, 0)]
    [InlineData("var simpleToListSyncViolation = _appDbContext.Users.ToList();", 13, 45, 13, 73)]
    [InlineData("var firstSyncOK = _appDbContext.Users.TagWithCallSite().First();", 0, 0, 0, 0)]
    [InlineData("var firstSyncViolation = _appDbContext.Users.First();", 13, 38, 13, 65)]
    [InlineData("var firstAsyncOK = await _appDbContext.Users.TagWithCallSite().FirstAsync();", 0, 0, 0, 0)]
    [InlineData("var firstAsyncViolation = await _appDbContext.Users.FirstAsync();", 13, 45, 13, 77)]
    [InlineData("var firstOrDefaultSyncOK = _appDbContext.Users.TagWithCallSite().FirstOrDefault();", 0, 0, 0, 0)]
    [InlineData("var firstOrDefaultSyncViolation = _appDbContext.Users.FirstOrDefault();", 13, 47, 13, 83)]
    [InlineData("var firstOrDefaultAsyncOK = await _appDbContext.Users.TagWithCallSite().FirstOrDefaultAsync();", 0, 0, 0, 0)]
    [InlineData("var firstOrDefaultAsyncViolation = await _appDbContext.Users.FirstOrDefaultAsync();", 13, 54, 13, 95)]
    [InlineData("var singleSyncOK = _appDbContext.Users.TagWithCallSite().Single();", 0, 0, 0, 0)]
    [InlineData("var singleSyncViolation = _appDbContext.Users.Single();", 13, 39, 13, 67)]
    [InlineData("var singleAsyncOK = await _appDbContext.Users.TagWithCallSite().SingleAsync();", 0, 0, 0, 0)]
    [InlineData("var singleAsyncViolation = await _appDbContext.Users.SingleAsync();", 13, 46, 13, 79)]
    [InlineData("var singleOrDefaultSyncOK = _appDbContext.Users.TagWithCallSite().SingleOrDefault();", 0, 0, 0, 0)]
    [InlineData("var singleOrDefaultSyncViolation = _appDbContext.Users.SingleOrDefault();", 13, 48, 13, 85)]
    [InlineData("var singleOrDefaultAsyncOK = await _appDbContext.Users.TagWithCallSite().SingleOrDefaultAsync();", 0, 0, 0, 0)]
    [InlineData("var singleOrDefaultAsyncViolation = await _appDbContext.Users.SingleOrDefaultAsync();", 13, 55, 13, 97)]
    [InlineData("var anySyncOK = _appDbContext.Users.TagWithCallSite().Any();", 0, 0, 0, 0)]
    [InlineData("var anySyncViolation = _appDbContext.Users.Any();", 13, 36, 13, 61)]
    [InlineData("var anyAsyncOK = await _appDbContext.Users.TagWithCallSite().AnyAsync();", 0, 0, 0, 0)]
    [InlineData("var anyAsyncViolation = await _appDbContext.Users.AnyAsync();", 13, 43, 13, 73)]
    [InlineData("var countSyncOK = _appDbContext.Users.TagWithCallSite().Count();", 0, 0, 0, 0)]
    [InlineData("var countSyncViolation = _appDbContext.Users.Count();", 13, 38, 13, 65)]
    [InlineData("var countAsyncOK = await _appDbContext.Users.TagWithCallSite().CountAsync();", 0, 0, 0, 0)]
    [InlineData("var countAsyncViolation = await _appDbContext.Users.CountAsync();", 13, 45, 13, 77)]
    [InlineData("var sumSyncOK = _appDbContext.Users.TagWithCallSite().Sum(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var sumSyncViolation = _appDbContext.Users.Sum(x => x.Age);", 13, 36, 13, 71)]
    [InlineData("var sumAsyncOK = await _appDbContext.Users.TagWithCallSite().SumAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var sumAsyncViolation = await _appDbContext.Users.SumAsync(x => x.Age);", 13, 43, 13, 83)]
    [InlineData("var minSyncOK = _appDbContext.Users.TagWithCallSite().Min(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var minSyncViolation = _appDbContext.Users.Min(x => x.Age);", 13, 36, 13, 71)]
    [InlineData("var minAsyncOK = await _appDbContext.Users.TagWithCallSite().MinAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var minAsyncViolation = await _appDbContext.Users.MinAsync(x => x.Age);", 13, 43, 13, 83)]
    [InlineData("var maxSyncOK = _appDbContext.Users.TagWithCallSite().Max(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var maxSyncViolation = _appDbContext.Users.Max(x => x.Age);", 13, 36, 13, 71)]
    [InlineData("var maxAsyncOK = await _appDbContext.Users.TagWithCallSite().MaxAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var maxAsyncViolation = await _appDbContext.Users.MaxAsync(x => x.Age);", 13, 43, 13, 83)]
    [InlineData("var avgSyncOK = _appDbContext.Users.TagWithCallSite().Average(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var avgSyncViolation = _appDbContext.Users.Average(x => x.Age);", 13, 36, 13, 75)]
    [InlineData("var avgAsyncOK = await _appDbContext.Users.TagWithCallSite().AverageAsync(x => x.Age);", 0, 0, 0, 0)]
    [InlineData("var avgAsyncViolation = await _appDbContext.Users.AverageAsync(x => x.Age);", 13, 43, 13, 87)]
    [InlineData("var longCountSyncOK = _appDbContext.Users.TagWithCallSite().LongCount();", 0, 0, 0, 0)]
    [InlineData("var longCountSyncViolation = _appDbContext.Users.LongCount();", 13, 42, 13, 73)]
    [InlineData("var longCountAsyncOK = await _appDbContext.Users.TagWithCallSite().LongCountAsync();", 0, 0, 0, 0)]
    [InlineData("var longCountAsyncViolation = await _appDbContext.Users.LongCountAsync();", 13, 49, 13, 85)]
    [InlineData("var allSyncOK = _appDbContext.Users.TagWithCallSite().All(x => x.Age > 18);", 0, 0, 0, 0)]
    [InlineData("var allSyncViolation = _appDbContext.Users.All(x => x.Age > 18);", 13, 36, 13, 76)]
    [InlineData("var allAsyncOK = await _appDbContext.Users.TagWithCallSite().AllAsync(x => x.Age > 18);", 0, 0, 0, 0)]
    [InlineData("var allAsyncViolation = await _appDbContext.Users.AllAsync(x => x.Age > 18);", 13, 43, 13, 88)]
    [InlineData("var dictSyncOK = _appDbContext.Users.TagWithCallSite().ToDictionary(x => x.Id);", 0, 0, 0, 0)]
    [InlineData("var dictSyncViolation = _appDbContext.Users.ToDictionary(x => x.Id);", 13, 37, 13, 80)]
    [InlineData("var dictAsyncOK = await _appDbContext.Users.TagWithCallSite().ToDictionaryAsync(x => x.Id);", 0, 0, 0, 0)]
    [InlineData("var dictAsyncViolation = await _appDbContext.Users.ToDictionaryAsync(x => x.Id);", 13, 44, 13, 92)]
    [InlineData("var hashSetSyncOK = _appDbContext.Users.TagWithCallSite().ToHashSet();", 0, 0, 0, 0)]
    [InlineData("var hashSetSyncViolation = _appDbContext.Users.ToHashSet();", 13, 40, 13, 71)]
    [InlineData("var hashSetAsyncOK = await _appDbContext.Users.TagWithCallSite().ToHashSetAsync();", 0, 0, 0, 0)]
    [InlineData("var hashSetAsyncViolation = await _appDbContext.Users.ToHashSetAsync();", 13, 47, 13, 83)]
    [InlineData("var lastSyncOK = _appDbContext.Users.TagWithCallSite().Last();", 0, 0, 0, 0)]
    [InlineData("var lastSyncViolation = _appDbContext.Users.Last();", 13, 37, 13, 63)]
    [InlineData("var lastAsyncOK = await _appDbContext.Users.TagWithCallSite().LastAsync();", 0, 0, 0, 0)]
    [InlineData("var lastAsyncViolation = await _appDbContext.Users.LastAsync();", 13, 44, 13, 75)]
    [InlineData("var lastOrDefaultSyncOK = _appDbContext.Users.TagWithCallSite().LastOrDefault();", 0, 0, 0, 0)]
    [InlineData("var lastOrDefaultSyncViolation = _appDbContext.Users.LastOrDefault();", 13, 46, 13, 81)]
    [InlineData("var lastOrDefaultAsyncOK = await _appDbContext.Users.TagWithCallSite().LastOrDefaultAsync();", 0, 0, 0, 0)]
    [InlineData("var lastOrDefaultAsyncViolation = await _appDbContext.Users.LastOrDefaultAsync();", 13, 53, 13, 93)]
    [InlineData("await _appDbContext.Users.TagWithCallSite().ExecuteDeleteAsync();", 0, 0, 0, 0)]
    [InlineData("await _appDbContext.Users.ExecuteDeleteAsync();", 13, 19, 13, 59)]
    [InlineData("await _appDbContext.Users.TagWithCallSite().ExecuteUpdateAsync(s => s.SetProperty(x => x.Age, 30));", 0, 0, 0, 0)]
    [InlineData("await _appDbContext.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.Age, 30));", 13, 19, 13, 93)]
    [InlineData("var asEnumerableSyncOK = _appDbContext.Users.TagWithCallSite().AsEnumerable();", 0, 0, 0, 0)]
    [InlineData("var asEnumerableSyncViolation = _appDbContext.Users.AsEnumerable();", 13, 45, 13, 79)]
    [InlineData("var asAsyncEnumerableOK = _appDbContext.Users.TagWithCallSite().AsAsyncEnumerable();", 0, 0, 0, 0)]
    [InlineData("var asAsyncEnumerableViolation = _appDbContext.Users.AsAsyncEnumerable();", 13, 46, 13, 85)]
    [InlineData("var toArraySyncOK = _appDbContext.Users.TagWithCallSite().ToArray();", 0, 0, 0, 0)]
    [InlineData("var toArraySyncViolation = _appDbContext.Users.ToArray();", 13, 40, 13, 69)]
    [InlineData("var toArrayAsyncOK = await _appDbContext.Users.TagWithCallSite().ToArrayAsync();", 0, 0, 0, 0)]
    [InlineData("var toArrayAsyncViolation = await _appDbContext.Users.ToArrayAsync();", 13, 47, 13, 81)]
    [InlineData("await _appDbContext.Users.TagWithCallSite().LoadAsync();", 0, 0, 0, 0)]
    [InlineData("await _appDbContext.Users.LoadAsync();", 13, 19, 13, 50)]
    [InlineData("_appDbContext.Users.TagWithCallSite().Load();", 0, 0, 0, 0)]
    [InlineData("_appDbContext.Users.Load();", 13, 13, 13, 39)]
    [InlineData("var containsOK = _appDbContext.Users.TagWithCallSite().Contains(new User());", 0, 0, 0, 0)]
    [InlineData("var containsViolation = _appDbContext.Users.Contains(new User());", 13, 37, 13, 77)]
    [InlineData("var containsAsyncOK = await _appDbContext.Users.TagWithCallSite().ContainsAsync(new User());", 0, 0, 0, 0)]
    [InlineData("var containsAsyncViolation = await _appDbContext.Users.ContainsAsync(new User());", 13, 48, 13, 93)]
    [InlineData("await _appDbContext.Users.TagWithCallSite().ForEachAsync(x => { });", 0, 0, 0, 0)]
    [InlineData("await _appDbContext.Users.ForEachAsync(x => { });", 13, 19, 13, 61)]
    [InlineData("var taskRunLambda = await Task.Run(() => _appDbContext.Users.TagWithCallSite().ToList());", 0, 0, 0, 0)]
    [InlineData("var taskRunLambdaViolation = await Task.Run(() => _appDbContext.Users.ToList());", 13, 63, 13, 91)]
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
       """, 13, 19, 22, 5)]
    [InlineData("""
       var companiesAny = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users.Any(u => u.CompanyId == c.Id && u.Name == "John"))
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesAnyWithoutTagWithCallSite = await _appDbContext.Companies
           .Where(c => _appDbContext.Users.Any(u => u.CompanyId == c.Id && u.Name == "John"))
           .ToListAsync();
       """, 13, 60, 15, 19)]
    [InlineData("""
       var companiesCount = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users.Count(u => u.CompanyId == c.Id) > 0)
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesAll = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users.All(u => u.CompanyId == c.Id))
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesFirst = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users
               .Where(u => u.CompanyId == c.Id)
               .Select(u => u.Id)
               .FirstOrDefault() > 0)
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesSingle = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users
               .Where(u => u.CompanyId == c.Id)
               .Select(u => u.Id)
               .SingleOrDefault() > 0)
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesSelectAndCount = await _appDbContext.Companies.TagWithCallSite()
           .Select(c => new
           {
               Count = _appDbContext.Users.Count(u => u.CompanyId == c.Id)
           })
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesWithAggregation = await _appDbContext.Companies.TagWithCallSite()
           .Select(c => new
           {
               MaxAge = _appDbContext.Users
                   .Where(u => u.CompanyId == c.Id)
                   .Max(u => u.Age)
           })
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesOrderByWithCount = await _appDbContext.Companies.TagWithCallSite()
           .OrderBy(c => _appDbContext.Users.Count(u => u.CompanyId == c.Id))
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesToListJoin = await _appDbContext.Companies.TagWithCallSite()
           .Select(c => new
           {
               Users = _appDbContext.Users
                   .Where(u => u.CompanyId == c.Id)
                   .ToList()
           })
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesContains = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users
               .Where(u => u.Age > 18)
               .Select(u => u.CompanyId)
               .Contains(c.Id))
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       // companies via variable
       var users = _appDbContext.Users;

       var q = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => users.Any(u => u.CompanyId == c.Id))
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       // companies via let variable
       var q =
           from c in _appDbContext.Companies
           let hasUsers = _appDbContext.Users.Any(u => u.CompanyId == c.Id)
           where hasUsers
           select c;

       await q.TagWithCallSite().ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       // companies via let variable
       var companies = await (
           from c in _appDbContext.Companies
           let hasUsers = _appDbContext.Users.Any(u => u.CompanyId == c.Id)
           where hasUsers
           select c
       ).TagWithCallSite().ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesDoubleAny = await _appDbContext.Companies.TagWithCallSite()
           .Where(c => _appDbContext.Users.Any(u =>
               _appDbContext.Companies.Any(c2 => c2.Id == u.CompanyId)))
           .ToListAsync();
       """, 0, 0, 0, 0)]
    [InlineData("""
       var companiesExecuteUpdateAsync = await _appDbContext.Users.TagWithCallSite()
           .Where(u => _appDbContext.Companies.Any(c => c.Id == u.CompanyId))
           .ExecuteUpdateAsync(s => s.SetProperty(x => x.Age, 30));
       """, 0, 0, 0, 0)]
    [InlineData("""
        var singleOrDefaultWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().SingleOrDefaultAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var singleOrDefaultWithLambdaViolation = await _appDbContext.Companies.SingleOrDefaultAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 60, 15, 2)]
    [InlineData("""
        var singleOrDefaultSyncWithLambdaOK = _appDbContext.Companies.TagWithCallSite().SingleOrDefault(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var singleOrDefaultSyncWithLambdaViolation = _appDbContext.Companies.SingleOrDefault(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 58, 15, 2)]
    [InlineData("""
        var firstOrDefaultWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().FirstOrDefaultAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var firstOrDefaultWithLambdaViolation = await _appDbContext.Companies.FirstOrDefaultAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 59, 15, 2)]
    [InlineData("""
        var firstWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().FirstAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var firstWithLambdaViolation = await _appDbContext.Companies.FirstAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 50, 15, 2)]
    [InlineData("""
        var singleWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().SingleAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var singleWithLambdaViolation = await _appDbContext.Companies.SingleAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 51, 15, 2)]
    [InlineData("""
        var lastWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().LastOrDefaultAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var lastWithLambdaViolation = await _appDbContext.Companies.LastOrDefaultAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 49, 15, 2)]
    [InlineData("""
        var anyWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().AnyAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var anyWithLambdaViolation = await _appDbContext.Companies.AnyAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 48, 15, 2)]
    [InlineData("""
        var allWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().AllAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var allWithLambdaViolation = await _appDbContext.Companies.AllAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 48, 15, 2)]
    [InlineData("""
        var countWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().CountAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var countWithLambdaViolation = await _appDbContext.Companies.CountAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id)
        );
        """, 13, 50, 15, 2)]
    [InlineData("""
        var maxWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().MaxAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var maxWithLambdaViolation = await _appDbContext.Companies.MaxAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 13, 48, 15, 2)]
    [InlineData("""
        var minWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().MinAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var minWithLambdaViolation = await _appDbContext.Companies.MinAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 13, 48, 15, 2)]
    [InlineData("""
        var sumWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().SumAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var sumWithLambdaViolation = await _appDbContext.Companies.SumAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 13, 48, 15, 2)]
    [InlineData("""
        var avgWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().AverageAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var avgWithLambdaViolation = await _appDbContext.Companies.AverageAsync(
            c => _appDbContext.Users.Any(u => u.CompanyId == c.Id) ? c.Id : 10
        );
        """, 13, 48, 15, 2)]
    [InlineData("""
        var longCountWithLambdaOK = await _appDbContext.Companies.TagWithCallSite().LongCountAsync(
            c => _appDbContext.Users.Count(u => u.Id == c.Id) > 0
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var longCountWithLambdaViolation = await _appDbContext.Companies.LongCountAsync(
            c => _appDbContext.Users.Count(u => u.Id == c.Id) > 0
        );
        """, 13, 54, 15, 2)]
    [InlineData("var customFuncOK = MyFunc(() => _appDbContext.Users.TagWithCallSite().ToArray());", 0, 0, 0, 0)]
    [InlineData("var customFuncViolation = MyFunc(() => _appDbContext.Users.ToArray());", 13, 52, 13, 81)]
    [InlineData("""
        var customFuncWithSubqueriesOK = MyFuncCompany(
            () => _appDbContext.Companies.TagWithCallSite()
                      .Where(c => _appDbContext.Users.Any(u => u.CompanyId == c.Id && u.Name == "John"))
                      .ToArray()
        );
        """, 0, 0, 0, 0)]
    [InlineData("""
        var customFuncWithSubqueriesViolation = MyFuncCompany(
            () => _appDbContext.Companies
                      .Where(c => _appDbContext.Users.Any(u => u.CompanyId == c.Id && u.Name == "John"))
                      .ToArray()
        );
        """, 14, 11, 16, 25)]
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

            public class TestClass
            {
                public async Task M1()
                {
                    {%inputQuery%}
                }

                private readonly AppDbContext _appDbContext;

                public TestClass(AppDbContext appDbContext)
                {
                    _appDbContext = appDbContext;
                }

                private IQueryable<User> GetUserQuery()
                {
                    return _appDbContext.Users.TagWithCallSite();
                }

                private User[] MyFunc(Func<User[]> queryFunc)
                {
                    return queryFunc();
                }

                private Company[] MyFuncCompany(Func<Company[]> queryFunc)
                {
                    return queryFunc();
                }
            }

            public class User
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
                public bool Gender { get; set; }
                public long CompanyId { get; set; }
            }

            public class Company
            {
                public long Id { get; set; }
                public string Name { get; set; }
            }
        
            public class AppDbContext : DbContext
            {
                public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
                public DbSet<User> Users { get; set; }
                public DbSet<Company> Companies { get; set; }
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
