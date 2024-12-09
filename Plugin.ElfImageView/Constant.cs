using System;
using Plugin.ElfImageView.Properties;

namespace Plugin.ElfImageView
{
	/// <summary>Константы приложения</summary>
	internal static class Constant
	{
		/// <summary>Наименование бинарного файла загруженного из памяти</summary>
		public const String BinaryFile = "Binary";

		/// <summary>Получить наименование заголовка</summary>
		/// <param name="type">Тип заголовка</param>
		/// <returns>Наименование заголовка</returns>
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