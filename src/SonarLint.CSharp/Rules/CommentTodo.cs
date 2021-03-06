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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Helpers;
using SonarLint.Common;
using SonarLint.Common.Sqale;

namespace SonarLint.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NoSqaleRemediation]
    [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
    public class CommentTodo : CommentRegularExpressionBase
    {
        protected override string RegularExpression => "(?i).*(TODO).*";

        internal const string DiagnosticId = "S1135";
        internal const string Title = "\"TODO\" tags should be handled";
        internal const string Description =
            "\"TODO\" tags are commonly used to mark places where some more code is required, but which the developer " +
            "wants to implement later. Sometimes the developer will not have the time or will simply forget to get back " +
            "to that tag. This rule is meant to track those tags, and ensure that they do not go unnoticed.";
        internal const string MessageFormat =
            "Complete the task associated to this \"TODO\" comment.";
        internal const string Category = SonarLint.Common.Category.Maintainability;
        internal const Severity RuleSeverity = Severity.Info;
        internal const bool IsActivatedByDefault = true;

        internal static readonly DiagnosticDescriptor rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault,
                helpLinkUri: DiagnosticId.GetHelpLink(),
                description: Description);

        protected override DiagnosticDescriptor Rule => rule;
    }
}
