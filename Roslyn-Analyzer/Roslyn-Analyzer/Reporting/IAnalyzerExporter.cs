using System;
using System.IO;

namespace RoslynAnalyzer.CLI.Reporting
{
    public interface IAnalyzerExporter
    {
        void AppendDiagnostic(DiagnosticInfo diagnosticInfo);
        void Finish(TimeSpan duration);
        void InitializeExporter(FileInfo projectFile);
    }
}