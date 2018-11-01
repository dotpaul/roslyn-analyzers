using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetFramework.Analyzers.Helpers;

namespace Microsoft.NetFramework.Analyzers.UnitTests
{
    public class DoNotUseInsecureDtdProcessingAnalyzerNetFx451Tests : DoNotUseInsecureDtdProcessingAnalyzerTests
    {
        /// <summary>
        /// What <see cref="SecurityDiagnosticHelpers.GetDotNetFrameworkVersion(CodeAnalysis.Compilation)"/>
        /// returns for .NET Framework 4.5.1.
        /// </summary>
        private static readonly Version NetFx451Version = new Version(4, 5, 1);

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DoNotUseInsecureDtdProcessingAnalyzer() { UnitTestNetFrameworkVersion = NetFx451Version };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DoNotUseInsecureDtdProcessingAnalyzer() { UnitTestNetFrameworkVersion = NetFx451Version };
        }
    }
}
