namespace Location.Models
{
    public class Provider
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public int LimitPerMinute { get; set; }
        public string Token { get; set; }
    }
}
