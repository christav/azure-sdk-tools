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
    using System;
    using System.IO;
    using System.Linq;
    using Commands.Subscription;
    using Commands.Utilities.Common;
    using Commands.Utilities.Properties;
    using Moq;
    using Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ImportAzurePublishSettingsTests
    {
        private WindowsAzureProfile profile;
        private MockCommandRuntime mockCommandRuntime;
        private ImportAzurePublishSettingsCommand cmdlet;

        [TestInitialize]
        public void Setup()
        {
            mockCommandRuntime = new MockCommandRuntime();
            profile = new WindowsAzureProfile(new Mock<IProfileStore>().Object);
            cmdlet = new ImportAzurePublishSettingsCommand
            {
                Profile = profile,
                CommandRuntime = mockCommandRuntime
            };
        }

        [TestMethod]
        public void CanImportValidPublishSettings()
        {
            cmdlet.PublishSettingsFile = Data.ValidPublishSettings.First();

            cmdlet.ExecuteCmdlet();

            Assert.AreEqual(Data.Subscription1, profile.CurrentSubscription.Name);
            Assert.IsTrue(profile.CurrentSubscription.IsDefault);
        }

        [TestMethod]
        public void CanImportPublishSettingsV2File()
        {
            cmdlet.PublishSettingsFile = Data.ValidPublishSettings2.First();

            cmdlet.ExecuteCmdlet();

            Assert.AreEqual(Data.SampleSubscription1, profile.CurrentSubscription.Name);
            Assert.AreEqual(new Uri("https://newmanagement.core.windows.net/"),
                profile.CurrentSubscription.ServiceEndpoint);
            Assert.IsNotNull(profile.CurrentSubscription.Certificate);
            Assert.IsTrue(profile.CurrentSubscription.IsDefault);
        }

        [TestMethod]
        public void MultipleImportDoesntAffectExplicitlySetCurrentSubscription()
        {
            cmdlet.PublishSettingsFile = Data.ValidPublishSettings.First();
            cmdlet.ExecuteCmdlet();

            var currentSubscription = profile.CurrentSubscription;

            Assert.AreEqual(Data.Subscription1, currentSubscription.Name);
            Assert.IsTrue(currentSubscription.IsDefault);

            var newCurrentSubscription =
                profile.Subscriptions.First(s => !s.SubscriptionId.Equals(currentSubscription.SubscriptionId));
            profile.CurrentSubscription = newCurrentSubscription;
            var newSubscriptionId = newCurrentSubscription.SubscriptionId;

            cmdlet.PublishSettingsFile = Data.ValidPublishSettings.First();
            cmdlet.ExecuteCmdlet();

            Assert.AreEqual(profile.CurrentSubscription.SubscriptionId, newSubscriptionId);
            Assert.IsTrue(profile.Subscriptions.Contains(profile.CurrentSubscription));
        }

        [TestMethod]
        public void CanImportFromDirectoryWithSingleFile()
        {
            var testDir = new TestDirBuilder("testdir")
                .AddFile(Data.ValidPublishSettings.First(), "myfile.publishsettings");

            cmdlet.PublishSettingsFile = testDir.DirectoryName;

            cmdlet.ExecuteCmdlet();

            Assert.AreEqual(profile.CurrentSubscription.Name, Data.Subscription1);
            Assert.IsTrue(profile.CurrentSubscription.IsDefault);
            Assert.AreEqual(testDir.FilePaths[0], mockCommandRuntime.OutputPipeline[0].ToString());
        }

        [TestMethod]
        public void ImportFindsFileInCurrentDirectoryIfNoPathGiven()
        {
            var testDir = new TestDirBuilder("testdir")
                .AddFile(Data.ValidPublishSettings.First(), "myfile.publishsettings");

            using (testDir.Pushd())
            {
                cmdlet.ExecuteCmdlet();
            }
            Assert.AreEqual(profile.CurrentSubscription.Name, Data.Subscription1);
            Assert.IsTrue(profile.CurrentSubscription.IsDefault);
            Assert.AreEqual(Path.GetFullPath(testDir.FilePaths[0]), mockCommandRuntime.OutputPipeline[0].ToString());
        }

        [TestMethod]
        public void ImportingFromDirectoryWithNoFilesThrows()
        {
            var testDir = new TestDirBuilder("testdir3");
            using (testDir.Pushd())
            {
                Testing.AssertThrows<Exception>(
                    () => cmdlet.ExecuteCmdlet(),
                    string.Format(Resources.NoPublishSettingsFilesFoundMessage, Directory.GetCurrentDirectory()));
            }
        }

        [TestMethod]
        public void ImportingFromDirectoryWithMultiplePublishSettingsImportsFirstOneAndGivesWarning()
        {
            var testDir = new TestDirBuilder("testdir2")
                .AddFile(Data.ValidPublishSettings.First(), "myfile1.publishsettings")
                .AddFile(Data.ValidPublishSettings.First(), "myfile2.publishsettings");

            cmdlet.PublishSettingsFile = testDir.DirectoryName;
            cmdlet.ExecuteCmdlet();

            Assert.AreEqual(Data.Subscription1, profile.CurrentSubscription.Name);
            Assert.IsTrue(profile.CurrentSubscription.IsDefault);
            Assert.AreEqual(testDir.FilePaths[0], mockCommandRuntime.OutputPipeline[0].ToString());
            Assert.AreEqual(string.Format(Resources.MultiplePublishSettingsFilesFoundMessage, testDir.FilePaths[0]), 
                mockCommandRuntime.WarningStream[0]);
        }
    }
}