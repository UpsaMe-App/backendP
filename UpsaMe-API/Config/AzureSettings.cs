namespace UpsaMe_API.Config
{
    public class AzureSettings
    {
        public string BlobConnectionString { get; set; } = string.Empty;
        public string ProfilePhotosContainer { get; set; } = "profile-photos";
        public string PostImagesContainer { get; set; } = "post-images";
        public string ReplyImagesContainer { get; set; } = "reply-images";
    }
}