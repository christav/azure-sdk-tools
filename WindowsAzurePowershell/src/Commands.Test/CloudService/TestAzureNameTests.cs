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

namespace Microsoft.WindowsAzure.Commands.Test.CloudService
{
    using Commands.Utilities.Common;
    using Commands.CloudService;
    using Test.Utilities.CloudService;
    using Test.Utilities.Common;
    using Test.Utilities.ServiceBus;
    using Commands.Utilities.ServiceBus.ResourceModel;
    using ServiceManagement;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestAzureNameTests : TestBase
    {
        SimpleServiceManagement channel;
        SimpleServiceBusManagement serviceBusChannel;
        MockCommandRuntime mockCommandRuntime;
        TestAzureNameCommand cmdlet;
        string subscriptionId = "my subscription Id";

        [TestInitialize]
        public void SetupTest()
        {
            channel = new SimpleServiceManagement();
            serviceBusChannel = new SimpleServiceBusManagement();
            mockCommandRuntime = new MockCommandRuntime();
            cmdlet = new TestAzureNameCommand(channel, serviceBusChannel) { CommandRuntime = mockCommandRuntime };
        }

        [TestMethod]
        public void TestAzureServiceNameUsed()
        {
            string name = "test";
            channel.IsDNSAvailableThunk = idnsa => { return new AvailabilityResponse { Result = false }; };

            cmdlet.IsDNSAvailable(subscriptionId, name);

            bool actual = (bool)mockCommandRuntime.OutputPipeline[0];
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TestAzureServiceNameIsNotUsed()
        {
            string name = "test";
            channel.IsDNSAvailableThunk = idnsa => { return new AvailabilityResponse { Result = true }; };

            cmdlet.IsDNSAvailable(subscriptionId, name);

            bool actual = (bool)mockCommandRuntime.OutputPipeline[0];
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void TestAzureStorageNameUsed()
        {
            string name = "test";
            channel.IsStorageServiceAvailableThunk = idnsa => { return new AvailabilityResponse { Result = false }; };

            cmdlet.IsStorageServiceAvailable(subscriptionId, name);

            bool actual = (bool)mockCommandRuntime.OutputPipeline[0];
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TestAzureStorageNameIsNotUsed()
        {
            string name = "test";
            channel.IsStorageServiceAvailableThunk = idnsa => { return new AvailabilityResponse { Result = true }; };

            cmdlet.IsStorageServiceAvailable(subscriptionId, name);

            bool actual = (bool)mockCommandRuntime.OutputPipeline[0];
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void TestAzureServiceBusNamespaceUsed()
        {
            string name = "test";
            serviceBusChannel.IsServiceBusNamespaceAvailableThunk = idnsa => { return new ServiceBusNamespaceAvailabilityResponse { Result = false }; };

            cmdlet.IsServiceBusNamespaceAvailable(subscriptionId, name);

            bool actual = (bool)mockCommandRuntime.OutputPipeline[0];

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TestAzureServiceBusNamespaceIsNotUsed()
        {
            string name = "test";
            serviceBusChannel.IsServiceBusNamespaceAvailableThunk = idnsa => { return new ServiceBusNamespaceAvailabilityResponse { Result = false }; };

            cmdlet.IsServiceBusNamespaceAvailable(subscriptionId, name);

            bool actual = (bool)mockCommandRuntime.OutputPipeline[0];
            
            Assert.IsTrue(actual);
        }
    }
}
