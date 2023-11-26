namespace OnlineStatusLight.Core.Exceptions
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException() : base()
        {
        }

        public ConfigurationException(string? message) : base(message)
        {
        }
    }
}