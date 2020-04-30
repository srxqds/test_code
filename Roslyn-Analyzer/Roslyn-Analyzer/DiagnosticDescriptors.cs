using Microsoft.CodeAnalysis;
using RoslynAnalyzer.Readonly;

namespace RoslynAnalyzer
{
    static class DiagnosticDescriptors
    {


        public static readonly DiagnosticDescriptor DoNotModifyReadonlyArray = new DiagnosticDescriptor(
            id: DiagnosticIDs.DoNotModifyReadonlyContent,
            title: new LocalizableResourceString(nameof(DoNotModifyReadonlyResource.Title), DoNotModifyReadonlyResource.ResourceManager, typeof(DoNotModifyReadonlyResource)),
            messageFormat: new LocalizableResourceString(nameof(DoNotModifyReadonlyResource.MessageFormat), DoNotModifyReadonlyResource.ResourceManager, typeof(DoNotModifyReadonlyResource)),
            category: DiagnosticCategories.Miscellaneous,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(DoNotModifyReadonlyResource.Description), DoNotModifyReadonlyResource.ResourceManager, typeof(DoNotModifyReadonlyResource))

        );
    }
}
