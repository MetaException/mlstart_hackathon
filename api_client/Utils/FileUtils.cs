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
            Log.Information($"Выбрано {results.Count()} файлов.");
        }

        return results;
    }

    public static async Task SaveFileStreamByDialog(string fileName, Stream stream)
    {
        await FileSaver.Default.SaveAsync(fileName, stream);
        Log.Information("Файл сохранён");
    }

    public static async Task SaveFileByDialog(string sourcePath)
    {
        var file = OpenFileAsync(sourcePath);
        Log.Debug($"Сохранение файла по пути: {sourcePath}");

        await FileSaver.Default.SaveAsync(Path.GetFileName(sourcePath), file);
        Log.Information("Файл сохранён");
    }

    public static MediaSource OpenVideoAsync(string path)
    {
        Log.Debug($"Открытие видео-файла по пути: {path}");
        return MediaSource.FromFile(path);  
    }

    public static Stream OpenFileAsync(string path)
    {
        Log.Debug($"Открытие файла по пути: {path}");
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
        Log.Debug($"Создание миниатюры для видео для файла по пути: {file.FullPath}");

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