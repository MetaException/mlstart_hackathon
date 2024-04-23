using api_client.Configuration;
using api_client.Model;
using api_client.Utils;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Point = OpenCvSharp.Point;

namespace api_client.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private double interval;

    private readonly NetUtils _netUtils;
    private readonly ConfigurationManager _configuration;

    [ObservableProperty]
    private MediaSource _currentVideoSource;

    [ObservableProperty]
    private ObservableCollection<VideoItem> _imgs = new ObservableCollection<VideoItem>();

    [ObservableProperty]
    private VideoItem _selectedItem;

    [ObservableProperty]
    private bool _isUploadButtonEnabled = false;

    [ObservableProperty]
    private bool _isOpenOriginalButtonEnabled = false;

    [ObservableProperty]
    private bool _isOriginalCurrentFileOpened;

    [ObservableProperty]
    private bool _isActivityIndicatorVisible;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private TimeCodeModel _selectedTimeCode;

    [ObservableProperty]
    private MediaElement _videoPlayer;

    [ObservableProperty]
    private ImageSource _imagePlayerSource;

    public RelayCommand OpenOriginalButtonClickedCommand { get; }

    public AsyncRelayCommand UploadButtonClickedCommand { get; }

    public AsyncRelayCommand OpenFileCommand { get; }

    public RelayCommand SelectionChangedCommand { get; }

    public AsyncRelayCommand SaveFileCommand { get; }

    public RelayCommand<string> SetFrameSettingIntervalCommand { get; }

    public AsyncRelayCommand OpenSettingsCommand { get; }

    public AsyncRelayCommand TimeCodeSelectionChangedCommand { get; }

    public AsyncRelayCommand SaveFrameCommand { get; }

    public MainPageViewModel(NetUtils netUtils, ConfigurationManager configuration)
    {
        Log.Debug("Открытие главной страницы.");

        _netUtils = netUtils;
        _configuration = configuration;

        UploadButtonClickedCommand = new AsyncRelayCommand(UploadButtonClicked);
        OpenFileCommand = new AsyncRelayCommand(OpenFile);
        SaveFileCommand = new AsyncRelayCommand(SaveFile);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsPage);
        SelectionChangedCommand = new RelayCommand(SelectionChangedHandler);
        OpenOriginalButtonClickedCommand = new RelayCommand(SwitchVideoView);
        TimeCodeSelectionChangedCommand = new AsyncRelayCommand(TimeCodeSelectionChanged);
        SaveFrameCommand = new AsyncRelayCommand(SaveFrame);

        LoadFromConfiguration();

        _ = CheckServerConnection();
    }

    private async Task TimeCodeSelectionChanged()
    {
        if (SelectedTimeCode != null) // BUG: Если выбрать видео в списке, то второй раз уже не будет работать
        {
            await VideoPlayer.SeekTo(TimeSpan.FromSeconds(SelectedTimeCode.TimeCode));
            SelectedTimeCode = null; // ???
        }
    }

    private async Task SaveFrame()
    {
        if (SelectedItem == null)
        {
            return;
        }

        string path = IsOriginalCurrentFileOpened ? SelectedItem.OriginalFilePath : SelectedItem.ProcessedFilePath; // Краш при ошибке открытия

        try
        {
            using (var capture = new VideoCapture(path))
            {
                if (!capture.IsOpened())
                {
                    throw new IOException($"Ошибка открытия файла");
                }

                capture.Set(VideoCaptureProperties.PosMsec, VideoPlayer.Position.TotalMilliseconds);

                Mat frame = new Mat();

                capture.Read(frame);

                if (frame.Empty())
                {
                    return; //...
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    frame.ToMemoryStream(".jpg").CopyTo(memoryStream);
                    await FileUtils.SaveFileStreamByDialog(".jpg", memoryStream);
                }

                frame.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
   }

    private Task OpenSettingsPage()
    {
        Log.Debug("Главная страница. Открытие страницы с настройками.");

        return Shell.Current.GoToAsync("SettingsPage");
    }

    private void LoadFromConfiguration()
    {
        Log.Debug("Главная страница. Загрузка конфигурации.");

        interval = Convert.ToDouble(_configuration.RootSettings.API.FrameSendingDelay);
    }

    private async Task CheckServerConnection() //TODO: cancellation token cancel когда выходишь со страницы
    {
        Log.Debug("Главная страница. Запуск потока проверки подключения к api сервису.");

        while (true)
        {
            IsConnected = await _netUtils.CheckServerConnection();

            IsUploadButtonEnabled = IsConnected && SelectedItem != null && IsOriginalCurrentFileOpened && !IsActivityIndicatorVisible;

            await Task.Delay(1000);
        }
    }

    private void SelectionChangedHandler()
    {
        if (SelectedItem == null)
        {
            CurrentVideoSource = null;
            IsUploadButtonEnabled = false;
            return;
        }

        IsOpenOriginalButtonEnabled = SelectedItem.ProcessedFilePath != null;
        IsOriginalCurrentFileOpened = true;

        try
        {
            Log.Debug($"Главная страница. Установка нового активного видео файла по пути {SelectedItem.OriginalFilePath}");
            CurrentVideoSource = FileUtils.OpenVideoAsync(SelectedItem.OriginalFilePath);
        }
        catch
        {
            Log.Warning($"Не удалось открыть активный видео файл по пути: {SelectedItem.OriginalFilePath}");
            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть
        }

    }

    private void SwitchVideoView()
    {
        try
        {
            CurrentVideoSource = IsOriginalCurrentFileOpened ? FileUtils.OpenVideoAsync(SelectedItem.ProcessedFilePath) : FileUtils.OpenVideoAsync(SelectedItem.OriginalFilePath); // Краш при ошибке открытия

            if (IsOriginalCurrentFileOpened)
                Log.Debug($"Открыт оригинальный видео файл по пути: {SelectedItem.ProcessedFilePath}");
            else
                Log.Debug($"Открыт обработанный видео файл по пути: {SelectedItem.OriginalFilePath}");

            IsUploadButtonEnabled = IsOriginalCurrentFileOpened;
            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = !IsOriginalCurrentFileOpened;
        }
        catch
        {
            if (IsOriginalCurrentFileOpened)
                Log.Warning($"Не удалось открыть оригинальный видео файл по пути: {SelectedItem.ProcessedFilePath}");
            else
                Log.Warning($"Не удалось открыть обработанный видео файл по пути: {SelectedItem.OriginalFilePath}");

            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть
        }
    }

    private async Task SaveFile()
    {
        if (SelectedItem is null)
            return;

        if (SelectedItem.ProcessedFilePath is null)
        {
            await App.Current.MainPage.DisplayAlert("Внимание", "Сначала обработайте файл", "OK");
            Log.Debug($"Главная страница. Файл не сохранён, так нет открытого видео");
        }
        else
        {
            Log.Debug($"Главная страница. Сохранение файла {SelectedItem.ProcessedFilePath}.");
        }

        await FileUtils.SaveFileByDialog(SelectedItem.ProcessedFilePath);
    }

    private async Task OpenFile()
    {
        var files = await FileUtils.OpenFilesByDialog();

        if (!files.Any())
        {
            Log.Debug($"Главная страница. Файлы не выбраны.");
            return;
        }

        Log.Debug($"Главная страница. Открыто {files.Count()} файлов");

        foreach (var file in files)
        {
            Imgs.Add(new VideoItem
            {
                Thumbnail = await FileUtils.GetVideoThumbnailsAsync(file, 640, 360),
                OriginalFilePath = file.FullPath,
                IsOriginalFileOpened = true,
                TimeCodes = new ObservableCollection<TimeCodeModel>()
            });
            Log.Debug($"Главная страница. Открыт файл {file.FullPath}.");
        }

        SelectedItem = Imgs[^1]; // Устанавливаем активный элемент - последний открытый
    }

    private async Task UploadButtonClicked()
    {
        Log.Information("Главная страница. Обработка видео-файла");

        IsActivityIndicatorVisible = true;
        IsUploadButtonEnabled = false;

        IsConnected = await _netUtils.CheckServerConnection();

        if (!await _netUtils.ResetTrackingParameters())
        {
            Log.Warning("Не удалось сбросить пареметры трекинга");
        }

        interval = _configuration.RootSettings.API.FrameSendingDelay; // Вынести в navigatedto

        Dictionary<double, string> objectsStateStory = new Dictionary<double, string>();

        try
        {
            var tempFilePath = SelectedItem.ProcessedFilePath = $"{Path.GetTempPath()}{Path.GetFileName(SelectedItem.OriginalFilePath)}-output.avi";
            var sourceFilePath = SelectedItem.OriginalFilePath; //TODO: сделать копию объекта и возвращать главное окно к нему после обработки

            using (var capture = new VideoCapture(sourceFilePath))
            {
                if (!capture.IsOpened())
                {
                    throw new IOException($"Ошибка открытия файла по пути: {sourceFilePath}");
                }
                Log.Debug($"Главная страница. Открытие файла {sourceFilePath} для обработки");

                var fps = capture.Fps;
                int frameCount = 0;
                var frameInterval = fps * interval; // Во время работы не получится изменить частоту

                var frameSize = new OpenCvSharp.Size(capture.FrameWidth, capture.FrameHeight);

                Log.Information($"FPS: {fps}, frame interval: {frameInterval}, размер кадра: {capture.FrameWidth}x{capture.FrameHeight}");

                var writer = new VideoWriter(tempFilePath, FourCC.XVID, fps, frameSize);

                Mat frame = new Mat();

                List<FrameInfo> frameInfos = new List<FrameInfo>();

                while (true)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    capture.Read(frame);

                    if (frame.Empty())
                        break;

                    if ((int)(frameCount % frameInterval) == 0)
                    {
                        bool isError = false;

                        for (int attempts = 0; attempts < 5; attempts++)
                        {
                            try
                            {
                                frameInfos = await _netUtils.SendVideoFrameAsync(frame, sourceFilePath); //TODO: если выбрать другой файл по время обработки??
                                //CurrentVideoSource = MediaSource.FromFile(tempFilePath);
                                isError = false;
                                break;
                            }
                            catch (Exception)
                            {
                                Log.Warning($"Ошибка обработки кадра {frameCount} видео, попытка: {attempts}");
                                isError = true;
                            }
                        }
                        if (isError)
                        {
                            await App.Current.MainPage.DisplayAlert("Ошибка", "Произошла ошибка получения данных с сервера", "OK");
                            Log.Warning("Ошибка обработки видео-файла: ошибка получения данных с сервреа");
                            break;
                        }
                    }

                    Log.Debug($"Главная страница. Полученные данные о кадре: {frameCount}");
                    foreach (var info in frameInfos)
                    {
                        Log.Debug($"Имя класса: {info.classname}, ID объекта: {info.objectid}, xbr: {info.xbr}, ybr: {info.ybr}, xtl: {info.xtl}, ytl: {info.ytl}");

                        if (objectsStateStory.TryGetValue(info.objectid, out var classname))
                        {
                            if (classname == "Standing" && info.classname == "Lying") // Стоял -> упал
                            {
                                SelectedItem.TimeCodes.Add(new TimeCodeModel() { TimeCode = Math.Round(frameCount / fps, 2) });
                                objectsStateStory.Remove(info.objectid);
                            }
                        }
                        else
                        {
                            objectsStateStory.Add(info.objectid, info.classname);
                        }

                        Scalar color;

                        if (info.classname == "Standing")
                        {
                            color = Scalar.Green;
                        }
                        else if (info.classname == "Lying")
                        {
                            color = Scalar.Yellow;
                        }
                        else
                        {
                            color = Scalar.Red;
                        }

                        Cv2.Rectangle(frame, new Point(info.xtl, info.ytl), new Point(info.xbr, info.ybr), color, 2);
                        Cv2.PutText(frame, info.objectid.ToString(), new Point(info.xtl + 50, info.ytl - 5), HersheyFonts.HersheySimplex, 1, color, 2);
                        Cv2.PutText(frame, info.classname, new Point(info.xtl + 100, info.ytl - 5), HersheyFonts.HersheySimplex, 1, color, 2); // TODO: всегда вмещать в экран10
                    }

                    byte[] imageBytes = frame.ToBytes();
                    ImagePlayerSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));

                    stopwatch.Stop();
                    int processingTimeMs = (int)stopwatch.ElapsedMilliseconds;

                    // Задержка между кадрами с учетом времени обработки кадра
                    int delay = (int)((1000 / fps * 2) - processingTimeMs);
                    if (delay > 0)
                        await Task.Delay(delay);

                    frameCount++;
                    writer.Write(frame);
                }

                frame.Release();
                writer.Release();
            }

            Log.Information("Видео-файл успешно обработан");

            CurrentVideoSource = MediaSource.FromFile(tempFilePath);

            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = false;

            IsOpenOriginalButtonEnabled = true;
            IsActivityIndicatorVisible = false;

            IsUploadButtonEnabled = true;
        }
        catch (IOException ex)
        {
            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть. Удаление вызовет SelectionChangedHandler

            IsActivityIndicatorVisible = false;
            IsUploadButtonEnabled = true;

            Log.Error(ex.Message);
        }
        catch (Exception ex)
        {
            // Проверить интернет, вывести ошибку на экран?? 

            IsActivityIndicatorVisible = false;
            IsUploadButtonEnabled = true;

            Log.Error(ex.Message);
        }
    }
}