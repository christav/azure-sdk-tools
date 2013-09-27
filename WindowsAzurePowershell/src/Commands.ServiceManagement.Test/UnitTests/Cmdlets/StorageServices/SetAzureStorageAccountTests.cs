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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.Test.UnitTests.Cmdlets.StorageServices
{
    using System.IO;
    using System.Text;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;
    using WindowsAzure.ServiceManagement;
    using Newtonsoft.Json;
    using Commands.Test.Utilities.Common;
    using Commands.ServiceManagement.StorageServices;
    using Commands.Test.Utilities.CloudService;

    [TestClass]
    public class SetAzureStorageAccountTests : TestBase
    {
        private TestData found;
        private SimpleServiceManagement channel;

        [TestInitialize]
        public void SetupTest()
        {
            found = new TestData();
            channel = new SimpleServiceManagement
            {
                UpdateStorageServiceThunk = ar =>
                {
                    found.SubscriptionId = (string)ar.Values["subscriptionId"];
                    found.StorageServiceName = (string)ar.Values["StorageServiceName"];
                    found.UpdateStorageServiceInput = (UpdateStorageServiceInput)ar.Values["updateStorageServiceInput"];
                }
            };
        }

        private void AssertExpectedValue(TestData expected)
        {
            var command = new SetAzureStorageAccountCommand(channel)
            {
                CommandRuntime = new MockCommandRuntime(),
                ShareChannel = true,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = expected.SubscriptionId },
                StorageAccountName = expected.StorageServiceName,
                Description = expected.UpdateStorageServiceInput.Description,
                Label = expected.UpdateStorageServiceInput.Label,
                GeoReplicationEnabled = expected.UpdateStorageServiceInput.GeoReplicationEnabled
            };
            command.SetStorageAccountProcess();

            Assert.AreEqual(expected.ToString(), found.ToString());
        }

        [TestMethod]
        public void TestGeoReplicationEnabled()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        GeoReplicationEnabled = true
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationDisabled()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        GeoReplicationEnabled = false
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationEnabledWithDescriptionAndLabel()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        Description = "MyDescription",
                        Label = "MyLabel",
                        GeoReplicationEnabled = true
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationEnabledWithDescription()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        Description = "MyDescription",
                        GeoReplicationEnabled = true
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationEnabledWithLabel()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        Label = "MyLabel",
                        GeoReplicationEnabled = true
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationDisabledWithDescriptionAndLabel()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        Description = "MyDescription",
                        Label = "MyLabel",
                        GeoReplicationEnabled = false
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationDisabledWithDescription()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        Description = "MyDescription",
                        GeoReplicationEnabled = false
                    }
                }
           );
        }

        [TestMethod]
        public void TestGeoReplicationDisabledWithLabel()
        {
            AssertExpectedValue(
                new TestData
                {
                    SubscriptionId = "MySubscription",
                    StorageServiceName = "MyStorageService",
                    UpdateStorageServiceInput = new UpdateStorageServiceInput
                    {
                        Label = "MyLabel",
                        GeoReplicationEnabled = false
                    }
                }
           );
        }

    }

    class TestData
    {
        public string SubscriptionId { get; set; }
        public string StorageServiceName { get; set; }
        public UpdateStorageServiceInput UpdateStorageServiceInput { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            using (TextWriter writer = new StringWriter(builder))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, this);
            }

            return builder.ToString();
        }
    }
}