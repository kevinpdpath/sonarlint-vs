﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.TestCases
{
    class LiteralSuffixUpperCase
    {
        public void Test(uint ui)
        {
            const uint a = 0U;      // Noncompliant
            const long b = 0L;      // Noncompliant
            const ulong c = 0UL;     // Noncompliant
            const ulong d = 0UL;     // Noncompliant
            const decimal e = 1.2M;  // Noncompliant
            const float f = 1.2F;    // Noncompliant
            const double g = 1.2D;    // Noncompliant
            const double h = 1.2e-10F; // Noncompliant
            const int i = 0xf; // Compliant
            const int j = 0Xf; // Compliant
            const int k = 0; // Compliant
            const int l = 0U; // Noncompliant

            Test(0U); // Noncompliant
        }
        public void TestOk()
        {
            const uint a = 0U;
            const long b = 0L;
            const ulong c = 0UL;
            const ulong d = 0UL;
            const decimal e = 1.2M;
            const float f = 1.2F;
            const double g = 1.2D;
            const double h = 1.2E-10;
            const int i = 0xF;
        }
    }
}
