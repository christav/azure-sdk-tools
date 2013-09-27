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
    /// <summary>
    /// Base class for cmdlets that need the current subscription but don't
    /// otherwise need the channel stuff.
    /// </summary>

    public abstract class CmdletWithSubscriptionBase : CmdletBase
    {
        /// <summary>
        /// Override this method if you need to do processing
        /// when the current subscription changes.
        /// </summary>
        protected virtual void OnCurrentSubscriptionUpdated()
        {

        }

        private WindowsAzureProfile profile;

        public WindowsAzureProfile Profile
        {
            get { return profile ?? WindowsAzureProfile.Instance; }

            set { profile = value; }
        }

        private WindowsAzureSubscription currentSubscription;

        public WindowsAzureSubscription CurrentSubscription
        {
            get { return currentSubscription ?? Profile.CurrentSubscription; }
            set
            {
                if (currentSubscription != value)
                {
                    currentSubscription = value;
                    OnCurrentSubscriptionUpdated();
                }
            }
        }
    }
}