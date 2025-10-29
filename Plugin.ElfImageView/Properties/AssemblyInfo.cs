using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("1c34fd01-39a7-4774-b330-60d33b3c1b0d")]
[assembly: System.CLSCompliant(false)]

[assembly: AssemblyDescription("Read Executable and Linkable Format image viewer")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2016-2025")]

/*if $(ConfigurationName) == Release (
..\..\..\..\ILMerge.exe  "/out:$(ProjectDir)..\bin\$(TargetFileName)" "$(TargetPath)" "$(TargetDir)ElfReader.dll" "/lib:..\..\..\SAL\bin"
)*/