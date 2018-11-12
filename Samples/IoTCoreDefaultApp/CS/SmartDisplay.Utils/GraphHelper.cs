// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Graph;
using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Identity
{
    /// <summary>
    /// Helper class for using graph.microsoft.com
    /// </summary>
    public class GraphHelper : IDisposable
    {
        public event EventHandler SignedInStatusChanged;

        private IAuthProvider _authProvider;
        private GraphServiceClient _graphClient;

        private ILogService LogService { get; } = ServiceUtil.LogService;

        public GraphHelper(IAuthProvider authProvider)
        {
            _authProvider = authProvider;
            _authProvider.TokenUpdate += (s, e) =>
            {
                SignedInStatusChanged?.Invoke(this, e);
            };

            _graphClient = new GraphServiceClient(
                    "https://graph.microsoft.com/v1.0",
                    new DelegateAuthenticationProvider(
                        async (requestMessage) =>
                        {
                            string token = await _authProvider.GetTokenSilentAsync();
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                        }));
        }

        public async Task<IUserCalendarViewCollectionPage> GetCalendarEventsAsync(string startDateTime, string endDateTime)
        {
            LogService.Write($"Getting calendar events from {startDateTime} to {endDateTime}");

            try
            {
                List<Option> options = new List<Option>
                {
                    new QueryOption("StartDateTime", startDateTime),
                    new QueryOption("EndDateTime", endDateTime),
                    new HeaderOption("Prefer", $"outlook.timezone=\"{TimeZoneInfo.Local.Id}\""),
                };

                return await _graphClient.Me.CalendarView.Request(options).OrderBy("start/DateTime").GetAsync();
            }
            catch (ServiceException e)
            {
                LogService.Write($"Code: {e.Error.Code}, Message: {e.Error.Message}, ThrowSite: {e.Error.ThrowSite}");
                foreach (var kvp in e.Error.AdditionalData)
                {
                    LogService.Write($"Additional Data: {kvp.Key}, {kvp.Value.ToString()}");
                }
                LogService.WriteException(e);         
            }

            return null;
        }

        public async Task SendEmailAsync(string recipientAddress, string subject, string messageContent, StorageFile[] files)
        {
            try
            {
                // Create attachments
                var attachments = new MessageAttachmentsCollectionPage();
                foreach (var file in files)
                {
                    using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
                    {
                        var contentBytes = new byte[stream.Size];
                        using (DataReader reader = new DataReader(stream))
                        {
                            await reader.LoadAsync((uint)stream.Size);
                            reader.ReadBytes(contentBytes);
                        }

                        // Attach log file
                        attachments.Add(new FileAttachment
                        {
                            ODataType = "#microsoft.graph.fileAttachment",
                            ContentBytes = contentBytes,
                            ContentType = "application/octet-stream",
                            ContentId = file.Name,
                            Name = file.Name,
                        });
                    }
                }

                // Construct email
                var recipients = new List<Recipient>
                {
                    new Recipient() { EmailAddress = new EmailAddress() { Address = recipientAddress } },
                };

                Message email = new Message
                {
                    Body = new ItemBody
                    {
                        Content = messageContent,
                        ContentType = BodyType.Text,
                    },
                    Subject = subject,
                    ToRecipients = recipients,
                    Attachments = attachments,
                };

                await SendEmailAsync(email);
            }
            catch (Exception ex)
            {
                LogService.Write(ex.ToString(), LoggingLevel.Error);
                throw;
            }
        }

        public async Task SendEmailAsync(Message message)
        {
            LogService.Write($"Sending mail: {message.Subject}");
            await _graphClient.Me.SendMail(message, true).Request().PostAsync();
        }

        public async Task<User> GetUserAsync()
        {
            try
            {
                return await _graphClient.Me.Request().GetAsync();
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }

            return null;
        }

        public async Task<BitmapImage> GetPhotoAsync()
        {
            try
            {
                using (var stream = await _graphClient.Me.Photo.Content.Request().GetAsync())
                {
                    using (var memStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memStream);
                        memStream.Position = 0;

                        var bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(memStream.AsRandomAccessStream());

                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }

            return null;
        }

        #region IDisposable
        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    _graphClient.HttpProvider.Dispose();
                    _graphClient = null;
                }

                // Free any unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
