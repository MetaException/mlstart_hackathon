using api_client.Model;
using api_client.Utils;
using apiclient.Model;
using apiclient.Utils;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace apiclient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public class Item
    {
        public ImageSource Thumbnail { get; set; }
        public FileResult File { get; set; }
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
            CurrentVideoSource = await FileUtils.OpenVideoAsync(item.File);
        }
        catch
        {
            Imgs.Remove(item); // Удаляем файл, который не получилось открыть
            return;
        }

        /*
        if (IsImageDetailsVisible = item.ImageInfo != null)
        {
            ImageWidth = $"Ширина: {item.ImageInfo.width}";
            ImageHeight = $"Высота: {item.ImageInfo.height}";
            ImageChannels = $"Количество каналов: {item.ImageInfo.channels}";
        }*/
    }

    private async Task SwitchVideoView()
    {
        try
        {
            if (!IsOriginalCurrentFileOpened)
            {
                CurrentVideoSource = await FileUtils.OpenVideoAsync(SelectedItem.File);
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
                File = file,
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
            VideoResponseModel details;
            using (var fileStream = await FileUtils.OpenFileAsync(SelectedItem.File))
            {
                details = new VideoResponseModel { video_path = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }; //await _netUtils.SendVideoAsync(fileStream, Path.GetFileName(SelectedItem.File.FullPath));
            }

            //ImageWidth = $"Ширина: {details.width}";
            //ImageHeight = $"Высота: {details.height}";
            //ImageChannels = $"Количество каналов: {details.channels}";

            SelectedItem.VideoInfo = details;
            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = false;

            CurrentVideoSource = MediaSource.FromUri(details.video_path);

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
}