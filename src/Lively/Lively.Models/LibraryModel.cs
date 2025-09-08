using CommunityToolkit.Mvvm.ComponentModel;

namespace Lively.Models
{
    public partial class LibraryModel : ObservableObject
    {
        [ObservableProperty]
        private bool isSubscribed;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private bool isReadyToSet = true;

        [ObservableProperty]
        private float downloadingProgress;

        [ObservableProperty]
        private string downloadingProgressText = "-/- MB";

        [ObservableProperty]
        private LivelyInfoModel livelyInfo;

        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private string livelyInfoFolderPath;

        [ObservableProperty]
        private string livelyInfoLocalizationPath;

        [ObservableProperty]
        private string livelyPropertyLocalizationPath;

        [ObservableProperty]
        private string imagePath;

        [ObservableProperty]
        private string previewClipPath;

        [ObservableProperty]
        private string thumbnailPath;

        [ObservableProperty]
        private string livelyPropertyPath;

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? "---" : value;
                SetProperty(ref _title, value);
            }
        }

        private string _author;
        public string Author
        {
            get => _author;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? "---" : value;
                SetProperty(ref _author, value);
            }
        }

        private string _desc;
        public string Desc
        {
            get => _desc;
            set
            {
                value = string.IsNullOrWhiteSpace(value) ? "---" : value;
                SetProperty(ref _desc, value);
            }
        }
    }
}
