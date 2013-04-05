// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Management.Utilities.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Common;
    using Contract;

    /// <summary>
    /// Class implementing <see cref="ISubscriptionClient"/>, providing the
    /// ability to list, register and unregister resource types for a
    /// subscription.
    /// </summary>
    public class SubscriptionClient : ISubscriptionClient
    {
        private readonly HttpClient httpClient;
        private readonly SubscriptionData subscription;

        private static readonly XNamespace azureNS = ManagementConstants.ServiceManagementNS;

        /// <summary>
        /// Create an instance of <see cref="SubscriptionClient"/> that
        /// works against the given <paramref name="subscription"/>.
        /// </summary>
        /// <param name="subscription">The subscription to manipulate</param>
        public SubscriptionClient(SubscriptionData subscription)
            : this(subscription, CreateDefaultFinalHandler(subscription))
        {
        }

        /// <summary>
        /// <para>
        /// Creates an instance of <see cref="SubscriptionClient"/> that
        /// works against the given <paramref name="subscription"/> and uses
        /// the <paramref name="finalHandler"/> to send the Http requests.
        /// </para>
        /// <para>This constructor is primarily used for testing to mock out
        /// the actual HTTP traffic.</para>
        /// </summary>
        /// <param name="subscription">Subscription to manipulate</param>
        /// <param name="finalHandler">HttpMessageHandler used to send the messages to the server.</param>
        public SubscriptionClient(SubscriptionData subscription, HttpMessageHandler finalHandler)
        {
            httpClient = CreateHttpClient(finalHandler);
            this.subscription = subscription;
            httpClient.BaseAddress = new Uri(subscription.ServiceEndpoint);
        }

        /// <summary>
        /// Get a list of resources that are registered for this subscription
        /// </summary>
        /// <param name="knownResourceTypes">Resource types to query for.</param>
        /// <returns></returns>
        public Task<IEnumerable<ProviderResource>> ListResourcesAsync(IEnumerable<string> knownResourceTypes)
        {
            var path = ProviderRegistrationConstants.ListResourcesPath(subscription.SubscriptionId, knownResourceTypes);

            var request = CreateRequest(HttpMethod.Get, path);

            return httpClient.SendAsync(request)
                .ContinueWith(tr => ProcessListResourcesResponse(tr)).Unwrap()
                .ContinueWith(ts => DeserializeListResourcesResponse(ts));
        }

        private Task<string> ProcessListResourcesResponse(Task<HttpResponseMessage> responseMessage)
        {
            var response = responseMessage.Result;
            response.EnsureSuccessStatusCode();

            return response.Content.ReadAsStringAsync();
        }

        private IEnumerable<ProviderResource> DeserializeListResourcesResponse(Task<string> contentTask)
        {
            string content = contentTask.Result;

            XDocument doc = XDocument.Parse(content);
            return doc.Root.Descendants(azureNS + "Service").Select(element =>
                new ProviderResource
                {
                    State = (string) (element.Element(azureNS + "State")),
                    Type = (string) (element.Element(azureNS + "Type"))
                });
        }

        /// <summary>
        /// Register the requested resource type
        /// </summary>
        /// <param name="resourceType">Resource type to register</param>
        /// <returns>true if successful, false if already registered, throws on other errors.</returns>
        public Task<bool> RegisterResourceTypeAsync(string resourceType)
        {
            var path = ProviderRegistrationConstants.ActionPath(subscription.SubscriptionId, 
                resourceType, 
                ProviderRegistrationConstants.Register);

            var request = CreateRequest(HttpMethod.Put, path);

            return httpClient.SendAsync(request)
                .ContinueWith(tr => ProcessActionResponse(tr));
        }

        /// <summary>
        /// Unregister the requested resource type
        /// </summary>
        /// <param name="resourceType">Resource type to unregister</param>
        /// <returns>true if successful, false if not registered, throws on other errors.</returns>
        public Task<bool> UnregisterResourceTypeAsync(string resourceType)
        {
            var path = ProviderRegistrationConstants.ActionPath(subscription.SubscriptionId,
                resourceType,
                ProviderRegistrationConstants.Unregister);

            var request = CreateRequest(HttpMethod.Put, path);

            return httpClient.SendAsync(request)
                .ContinueWith(tr => ProcessActionResponse(tr));
        }

        private bool ProcessActionResponse(Task<HttpResponseMessage> responseMessageTask)
        {
            HttpResponseMessage response = responseMessageTask.Result;
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return false;
            }
            response.EnsureSuccessStatusCode();
            return true;
        }

        private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler);
        }

        private static HttpMessageHandler CreateDefaultFinalHandler(SubscriptionData subscription)
        {
            var handler = new WebRequestHandler();
            handler.ClientCertificates.Add(subscription.Certificate);
            return handler;
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
            request.Headers.Add(ApiConstants.VersionHeaderName, ApiConstants.ResourceRegistrationApiVersion);
            request.Headers.Accept.Add(HttpConstants.XmlMediaType);
            return request;
        }
    }
}
