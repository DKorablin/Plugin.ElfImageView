using System;
using Plugin.ElfImageView.Properties;

namespace Plugin.ElfImageView
{
	/// <summary>Application Constants</summary>
	internal static class Constant
	{
		/// <summary>Name of the binary file loaded from memory</summary>
		public const String BinaryFile = "Binary";

		/// <summary>Get the header name</summary>
		/// <param name="type">Header type</param>
		/// <returns>Header name</returns>
		public static String GetHeaderName(ElfItemType type)
		{
			switch(type)
			{
			case ElfItemType.Header:			return Resources.Section_Header;
			case ElfItemType.Identification:	return Resources.Section_Identification;
			case ElfItemType.Sections:			return Resources.Section_Sections;
			case ElfItemType.StringTables:		return Resources.Section_StringTables;
			case ElfItemType.Symbols:			return Resources.Section_Symbols;
			case ElfItemType.Relocations:		return Resources.Section_Relocations;
			case ElfItemType.RelocationsA:		return Resources.Section_RelocationsA;
			case ElfItemType.Notes:				return Resources.Section_Notes;
			case ElfItemType.DebugStrings:		return Resources.Section_DebugStrings;
			default:
				throw new NotImplementedException($"Type {type} not implemented");
			}
		}
	}
}