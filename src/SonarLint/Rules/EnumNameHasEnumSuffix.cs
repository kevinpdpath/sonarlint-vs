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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common;
using SonarLint.Common.Sqale;

namespace SonarLint.Rules
{
    namespace CSharp
    {
        using Microsoft.CodeAnalysis.CSharp;
        using Microsoft.CodeAnalysis.CSharp.Syntax;

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        [SqaleSubCharacteristic(SqaleSubCharacteristic.Readability)]
        [SqaleConstantRemediation("5min")]
        [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
        [Tags(Tag.Convention)]
        public class EnumNameHasEnumSuffix : EnumNameHasEnumSuffixBase<SyntaxKind>
        {
            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.EnumDeclaration);
            public override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => kindsOfInterest;
            protected override SyntaxToken GetIdentifier(SyntaxNode node) => ((EnumDeclarationSyntax)node).Identifier;
        }
    }

    namespace VisualBasic
    {
        using Microsoft.CodeAnalysis.VisualBasic;
        using Microsoft.CodeAnalysis.VisualBasic.Syntax;

        [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
        [SqaleSubCharacteristic(SqaleSubCharacteristic.Readability)]
        [SqaleConstantRemediation("5min")]
        [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
        [Tags(Tag.Convention)]
        public class EnumNameHasEnumSuffix : EnumNameHasEnumSuffixBase<SyntaxKind>
        {
            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.EnumStatement);
            public override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => kindsOfInterest;
            protected override SyntaxToken GetIdentifier(SyntaxNode node) => ((EnumStatementSyntax)node).Identifier;
        }
    }
}
