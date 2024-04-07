using CommunityToolkit.Maui.Storage;
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

    public static async Task SaveFileByDialog(string sourcePath)
    {
        var file = await OpenFileAsync(sourcePath);

        var a = await FileSaver.Default.SaveAsync(Path.GetFileName(sourcePath), file);

        //File.Move(sourcePath, a.FilePath);
    }

    public static async Task<MediaSource> OpenVideoAsync(string path)
    {
        return MediaSource.FromFile(path);  
    }

    public static async Task<Stream> OpenFileAsync(string path)
    {
        try
        {
            return File.OpenRead(path);
        }
        catch (Exception ex)
        {
            Log.Error($"Не удалось открыть файл по указанному пути: {path}: {ex.Message}");
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
}