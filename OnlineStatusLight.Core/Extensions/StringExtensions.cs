namespace OnlineStatusLight.Core.Extensions
{
    public static class StringExtensions
    {
        public static TEnum? GetEnumValue<TEnum>(this string value) where TEnum : struct
        {
            if (Enum.TryParse(value, true, out TEnum result))
                return result;

            return null;
        }
    }
}