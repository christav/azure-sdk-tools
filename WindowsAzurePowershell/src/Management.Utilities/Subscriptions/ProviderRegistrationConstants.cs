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
    using System.Collections.Generic;

    internal static class ProviderRegistrationConstants
    {
        public const string Register = "register";
        public const string Unregister = "unregister";

        internal static string ListResourcesPath(string subscriptionId, IEnumerable<string> knownResourceTypes)
        {
            return string.Format("/{0}/services/?serviceList={1}",
                subscriptionId, string.Join(",", knownResourceTypes));
        }

        internal static string ActionPath(string subscriptionId, string resourceType, string action)
        {
            return string.Format("/{0}/services?{1}&action={2}",
                subscriptionId, resourceType, action);
        }
    }
}