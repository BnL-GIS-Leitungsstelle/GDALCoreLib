using System;
using System.ComponentModel;

namespace GdalToolsLib.Extensions;

public static class EnumHelper
{

    /// <summary>
    /// returns the description string of the given enum value of given type
    /// </summary>
    /// <param name="enumeration">enumaration value</param>
    /// <param name="type">typeOf(enum)</param>
    /// <returns></returns>
    public static string GetEnumDescription(this Enum enumeration, Type type)
    {
        var memInfo = type.GetMember(enumeration.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        return ((DescriptionAttribute)attributes[0]).Description;
    }
}