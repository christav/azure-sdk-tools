﻿// ----------------------------------------------------------------------------------
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

namespace Microsoft.Azure.Commands.ManagedCache
{
    using System;
    using System.Globalization;
    using System.Management.Automation;

    using Microsoft.Azure.Management.ManagedCache;
    using Microsoft.WindowsAzure.Commands.Utilities;
    using Microsoft.WindowsAzure.Commands.Utilities.Common;

    /// <summary>
    /// The base class for all Windows Azure Managed Cache Management Cmdlets
    /// </summary>
    public abstract class ManagedCacheCmdletBase : CmdletWithSubscriptionBase
    {
        private ManagedCacheClient cacheClient;
        protected const string CACHE_RESOURCE_TYPE = "Caching";
        protected const string CACHE_RESOURCE_PROVIDER_NAMESPACE = "cacheservice";
        protected const string CACHE_SERVICE_READY_STATE = "Active";

        public ManagedCacheClient CacheClient
        {
            get
            {
                if (cacheClient == null)
                {
                    cacheClient = CurrentSubscription.CreateClient <ManagedCacheClient>();
                }
                return cacheClient;
            }
        }
    }
}