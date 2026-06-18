// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuker.Analyzers.ConsoleTests;

internal class TestClass
{
    public void M1()
    {
        int a = 12;
        int b = 13;

        a.Equals(
            b
       );
    }
}
