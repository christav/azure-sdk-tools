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

namespace Microsoft.WindowsAzure.Management.Utilities.Subscriptions.Contract
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public interface ISubscriptionClient
    {
        /// <summary>
        ///   Get a list of resources that are registered for this subscription
        /// </summary>
        /// <param name="knownResourceTypes">Resource types to query for.</param>
        /// <returns></returns>
        Task<IEnumerable<ProviderResource>> ListResourcesAsync(IEnumerable<string> knownResourceTypes);

        /// <summary>
        ///   Register the requested resource type
        /// </summary>
        /// <param name="resourceType">Resource type to register</param>
        /// <returns>true if successful, false if already registered, throws on other errors.</returns>
        Task<bool> RegisterResourceTypeAsync(string resourceType);

        /// <summary>
        ///   Unregister the requested resource type
        /// </summary>
        /// <param name="resourceType">Resource type to unregister</param>
        /// <returns>true if successful, false if not registered, throws on other errors.</returns>
        Task<bool> UnregisterResourceTypeAsync(string resourceType);
    }

    public static class SubscriptionClientExtensions
    {
        /// <summary>
        ///   Synchronously get a list of resources that are registered for this subscription
        /// </summary>
        /// <param name="client">The client object.</param>
        /// <param name="knownResourceTypes">Resource types to query for.</param>
        /// <returns>Sequence of the state of each of the requested resources.</returns>
        public static IEnumerable<ProviderResource> ListResources(this ISubscriptionClient client,
            IEnumerable<string> knownResourceTypes)
        {
            return client.ListResourcesAsync(knownResourceTypes).Result;
        }

        /// <summary>
        ///   Synchronously register a resource type
        /// </summary>
        /// <param name="client">The client object.</param>
        /// <param name="resourceType">Resource type to register</param>
        /// <returns>true on success, false if already registered, throws on other errors.</returns>
        public static bool RegisterResourceType(this ISubscriptionClient client, string resourceType)
        {
            try
            {
                return client.RegisterResourceTypeAsync(resourceType).Result;
            }
            catch (AggregateException ex)
            {
                throw new HttpRequestException(ex.InnerExceptions[0].Message, ex);
            }
        }

        /// <summary>
        ///   Synchronously unregister a resource type
        /// </summary>
        /// <param name="client">The client object.</param>
        /// <param name="resourceType">Resource type to unregister</param>
        /// <returns>true on success, false if not registered, throws on other errors.</returns>
        public static bool UnregisterResourceType(this ISubscriptionClient client, string resourceType)
        {
            try
            {
                return client.UnregisterResourceTypeAsync(resourceType).Result;
            }
            catch (AggregateException ex)
            {
                throw new HttpRequestException(ex.InnerExceptions[0].Message, ex);
            }
        }
    }
}
