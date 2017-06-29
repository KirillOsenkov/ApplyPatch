using System.IO;

abstract class Edit
{
    public abstract void Apply();
}

abstract class FileEdit : Edit
{
    public string FilePath { get; set; }
}

class ReplaceEdit : FileEdit
{
    public string Pattern { get; set; }
    public string Replacement { get; set; }

    public override void Apply()
    {
        var text = File.ReadAllText(FilePath);
        text = text.Replace(Pattern, Replacement);
        File.WriteAllText(FilePath, text);
    }
}

class InsertLineEdit : FileEdit
{
    public string PatternLine { get; set; }
    public string LineToInsertAfterPatternLine { get; set; }

    public override void Apply()
    {
        var text = File.ReadAllText(FilePath);
        var patternStart = text.IndexOf(PatternLine);
        if (patternStart == -1)
        {
            return;
        }

        var lineStart = patternStart;
        while (lineStart > 0 && char.IsWhiteSpace(text[lineStart - 1]) && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        var lineBreak = "";
        var lineEnd = patternStart + PatternLine.Length;
        if (text.Length > lineEnd + 1)
        {
            if (text[lineEnd] == '\r' && text[lineEnd + 1] == '\n')
            {
                lineBreak = "\r\n";
            }
            else if (text[lineEnd] == '\n')
            {
                lineBreak = "\n";
            }
        }

        var indent = text.Substring(lineStart, patternStart - lineStart);
        var newLineStart = patternStart + PatternLine.Length + lineBreak.Length + indent.Length;
        var newLineLength = LineToInsertAfterPatternLine.Length;
        if (text.Length > newLineStart + newLineLength && text.Substring(newLineStart, newLineLength) == LineToInsertAfterPatternLine)
        {
            // don't apply the patch if already applied
            return;
        }

        text = text.Replace(PatternLine, PatternLine + lineBreak + indent + LineToInsertAfterPatternLine);
        File.WriteAllText(FilePath, text);
    }
}

class Program
{
    private static string repoRoot = @"C:\monodevelop\main";
    private static string msbuildBin = @"C:\MSBuild\bin\Bootstrap\MSBuild\15.0\Bin";
    private static string msbuildOSSBinDir = @"<MSBuild_OSS_BinDir Condition=""'$(OS)' == 'Windows_NT'"">$(MSBuildToolsPath)\</MSBuild_OSS_BinDir>";
    private static string msbuildOSSBinDirReplacement = $@"<MSBuild_OSS_BinDir Condition=""'$(OS)' == 'Windows_NT'"">{msbuildBin}\</MSBuild_OSS_BinDir>";
    private static Edit[] Edits = new Edit[]
    {
        new ReplaceEdit
        {
            FilePath = $@"{repoRoot}\src\core\MonoDevelop.Core\MonoDevelop.Core.csproj",
            Pattern = msbuildOSSBinDir,
            Replacement = msbuildOSSBinDirReplacement
        },
        new InsertLineEdit
        {
            FilePath = $@"{repoRoot}\src\core\MonoDevelop.Core\MonoDevelop.Core\Runtime.cs",
            PatternLine = $@"var path = systemAssemblyService.CurrentRuntime.GetMSBuildBinPath (""15.0"");",
            LineToInsertAfterPatternLine = $@"path = @""{msbuildBin}"";"
        },
        new ReplaceEdit
        {
            FilePath = $@"{repoRoot}\src\core\MonoDevelop.Projects.Formats.MSBuild\MonoDevelop.MSBuildResolver\MonoDevelop.MSBuildResolver.csproj",
            Pattern = msbuildOSSBinDir,
            Replacement = msbuildOSSBinDirReplacement
        }
    };

    static void Main(string[] args)
    {
        foreach (var edit in Edits)
        {
            edit.Apply();
        }
    }
}
