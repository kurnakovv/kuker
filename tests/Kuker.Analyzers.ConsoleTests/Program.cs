// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

Console.WriteLine("Hello, World!");

List<int> list = [];

////if (list is { Count: > 0 })
////{
////    return;
////}

if (!list.Any())
{
    return;
}

int a = list.Max();

// int a = 12;
// int b = 13;

// a.Equals(a);
