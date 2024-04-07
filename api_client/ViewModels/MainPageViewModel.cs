using api_client.Utils;
using apiclient.Model;
using apiclient.Utils;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using System.Collections.ObjectModel;
using Point = OpenCvSharp.Point;

namespace apiclient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public class Item
    {
        public ImageSource Thumbnail { get; set; }
        public string OriginalFilePath { get; set; }
        public string ProcessedFilePath { get; set; }
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

    [ObservableProperty]
    private bool _isActivityIndicatorVisible;

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
                IsUploadButtonEnabled = SelectedItem != null && IsOriginalCurrentFileOpened && !IsActivityIndicatorVisible;
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


        IsOpenOriginalButtonEnabled = SelectedItem.ProcessedFilePath != null;
        IsOriginalCurrentFileOpened = true;

        try
        {
            CurrentVideoSource = await FileUtils.OpenVideoAsync(item.OriginalFilePath);
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
                CurrentVideoSource = await FileUtils.OpenVideoAsync(SelectedItem.OriginalFilePath);
                IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = true;
                IsUploadButtonEnabled = false;
            }
            else
            {
                CurrentVideoSource = await FileUtils.OpenVideoAsync(SelectedItem.ProcessedFilePath);
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
        if (SelectedItem.ProcessedFilePath is null)
        {
            await App.Current.MainPage.DisplayAlert("Внимание", "Сначала обработайте файл", "OK");
        }

        await FileUtils.SaveFileByDialog(SelectedItem.ProcessedFilePath);
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
                OriginalFilePath = file.FullPath,
                IsOriginalFileOpened = true
            });
        }

        SelectedItem = Imgs[^1]; // Устанавливаем активный элемент - последний открытый
    }

    private async Task UploadButtonClicked()
    {
        IsActivityIndicatorVisible = true;
        IsUploadButtonEnabled = false;

        bool isConnected = await _netUtils.CheckServerConnection();

        if (!isConnected)
        {
            StatusColor = Colors.Red;
            ConnectionStatus = "Отключено";
            IsUploadButtonEnabled = false;
            return;
        }

        try
        {
            var tempFilePath = SelectedItem.ProcessedFilePath = $"{Path.GetTempPath()}{Path.GetFileName(SelectedItem.OriginalFilePath)}-output.avi"; 

            using (var capture = new VideoCapture(SelectedItem.OriginalFilePath))
            {
                if (!capture.IsOpened())
                {
                    return;
                }

                var fps = capture.Fps;
                int frameCount = 0;
                var frameInterval = (int)Math.Round(fps * 0.5);

                var frameSize = new OpenCvSharp.Size(capture.FrameWidth, capture.FrameHeight);

                var writer = new VideoWriter(tempFilePath, FourCC.XVID, fps, frameSize);

                Mat frame = new Mat();

                List<FrameInfo> frameInfos = new List<FrameInfo>();

                while (true)
                {
                    capture.Read(frame);

                    if (frame.Empty())
                        break;

                    if (frameCount % frameInterval == 0)
                    {
                        frameInfos = await _netUtils.SendVideoFrameAsync(frame, SelectedItem.OriginalFilePath); //TODO: если выбрать другой файл по время обработки??
                        CurrentVideoSource = MediaSource.FromFile(tempFilePath);
                    }

                    foreach (var info in frameInfos)
                    {
                        Scalar color;

                        if (info.classname == "Ypal")
                        {
                            color = Scalar.Orange;
                        }
                        else
                        {
                            color = Scalar.Green;
                        }
                        Cv2.Rectangle(frame, new Point(info.xtl, info.ytl), new Point(info.xbr, info.ybr), color, 2);
                        Cv2.PutText(frame, info.classname, new Point(info.xtl + 10, info.ytl + 10), HersheyFonts.HersheySimplex, 1, color, 2); // TODO: всегда вмещать в экран
                    }

                    frameCount++;
                    writer.Write(frame);
                }

                writer.Release();
            }

            using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                var memoryStream = new MemoryStream();
                fileStream.CopyTo(memoryStream);
            }
            
            CurrentVideoSource = MediaSource.FromFile(tempFilePath);
            //File.Delete(tempFilePath);

            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = false;

            IsImageDetailsVisible = true;
            IsOpenOriginalButtonEnabled = true;
            IsActivityIndicatorVisible = false;

            IsUploadButtonEnabled = true;
        }
        catch (IOException)
        {
            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть. Удаление вызовет SelectionChangedHandler

            IsActivityIndicatorVisible = false;
            IsUploadButtonEnabled = true;
        }
        catch (Exception)
        {
            // Проверить интернет, вывести ошибку на экран?? 

            IsActivityIndicatorVisible = false;
            IsUploadButtonEnabled = true;
        }
    }
}