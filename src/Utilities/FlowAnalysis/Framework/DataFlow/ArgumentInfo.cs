﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Contains information about an argument passed to interprocedural analysis.
    /// </summary>
    internal sealed class ArgumentInfo<TAbstractAnalysisValue> : CacheBasedEquatable4<ArgumentInfo<TAbstractAnalysisValue>>
    {
        public ArgumentInfo(
            IOperation operation,
            AnalysisEntity analysisEntityOpt,
            PointsToAbstractValue instanceLocation,
            TAbstractAnalysisValue value)
        {
            Operation = operation;
            AnalysisEntityOpt = analysisEntityOpt;
            InstanceLocation = instanceLocation;
            Value = value;
        }

        public IOperation Operation { get; }
        // Can be null for allocations.
        public AnalysisEntity AnalysisEntityOpt { get; }
        public PointsToAbstractValue InstanceLocation { get; }
        public TAbstractAnalysisValue Value { get; }

        protected override void ComputeHashCodeParts(ArrayBuilder<int> builder)
        {
            builder.Add(Operation.GetHashCode());
            builder.Add(AnalysisEntityOpt.GetHashCodeOrDefault());
            builder.Add(InstanceLocation.GetHashCode());
            builder.Add(Value.GetHashCode());
        }
    }
}
