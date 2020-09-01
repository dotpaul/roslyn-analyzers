﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpCreateTestAccessor>;
using VerifyVB = Test.Utilities.VisualBasicCodeRefactoringVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicCreateTestAccessor>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CreateTestAccessorTests
    {
        [Theory]
        [InlineData("$$class TestClass ")]
        [InlineData("class $$TestClass ")]
        [InlineData("class TestClass$$ ")]
        [InlineData("class [|TestClass|] ")]
        [InlineData("[|class TestClass|] ")]
        public async Task CreateTestAccessorCSharp(string typeHeader)
        {
            var source = typeHeader + @"{
}";
            var fixedSourceBody = @"{
    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly TestClass _testClass;

        internal TestAccessor(TestClass testClass)
        {
            _testClass = testClass;
        }
    }
}";

            var fixedSource = "class TestClass " + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData("$$struct TestStruct ")]
        [InlineData("struct $$TestStruct ")]
        [InlineData("struct TestStruct$$ ")]
        [InlineData("struct [|TestStruct|] ")]
        [InlineData("[|struct TestStruct|] ")]
        public async Task CreateTestAccessorStructCSharp(string typeHeader)
        {
            var source = typeHeader + @"{
}";
            var fixedSourceBody = @"{
    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly TestStruct _testStruct;

        internal TestAccessor(TestStruct testStruct)
        {
            _testStruct = testStruct;
        }
    }
}";

            var fixedSource = "struct TestStruct " + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyCS.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData(TypeKind.Delegate)]
        [InlineData(TypeKind.Enum)]
        [InlineData(TypeKind.Interface)]
        public async Task UnsupportedTypeCSharp(TypeKind typeKind)
        {
            var declaration = typeKind switch
            {
                TypeKind.Delegate => "delegate void $$Method();",
                TypeKind.Enum => "public enum $$SomeType { }",
                TypeKind.Interface => "public interface $$SomeType { }",
                _ => throw new NotSupportedException(),
            };

            await VerifyCS.VerifyRefactoringAsync(declaration, declaration);
        }

        [Theory]
        [InlineData("$$Class TestClass")]
        [InlineData("Class $$TestClass")]
        [InlineData("Class TestClass$$")]
        [InlineData("Class [|TestClass|]")]
        [InlineData("[|Class TestClass|]")]
        public async Task CreateTestAccessorVisualBasic(string typeHeader)
        {
            var source = $@"{typeHeader}
End Class";
            var fixedSourceBody = @"
    Friend Function GetTestAccessor() As TestAccessor
        Return New TestAccessor(Me)
    End Function

    Friend Structure TestAccessor
        Private ReadOnly _testClass As TestClass

        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";

            var fixedSource = "Class TestClass" + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync(source, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData("$$Structure TestStructure")]
        [InlineData("Structure $$TestStructure")]
        [InlineData("Structure TestStructure$$")]
        [InlineData("Structure [|TestStructure|]")]
        [InlineData("[|Structure TestStructure|]")]
        public async Task CreateTestAccessorStructureVisualBasic(string typeHeader)
        {
            var source = $@"{typeHeader}
End Structure";
            var fixedSourceBody = @"
    Friend Function GetTestAccessor() As TestAccessor
        Return New TestAccessor(Me)
    End Function

    Friend Structure TestAccessor
        Private ReadOnly _testStructure As TestStructure

        Friend Sub New(testStructure As TestStructure)
            _testStructure = testStructure
        End Sub
    End Structure
End Structure";

            var fixedSource = "Structure TestStructure" + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync(source, fixedSource);

            // Applying the refactoring a second time does not produce any changes
            fixedSource = typeHeader + fixedSourceBody;
            await VerifyVB.VerifyRefactoringAsync(fixedSource, fixedSource);
        }

        [Theory]
        [InlineData(TypeKind.Delegate)]
        [InlineData(TypeKind.Enum)]
        [InlineData(TypeKind.Interface)]
        [InlineData(TypeKind.Module)]
        public async Task UnsupportedTypeVisualBasic(TypeKind typeKind)
        {
            var declaration = typeKind switch
            {
                TypeKind.Delegate => "Delegate Function $$SomeType() As Integer",
                TypeKind.Enum => "Enum $$SomeType\r\n    Member\r\nEnd Enum",
                TypeKind.Interface => "Interface $$SomeType\r\nEnd Interface",
                TypeKind.Module => "Module $$SomeType\r\nEnd Module",
                _ => throw new NotSupportedException(),
            };

            await VerifyVB.VerifyRefactoringAsync(declaration, declaration);
        }
    }
}
