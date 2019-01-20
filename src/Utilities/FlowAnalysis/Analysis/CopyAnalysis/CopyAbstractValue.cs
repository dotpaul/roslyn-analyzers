﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Abstract copy value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="CopyAnalysis"/>.
    /// </summary>
    internal class CopyAbstractValue : CacheBasedEquatable2<CopyAbstractValue>
    {
        public static CopyAbstractValue NotApplicable = new CopyAbstractValue(CopyAbstractValueKind.NotApplicable);
        public static CopyAbstractValue Invalid = new CopyAbstractValue(CopyAbstractValueKind.Invalid);
        public static CopyAbstractValue Unknown = new CopyAbstractValue(CopyAbstractValueKind.Unknown);
        
        private CopyAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities, CopyAbstractValueKind kind)
        {
            Debug.Assert(analysisEntities.IsEmpty == (kind != CopyAbstractValueKind.Known));

            AnalysisEntities = analysisEntities;
            Kind = kind;
        }

        private CopyAbstractValue(CopyAbstractValueKind kind)
            : this(ImmutableHashSet<AnalysisEntity>.Empty, kind)
        {
            Debug.Assert(kind != CopyAbstractValueKind.Known);
        }

        public CopyAbstractValue(AnalysisEntity analysisEntity)
            : this(ImmutableHashSet.Create(analysisEntity), CopyAbstractValueKind.Known)
        {
        }

        public CopyAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities)
            : this (analysisEntities, CopyAbstractValueKind.Known)
        {
            Debug.Assert(!analysisEntities.IsEmpty);
        }

        public CopyAbstractValue WithEntityRemoved(AnalysisEntity entityToRemove)
        {
            Debug.Assert(AnalysisEntities.Contains(entityToRemove));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind == CopyAbstractValueKind.Known);

            return new CopyAbstractValue(AnalysisEntities.Remove(entityToRemove));
        }

        public CopyAbstractValue WithEntitiesRemoved(IEnumerable<AnalysisEntity> entitiesToRemove)
        {
            Debug.Assert(entitiesToRemove.All(entityToRemove => AnalysisEntities.Contains(entityToRemove)));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind == CopyAbstractValueKind.Known);

            return new CopyAbstractValue(AnalysisEntities.Except(entitiesToRemove));
        }

        public ImmutableHashSet<AnalysisEntity> AnalysisEntities { get; }
        public CopyAbstractValueKind Kind { get; }

        protected override void ComputeHashCodeParts(ArrayBuilder<int> builder)
        {
            builder.Add(HashUtilities.Combine(AnalysisEntities));
            builder.Add(Kind.GetHashCode());
        }
    }
}
