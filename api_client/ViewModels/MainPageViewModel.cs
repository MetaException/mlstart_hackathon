using api_client.Model;
using api_client.Utils;
using apiclient.Model;
using apiclient.Utils;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using System.Collections.ObjectModel;
using System.Globalization;
using Point = OpenCvSharp.Point;

namespace apiclient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public class Item
    {
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }
        public VideoResponseModel VideoInfo { get; set; }
        public bool IsOriginalFileOpened { get; set; }
    }

    private readonly NetUtils _netUtils;

    [ObservableProperty]
    private bool _isInternetErrorVisible;

    [ObservableProperty]
    private MediaSource _currentVideoSource;

    [ObservableProperty]
    private string _imageWidth;

    [ObservableProperty]
    private string _imageHeight;

    [ObservableProperty]
    private string _imageChannels;

    [ObservableProperty]
    private ObservableCollection<Item> _imgs = new ObservableCollection<Item>();

    [ObservableProperty]
    private Item _selectedItem;

    [ObservableProperty]
    private bool _isImageDetailsVisible = false;

    [ObservableProperty]
    private string _connectionStatus;

    [ObservableProperty]
    private Color _statusColor;

    [ObservableProperty]
    private bool _isUploadButtonEnabled = false;

    [ObservableProperty]
    private bool _isOpenOriginalButtonEnabled = false;

    [ObservableProperty]
    private bool _isOriginalCurrentFileOpened;

    public RelayCommand OpenOriginalButtonClickedCommand { get; }

    public RelayCommand UploadButtonClickedCommand { get; }

    public RelayCommand OpenFileCommand { get; }

    public RelayCommand<Item> SelectionChangedCommand { get; }

    public RelayCommand SaveFileCommand { get; }

    public MainPageViewModel(NetUtils netUtils)
    {
        _netUtils = netUtils;

        UploadButtonClickedCommand = new RelayCommand(async () => await UploadButtonClicked());
        OpenFileCommand = new RelayCommand(async () => await OpenFile());
        SelectionChangedCommand = new RelayCommand<Item>(async (item) => await SelectionChangedHandler(item));
        SaveFileCommand = new RelayCommand(async () => await SaveFile());
        OpenOriginalButtonClickedCommand = new RelayCommand(async () => await SwitchVideoView());

        _ = CheckServerConnection();
    }

    private async Task CheckServerConnection()
    {
        while (true)
        {
            bool isConnected = await _netUtils.CheckServerConnection();

            if (isConnected)
            {
                StatusColor = Colors.Green;
                ConnectionStatus = "Подключено";
                IsUploadButtonEnabled = SelectedItem != null && IsOriginalCurrentFileOpened;
            }
            else
            {
                StatusColor = Colors.Red;
                ConnectionStatus = "Отключено";
                IsUploadButtonEnabled = false;
            }
            await Task.Delay(1000);
        }
    }

    private async Task SelectionChangedHandler(Item item)
    {
        if (item == null)
        {
            CurrentVideoSource = null;
            IsUploadButtonEnabled = false;
            IsImageDetailsVisible = false;
            item.IsOriginalFileOpened = false;
            return;
        }


        IsOpenOriginalButtonEnabled = SelectedItem.VideoInfo != null;
        IsOriginalCurrentFileOpened = true;

        try
        {
            CurrentVideoSource = await FileUtils.OpenVideoAsync(item.FilePath);
        }
        catch
        {
            Imgs.Remove(item); // Удаляем файл, который не получилось открыть
            return;
        }

    }

    private async Task SwitchVideoView()
    {
        try
        {
            if (!IsOriginalCurrentFileOpened)
            {
                CurrentVideoSource = await FileUtils.OpenVideoAsync(SelectedItem.FilePath);
                IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = true;
                IsUploadButtonEnabled = false;
            }
            else
            {
                CurrentVideoSource = MediaSource.FromUri(SelectedItem.VideoInfo.video_path);
                IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = false;
                IsUploadButtonEnabled = true;
            }
        }
        catch
        {
            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть
        }
    }

    private async Task SaveFile()
    {
        var fileToSave = await _netUtils.DownloadFileToStream(SelectedItem.VideoInfo.video_path);
        await FileUtils.SaveFileByDialog(fileToSave);
    }

    private async Task OpenFile()
    {
        var files = await FileUtils.OpenFilesByDialog();

        if (!files.Any())
            return;

        foreach (var file in files)
        {
            Imgs.Add(new Item
            {
                Thumbnail = await FileUtils.GetVideoThumbnailsAsync(file, 640, 360),
                FilePath = file.FullPath,
                IsOriginalFileOpened = true
            });
        }

        SelectedItem = Imgs[^1]; // Устанавливаем активный элемент - последний открытый
    }

    private async Task UploadButtonClicked()
    {
        bool isConnected = await _netUtils.CheckServerConnection();

        if (!isConnected)
        {
            StatusColor = Colors.Red;
            ConnectionStatus = "Отключено";
            IsUploadButtonEnabled = false;
            return;
        }

        //"https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"

        try
        {
            var tempFilePath = Path.GetTempFileName();

            using (var capture = new VideoCapture(SelectedItem.FilePath))
            {
                if (!capture.IsOpened())
                {
                    return;
                }

                var frameSize = new OpenCvSharp.Size(capture.FrameWidth, capture.FrameHeight);

                var writer = new VideoWriter(tempFilePath, FourCC.XVID, capture.Fps, frameSize);

                Mat frame = new Mat();

                while (true)
                {
                    capture.Read(frame);

                    if (frame.Empty())
                        break;

                    var frameInfo = await _netUtils.SendVideoFrameAsync(frame, SelectedItem.FilePath);

                    foreach (var info in frameInfo)
                    {
                        Cv2.Rectangle(frame, new Point(info.xtl, info.ytl), new Point(info.xbr, info.ybr), Scalar.Red, 2);
                        Cv2.PutText(frame, info.classname, new Point(info.xtl + 10, info.ytl + 10), HersheyFonts.HersheyComplex, 1, Scalar.Green, 2); // TODO: всегда вмещать в экран
                    }

                    writer.Write(frame);
                }

                writer.Release();
            }

            using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                var memoryStream = new MemoryStream();
                fileStream.CopyTo(memoryStream);

            }

            CurrentVideoSource = MediaSource.FromFile("output.avi");

            File.Delete(tempFilePath); //TODO: баг с воспроизведением локального видео

            VideoResponseModel details;
            /*
            using (var fileStream = await FileUtils.OpenFileAsync(SelectedItem.File))
            {


                details = await _netUtils.SendVideoAsync(fileStream, Path.GetFileName(SelectedItem.File.FullPath));
            }

            SelectedItem.VideoInfo = details;
            */
            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = false;

            IsImageDetailsVisible = true;
            IsOpenOriginalButtonEnabled = true;
        }
        catch (IOException)
        {
            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть. Удаление вызовет SelectionChangedHandler
        }
        catch (Exception)
        {
            // Проверить интернет, вывести ошибку на экран?? 
        }
    }

    /*
    private async Task GetVideoPartsAsync(VideoResponseModel videoInfo)
    {
        List<Mat> frames;

        while (true)
        {
            frames = await _netUtils.TryGetVideoParts(videoInfo.ID, 0);

            if (frames)
                await Task.Delay(100);
        }

        int frameWidth = frames[0].Cols;
        int frameHeight = frames[0].Rows;
        double frameRate = 30; // Что делаеть если 120 fps 

        using (var fileStream = new FileStream("temp_video.avi", FileMode.Create))
        {
            // Создайте объект VideoWriter с временным файловым потоком
            using (var videoWriter = new VideoWriter("temp_video.avi", FourCC.XVID, frameRate, new Size(frameWidth, frameHeight)))
            {
                foreach (var frame in frames)
                {
                    videoWriter.Write(frame);
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                fileStream.Position = 0;
                fileStream.CopyTo(memoryStream);

                byte[] videoBytes = memoryStream.ToArray();
            }
        }

        File.Delete("temp_video.avi");
    }
    */
}