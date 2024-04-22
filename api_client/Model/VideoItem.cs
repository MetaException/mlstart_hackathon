using System.Collections.ObjectModel;

namespace api_client.Model;

public partial class VideoItem
{
    public ImageSource Thumbnail { get; set; }
    public string OriginalFilePath { get; set; }
    public string ProcessedFilePath { get; set; }
    public bool IsOriginalFileOpened { get; set; }
    public ObservableCollection<TimeCodeModel> TimeCodes  { get; set; }
}

public class TimeCodeModel
{
    public double TimeCode { get; set; }
}