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
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace SonarLint.Rules.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class GetHashCodeMutableCodeFixProvider : CodeFixProvider
    {
        internal const string Title = "Make field \"readonly\"";
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(GetHashCodeMutable.DiagnosticId);
            }
        }
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var syntaxNode = (IdentifierNameSyntax)root.FindNode(diagnosticSpan);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var fieldSymbol = semanticModel.GetSymbolInfo(syntaxNode).Symbol as IFieldSymbol;

            if (fieldSymbol == null ||
                !fieldSymbol.DeclaringSyntaxReferences.Any())
            {
                return;
            }

            var reference = await fieldSymbol.DeclaringSyntaxReferences.First().GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false);
            var fieldDeclaration = (FieldDeclarationSyntax)reference.Parent.Parent;

            if (fieldDeclaration.Declaration.Variables.Count != 1)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    c =>
                    {
                        var newFieldDeclaration = fieldDeclaration.AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                        var newRoot = root.ReplaceNode(fieldDeclaration, newFieldDeclaration);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    }),
                context.Diagnostics);
        }
    }
}

