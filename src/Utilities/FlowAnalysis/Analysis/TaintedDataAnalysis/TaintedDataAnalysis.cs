﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        private static readonly TaintedDataAnalysisDomain TaintedDataAnalysisDomainInstance = new TaintedDataAnalysisDomain(CoreTaintedDataAnalysisDataDomain.Instance);

        private TaintedDataAnalysis(TaintedDataOperationVisitor operationVisitor)
            : base(TaintedDataAnalysisDomainInstance, operationVisitor)
        {
        }

        internal static TaintedDataAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol containingMethod,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos,
            CancellationToken cancellationToken)
        {
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, InterproceduralAnalysisKind.ContextSensitive, cancellationToken);
            return GetOrComputeResult(cfg, compilation, containingMethod, taintedSourceInfos,
                taintedSanitizerInfos, taintedSinkInfos, interproceduralAnalysisConfig);
        }

        private static TaintedDataAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol containingMethod,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig)
        {
            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            PointsToAnalysisResult pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(
                cfg,
                containingMethod,
                wellKnownTypeProvider,
                interproceduralAnalysisConfig,
                pessimisticAnalysis: true,
                performCopyAnalysis: true);

            TaintedDataAnalysisContext analysisContext = TaintedDataAnalysisContext.Create(
                TaintedDataAbstractValueDomain.Default,
                wellKnownTypeProvider,
                cfg,
                containingMethod,
                interproceduralAnalysisConfig,
                pessimisticAnalysis: false,
                pointsToAnalysisResult: pointsToAnalysisResult,
                getOrComputeAnalysisResult: GetOrComputeResultForAnalysisContext,
                taintedSourceInfos: taintedSourceInfos,
                taintedSanitizerInfos: taintedSanitizerInfos,
                taintedSinkInfos: taintedSinkInfos);

            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static TaintedDataAnalysisResult GetOrComputeResultForAnalysisContext(TaintedDataAnalysisContext analysisContext)
        {
            TaintedDataOperationVisitor visitor = new TaintedDataOperationVisitor(analysisContext);
            TaintedDataAnalysis analysis = new TaintedDataAnalysis(visitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override TaintedDataAnalysisResult ToResult(
            TaintedDataAnalysisContext analysisContext,
            DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> dataFlowAnalysisResult)
        {
            TaintedDataOperationVisitor visitor = (TaintedDataOperationVisitor) this.OperationVisitor;
            return new TaintedDataAnalysisResult(dataFlowAnalysisResult, visitor.GetTaintedDataSourceSinkEntries());
        }

        internal override TaintedDataBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TaintedDataAnalysisData> blockAnalysisData)
        {
            return new TaintedDataBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
