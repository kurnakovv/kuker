// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

Console.WriteLine("Hello, World!");

List<int> list = [];

////list.GroupBy(x => x).Select(x => new { x.Key, Count = x.Count() }).Max(x => x.Count);

int a = list.GroupBy(x => x).Max(x => x.Count());

Console.WriteLine(list.GroupBy(x => x).Max(x => x));

// int a = 12;
// int b = 13;

// a.Equals(a);
