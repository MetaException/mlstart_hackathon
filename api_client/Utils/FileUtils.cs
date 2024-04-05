using CommunityToolkit.Maui.Views;
using Serilog;
using Windows.Media.Editing;
using Windows.Storage;

namespace api_client.Utils;

public static class FileUtils
{
    public static async Task<IEnumerable<FileResult>> OpenFilesByDialog()
    {
        var results = await FilePicker.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Выбирите видео",
            FileTypes = FilePickerFileType.Videos
        });

        if (results.Any())
        {
            Log.Information($"Opened {results.Count()} files");
        }

        return results;
    }

    public static async Task<MediaSource> OpenVideoAsync(FileResult file)
    {
        return MediaSource.FromFile(file.FullPath);  
    }

    public static async Task<ImageSource> OpenImageAsync(FileResult file)
    {
        var stream = await OpenFileAsync(file);
        return ImageSource.FromStream(() => stream);
    }

    public static async Task<Stream> OpenFileAsync(FileResult file)
    {
        try
        {
            return await file.OpenReadAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось открыть файл по указанному пути: {file.FullPath}: {ex.Message}");
            throw;
        }
    }

    public static async Task<ImageSource> GetVideoThumbnailsAsync(FileResult file, int width, int height)
    {
        TimeSpan getFrameInTime = new TimeSpan(0, 0, 1);

        var yourClip = await MediaClip.CreateFromFileAsync(await StorageFile.GetFileFromPathAsync(file.FullPath));
        var composition = new MediaComposition();

        composition.Clips.Add(yourClip);

        var yourImageStream = await composition.GetThumbnailAsync(
           getFrameInTime,
           Convert.ToInt32(width),
           Convert.ToInt32(height),
           VideoFramePrecision.NearestFrame);

        return ImageSource.FromStream(() => yourImageStream.AsStream());

    }

    public static ImageSource GenerateImageThumbnail(ImageSource original, int width, int height)
    {
        var image = new Image { Source = original };
        image.Aspect = Aspect.AspectFill;

        var scaledImage = new Image
        {
            Source = image.Source,
            WidthRequest = width,
            HeightRequest = height
        };

        return scaledImage.Source;
    }
}