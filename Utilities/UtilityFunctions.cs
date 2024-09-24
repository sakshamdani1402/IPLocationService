using System.Text.RegularExpressions;

namespace Location.Utilities
{
    public static class UtilityFunctions
    {
        public static bool IsValidIpAddress(string ipAddress)
        {
            string IpAddressPattern = @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
            try
            {
                // Specify a 1-second timeout for the regex match
                var match = Regex.Match(ipAddress, IpAddressPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                return match.Success;
            }
            catch (RegexMatchTimeoutException)
            {
                Console.WriteLine("Regex matching timed out.");
                return false;
            }
        }
    }
}