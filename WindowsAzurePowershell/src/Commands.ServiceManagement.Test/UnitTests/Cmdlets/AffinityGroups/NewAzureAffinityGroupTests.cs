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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.Test.UnitTests.Cmdlets.AffinityGroups
{
    using Commands.Utilities.Common;
    using Commands.Test.Utilities.CloudService;
    using Commands.Test.Utilities.Common;
    using Commands.ServiceManagement.AffinityGroups;
    using VisualStudio.TestTools.UnitTesting;
    using WindowsAzure.ServiceManagement;


    [TestClass]
    public class NewAzureAffinityGroupTests: TestBase
    {
        FileSystemHelper files;

        [TestInitialize]
        public void SetupTest()
        {
            files = new FileSystemHelper(this);
            //files.CreateAzureSdkDirectoryAndImportPublishSettings();
        }

        [TestCleanup]
        public void CleanupTest()
        {
            //files.Dispose();
        }

        [TestMethod]
        public void NewAzureAffinityGroupTest()
        {
            const string affinityGroupName = "myAffinity";
            CreateAffinityGroupInput affinityGroup = new CreateAffinityGroupInput
            {
                Name = affinityGroupName
            };

            // Setup
            bool created = false;
            SimpleServiceManagement channel = new SimpleServiceManagement();
            channel.CreateAffinityGroupThunk = ar =>
            {
                CreateAffinityGroupInput createAffinityGroupInput = (CreateAffinityGroupInput)ar.Values["input"];
                Assert.AreEqual(affinityGroup.Name, createAffinityGroupInput.Name);
                created = true;
            };

            // Test
            NewAzureAffinityGroup newAzureAffinityGroupCommand = new NewAzureAffinityGroup(channel)
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime(),
                Name = affinityGroupName
            };

            newAzureAffinityGroupCommand.ExecuteCommand();
            Assert.IsTrue(created);
        }
    }
}