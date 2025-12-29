namespace RESTClientRunner.Configuration
{
    internal class AppSettings
    {
        public ApplicationSettings Application { get; set; } = new();
    }

    public class ApplicationSettings
    {
        public string Name { get; set; } = "";
        public string LogFilePath { get; set; } = "";
        public string DefaultCollectionUrl { get; set; } = "";
    }
}
