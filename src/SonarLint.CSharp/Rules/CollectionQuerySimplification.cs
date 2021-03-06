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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common;
using SonarLint.Common.Sqale;
using SonarLint.Helpers;

namespace SonarLint.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SqaleConstantRemediation("5min")]
    [SqaleSubCharacteristic(SqaleSubCharacteristic.Readability)]
    [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
    [Tags(Tag.Clumsy)]
    public class CollectionQuerySimplification : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2971";
        internal const string Title = "\"IEnumerable\" LINQs should be simplified";
        internal const string Description =
            "In the interests of readability, code that can be simplified should be simplified. To that end, there are several " +
            "ways \"IEnumerable LINQ\"s can be simplified. Use \"OfType\" instead of using \"Select\" with \"as\" to type cast " +
            "elements and then null-checking in a query expression to choose elements based on type. Use \"OfType\" instead of " +
            "using \"Where\" and the \"is\" operator, followed by a cast in a \"Select\". Use an expression in \"Any\" instead " +
            "of \"Where(element => [expression]).Any()\". Use \"Count\" instead of \"Count()\" when it's available. Don't call " +
            "\"ToArray()\" or \"ToList()\" in the middle of a query chain.";
        internal const string MessageUseInstead = "Use {0} here instead.";
        internal const string MessageDropAndChange = "Drop \"{0}\" and move the condition into the \"{1}\".";
        internal const string MessageDropFromMiddle = "Drop \"{0}\" from the middle of the call chain.";
        internal const string Category = SonarLint.Common.Category.Maintainability;
        internal const Severity RuleSeverity = Severity.Major;
        internal const bool IsActivatedByDefault = true;

        internal static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, "{0}", Category,
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault,
                helpLinkUri: DiagnosticId.GetHelpLink(),
                description: Description);

        private static readonly ExpressionSyntax NullExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private static readonly string[] MethodNamesWithPredicate =
        {
            "Any", "LongCount", "Count",
            "First", "FirstOrDefault", "Last", "LastOrDefault",
            "Single", "SingleOrDefault"
        };
        private static readonly string[] MethodNamesForTypeCheckingWithSelect =
        {
            "Any", "LongCount", "Count",
            "First", "FirstOrDefault", "Last", "LastOrDefault",
            "Single", "SingleOrDefault", "SkipWhile", "TakeWhile"
        };
        private static readonly string[] MethodNamesToCollection =
        {
            "ToList", "ToArray"
        };

        private static readonly SyntaxKind[] AsIsSyntaxKinds = {SyntaxKind.AsExpression, SyntaxKind.IsExpression};
        private const string WhereMethodName = "Where";
        private const string SelectMethodName = "Select";

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    CheckExtensionMethodsOnIEnumerable(c);
                },
                SyntaxKind.InvocationExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    CheckToCollectionCalls(c);
                },
                SyntaxKind.InvocationExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    CheckCountCall(c);
                },
                SyntaxKind.InvocationExpression);
        }

        private static void CheckCountCall(SyntaxNodeAnalysisContext c)
        {
            var invocation = (InvocationExpressionSyntax)c.Node;
            var methodSymbol = c.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (invocation.ArgumentList == null ||
                invocation.ArgumentList.Arguments.Any() ||
                methodSymbol == null ||
                !CollectionEmptinessChecking.MethodIsOnIEnumerable(methodSymbol, c.SemanticModel) ||
                methodSymbol.Name != "Count")
            {
                return;
            }

            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return;
            }

            var symbol = c.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
            if (symbol.GetMembers("Count").OfType<IPropertySymbol>().Any())
            {
                c.ReportDiagnostic(Diagnostic.Create(Rule, GetReportLocation(invocation),
                    string.Format(MessageUseInstead, "\"Count\" property")));
            }
        }

        private static void CheckToCollectionCalls(SyntaxNodeAnalysisContext c)
        {
            var outerInvocation = (InvocationExpressionSyntax)c.Node;
            var outerMethodSymbol = c.SemanticModel.GetSymbolInfo(outerInvocation).Symbol as IMethodSymbol;
            if (outerMethodSymbol == null ||
                !MethodCanBeCalledOnIEnumerable(outerMethodSymbol, c.SemanticModel))
            {
                return;
            }

            var innerInvocation = GetInnerInvocation(outerInvocation, outerMethodSymbol);
            if (innerInvocation == null)
            {
                return;
            }

            var innerMethodSymbol = c.SemanticModel.GetSymbolInfo(innerInvocation).Symbol as IMethodSymbol;
            if (innerMethodSymbol == null ||
                !MethodCanBeCalledOnIEnumerable(innerMethodSymbol, c.SemanticModel))
            {
                return;
            }

            if (MethodNamesToCollection.Contains(innerMethodSymbol.Name))
            {
                c.ReportDiagnostic(Diagnostic.Create(Rule, GetReportLocation(innerInvocation),
                    string.Format(MessageDropFromMiddle, innerMethodSymbol.Name)));
            }
        }

        private static bool MethodCanBeCalledOnIEnumerable(IMethodSymbol methodSymbol, SemanticModel semanticModel)
        {
            if (methodSymbol == null)
            {
                return false;
            }

            var enumerableType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            var receiverType = methodSymbol.ReceiverType as INamedTypeSymbol;

            if (methodSymbol.MethodKind == MethodKind.Ordinary &&
                methodSymbol.IsExtensionMethod)
            {
                receiverType = methodSymbol.Parameters.First().Type as INamedTypeSymbol;
            }

            return receiverType != null &&
                receiverType.ConstructedFrom.DerivesOrImplementsAny(enumerableType);
        }

        private static void CheckExtensionMethodsOnIEnumerable(SyntaxNodeAnalysisContext c)
        {
            var outerInvocation = (InvocationExpressionSyntax)c.Node;
            var outerMethodSymbol = c.SemanticModel.GetSymbolInfo(outerInvocation).Symbol as IMethodSymbol;
            if (outerMethodSymbol == null ||
                !CollectionEmptinessChecking.MethodIsOnIEnumerable(outerMethodSymbol, c.SemanticModel))
            {
                return;
            }

            var innerInvocation = GetInnerInvocation(outerInvocation, outerMethodSymbol);
            if (innerInvocation == null)
            {
                return;
            }

            var innerMethodSymbol = c.SemanticModel.GetSymbolInfo(innerInvocation).Symbol as IMethodSymbol;
            if (innerMethodSymbol == null ||
                !CollectionEmptinessChecking.MethodIsOnIEnumerable(innerMethodSymbol, c.SemanticModel))
            {
                return;
            }

            if (CheckForSimplifiable(outerMethodSymbol, outerInvocation, innerMethodSymbol, innerInvocation, c))
            {
                return;
            }

            if (CheckForCastSimplification(outerMethodSymbol, innerMethodSymbol, innerInvocation,
                outerInvocation, c))
            {
                return;
            }
        }

        private static InvocationExpressionSyntax GetInnerInvocation(InvocationExpressionSyntax outerInvocation, IMethodSymbol outerMethodSymbol)
        {
            InvocationExpressionSyntax innerInvocation = null;
            if (outerMethodSymbol.MethodKind == MethodKind.ReducedExtension)
            {
                var memberAccess = outerInvocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    innerInvocation = memberAccess.Expression as InvocationExpressionSyntax;
                }
            }
            else
            {
                var argument = outerInvocation.ArgumentList.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    innerInvocation = argument.Expression as InvocationExpressionSyntax;
                }
                else
                {
                    var memberAccess = outerInvocation.Expression as MemberAccessExpressionSyntax;
                    if (memberAccess != null)
                    {
                        innerInvocation = memberAccess.Expression as InvocationExpressionSyntax;
                    }
                }
            }

            return innerInvocation;
        }

        private static List<ArgumentSyntax> GetReducedArguments(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation)
        {
            return methodSymbol.MethodKind == MethodKind.ReducedExtension
                ? invocation.ArgumentList.Arguments.ToList()
                : invocation.ArgumentList.Arguments.Skip(1).ToList();
        }

        private static bool CheckForCastSimplification(IMethodSymbol outerMethodSymbol, IMethodSymbol innerMethodSymbol,
            InvocationExpressionSyntax innerInvocation, InvocationExpressionSyntax outerInvocation, SyntaxNodeAnalysisContext c)
        {
            string typeNameInInner;
            if (MethodNamesForTypeCheckingWithSelect.Contains(outerMethodSymbol.Name) &&
                innerMethodSymbol.Name == SelectMethodName &&
                IsFirstExpressionInLambdaIsNullChecking(outerMethodSymbol, outerInvocation) &&
                TryGetCastInLambda(SyntaxKind.AsExpression, innerMethodSymbol, innerInvocation, out typeNameInInner))
            {
                c.ReportDiagnostic(Diagnostic.Create(Rule, GetReportLocation(innerInvocation),
                    string.Format(MessageUseInstead, $"\"OfType<{typeNameInInner}>()\"")));
                return true;
            }

            string typeNameInOuter;
            if (outerMethodSymbol.Name == SelectMethodName &&
                innerMethodSymbol.Name == WhereMethodName &&
                IsExpressionInLambdaIsCast(outerMethodSymbol, outerInvocation, out typeNameInOuter) &&
                TryGetCastInLambda(SyntaxKind.IsExpression, innerMethodSymbol, innerInvocation, out typeNameInInner) &&
                typeNameInOuter == typeNameInInner)
            {
                c.ReportDiagnostic(Diagnostic.Create(Rule, GetReportLocation(innerInvocation),
                    string.Format(MessageUseInstead, $"\"OfType<{typeNameInInner}>()\"")));
                return true;
            }

            return false;
        }

        private static Location GetReportLocation(InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            return memberAccess == null
                ? invocation.Expression.GetLocation()
                : memberAccess.Name.GetLocation();
        }

        private static bool IsExpressionInLambdaIsCast(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation, out string typeName)
        {
            return TryGetCastInLambda(SyntaxKind.AsExpression, methodSymbol, invocation, out typeName) ||
                   TryGetCastInLambda(methodSymbol, invocation, out typeName);
        }

        private static bool IsFirstExpressionInLambdaIsNullChecking(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation)
        {
            var arguments = GetReducedArguments(methodSymbol, invocation);
            if (arguments.Count != 1)
            {
                return false;
            }
            var expression = arguments.First().Expression;

            var binaryExpression = GetExpressionFromParens(GetExpressionFromLambda(expression)) as BinaryExpressionSyntax;
            var lambdaParameter = GetLambdaParameter(expression);

            while (binaryExpression != null)
            {
                if (!binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    return binaryExpression.IsKind(SyntaxKind.NotEqualsExpression) &&
                           IsNullChecking(binaryExpression, lambdaParameter);
                }
                binaryExpression = GetExpressionFromParens(binaryExpression.Left) as BinaryExpressionSyntax;
            }
            return false;
        }

        private static bool IsNullChecking(BinaryExpressionSyntax binaryExpression, string lambdaParameter)
        {
            if (EquivalenceChecker.AreEquivalent(NullExpression, GetExpressionFromParens(binaryExpression.Left)) &&
                GetExpressionFromParens(binaryExpression.Right).ToString() == lambdaParameter)
            {
                return true;
            }
            if (EquivalenceChecker.AreEquivalent(NullExpression, GetExpressionFromParens(binaryExpression.Right)) &&
                GetExpressionFromParens(binaryExpression.Left).ToString() == lambdaParameter)
            {
                return true;
            }
            return false;
        }

        private static ExpressionSyntax GetExpressionFromParens(ExpressionSyntax expression)
        {
            var parens = expression as ParenthesizedExpressionSyntax;
            var current = expression;
            while (parens != null)
            {
                current = parens.Expression;
                parens = current as ParenthesizedExpressionSyntax;
            }

            return current;
        }

        private static ExpressionSyntax GetExpressionFromLambda(ExpressionSyntax expression)
        {
            ExpressionSyntax lambdaBody;
            var lambda = expression as SimpleLambdaExpressionSyntax;
            if (lambda == null)
            {
                var parenthesizedLambda = expression as ParenthesizedLambdaExpressionSyntax;
                if (parenthesizedLambda == null)
                {
                    return null;
                }
                lambdaBody = parenthesizedLambda.Body as ExpressionSyntax;
            }
            else
            {
                lambdaBody = lambda.Body as ExpressionSyntax;
            }

            return lambdaBody;
        }
        private static string GetLambdaParameter(ExpressionSyntax expression)
        {
            var lambda = expression as SimpleLambdaExpressionSyntax;
            if (lambda != null)
            {
                return lambda.Parameter.Identifier.ValueText;
            }

            var parenthesizedLambda = expression as ParenthesizedLambdaExpressionSyntax;
            if (parenthesizedLambda == null ||
                !parenthesizedLambda.ParameterList.Parameters.Any())
            {
                return null;
            }
            return parenthesizedLambda.ParameterList.Parameters.First().Identifier.ValueText;
        }

        private static bool TryGetCastInLambda(SyntaxKind asOrIs, IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation, out string type)
        {
            type = null;
            if (!AsIsSyntaxKinds.Contains(asOrIs))
            {
                return false;
            }

            var arguments = GetReducedArguments(methodSymbol, invocation);
            if (arguments.Count != 1)
            {
                return false;
            }

            var expression = arguments.First().Expression;
            var lambdaBody = GetExpressionFromParens(GetExpressionFromLambda(expression)) as BinaryExpressionSyntax;
            var lambdaParameter = GetLambdaParameter(expression);
            if (lambdaBody == null ||
                lambdaParameter == null ||
                !lambdaBody.IsKind(asOrIs))
            {
                return false;
            }

            var castedExpression = GetExpressionFromParens(lambdaBody.Left);
            if (lambdaParameter != castedExpression.ToString())
            {
                return false;
            }

            type = lambdaBody.Right.ToString();
            return true;
        }
        private static bool TryGetCastInLambda(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation, out string type)
        {
            type = null;
            var arguments = GetReducedArguments(methodSymbol, invocation);
            if (arguments.Count != 1)
            {
                return false;
            }

            var expression = arguments.First().Expression;
            var castExpression = GetExpressionFromParens(GetExpressionFromLambda(expression)) as CastExpressionSyntax;
            var lambdaParameter = GetLambdaParameter(expression);
            if (castExpression == null ||
                lambdaParameter == null)
            {
                return false;
            }

            var castedExpression = GetExpressionFromParens(castExpression.Expression);
            if (lambdaParameter != castedExpression.ToString())
            {
                return false;
            }

            type = castExpression.Type.ToString();
            return true;
        }

        private static bool CheckForSimplifiable(IMethodSymbol outerMethodSymbol, InvocationExpressionSyntax outerInvocation,
            IMethodSymbol innerMethodSymbol, InvocationExpressionSyntax innerInvocation, SyntaxNodeAnalysisContext c)
        {
            if (MethodIsNotUsingPredicate(outerMethodSymbol, outerInvocation) &&
                innerMethodSymbol.Name == WhereMethodName &&
                innerMethodSymbol.Parameters.Any(symbol =>
                {
                    var namedType = symbol.Type as INamedTypeSymbol;
                    if (namedType == null)
                    {
                        return false;
                    }
                    return namedType.TypeArguments.Count() == 2;
                }))
            {
                c.ReportDiagnostic(Diagnostic.Create(Rule, GetReportLocation(innerInvocation),
                    string.Format(MessageDropAndChange, WhereMethodName, outerMethodSymbol.Name)));
                return true;
            }

            return false;
        }

        private static bool MethodIsNotUsingPredicate(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation)
        {
            var arguments = GetReducedArguments(methodSymbol, invocation);

            return !arguments.Any() && MethodNamesWithPredicate.Contains(methodSymbol.Name);
        }
    }
}
