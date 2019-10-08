using System.Diagnostics.Tracing;

namespace Analyzer.Utilities
{
    internal sealed class FcaEventSource : EventSource
    {
        public void StartDfa(string analysisType, string target, int operationHashCode, int analysisContextHashCode)
        {
            WriteEvent(1, analysisType, target, operationHashCode, analysisContextHashCode);
        }

        public void EndDfa(string analysisType, string target, int operationHashCode, int analysisContextHashCode, long elapsedMilliseconds)
        {
            WriteEvent(2, analysisType, target, operationHashCode, analysisContextHashCode, elapsedMilliseconds);
        }

        public void StartTaintedDataAnalysis(string target, int cfgHashCode)
        {
            WriteEvent(3, target, cfgHashCode);
        }

        public void EndTaintedDataAnalysis(string target, int cfgHashCode, int analysisContextHashCode)
        {
            WriteEvent(4, target, cfgHashCode, analysisContextHashCode);
        }

        public void StartPointsToAnalysis(string target, int cfgHashCode)
        {
            WriteEvent(5, target, cfgHashCode);
        }

        public void EndPointsToAnalysis(string target, int cfgHashCode, int analysisContextHashCode)
        {
            WriteEvent(6, target, cfgHashCode, analysisContextHashCode);
        }

        public void StartValueContentAnalysis(string target, int cfgHashCode)
        {
            WriteEvent(7, target, cfgHashCode);
        }

        public void EndValueContentAnalysis(string target, int cfgHashCode, int analysisContextHashCode)
        {
            WriteEvent(8, target, cfgHashCode, analysisContextHashCode);
        }

        public static readonly FcaEventSource Log = new FcaEventSource();
    }
}
