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

using Microsoft.CodeAnalysis.Diagnostics;
using System;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace SonarLint.Helpers
{
    public class WrappingAnalysisContext : AnalysisContext
    {
        private readonly AnalysisContext context;

        public WrappingAnalysisContext(AnalysisContext context)
        {
            this.context = context;
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            context.RegisterCodeBlockAction(action);
        }

        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
        {
            context.RegisterCompilationAction(action);
        }

        public List<Action<CompilationStartAnalysisContext>> CompilationStartActions { get; set; } = new List<Action<CompilationStartAnalysisContext>>();
        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            CompilationStartActions.Add(action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            context.RegisterSemanticModelAction(action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            context.RegisterSymbolAction(action, symbolKinds);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            context.RegisterSyntaxNodeAction(action, syntaxKinds);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            context.RegisterCodeBlockStartAction(action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            context.RegisterSyntaxTreeAction(action);
        }
    }
}
