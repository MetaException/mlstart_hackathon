using Serilog;

namespace api_client.Utils;

public static class FileUtils
{
    public static async Task<IEnumerable<FileResult>> OpenFilesByDialog()
    {
        var results = await FilePicker.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Выбирите изображения",
            FileTypes = FilePickerFileType.Images
        });

        if (results.Any())
        {
            Log.Information($"Opened {results.Count()} files");
        }

        return results;
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

    public static ImageSource GenerateThumbnail(ImageSource original, int width, int height)
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