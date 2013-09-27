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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.StorageServices
{
    using System;
    using Commands.Utilities.Common;
    using Sync.Download;
    using WindowsAzure.ServiceManagement;
    using Storage.Auth;
    using Commands.ServiceManagement.Properties;

    public class StorageCredentialsFactory
    {
        private IServiceManagement channel;
        private WindowsAzureSubscription currentSubscription;

        public static bool IsChannelRequired(Uri destination)
        {
            return String.IsNullOrEmpty(destination.Query);
        }

        public StorageCredentialsFactory()
        {
        }

        public StorageCredentialsFactory(IServiceManagement channel, WindowsAzureSubscription currentSubscription)
        {
            this.channel = channel;
            this.currentSubscription = currentSubscription;
        }

        public StorageCredentials Create(BlobUri destination)
        {
            if (IsChannelRequired(destination.Uri))
            {
                if(currentSubscription == null)
                {
                    throw new ArgumentException(Resources.StorageCredentialsFactoryCurrentSubscriptionNotSet, "SubscriptionId");
                }
                StorageService sService = this.channel.GetStorageKeys(currentSubscription.SubscriptionId, destination.StorageAccountName);
                return new StorageCredentials(destination.StorageAccountName, sService.StorageServiceKeys.Primary);
            }
            return new StorageCredentials(destination.Uri.Query);
        }
    }
}