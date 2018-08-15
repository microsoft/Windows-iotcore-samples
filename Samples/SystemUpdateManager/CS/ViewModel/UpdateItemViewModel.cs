using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.System.Update;

namespace SystemUpdate.ViewModel
{
    public class UpdateItemViewModel : INotifyPropertyChanged
    {
        SystemUpdateItem _item;
        public UpdateItemViewModel(SystemUpdateItem item)
        {
            Title = item.Title;
            Description = item.Description;
            Id = item.Id;
            Revision = item.Revision;
            DownloadProgress = item.DownloadProgress;
            InstallProgress = item.InstallProgress;
            State = item.State;
            ErrorCode = 0;
            if (item.ExtendedError != null)
            {
                ErrorCode = item.ExtendedError.HResult;
            }
            _item = item;
        }

        private void Item_StateChanged(SystemUpdateItem item, object e)
        {
            DownloadProgress = item.DownloadProgress;
            InstallProgress = item.InstallProgress;
            State = item.State;
            ErrorCode = item.ExtendedError.HResult;
        }

        public void Update(SystemUpdateItem item)
        {
            DownloadProgress = item.DownloadProgress;
            InstallProgress = item.InstallProgress;
            State = item.State;
            if (item.ExtendedError != null)
            {
                ErrorCode = item.ExtendedError.HResult;
            }
        }


        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged("Description");
            }
        }

        public String Id { get; }
        public UInt32 Revision { get; }

        private double _downloadProgress;
        public double DownloadProgress
        {
            get { return _downloadProgress; }
            set
            {
                if (_downloadProgress != value)
                {
                    _downloadProgress = value;
                    OnPropertyChanged("DownloadProgress");
                    Debug.WriteLine($"{_title} {_state} {_downloadProgress}");
                }
            }
        }

        private double _installProgress;
        public double InstallProgress
        {
            get { return _installProgress; }
            set
            {
                if (_installProgress != value)
                {
                    _installProgress = value;
                    OnPropertyChanged("InstallProgress");
                    Debug.WriteLine($"{_title} {_state} {_installProgress}");
                }
            }
        }

        private SystemUpdateItemState _state;
        public SystemUpdateItemState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        private Int64 _errorCode;
        public Int64 ErrorCode
        {
            get { return _errorCode; }
            set
            {
                if (_errorCode != value)
                {
                    _errorCode = value;

                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
