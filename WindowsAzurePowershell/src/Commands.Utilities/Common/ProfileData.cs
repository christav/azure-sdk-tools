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
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// This class provides the representation of
    /// data loaded and saved into data files
    /// for WindowsAzureProfile.
    /// </summary>
    [DataContract]
    public class ProfileData
    {
        [DataMember]
        public string DefaultEnvironmentName { get; set; }
         
        [DataMember]
        public IEnumerable<AzureEnvironmentData> Environments { get; set; }

        [DataMember]
        public IEnumerable<AzureSubscriptionData> Subscriptions { get; set; } 
    }

    /// <summary>
    /// This class provides the representation of
    /// data loaded and saved into data files for
    /// an individual Azure environment
    /// </summary>
    [DataContract]
    public class AzureEnvironmentData
    {
        /// <summary>
        /// Constructor used by data contract serializer
        /// </summary>
        public AzureEnvironmentData()
        {
        }

        /// <summary>
        /// Helper constructor for converting from in memory object
        /// to serializable one.
        /// </summary>
        /// <param name="inMemoryEnvironment">Environment to serialize data from.</param>

        public AzureEnvironmentData(WindowsAzureEnvironment inMemoryEnvironment)
        {
            Name = inMemoryEnvironment.Name;
            PublishSettingsFileUrl = inMemoryEnvironment.PublishSettingsFileUrl;
            ServiceEndpoint = inMemoryEnvironment.ServiceEndpoint;
            ManagementPortalUrl = inMemoryEnvironment.ManagementPortalUrl;
            StorageEndpointSuffix = inMemoryEnvironment.StorageEndpointSuffix;
            AdTenantUrl = inMemoryEnvironment.AdTenantUrl;
            CommonTenantId = inMemoryEnvironment.CommonTenantId;
        }

        /// <summary>
        /// Helper method to convert to an in-memory environment object.
        /// </summary>
        public WindowsAzureEnvironment ToAzureEnvironment()
        {
            return new WindowsAzureEnvironment
            {
                Name = this.Name,
                PublishSettingsFileUrl = this.PublishSettingsFileUrl,
                ServiceEndpoint = this.ServiceEndpoint,
                ManagementPortalUrl = this.ManagementPortalUrl,
                StorageEndpointSuffix = this.StorageEndpointSuffix,
                AdTenantUrl = this.AdTenantUrl,
                CommonTenantId = this.CommonTenantId
            };
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string PublishSettingsFileUrl { get; set; }

        [DataMember]
        public string ServiceEndpoint { get; set; }

        [DataMember]
        public string ManagementPortalUrl { get; set; }

        [DataMember]
        public string StorageEndpointSuffix { get; set; }

        [DataMember]
        public string AdTenantUrl { get; set; }

        [DataMember]
        public string CommonTenantId { get; set; }
    }

    /// <summary>
    /// This class provides the representation of data loaded
    /// and saved into data file for an individual Azure subscription.
    /// </summary>
    [DataContract]
    public class AzureSubscriptionData
    {
        /// <summary>
        /// Constructor used by DataContractSerializer
        /// </summary>
        public AzureSubscriptionData()
        {
            
        }

        /// <summary>
        /// Helper constructor to copy data from in memory to serialization format.
        /// </summary>
        /// <param name="inMemorySubscription">The subscription to serialize</param>
        public AzureSubscriptionData(WindowsAzureSubscription inMemorySubscription)
        {
            Name = inMemorySubscription.Name;
            SubscriptionId = inMemorySubscription.SubscriptionId;
            ManagementEndpoint = inMemorySubscription.ServiceEndpoint.ToString();
            IsDefault = inMemorySubscription.IsDefault;
            ManagementCertificate = inMemorySubscription.Certificate.Thumbprint;
            CloudStorageAccount = inMemorySubscription.CurrentStorageAccountName;
        }

        /// <summary>
        /// Helper method to convert to an in memory subscription object.
        /// </summary>
        /// <returns>The in memory subscription</returns>
        public WindowsAzureSubscription ToAzureSubscription()
        {
            return new WindowsAzureSubscription
            {
                Name = this.Name,
                SubscriptionId = this.SubscriptionId,
                ServiceEndpoint = new Uri(ManagementEndpoint),
                IsDefault = this.IsDefault,
                Certificate = WindowsAzureCertificate.FromThumbprint(ManagementCertificate),
                CurrentStorageAccountName = CloudStorageAccount
            };
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SubscriptionId { get; set; }

        [DataMember]
        public string ManagementEndpoint { get; set; }

        [DataMember]
        public bool IsDefault { get; set; }

        [DataMember]
        public string ManagementCertificate { get; set; }

        [DataMember]
        public string CloudStorageAccount { get; set; }
    }
}
