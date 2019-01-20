﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Represents an abstract index into a location.
    /// It is used by an <see cref="AnalysisEntity"/> for operations such as an <see cref="IArrayElementReferenceOperation"/>, index access <see cref="IPropertyReferenceOperation"/>, etc.
    /// </summary>
    internal abstract partial class AbstractIndex : CacheBasedEquatable2<AbstractIndex>
    {
        public static AbstractIndex Create(int index) => new ConstantValueIndex(index);
        public static AbstractIndex Create(AnalysisEntity analysisEntity) => new AnalysisEntityBasedIndex(analysisEntity);
        public static AbstractIndex Create(IOperation operation) => new OperationBasedIndex(operation);
    }
}
