using System.Text;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup
{
    public static class RoomName
    {
        public const int Length = 8;
        public static bool IsAllowed(char c)
        {
            return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
        }

        public static string Normalize(string value)
        {
            var result = new StringBuilder(Length);
            foreach (var c in value ?? "")
            {
                if (IsAllowed(c))
                    result.Append(c);
                if (result.Length == Length)
                    break;
            }

            return result.ToString();
        }

        public static bool IsValid(string value)
        {
            return value != null && value.Length == Length && Normalize(value) == value;
        }
    }
}
