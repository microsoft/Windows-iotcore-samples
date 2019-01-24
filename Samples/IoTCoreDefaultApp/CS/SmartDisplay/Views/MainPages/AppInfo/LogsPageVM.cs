// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Identity;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.ViewModels
{
    public class LogsPageVM : BaseViewModel
    {
        #region UI properties and commands

        public ObservableCollection<LogFile> LogFilesCollection { get; } = new ObservableCollection<LogFile>();

        private ILogService LogService => AppService?.LogService;

        #endregion
        
        private string _helpText;
        private StorageFileQueryResult _logQuery;
        private object _collectionLock = new object();
        private ICustomContentService CustomContentService { get; set; }

        public LogsPageVM() : base()
        {
            _helpText = string.Empty;
        }

        public async Task<bool> SetUpVM()
        {
            try
            {
                CustomContentService = AppService.GetRegisteredService<ICustomContentService>();
                _helpText = await FileUtil.ReadStringFromInstalledLocationAsync(@"Assets\Messages\LogDescription.txt");
                _logQuery = await LogUtil.GetLogFilesQueryResultAsync();
                _logQuery.ContentsChanged += Query_ContentsChanged;

                RefreshUI();
                PopulateCommandBar();
                return true;
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.Message, LoggingLevel.Error);
                return false;
            }
        }

        public void TearDownVM()
        {
            if (_logQuery != null)
            {
                _logQuery.ContentsChanged -= Query_ContentsChanged;
            }
        }

        private async void RefreshUI()
        {
            var files = await _logQuery.GetFilesAsync(0, 100);

            lock (_collectionLock)
            {
                LogFilesCollection.Clear();
                foreach (var file in files)
                {
                    LogFilesCollection.Add(new LogFile(file));
                }
            }
        }

        private void Query_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            InvokeOnUIThread(() => RefreshUI());
        }

        private void PopulateCommandBar()
        {
            PageService.AddCommandBarButton(CommandBarButton.Separator);

            PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Add),
                Label = Common.GetLocalizedText("SelectAllButtonText"),
                Handler = SelectAllButton_Click,
            });

            PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Clear),
                Label = Common.GetLocalizedText("UnselectAllButtonText"),
                Handler = UnselectAllButton_Click,
            });

            if (AppService.AuthManager.IsAadProviderAvailable())
            {
                PageService.AddCommandBarButton(new CommandBarButton
                {
                    Icon = new SymbolIcon(Symbol.Mail),
                    Label = Common.GetLocalizedText("EmailSelectedButtonText"),
                    Handler = EmailLogsButton_Click,
                });
            }

            PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Delete),
                Label = Common.GetLocalizedText("DeleteSelectedButtonText"),
                Handler = DeleteLogsButton_Click,
            });

            PageService.AddSecondaryCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Help),
                Label = Common.GetLocalizedText("HelpButtonText"),
                Handler = HelpButton_Click,
            });
        }

        private async void DeleteLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = LogFilesCollection.Where(item => item.IsSelected);
            var logs = await LogUtil.GetLogFilesAsync();
            var fileList = new List<StorageFile>();

            foreach (var sel in selected)
            {
                var temp = logs.Where(file => file.Path == sel.Path).FirstOrDefault();
                if (temp != null)
                {
                    fileList.Add(temp as StorageFile);
                }
            }

            if (fileList.Count > 0)
            {
                var deleteConfirmText = string.Format(fileList.Count > 1 || fileList.Count == 0 ?
                    Common.GetLocalizedText("DeleteLogsText") :
                    Common.GetLocalizedText("DeleteLogText"), fileList.Count);
                if (!await AppService.YesNoAsync(Common.GetLocalizedText("ConfirmationTitleText"), deleteConfirmText))
                {
                    return;
                }

                foreach (var file in fileList)
                {
                    await file.DeleteAsync();
                }

                var deleteSuccessText = string.Format(fileList.Count > 1 || fileList.Count == 0 ?
                    Common.GetLocalizedText("DeletedLogsText") :
                    Common.GetLocalizedText("DeletedLogText"), fileList.Count);
                PageService.ShowNotification(deleteSuccessText);
                RefreshUI();
            }
            else
            {
                PageService.ShowNotification(Common.GetLocalizedText("NoLogsSelectedText"));
            }
        }

        private async void EmailLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var provider = App.AuthManager.GetGraphProvider();
            if (provider == null)
            {
                return;
            }

            if (!provider.IsTokenValid())
            {
                AppService.DisplayAadSignInDialog(typeof(LogsPage));
                return;
            }

            var selected = LogFilesCollection.Where(item => item.IsSelected);
            var logs = await LogUtil.GetLogFilesAsync();
            var fileList = new List<StorageFile>();

            foreach (var sel in selected)
            {
                var temp = logs.Where(file => file.Path == sel.Path).FirstOrDefault();
                if (temp != null)
                {
                    fileList.Add(temp as StorageFile);
                }
            }

            if (fileList.Count > 0)
            {
                var emailConfirmText = string.Format(fileList.Count > 1 || fileList.Count == 0 ?
                    Common.GetLocalizedText("EmailLogsSelfText") :
                    Common.GetLocalizedText("EmailLogSelfText"), fileList.Count);
                if (!await AppService.YesNoAsync(Common.GetLocalizedText("EmailLogsText"), emailConfirmText))
                {
                    return;
                }

                var messageContent = LogUtil.CreateMessageContent(GetType().Name, CustomContentService?.GetContent<string>(CustomContentConstants.BugTemplate));
                using (var graphHelper = new GraphHelper(provider))
                {
                    try
                    {
                        var email = await LogUtil.EmailLogsAsync(graphHelper, "[Smart Display] LOG MAILER", messageContent, fileList.ToArray());
                        var emailSuccessText = string.Format(fileList.Count > 1 || fileList.Count == 0 ?
                            Common.GetLocalizedText("EmailedLogsText") :
                            Common.GetLocalizedText("EmailedLogText"), fileList.Count, email);
                        PageService.ShowNotification(emailSuccessText);
                    }
                    catch (Exception ex)
                    {
                        PageService.ShowNotification(string.Format(Common.GetLocalizedText("EmailLogsProblemText"), ex.Message));
                    }
                }
            }
            else
            {
                PageService.ShowNotification(Common.GetLocalizedText("NoLogsSelectedText"));
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            AppService.DisplayDialog(Common.GetLocalizedText("HelpText"), _helpText);
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in LogFilesCollection)
            {
                item.IsSelected = true;
            }
        }

        private void UnselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in LogFilesCollection)
            {
                item.IsSelected = false;
            }
        }

    }

    #region Helper Classes

    public class LogFile : INotifyPropertyChanged
    {
        public string Name { get; private set; }
        public DateTimeOffset DateCreated { get; private set; }
        public string Path { get; private set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public LogFile(StorageFile file, bool fileSelected = false)
        {
            Name = file.Name;
            DateCreated = file.DateCreated;
            Path = file.Path;
            IsSelected = fileSelected;
        }
    }
    #endregion
}
