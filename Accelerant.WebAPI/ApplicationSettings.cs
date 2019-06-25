namespace Accelerant.WebAPI
{
    public class Authentication
    {
        public string Secret { get; set; }
    }
    public class ApplicationSettings
    {
        public Authentication Authentication { get; set; }
    }
}
