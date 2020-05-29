﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gress;
using YoutubeDownloader.Internal;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services;
using YoutubeDownloader.ViewModels.Framework;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.ViewModels.Components
{
    public class DownloadViewModel : PropertyChangedBase
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DialogManager _dialogManager;
        private readonly SettingsService _settingsService;
        private readonly DownloadService _downloadService;
        private readonly TaggingService _taggingService;

        private CancellationTokenSource? _cancellationTokenSource;

        public Video Video { get; set; }

        public string FilePath { get; set; }

        public string FileName => Path.GetFileName(FilePath);

        public string Format { get; set; }

        public DownloadOption? DownloadOption { get; set; }

        public IProgressManager ProgressManager { get; set; }

        public IProgressOperation ProgressOperation { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsSuccessful { get; private set; }

        public bool IsCanceled { get; private set; }

        public bool IsFailed { get; private set; }

        public string? FailReason { get; private set; }

        public DownloadViewModel(IViewModelFactory viewModelFactory, DialogManager dialogManager, SettingsService settingsService,
            DownloadService downloadService, TaggingService taggingService)
        {
            _viewModelFactory = viewModelFactory;
            _dialogManager = dialogManager;
            _settingsService = settingsService;
            _downloadService = downloadService;
            _taggingService = taggingService;
        }

        public bool CanStart => !IsActive;

        public void Start()
        {
            if (!CanStart)
                return;

            IsActive = true;
            IsSuccessful = false;
            IsCanceled = false;
            IsFailed = false;

            Task.Run(async () =>
            {
                // Create cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();

                // Create progress operation
                ProgressOperation = ProgressManager.CreateOperation();

                try
                {
                    // daca nu sunt setate obtiuni de descarcare se foloseste setarea de baza
                    // descarca video
                    if (DownloadOption == null)
                        DownloadOption = await _downloadService.GetBestDownloadOptionAsync(Video.Id, Format);

                    await _downloadService.DownloadVideoAsync(DownloadOption, FilePath, ProgressOperation, _cancellationTokenSource.Token);

                    if (_settingsService.ShouldInjectTags)
                        await _taggingService.InjectTagsAsync(Video, Format, FilePath, _cancellationTokenSource.Token);

                    IsSuccessful = true;
                }
                catch (OperationCanceledException)
                {
                    IsCanceled = true;
                }
                catch (Exception ex)
                {
                    IsFailed = true;
                    FailReason = ex.Message;
                }
                finally
                {
                    IsActive = false;

                    _cancellationTokenSource.Dispose();
                    ProgressOperation.Dispose();
                }
            });
        }

        public bool CanCancel => IsActive && !IsCanceled;

        public void Cancel()
        {
            if (!CanCancel)
                return;

            _cancellationTokenSource?.Cancel();
        }

        public bool CanShowFile => IsSuccessful;

        public async void ShowFile()
        {
            if (!CanShowFile)
                return;

            try
            {
                //  exploratorul de fisiere
                Process.Start("explorer", $"/select, \"{FilePath}\"");
            }
            catch (Exception ex)
            {
                var dialog = _viewModelFactory.CreateMessageBoxViewModel("Error", ex.Message);
                await _dialogManager.ShowDialogAsync(dialog);
            }
        }

        public bool CanOpenFile => IsSuccessful;

        public async void OpenFile()
        {
            if (!CanOpenFile)
                return;

            try
            {
                // Open video file using default player
                ProcessEx.StartShellExecute(FilePath);
            }
            catch (Exception ex)
            {
                var dialog = _viewModelFactory.CreateMessageBoxViewModel("Error", ex.Message);
                await _dialogManager.ShowDialogAsync(dialog);
            }
        }

        public bool CanRestart => CanStart && !IsSuccessful;

        public void Restart() => Start();
    }
}