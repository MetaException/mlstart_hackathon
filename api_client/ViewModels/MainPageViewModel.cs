﻿using api_client.Configuration;
using api_client.Model;
using api_client.Utils;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
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

    public RelayCommand OpenOriginalButtonClickedCommand { get; }

    public RelayCommand UploadButtonClickedCommand { get; }

    public AsyncRelayCommand OpenFileCommand { get; }

    public RelayCommand SelectionChangedCommand { get; }

    public AsyncRelayCommand SaveFileCommand { get; }

    public RelayCommand<string> SetFrameSettingIntervalCommand { get; }

    public AsyncRelayCommand OpenSettingsCommand { get; }

    public MainPageViewModel(NetUtils netUtils, ConfigurationManager configuration)
    {
        Log.Debug("Создание главной страницы.");

        _netUtils = netUtils;
        _configuration = configuration;

        UploadButtonClickedCommand = new RelayCommand(async () => await UploadButtonClicked());

        OpenFileCommand = new AsyncRelayCommand(OpenFile);
        SaveFileCommand = new AsyncRelayCommand(SaveFile);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsPage);

        SelectionChangedCommand = new RelayCommand(SelectionChangedHandler);

        OpenOriginalButtonClickedCommand = new RelayCommand(SwitchVideoView);

        LoadFromConfiguration();

        _ = CheckServerConnection();
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
        Log.Debug("Главная страница. Запуск сервиса проверки конфигурации.");

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
            CurrentVideoSource = FileUtils.OpenVideoAsync(SelectedItem.OriginalFilePath);
        }
        catch
        {
            Imgs.Remove(SelectedItem); // Удаляем файл, который не получилось открыть
        }

    }

    private void SwitchVideoView()
    {
        try
        {
            CurrentVideoSource = IsOriginalCurrentFileOpened ? FileUtils.OpenVideoAsync(SelectedItem.ProcessedFilePath) : FileUtils.OpenVideoAsync(SelectedItem.OriginalFilePath); // Краш при ошибке открытия

            IsUploadButtonEnabled = IsOriginalCurrentFileOpened;
            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = !IsOriginalCurrentFileOpened;
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
            Log.Debug($"Главная страница. Файл не обработан.");

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
        Log.Debug($"Главная страница. Выьор файлов.");


        if (!files.Any())
        {
            Log.Debug($"Главная страница. Файлы не выбраны.");

            return;
        }

        foreach (var file in files)
        {
            Imgs.Add(new VideoItem
            {
                Thumbnail = await FileUtils.GetVideoThumbnailsAsync(file, 640, 360),
                OriginalFilePath = file.FullPath,
                IsOriginalFileOpened = true
            });
            Log.Debug($"Главная страница. Выбран файл {file.FullPath}.");

        }

        SelectedItem = Imgs[^1]; // Устанавливаем активный элемент - последний открытый
    }

    private async Task UploadButtonClicked()
    {
        IsActivityIndicatorVisible = true;
        IsUploadButtonEnabled = false;

        IsConnected = await _netUtils.CheckServerConnection();

        interval = _configuration.RootSettings.API.FrameSendingDelay; // Вынести в navigatedto

        try
        {
            var tempFilePath = SelectedItem.ProcessedFilePath = $"{Path.GetTempPath()}{Path.GetFileName(SelectedItem.OriginalFilePath)}-output.avi";
            var sourceFilePath = SelectedItem.OriginalFilePath; //TODO: сделать копию объекта и возвращать главное окно к нему после обработки

            using (var capture = new VideoCapture(sourceFilePath))
            {
                if (!capture.IsOpened())
                {
                    throw new IOException($"Cannot open file {sourceFilePath}");
                }

                var fps = capture.Fps;
                int frameCount = 0;
                var frameInterval = fps * interval; // Во время работы не получится изменить частоту

                var frameSize = new OpenCvSharp.Size(capture.FrameWidth, capture.FrameHeight);

                var writer = new VideoWriter(tempFilePath, FourCC.XVID, fps, frameSize );

                Mat frame = new Mat();

                List<FrameInfo> frameInfos = new List<FrameInfo>();

                while (true)
                {
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
                                CurrentVideoSource = MediaSource.FromFile(tempFilePath);
                                isError = false;
                                break;
                            }
                            catch (Exception)
                            {
                                isError = true;
                            }
                        }
                        if (isError)
                        {
                            await App.Current.MainPage.DisplayAlert("Ошибка", "Произошла ошибка получения данных с сервера", "OK");
                            break;
                        }
                    }

                    foreach (var info in frameInfos)
                    {
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

                    frameCount++;
                    writer.Write(frame);
                }

                writer.Release();
            }

            CurrentVideoSource = MediaSource.FromFile(tempFilePath);

            IsOriginalCurrentFileOpened = SelectedItem.IsOriginalFileOpened = false;

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