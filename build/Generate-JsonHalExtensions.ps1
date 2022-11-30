$ErrorActionPreference = 'Stop'

$solutionDirectory = Split-Path $PSScriptRoot
$outputFile = Join-Path $solutionDirectory 'src' 'LogOtter.JsonHal' 'JsonHalLinkCollectionExtensions.generated.cs'

$predefinedLinkTypes = @('Self', 'First', 'Prev', 'Next', 'Last')

$contents = "
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
#nullable enable

using System.CodeDom.Compiler;

namespace LogOtter.JsonHal;

[GeneratedCode(""Powershell Script"", ""1.0.0"")]
public static partial class JsonHalLinkCollectionExtensions
{".TrimStart()

foreach($linkType in $predefinedLinkTypes) {
    $contents += "
    public static JsonHalLink? Get$($linkType)Link(this JsonHalLinkCollection collection)
    {
        return collection.GetLink(""$($linkType.ToLowerInvariant())"");
    }
    
    public static string? Get$($linkType)Href(this JsonHalLinkCollection collection)
    {
        return collection.GetLink(""$($linkType.ToLowerInvariant())"")?.Href;
    }
    
    public static void Add$($linkType)Link(this JsonHalLinkCollection collection, string href)
    {
        collection.AddLink(""$($linkType.ToLowerInvariant())"", href);
    }
"
}
 
$contents += "
}
".Trim()

$contents | Out-File $outputFile