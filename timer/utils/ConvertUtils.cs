using System.Text.RegularExpressions;

namespace timer.utils;

public class ConvertUtils
{
    public static long MainStringToMs(string str)
    {
        if (str.Contains(".") || str.Contains(":"))
            return StringToMs(str);
        return StringToMs2(str);
    }
    
    public static long StringToMs(string str)
    {
        str = str.Replace(":", ".");
        string[] parts = str.Split(".").Reverse().ToArray();
        
        DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(0);

        if (parts.Length >= 1) time = time.AddSeconds(ParseInt(parts[0]));
        if (parts.Length >= 2) time = time.AddMinutes(ParseInt(parts[1]));
        if (parts.Length >= 3) time = time.AddHours(ParseInt(parts[2]));
        if (parts.Length >= 4) time = time.AddDays(ParseInt(parts[3]));

        return GetDuration(time);
    }

    public static long StringToMs2(string str)
    {
        DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(0);
        string[] parts = str.Split(" ");

        foreach (var part in parts)
        {
            var partChars = part.ToCharArray();
            var lastChar = partChars[part.Length - 1];
            switch (lastChar)
            {
                case 's':
                    time = time.AddSeconds(ParseInt(new string(partChars.SkipLast(1).ToArray())));
                    break;
                case 'm':
                    time = time.AddMinutes(ParseInt(new string(partChars.SkipLast(1).ToArray())));
                    break;
                case 'h':
                    time = time.AddHours(ParseInt(new string(partChars.SkipLast(1).ToArray())));
                    break;
                case 'd':
                    time = time.AddDays(ParseInt(new string(partChars.SkipLast(1).ToArray())));
                    break;
            }
        }
        return GetDuration(time);
    }

    public static int ParseInt(string str)
    {
        if (Int32.TryParse(str, out int result)) return result;
        return 0;
    }

    public static long GetDuration(DateTimeOffset d)
    {
        DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(0);
        var duration = d.ToUnixTimeSeconds() - time.ToUnixTimeSeconds();
        return duration;
    }
}