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

namespace Microsoft.WindowsAzure.Commands.Utilities.Common
{
    using System;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using Management.Storage;
    using Storage;
    using Storage.Auth;
    using WindowsAzure.Common;
    using Properties;

    /// <summary>
    /// Representation of a subscription in memory
    /// </summary>
    public class WindowsAzureSubscription
    {
        public string Name { get; set; }
        public string SubscriptionId { get; set; }
        public Uri ServiceEndpoint { get; set; }
        public Uri SqlAzureServiceEndpoint { get; set; }
        public bool IsDefault { get; set; }
        public X509Certificate2 Certificate { get; set; }

        private string currentStorageAccountName;
        private CloudStorageAccount cloudStorageAccount;

        public string CurrentStorageAccountName
        {
            get { return currentStorageAccountName; }
            set
            {
                if (currentStorageAccountName != value)
                {
                    currentStorageAccountName = value;
                    cloudStorageAccount = null;
                }
            }
        }

        public CloudStorageAccount CurrentCloudStorageAccount
        {
            get { return cloudStorageAccount; }
        }

        // Access token / account name goes here once we hook up AD

        /// <summary>
        /// Create a service management client for this subscription,
        /// with appropriate credentials supplied.
        /// </summary>
        /// <typeparam name="TClient">Type of client to create, must be derived from <see cref="ServiceClient{T}"/></typeparam>
        /// <returns>The service client instance</returns>
        public TClient CreateClient<TClient>() where TClient : ServiceClient<TClient>
        {
            var credential = new CertificateCloudCredentials(SubscriptionId, Certificate);
            var constructor = typeof(TClient).GetConstructor(new[] { typeof(SubscriptionCloudCredentials), typeof(Uri) });
            if (constructor == null)
            {
                throw new InvalidOperationException(string.Format(Resources.InvalidManagementClientType, typeof(TClient).Name));
            }
            TClient client = (TClient)constructor.Invoke(new object[] { credential, ServiceEndpoint });

            var withHandlerMethod = typeof (TClient).GetMethod("WithHandler", new[] {typeof (DelegatingHandler)});
            return (TClient)withHandlerMethod.Invoke(client, new[] {new HttpRestCallLogger()});
        }

        public CloudStorageAccount GetCloudStorageAccount()
        {
            if (cloudStorageAccount == null)
            {
                using (var storageClient = CreateClient<StorageManagementClient>())
                {
                    var storageServiceResponse = storageClient.StorageAccounts.Get(CurrentStorageAccountName);
                    var storageKeysResponse = storageClient.StorageAccounts.GetKeys(CurrentStorageAccountName);

                    cloudStorageAccount = new CloudStorageAccount(
                        new StorageCredentials(storageServiceResponse.ServiceName, storageKeysResponse.PrimaryKey),
                        General.CreateHttpsEndpoint(storageServiceResponse.Properties.Endpoints[0].ToString()),
                        General.CreateHttpsEndpoint(storageServiceResponse.Properties.Endpoints[1].ToString()),
                        General.CreateHttpsEndpoint(storageServiceResponse.Properties.Endpoints[2].ToString()));
                }
            }
            return cloudStorageAccount;
        }
    }
}
