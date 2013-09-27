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

namespace Microsoft.WindowsAzure.Commands.Test.Common
{
    using System;
    using System.IO;
    using System.Linq;
    using Commands.Utilities.Common;
    using Commands.Test.Utilities.Common;
    using Moq;
    using Utilities.Resources;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProfileSubscriptionChangeTests
    {
        private WindowsAzureProfile profile;

        [TestInitialize]
        public void Setup()
        {
            profile = new WindowsAzureProfile(new Mock<IProfileStore>().Object);
            using (var s = GetPublishSettingsStream("ValidProfile.PublishSettings"))
            {
                profile.ImportPublishSettings(s);
            }
        }


        [TestMethod]
        public void RemovingCurrentSubscriptionResetsCurrentToDefault()
        {
            var defaultSubscriptionId = profile.DefaultSubscription.SubscriptionId;
            profile.CurrentSubscription = profile.Subscriptions.First(s => s.Name == Data.SampleSubscription1);
            var deletedSubscriptionId = profile.CurrentSubscription.SubscriptionId;
            Assert.IsFalse(profile.CurrentSubscription.IsDefault);

            profile.RemoveSubscription(profile.CurrentSubscription);

            Assert.IsFalse(profile.Subscriptions.Any(s => s.SubscriptionId == deletedSubscriptionId));
            Assert.AreSame(profile.DefaultSubscription, profile.CurrentSubscription);
            Assert.AreEqual(defaultSubscriptionId, profile.CurrentSubscription.SubscriptionId);
        }

        [TestMethod]
        public void DeletingDefaultSubscriptionSelectsNextSubscriptionAsDefault()
        {
            var oldDefault = profile.DefaultSubscription;
            var expectedNewDefault = profile.Subscriptions[1];

            profile.RemoveSubscription(oldDefault);

            Assert.AreSame(expectedNewDefault, profile.DefaultSubscription);
            Assert.IsFalse(profile.Subscriptions.Any(s => s == oldDefault));
        }


        [TestMethod]
        public void DeletingDefaultWhenCurrentIsDifferentDoesntChangeCurrent()
        {
            var oldDefault = profile.DefaultSubscription;
            var current = profile.Subscriptions.First(s => s.Name == Data.SampleSubscription1);
            profile.CurrentSubscription = current;

            profile.RemoveSubscription(oldDefault);

            Assert.AreSame(current, profile.CurrentSubscription);
            Assert.IsFalse(profile.Subscriptions.Any(s => s == oldDefault));
            Assert.IsTrue(profile.DefaultSubscription.IsDefault);
        }

        private Stream GetPublishSettingsStream(string resourceName)
        {
            Type locator = typeof(ResourceLocator);
            return locator.Assembly.GetManifestResourceStream(locator, resourceName);
        }
         
    }
}