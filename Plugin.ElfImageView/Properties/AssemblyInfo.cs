using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("1c34fd01-39a7-4774-b330-60d33b3c1b0d")]
[assembly: System.CLSCompliant(false)]

#if NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://dkorablin.ru/project/Default.aspx?File=112")]
#else

[assembly: AssemblyDescription("Read Executable and Linkable Format image viewer")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2016-2024")]
#endif

/*if $(ConfigurationName) == Release (
..\..\..\..\ILMerge.exe  "/out:$(ProjectDir)..\bin\$(TargetFileName)" "$(TargetPath)" "$(TargetDir)ElfReader.dll" "/lib:..\..\..\SAL\bin"
)*/