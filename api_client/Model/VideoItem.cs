namespace api_client.Model;

public class VideoItem
{
    public ImageSource Thumbnail { get; set; }
    public string OriginalFilePath { get; set; }
    public string ProcessedFilePath { get; set; }
    public bool IsOriginalFileOpened { get; set; }
}