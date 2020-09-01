﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class TypeConversionAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ValueTypeToReferenceTypeConversionRuleId = "HAA0601";
        public const string DelegateOnStructInstanceRuleId = "HAA0602";
        public const string MethodGroupAllocationRuleId = "HAA0603";
        public const string ReadonlyMethodGroupAllocationRuleId = "HAA0604";

        private static readonly LocalizableString s_localizableValueTypeToReferenceTypeConversionRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ValueTypeToReferenceTypeConversionRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableValueTypeToReferenceTypeConversionRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ValueTypeToReferenceTypeConversionRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableDelegateOnStructInstanceRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.DelegateOnStructInstanceRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableDelegateOnStructInstanceRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.DelegateOnStructInstanceRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableMethodGroupAllocationRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.MethodGroupAllocationRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableMethodGroupAllocationRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.MethodGroupAllocationRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableReadonlyMethodGroupAllocationRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ReadonlyMethodGroupAllocationRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableReadonlyMethodGroupAllocationRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ReadonlyMethodGroupAllocationRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        internal static DiagnosticDescriptor ValueTypeToReferenceTypeConversionRule = new DiagnosticDescriptor(
            ValueTypeToReferenceTypeConversionRuleId,
            s_localizableValueTypeToReferenceTypeConversionRuleTitle,
            s_localizableValueTypeToReferenceTypeConversionRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor DelegateOnStructInstanceRule = new DiagnosticDescriptor(
            DelegateOnStructInstanceRuleId,
            s_localizableDelegateOnStructInstanceRuleTitle,
            s_localizableDelegateOnStructInstanceRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor MethodGroupAllocationRule = new DiagnosticDescriptor(
            MethodGroupAllocationRuleId,
            s_localizableMethodGroupAllocationRuleTitle,
            s_localizableMethodGroupAllocationRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor ReadonlyMethodGroupAllocationRule = new DiagnosticDescriptor(
            ReadonlyMethodGroupAllocationRuleId,
            s_localizableReadonlyMethodGroupAllocationRuleTitle,
            s_localizableReadonlyMethodGroupAllocationRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ValueTypeToReferenceTypeConversionRule, DelegateOnStructInstanceRule, MethodGroupAllocationRule, ReadonlyMethodGroupAllocationRule);

        protected override ImmutableArray<SyntaxKind> Expressions => ImmutableArray.Create(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.ReturnStatement,
            SyntaxKind.YieldReturnStatement,
            SyntaxKind.CastExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.ConditionalExpression,
            SyntaxKind.ForEachStatement,
            SyntaxKind.EqualsValueClause,
            SyntaxKind.Argument,
            SyntaxKind.ArrowExpressionClause,
            SyntaxKind.Interpolation);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            bool assignedToReadonlyFieldOrProperty =
                (context.ContainingSymbol as IFieldSymbol)?.IsReadOnly == true ||
                (context.ContainingSymbol as IPropertySymbol)?.IsReadOnly == true;

            // this.fooObjCall(10);
            // new myobject(10);
            if (node is ArgumentSyntax argumentSyntax)
            {
                ArgumentSyntaxCheck(argumentSyntax, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, cancellationToken);
            }

            // object foo { get { return 0; } }
            if (node is ReturnStatementSyntax returnStatementSyntax)
            {
                ReturnStatementExpressionCheck(returnStatementSyntax, semanticModel, reportDiagnostic, cancellationToken);
                return;
            }

            // yield return 0
            if (node is YieldStatementSyntax yieldStatementSyntax)
            {
                YieldReturnStatementExpressionCheck(yieldStatementSyntax, semanticModel, reportDiagnostic, cancellationToken);
                return;
            }

            // object a = x ?? 0;
            // var a = 10 as object;
            if (node is BinaryExpressionSyntax binaryExpressionSyntax)
            {
                BinaryExpressionCheck(binaryExpressionSyntax, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, cancellationToken);
                return;
            }

            // for (object i = 0;;)
            if (node is EqualsValueClauseSyntax equalsValueClauseSyntax)
            {
                EqualsValueClauseCheck(equalsValueClauseSyntax, semanticModel, assignedToReadonlyFieldOrProperty, reportDiagnostic, cancellationToken);
                return;
            }

            // object = true ? 0 : obj
            if (node is ConditionalExpressionSyntax conditionalExpressionSyntax)
            {
                ConditionalExpressionCheck(conditionalExpressionSyntax, semanticModel, reportDiagnostic, cancellationToken);
                return;
            }

            // string a = $"{1}";
            if (node is InterpolationSyntax interpolationSyntax)
            {
                InterpolationCheck(interpolationSyntax, semanticModel, reportDiagnostic, cancellationToken);
                return;
            }

            // var f = (object)
            if (node is CastExpressionSyntax castExpressionSyntax)
            {
                CastExpressionCheck(castExpressionSyntax, semanticModel, reportDiagnostic, cancellationToken);
                return;
            }

            // object Foo => 1
            if (node is ArrowExpressionClauseSyntax arrowExpressionClauseSyntax)
            {
                ArrowExpressionCheck(arrowExpressionClauseSyntax, semanticModel, reportDiagnostic, cancellationToken);
                return;
            }
        }

        private static void ReturnStatementExpressionCheck(ReturnStatementSyntax returnStatementExpression, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (returnStatementExpression.Expression != null)
            {
                var returnConversionInfo = semanticModel.GetConversion(returnStatementExpression.Expression, cancellationToken);
                CheckTypeConversion(returnConversionInfo, reportDiagnostic, returnStatementExpression.Expression.GetLocation());
            }
        }

        private static void YieldReturnStatementExpressionCheck(YieldStatementSyntax yieldExpression, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (yieldExpression.Expression != null)
            {
                var returnConversionInfo = semanticModel.GetConversion(yieldExpression.Expression, cancellationToken);
                CheckTypeConversion(returnConversionInfo, reportDiagnostic, yieldExpression.Expression.GetLocation());
            }
        }

        private static void ArgumentSyntaxCheck(ArgumentSyntax argument, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (argument.Expression != null)
            {
                var argumentTypeInfo = semanticModel.GetTypeInfo(argument.Expression, cancellationToken);
                var argumentConversionInfo = semanticModel.GetConversion(argument.Expression, cancellationToken);
                CheckTypeConversion(argumentConversionInfo, reportDiagnostic, argument.Expression.GetLocation());
                CheckDelegateCreation(argument.Expression, argumentTypeInfo, semanticModel, isAssignmentToReadonly, reportDiagnostic, argument.Expression.GetLocation(), cancellationToken);
            }
        }

        private static void BinaryExpressionCheck(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            // as expression
            if (binaryExpression.IsKind(SyntaxKind.AsExpression) && binaryExpression.Left != null && binaryExpression.Right != null)
            {
                var leftT = semanticModel.GetTypeInfo(binaryExpression.Left, cancellationToken);
                var rightT = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);

                if (leftT.Type?.IsValueType == true && rightT.Type?.IsReferenceType == true)
                {
                    reportDiagnostic(Diagnostic.Create(ValueTypeToReferenceTypeConversionRule, binaryExpression.Left.GetLocation(), EmptyMessageArgs));
                }

                return;
            }

            if (binaryExpression.Right != null)
            {
                var assignmentExprTypeInfo = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);
                var assignmentExprConversionInfo = semanticModel.GetConversion(binaryExpression.Right, cancellationToken);
                CheckTypeConversion(assignmentExprConversionInfo, reportDiagnostic, binaryExpression.Right.GetLocation());
                CheckDelegateCreation(binaryExpression.Right, assignmentExprTypeInfo, semanticModel, isAssignmentToReadonly, reportDiagnostic, binaryExpression.Right.GetLocation(), cancellationToken);
                return;
            }
        }

        private static void InterpolationCheck(InterpolationSyntax interpolation, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(interpolation.Expression, cancellationToken);
            if (typeInfo.Type?.IsValueType == true)
            {
                reportDiagnostic(Diagnostic.Create(ValueTypeToReferenceTypeConversionRule, interpolation.Expression.GetLocation(), EmptyMessageArgs));
            }
        }

        private static void CastExpressionCheck(CastExpressionSyntax castExpression, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (castExpression.Expression != null)
            {
                var castTypeInfo = semanticModel.GetTypeInfo(castExpression, cancellationToken);
                var expressionTypeInfo = semanticModel.GetTypeInfo(castExpression.Expression, cancellationToken);

                if (castTypeInfo.Type?.IsReferenceType == true && expressionTypeInfo.Type?.IsValueType == true)
                {
                    reportDiagnostic(Diagnostic.Create(ValueTypeToReferenceTypeConversionRule, castExpression.Expression.GetLocation(), EmptyMessageArgs));
                }
            }
        }

        private static void ConditionalExpressionCheck(ConditionalExpressionSyntax conditionalExpression, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var trueExp = conditionalExpression.WhenTrue;
            var falseExp = conditionalExpression.WhenFalse;

            if (trueExp != null)
            {
                CheckTypeConversion(semanticModel.GetConversion(trueExp, cancellationToken), reportDiagnostic, trueExp.GetLocation());
            }

            if (falseExp != null)
            {
                CheckTypeConversion(semanticModel.GetConversion(falseExp, cancellationToken), reportDiagnostic, falseExp.GetLocation());
            }
        }

        private static void EqualsValueClauseCheck(EqualsValueClauseSyntax initializer, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (initializer.Value != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(initializer.Value, cancellationToken);
                var conversionInfo = semanticModel.GetConversion(initializer.Value, cancellationToken);
                CheckTypeConversion(conversionInfo, reportDiagnostic, initializer.Value.GetLocation());
                CheckDelegateCreation(initializer.Value, typeInfo, semanticModel, isAssignmentToReadonly, reportDiagnostic, initializer.Value.GetLocation(), cancellationToken);
            }
        }

        private static void ArrowExpressionCheck(ArrowExpressionClauseSyntax syntax, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(syntax.Expression, cancellationToken);
            var conversionInfo = semanticModel.GetConversion(syntax.Expression, cancellationToken);
            CheckTypeConversion(conversionInfo, reportDiagnostic, syntax.Expression.GetLocation());
            CheckDelegateCreation(syntax, typeInfo, semanticModel, false, reportDiagnostic,
                syntax.Expression.GetLocation(), cancellationToken);
        }

        private static void CheckTypeConversion(Conversion conversionInfo, Action<Diagnostic> reportDiagnostic, Location location)
        {
            if (conversionInfo.IsBoxing)
            {
                reportDiagnostic(Diagnostic.Create(ValueTypeToReferenceTypeConversionRule, location, EmptyMessageArgs));
            }
        }

        private static void CheckDelegateCreation(SyntaxNode node, TypeInfo typeInfo, SemanticModel semanticModel, bool isAssignmentToReadonly, Action<Diagnostic> reportDiagnostic, Location location, CancellationToken cancellationToken)
        {
            // special case: method groups
            if (typeInfo.ConvertedType?.TypeKind == TypeKind.Delegate)
            {
                // new Action<Foo>(MethodGroup); should skip this one
                var insideObjectCreation = node?.Parent?.Parent?.Parent?.Kind() == SyntaxKind.ObjectCreationExpression;
                if (node is ParenthesizedLambdaExpressionSyntax || node is SimpleLambdaExpressionSyntax ||
                    node is AnonymousMethodExpressionSyntax || node is ObjectCreationExpressionSyntax ||
                    insideObjectCreation)
                {
                    // skip this, because it's intended.
                }
                else
                {
                    if (node.IsKind(SyntaxKind.IdentifierName))
                    {
                        if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol)
                        {
                            reportDiagnostic(Diagnostic.Create(MethodGroupAllocationRule, location, EmptyMessageArgs));
                        }
                    }
                    else if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)node!;
                        if (semanticModel.GetSymbolInfo(memberAccess.Name, cancellationToken).Symbol is IMethodSymbol)
                        {
                            if (isAssignmentToReadonly)
                            {
                                reportDiagnostic(Diagnostic.Create(ReadonlyMethodGroupAllocationRule, location, EmptyMessageArgs));
                            }
                            else
                            {
                                reportDiagnostic(Diagnostic.Create(MethodGroupAllocationRule, location, EmptyMessageArgs));
                            }
                        }
                    }
                    else if (node is ArrowExpressionClauseSyntax arrowClause)
                    {
                        if (semanticModel.GetSymbolInfo(arrowClause.Expression, cancellationToken).Symbol is IMethodSymbol)
                        {
                            reportDiagnostic(Diagnostic.Create(MethodGroupAllocationRule, location, EmptyMessageArgs));
                        }
                    }
                }

                if (IsStructInstanceMethod(node!, semanticModel, cancellationToken))
                {
                    reportDiagnostic(Diagnostic.Create(DelegateOnStructInstanceRule, location, EmptyMessageArgs));
                }
            }
        }

        private static bool IsStructInstanceMethod(SyntaxNode? node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.IsKind(SyntaxKind.AnonymousMethodExpression) || node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || node.IsKind(SyntaxKind.SimpleLambdaExpression))
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
            return symbolInfo?.Kind == SymbolKind.Method && !symbolInfo.IsStatic && symbolInfo.ContainingType?.IsValueType == true;
        }
    }
}
