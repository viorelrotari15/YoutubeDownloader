using YoutubeDownloader.ViewModels.Components;
using YoutubeDownloader.ViewModels.Dialogs;

namespace YoutubeDownloader.ViewModels.Framework
{
    // factory method interface pentru a reliza injectarea dependentelor
    public interface IViewModelFactory
    {
        DownloadViewModel CreateDownloadViewModel();

        DownloadMultipleSetupViewModel CreateDownloadMultipleSetupViewModel();

        DownloadSingleSetupViewModel CreateDownloadSingleSetupViewModel();

        MessageBoxViewModel CreateMessageBoxViewModel();

        SettingsViewModel CreateSettingsViewModel();
    }
}