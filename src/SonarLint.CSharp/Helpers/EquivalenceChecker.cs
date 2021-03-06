﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2015 SonarSource
 * sonarqube@googlegroups.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SonarLint.Helpers
{
    public static class EquivalenceChecker
    {
        public static bool AreEquivalent(SyntaxNode node1, SyntaxNode node2)
        {
            if (node1.Language != node2.Language)
            {
                return false;
            }

            return SyntaxFactory.AreEquivalent(node1, node2);
        }

        public static bool AreEquivalent(SyntaxList<SyntaxNode> nodeList1, SyntaxList<SyntaxNode> nodeList2)
        {
            if (nodeList1.Count != nodeList2.Count)
            {
                return false;
            }

            for (var i = 0; i < nodeList1.Count; i++)
            {
                if (!AreEquivalent(nodeList1[i], nodeList2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
