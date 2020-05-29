using System;
using System.Linq;
using System.Threading.Tasks;
using Gress;
using MaterialDesignThemes.Wpf;
using Stylet;
using Tyrrrz.Extensions;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services;
using YoutubeDownloader.ViewModels.Components;
using YoutubeDownloader.ViewModels.Framework;

namespace YoutubeDownloader.ViewModels
{
    public class RootViewModel : Screen
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DialogManager _dialogManager;
        private readonly SettingsService _settingsService;
        private readonly QueryService _queryService;
        private readonly DownloadService _downloadService;

        public ISnackbarMessageQueue Notifications { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

        public IProgressManager ProgressManager { get; } = new ProgressManager();

        public bool IsBusy { get; private set; }

        public bool IsProgressIndeterminate { get; private set; }

        public string? Query { get; set; }

        public BindableCollection<DownloadViewModel> Downloads { get; } = new BindableCollection<DownloadViewModel>();

        public RootViewModel(IViewModelFactory viewModelFactory, DialogManager dialogManager,
            SettingsService settingsService, UpdateService updateService, QueryService queryService,
            DownloadService downloadService)
        {
            _viewModelFactory = viewModelFactory;
            _dialogManager = dialogManager;
            _settingsService = settingsService;
           
            _queryService = queryService;
            _downloadService = downloadService;

            // Title
            DisplayName = $"{App.Name} ";

           
        }

        

        protected override async void OnViewLoaded()
        {
            base.OnViewLoaded();

            // Load settings
            _settingsService.Load();

            // Check and prepare update
            await HandleAutoUpdateAsync();
        }

        protected override void OnClose()
        {
            base.OnClose();

            // Save settings
            _settingsService.Save();

            // Cancel all downloads
            foreach (var download in Downloads)
                download.Cancel();

            // Finalize updates if necessary
            _updateService.FinalizeUpdate(false);
        }

        public bool CanShowSettings => !IsBusy;

        public async void ShowSettings()
        {
            // Create dialog
            var dialog = _viewModelFactory.CreateSettingsViewModel();

            // Show dialog
            await _dialogManager.ShowDialogAsync(dialog);
        }

        private void EnqueueAndStartDownload(DownloadViewModel download)
        {
            
            var existingDownloads = Downloads.Where(d => d.FilePath == download.FilePath).ToArray();
            foreach (var existingDownload in existingDownloads)
            {
                existingDownload.Cancel();
                Downloads.Remove(existingDownload);
            }

            
            Downloads.Add(download);

            
            download.ProgressManager = ProgressManager;

            
            download.Start();
        }

        public bool CanProcessQuery => !IsBusy && !string.IsNullOrWhiteSpace(Query);

        public async void ProcessQuery()
        {
            
            var operation = ProgressManager.CreateOperation(0.01);

            
            IsBusy = true;

            try
            {
                
                var parsedQueries = _queryService.ParseMultilineQuery(Query!);

                
                var executedQueries = await _queryService.ExecuteQueriesAsync(parsedQueries, operation);

                
                var videos = executedQueries.SelectMany(q => q.Videos).Distinct(v => v.Id).ToArray();
                var dialogTitle = executedQueries.Count == 1 ? executedQueries.Single().Title : "Multiple queries";

                
                if (videos.Length <= 0)
                {
                    // Create dialog
                    var dialog = _viewModelFactory.CreateMessageBoxViewModel("Nothing found",
                        "Couldn't find any videos based on the query or URL you provided");

                    
                    await _dialogManager.ShowDialogAsync(dialog);
                }

                
                else if (videos.Length == 1)
                {
                   
                    var video = videos.Single();

                   
                    var downloadOptions = await _downloadService.GetDownloadOptionsAsync(video.Id);

                  
                    var dialog = _viewModelFactory.CreateDownloadSingleSetupViewModel(dialogTitle, video, downloadOptions);

                    
                    var download = await _dialogManager.ShowDialogAsync(dialog);

                    
                    if (download == null)
                        return;

                    
                    EnqueueAndStartDownload(download);
                }

               
                else
                {
                    
                    var dialog = _viewModelFactory.CreateDownloadMultipleSetupViewModel(dialogTitle, videos);

                   
                    if (executedQueries.All(q => q.Query.Type != QueryType.Search))
                        dialog.SelectedVideos = dialog.AvailableVideos;

                    
                    var downloads = await _dialogManager.ShowDialogAsync(dialog);

                    
                    if (downloads == null)
                        return;

                   
                    foreach (var download in downloads)
                        EnqueueAndStartDownload(download);
                }
            }
            catch (Exception ex)
            {
               
                var dialog = _viewModelFactory.CreateMessageBoxViewModel("Error", ex.Message);

                
                await _dialogManager.ShowDialogAsync(dialog);
            }
            finally
            {
                
                operation.Dispose();

               
                IsBusy = false;
            }
        }

        public void RemoveDownload(DownloadViewModel download)
        {
            download.Cancel();
            Downloads.Remove(download);
        }

        public void RemoveInactiveDownloads()
        {
            var inactiveDownloads = Downloads.Where(d => !d.IsActive).ToArray();
            Downloads.RemoveRange(inactiveDownloads);
        }

        public void RemoveSuccessfulDownloads()
        {
            var successfulDownloads = Downloads.Where(d => d.IsSuccessful).ToArray();
            Downloads.RemoveRange(successfulDownloads);
        }

        public void RestartFailedDownloads()
        {
            var failedDownloads = Downloads.Where(d => d.IsFailed).ToArray();
            foreach (var failedDownload in failedDownloads)
                failedDownload.Restart();
        }
    }
}