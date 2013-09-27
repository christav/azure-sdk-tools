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

namespace Microsoft.WindowsAzure.Commands.Test.Subscription
{
    using System.Linq;
    using Commands.Subscription;
    using Commands.Utilities.Common;
    using Moq;
    using Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetSubscriptionTest
    {
        private WindowsAzureProfile profile;
        private MockCommandRuntime mockCommandRuntime;
        private GetAzureSubscriptionCommand cmdlet;

        [TestInitialize]
        public void Setup()
        {
            profile = new WindowsAzureProfile(new Mock<IProfileStore>().Object);
            profile.ImportPublishSettings(Data.ValidPublishSettings.First());

            mockCommandRuntime = new MockCommandRuntime();

            cmdlet = new GetAzureSubscriptionCommand
            {
                Profile = profile,
                CommandRuntime = mockCommandRuntime
            };
        }

        [TestMethod]
        public void GetsAllSubscriptionsByNameWhenNameIsBlank()
        {
            cmdlet.SubscriptionName = null;
        
            cmdlet.GetByName();

            Assert.AreEqual(6, mockCommandRuntime.OutputPipeline.Count);
        }

        [TestMethod]
        public void CanGetSubscriptionByName()
        {
            var expected = profile.CurrentSubscription;
            cmdlet.SubscriptionName = expected.Name;

            cmdlet.GetByName();

            Assert.AreEqual(1, mockCommandRuntime.OutputPipeline.Count);
            Assert.IsInstanceOfType(mockCommandRuntime.OutputPipeline[0], typeof (SubscriptionData));
            Assert.AreEqual(expected.Name, ((SubscriptionData) mockCommandRuntime.OutputPipeline[0]).SubscriptionName);
            Assert.AreEqual(expected.SubscriptionId,
                ((SubscriptionData) (mockCommandRuntime.OutputPipeline[0])).SubscriptionId);
        }

        [TestMethod]
        public void CanGetCurrentSubscription()
        {
            // Select a subscription that is not the default
            profile.CurrentSubscription = profile.Subscriptions.First(s => !s.IsDefault);

            cmdlet.GetCurrent();

            Assert.AreEqual(1, mockCommandRuntime.OutputPipeline.Count);
            Assert.AreEqual(profile.CurrentSubscription.Name, 
                ((SubscriptionData)mockCommandRuntime.OutputPipeline[0]).SubscriptionName);
            Assert.AreEqual(profile.CurrentSubscription.SubscriptionId,
                ((SubscriptionData)(mockCommandRuntime.OutputPipeline[0])).SubscriptionId);
        }

        [TestMethod]
        public void CanGetDefaultSubscription()
        {
            // Select a subscription that is not the default
            profile.CurrentSubscription = profile.Subscriptions.First(s => !s.IsDefault);

            cmdlet.GetDefault();

            Assert.AreEqual(1, mockCommandRuntime.OutputPipeline.Count);
            Assert.AreEqual(profile.DefaultSubscription.Name,
                ((SubscriptionData)mockCommandRuntime.OutputPipeline[0]).SubscriptionName);
            Assert.AreEqual(profile.DefaultSubscription.SubscriptionId,
                ((SubscriptionData)(mockCommandRuntime.OutputPipeline[0])).SubscriptionId);
            
        }
    }
}