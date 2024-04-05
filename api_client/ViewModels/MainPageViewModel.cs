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
        public ImageInfo ImageInfo { get; set; }
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

    public RelayCommand UploadButtonClickedCommand { get; }

    public RelayCommand OpenFileCommand { get; }

    public RelayCommand<Item> SelectionChangedCommand { get; }

    public MainPageViewModel(NetUtils netUtils)
    {
        _netUtils = netUtils;

        UploadButtonClickedCommand = new RelayCommand(async () => await UploadButtonClicked());
        OpenFileCommand = new RelayCommand(async () => await OpenFile());
        SelectionChangedCommand = new RelayCommand<Item>(async (item) => await SelectionChangedHandler(item));

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
                IsUploadButtonEnabled = SelectedItem != null;
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
            return;
        }

        try
        {
            CurrentVideoSource = await FileUtils.OpenVideoAsync(item.File);
        }
        catch
        {
            Imgs.Remove(item); // Удаляем файл, который не получилось открыть
            return;
        }

        if (IsImageDetailsVisible = item.ImageInfo != null)
        {
            ImageWidth = $"Ширина: {item.ImageInfo.width}";
            ImageHeight = $"Высота: {item.ImageInfo.height}";
            ImageChannels = $"Количество каналов: {item.ImageInfo.channels}";
        }
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
                Thumbnail = await FileUtils.GetVideoThumbnailsAsync(file, 640, 360), //FileUtils.GenerateVideoThumbnail(file.FullPath),
                File = file
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

        throw new NotImplementedException("Не понятно в какой форме нужно загружать видео");

        try
        {
            ImageInfo details;
            using (var fileStream = await FileUtils.OpenFileAsync(SelectedItem.File))
            {
                details = await _netUtils.SendImageAsync(fileStream, Path.GetFileName(SelectedItem.File.FullPath));
            }

            ImageWidth = $"Ширина: {details.width}";
            ImageHeight = $"Высота: {details.height}";
            ImageChannels = $"Количество каналов: {details.channels}";

            SelectedItem.ImageInfo = details;
            IsImageDetailsVisible = true;
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