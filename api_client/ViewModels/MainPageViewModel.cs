using apiclient.Model;
using apiclient.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Serilog;

namespace apiclient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public class Item
    {
        public ImageSource ItemImageSource { get; set; }
        public string FilePath { get; set; }
        public ImageInfo ImageInfo { get; set; }
    }

    private readonly NetUtils _netUtils;

    [ObservableProperty]
    private bool _isInternetErrorVisible;

    [ObservableProperty]
    private ImageSource _imgSource;

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
    private bool _isUploadButtonVisible = false;

    [ObservableProperty]
    private bool _isImageDetailsVisible = false;

    [ObservableProperty]
    private string _connectionStatus;

    [ObservableProperty]
    private Color _statusColor;

    [ObservableProperty]
    private bool _isUploadButtonEnabled;

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
                IsUploadButtonEnabled = true;
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
        ImgSource = ImageSource.FromFile(item.FilePath);

        if (IsImageDetailsVisible = item.ImageInfo is not null)
        {
            ImageWidth = $"Ширина: {item.ImageInfo.width}";
            ImageHeight = $"Высота: {item.ImageInfo.height}";
            ImageChannels = $"Количество каналов: {item.ImageInfo.channels}";
        }
    }

    private async Task OpenFile()
    {
        var results = await FilePicker.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Выбирите изображения",
            FileTypes = FilePickerFileType.Images
        });

        if (!results.Any())
            return;

        Log.Information($"Opened {results.Count()} files");

        foreach (var file in results)
        {
            Imgs.Add(new Item { ItemImageSource = ImageSource.FromFile(file.FullPath), FilePath = file.FullPath}); //TODO: сжимать фото для миниатюр
        }

        SelectedItem = Imgs[^1];

        IsUploadButtonVisible = true;
    }

    private async Task UploadButtonClicked()
    {
        if (SelectedItem is null)
            return;

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
            var fileStream = File.OpenRead(SelectedItem.FilePath); //TODO: Вынести работу с файлами в отедельный класс

            var details = await _netUtils.SendImageAsync(fileStream, Path.GetFileName(SelectedItem.FilePath));

            if (details is null)
            {
                throw new ArgumentNullException();
            }

            ImageWidth = $"Ширина: {details.width}";
            ImageHeight = $"Высота: {details.height}";
            ImageChannels = $"Количество каналов: {details.channels}";

            SelectedItem.ImageInfo = details;
            IsImageDetailsVisible = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
}