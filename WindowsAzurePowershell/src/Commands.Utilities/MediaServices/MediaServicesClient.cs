// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using Microsoft.WindowsAzure.Commands.Utilities.MediaServices.Services.Entities;
using Microsoft.WindowsAzure.Commands.Utilities.Websites.Services;
using Microsoft.WindowsAzure.ServiceManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WindowsAzure.Commands.Utilities.MediaServices
{
    /// <summary>
    ///     Implements IMediaServicesClient to use HttpClient for communication
    /// </summary>
    public class MediaServicesClient : IMediaServicesClient
    {
        public const string MediaServiceVersion = "2013-03-01";
        private readonly HttpClient _httpClient;
        private readonly HttpClient _storageClient;

        /// <summary>
        ///     Creates new MediaServicesClient.
        /// </summary>
        /// <param name="subscription">The Windows Azure subscription data object</param>
        /// <param name="logger">The logger action</param>
        public MediaServicesClient(WindowsAzureSubscription subscription, Action<string> logger, HttpClient httpClient, HttpClient storageClient)
        {
            
            Subscription = subscription;
            Logger = logger;
            _httpClient = httpClient;
            _storageClient = storageClient;
        }

        /// <summary>
        ///     Creates new MediaServicesClient.
        /// </summary>
        /// <param name="subscription">The Windows Azure subscription data object</param>
        /// <param name="logger">The logger action</param>
        public MediaServicesClient(WindowsAzureSubscription subscription, Action<string> logger) : this(subscription, logger, CreateIMediaServicesHttpClient(subscription), CreateStorageServiceHttpClient(subscription))
        {
        }

        /// <summary>
        ///     Gets or sets the subscription.
        /// </summary>
        /// <value>
        ///     The subscription.
        /// </value>
        public WindowsAzureSubscription Subscription { get; set; }

        /// <summary>
        ///     Gets or sets the logger
        /// </summary>
        /// <value>
        ///     The logger.
        /// </value>
        public Action<string> Logger { get; set; }


        /// <summary>
        ///     Gets the media service accounts async.
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<MediaServiceAccount>> GetMediaServiceAccountsAsync()
        {
            return _httpClient.GetAsync(MediaServicesUriElements.Accounts, Logger).ContinueWith(tr => ProcessJsonResponse<IEnumerable<MediaServiceAccount>>(tr));
        }


        /// <summary>
        ///     Gets the storage service key.
        /// </summary>
        /// <param name="storageAccountName">Name of the storage account.</param>
        /// <returns></returns>
        public Task<StorageService> GetStorageServiceKeysAsync(string storageAccountName)
        {
            //Storage service returng xml as output format
            return _storageClient.GetAsync(String.Format("{0}/keys", storageAccountName), Logger).ContinueWith(tr => ProcessXmlResponse<StorageService>(tr));
        }

        /// <summary>
        ///     Gets the storage service properties.
        /// </summary>
        /// <param name="storageAccountName">Name of the storage account.</param>
        /// <returns></returns>
        public Task<StorageService> GetStorageServicePropertiesAsync(string storageAccountName)
        {
            return _storageClient.GetAsync(storageAccountName, Logger).ContinueWith(tr => ProcessXmlResponse<StorageService>(tr));
        }

        /// <summary>
        ///     Gets the media service account details async.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Task<MediaServiceAccountDetails> GetMediaServiceAsync(string name)
        {
            return _httpClient.GetAsync(String.Format("{0}/{1}", MediaServicesUriElements.Accounts, name), Logger).ContinueWith(tr => ProcessJsonResponse<MediaServiceAccountDetails>(tr));
        }

        /// <summary>
        ///     Create new azure media service async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public Task<AccountCreationResult> CreateNewAzureMediaServiceAsync(AccountCreationRequest request)
        {
            return _httpClient.PostAsJsonAsyncWithoutEnsureSuccessStatusCode(MediaServicesUriElements.Accounts, JObject.FromObject(request), Logger).ContinueWith(tr => ProcessJsonResponse<AccountCreationResult>(tr));
        }

        /// <summary>
        ///     Deletes azure media service account async.
        /// </summary>
        /// <returns></returns>
        public Task<bool> DeleteAzureMediaServiceAccountAsync(string name)
        {
            string url = String.Format("{0}/{1}", MediaServicesUriElements.Accounts, name);
            return _httpClient.DeleteAsync(url).ContinueWith(tr => ProcessJsonResponse<bool>(tr));
        }

        /// <summary>
        ///     Deletes azure media service account async.
        /// </summary>
        public Task<bool> RegenerateMediaServicesAccountAsync(string name, string keyType)
        {
            string url = String.Format("{0}/{1}/AccountKeys/{2}/Regenerate", MediaServicesUriElements.Accounts, name, keyType);
            return _httpClient.PostAsync(url, null).ContinueWith(tr => ProcessJsonResponse<bool>(tr));
        }

        /// <summary>
        ///     Processes the response and handle error cases.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="responseMessage">The response message.</param>
        /// <returns></returns>
        /// <exception cref="Microsoft.WindowsAzure.ServiceManagement.ServiceManagementClientException"></exception>
        /// <exception cref="ServiceManagementError"></exception>
        private static T ProcessJsonResponse<T>(Task<HttpResponseMessage> responseMessage)
        {
            HttpResponseMessage message = responseMessage.Result;
            if (typeof (T) == typeof (bool) && message.IsSuccessStatusCode)
            {
                return (T) (object) true;
            }

            string content = message.Content.ReadAsStringAsync().Result;

            if (message.IsSuccessStatusCode)
            {
                return (T) JsonConvert.DeserializeObject(content, typeof (T));
            }

            throw CreateExceptionFromJson(message.StatusCode, content);
        }

        private static T ProcessXmlResponse<T>(Task<HttpResponseMessage> responseMessage)
        {
            HttpResponseMessage message = responseMessage.Result;
            string content = message.Content.ReadAsStringAsync().Result;
            Encoding encoding = GetEncodingFromResponseMessage(message);

            if (message.IsSuccessStatusCode)
            {
                var ser = new DataContractSerializer(typeof (T));
                using (var stream = new MemoryStream(encoding.GetBytes(content)))
                {
                    stream.Position = 0;
                    XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
                    return (T) ser.ReadObject(reader, true);
                }
            }

            throw CreateExceptionFromXml(content, message);
        }

        private static Encoding GetEncodingFromResponseMessage(HttpResponseMessage message)
        {
            string encodingString = message.Content.Headers.ContentType.CharSet;
            Encoding encoding = Encoding.GetEncoding(encodingString);
            return encoding;
        }

        private static ServiceManagementClientException CreateExceptionFromXml(string content, HttpResponseMessage message)
        {
            Encoding encoding = GetEncodingFromResponseMessage(message);

            using (var stream = new MemoryStream(encoding.GetBytes(content)))
            {
                stream.Position = 0;
                var serializer = new XmlSerializer(typeof (ServiceError));
                var serviceError = (ServiceError) serializer.Deserialize(stream);
                return new ServiceManagementClientException(message.StatusCode,
                    new ServiceManagementError
                    {
                        Code = message.StatusCode.ToString(),
                        Message = serviceError.Message
                    },
                    string.Empty);
            }
        }

        /// <summary>
        ///     Unwraps error message and creates ServiceManagementClientException.
        /// </summary>
        private static ServiceManagementClientException CreateExceptionFromJson(HttpStatusCode statusCode, string content)
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);
            content = doc.InnerText;
            var serviceError = JsonConvert.DeserializeObject(content, typeof (ServiceError)) as ServiceError;
            var exception = new ServiceManagementClientException(statusCode,
                new ServiceManagementError
                {
                    Code = statusCode.ToString(),
                    Message = serviceError.Message
                },
                string.Empty);
            return exception;
        }

        /// <summary>
        ///     Creates and initialise instance of HttpClient
        /// </summary>
        /// <returns></returns>
        private static HttpClient CreateStorageServiceHttpClient(WindowsAzureSubscription subscription)
        {
            var requestHandler = new WebRequestHandler();
            requestHandler.ClientCertificates.Add(subscription.Certificate);
            var endpoint = new StringBuilder(General.EnsureTrailingSlash(subscription.ServiceEndpoint.ToString()));
            endpoint.Append(subscription.SubscriptionId);

            //Please note that / is nessesary here
            endpoint.Append("/services/storageservices/");
            HttpClient client = HttpClientHelper.CreateClient(endpoint.ToString(), handler: requestHandler);
            client.DefaultRequestHeaders.Add(Constants.VersionHeaderName, MediaServiceVersion);
            client.DefaultRequestHeaders.Accept.Clear();
            //In version 2013-03-01 there is not support of json output in storage services REST api's
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            return client;
        }

        /// <summary>
        ///     Creates and initialise instance of HttpClient
        /// </summary>
        /// <returns></returns>
        private static HttpClient CreateIMediaServicesHttpClient(WindowsAzureSubscription subscription)
        {
            var requestHandler = new WebRequestHandler();
            requestHandler.ClientCertificates.Add(subscription.Certificate);
            var endpoint = new StringBuilder(General.EnsureTrailingSlash(subscription.ServiceEndpoint.ToString()));
            endpoint.Append(subscription.SubscriptionId);

            //Please note that / is nessesary here
            endpoint.Append("/services/mediaservices/");
            HttpClient client = HttpClientHelper.CreateClient(endpoint.ToString(), handler: requestHandler);
            client.DefaultRequestHeaders.Add(Constants.VersionHeaderName, MediaServiceVersion);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}