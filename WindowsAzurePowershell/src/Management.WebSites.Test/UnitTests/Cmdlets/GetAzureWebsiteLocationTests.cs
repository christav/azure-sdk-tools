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

namespace Microsoft.WindowsAzure.Management.Websites.Test.UnitTests.Cmdlets
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Management.Services;
    using Management.Test.Stubs;
    using Management.Test.Tests.Utilities;
    using Utilities;
    using VisualStudio.TestTools.UnitTesting;
    using Websites.Cmdlets;

    [TestClass]
    public class GetAzureWebsiteLocationTests : WebsitesTestBase
    {
        [TestMethod]
        public void ProcessGetAzureWebsiteLocationTest()
        {
            // Setup
            SimpleWebsitesManagement channel = new SimpleWebsitesManagement();

            // Test
            GetAzureWebsiteLocationCommand getAzureWebsiteCommand = new GetAzureWebsiteLocationCommand(channel)
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime()
            };

            getAzureWebsiteCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.Count);
            var locations = (IEnumerable<string>)((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(locations);
            Assert.IsTrue(locations.Any(loc => loc.Equals("East US")));
            Assert.IsTrue(locations.Any(loc => loc.Equals("West US")));
            Assert.IsTrue(locations.Any(loc => loc.Equals("North Europe")));
            Assert.IsTrue(locations.Any(loc => loc.Equals("West Europe")));
            Assert.IsTrue(locations.Any(loc => loc.Equals("East Asia")));
        }
    }
}
