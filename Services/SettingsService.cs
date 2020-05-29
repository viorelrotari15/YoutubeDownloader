﻿using Tyrrrz.Settings;
using YoutubeDownloader.Internal;

namespace YoutubeDownloader.Services
{
    public class SettingsService : SettingsManager
    {
        public int MaxConcurrentDownloadCount { get; set; } = 2;

        public string FileNameTemplate { get; set; } = FileNameGenerator.DefaultTemplate;

        public bool ShouldInjectTags { get; set; } = true;

        public bool ShouldSkipExistingFiles { get; set; } = false;

        public string? LastFormat { get; set; }

        public SettingsService()
        {
            Configuration.StorageSpace = StorageSpace.Instance;
            Configuration.SubDirectoryPath = "";
            Configuration.FileName = "Settings.dat";
        }
    }
}