// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using System;
using System.Threading.Tasks;

namespace SmartDisplay.Contracts
{
    public interface IAppService
    {
        IPageService PageService { get; }

        ILogService LogService { get; }

        ITelemetryService TelemetryService { get; }

        IAuthManager AuthManager { get; }

        ISettingsProvider Settings { get; }

        Type[] RegisteredPages { get; }

        bool IsConnectedInternet();

        Task RunExclusiveTaskAsync(Action task);

        Task RunExclusiveTaskAsync(Func<Task> task);

        Task<T> RunExclusiveTaskAsync<T>(Func<Task<T>> task);

        Task<bool> YesNoAsync(string title, string question, string primaryButtonText = null, string secondaryButtonText = null);
        
        void DisplayDialog(string title, object content, DialogButton? primaryButton = null, DialogButton? secondaryButton = null, DialogButton? closeButton = null);

        Task DisplayDialogAsync(string title, object content, DialogButton? primaryButton = null, DialogButton? secondaryButton = null, DialogButton? closeButton = null);

        void DisplayNoInternetDialog(Type redirectPage = null);

        void DisplayAadSignInDialog(Type returnPage, string title = null);

        bool RegisterService<T>(T service) where T : ISmartDisplayService;

        bool UnregisterService<T>(T service) where T : ISmartDisplayService;

        T GetRegisteredService<T>() where T : ISmartDisplayService;
    }
}
