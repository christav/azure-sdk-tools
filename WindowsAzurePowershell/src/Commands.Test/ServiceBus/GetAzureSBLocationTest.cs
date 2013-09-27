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

namespace Microsoft.WindowsAzure.Commands.Test.ServiceBus
{
    using System.Collections.Generic;
    using Commands.Utilities.Common;
    using Commands.ServiceBus;
    using Utilities.Common;
    using Utilities.ServiceBus;
    using Commands.Utilities.ServiceBus.ResourceModel;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetAzureSBLocationTests : TestBase
    {
        [TestInitialize]
        public void SetupTest()
        {
            new FileSystemHelper(this).CreateAzureSdkDirectoryAndImportPublishSettings();
        }

        [TestMethod]
        public void GetAzureSBLocationSuccessfull()
        {
            // Setup
            SimpleServiceBusManagement channel = new SimpleServiceBusManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            GetAzureSBLocationCommand cmdlet = new GetAzureSBLocationCommand(channel) { CommandRuntime = mockCommandRuntime };
            List<ServiceBusRegion> expected = new List<ServiceBusRegion>();
            expected.Add(new ServiceBusRegion { Code = name, FullName = name });
            channel.ListServiceBusRegionsThunk = gn => { return expected; };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            List<ServiceBusRegion> actual = mockCommandRuntime.OutputPipeline[0] as List<ServiceBusRegion>;
            Assert.AreEqual<int>(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual<string>(expected[i].Code, actual[i].Code);
                Assert.AreEqual<string>(expected[i].FullName, actual[i].FullName);
            }
        }
    }
}