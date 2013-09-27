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

namespace Microsoft.WindowsAzure.Commands.Test.CloudService.Utilities
{
    using Commands.Subscription;
    using Commands.Utilities.CloudService;
    using Commands.Utilities.Common;
    using Commands.Utilities.Properties;
    using Commands.Utilities.Subscription.Contract;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Test.Utilities.CloudService;
    using Test.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PublishContextTests : TestBase
    {
        private static AzureServiceWrapper service;

        private static string packagePath;

        private static string configPath;

        private static ServiceSettings settings;

        private string rootPath = "serviceRootPath";

        /// <summary>
        /// When running this test double check that the certificate used in Azure.PublishSettings has not expired.
        /// </summary>
        [TestInitialize()]
        public void TestInitialize()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            service = new AzureServiceWrapper(Directory.GetCurrentDirectory(), Path.GetRandomFileName(), null);
            service.CreateVirtualCloudPackage();
            packagePath = service.Paths.CloudPackage;
            configPath = service.Paths.CloudConfiguration;
            settings = ServiceSettingsTestData.Instance.Data[ServiceSettingsState.Default];
            WindowsAzureProfile.Instance = new WindowsAzureProfile(new Mock<IProfileStore>().Object);
            WindowsAzureProfile.Instance.ImportPublishSettings(Data.ValidPublishSettings.First());
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            WindowsAzureProfile.ResetInstance();
            if (Directory.Exists(Data.AzureSdkAppDir))
            {
                new RemoveAzurePublishSettingsCommand().RemovePublishSettingsProcess(Data.AzureSdkAppDir);
            }
        }

        #region settings

        [TestMethod]
        public void TestDeploymentSettingsTestWithDefaultServiceSettings()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            settings.Subscription = "TestSubscription2";
            PublishContext deploySettings = new PublishContext(
                settings,
                packagePath,
                configPath,
                label,
                deploymentName,
                rootPath);

            AzureAssert.AreEqualPublishContext(settings, configPath, deploymentName, label, packagePath, "f62b1e05-af8f-4205-8f98-325079adc155", deploySettings);
        }

        [TestMethod]
        public void TestDeploymentSettingsTestWithFullServiceSettings()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            ServiceSettings fullSettings = ServiceSettingsTestData.Instance.Data[ServiceSettingsState.Sample1];
            PublishContext deploySettings = new PublishContext(
                fullSettings,
                packagePath,
                configPath,
                label,
                deploymentName,
                rootPath);

            AzureAssert.AreEqualPublishContext(
                fullSettings,
                configPath,
                deploymentName,
                label,
                packagePath,
                "f62b1e05-af8f-4205-8f98-325079adc155",
                deploySettings);
        }

        [TestMethod]
        public void TestDeploymentSettingsTestNullSettingsFail()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;

            try
            {
                PublishContext deploySettings = new PublishContext(
                    null,
                    packagePath,
                    configPath,
                    label,
                    deploymentName,
                    rootPath);
                Assert.Fail("No exception was thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentException));
                Assert.AreEqual<string>(Resources.InvalidServiceSettingMessage, ex.Message);
            }
        }

        #endregion

        #region packagePath

        [TestMethod]
        public void TestDeploymentSettingsTestEmptyPackagePathFail()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            string expectedMessage = string.Format(Resources.InvalidOrEmptyArgumentMessage, "packagePath");

            Testing.AssertThrows<ArgumentException>(() => new PublishContext(
                settings,
                string.Empty,
                configPath,
                label,
                deploymentName,
                rootPath), expectedMessage);
        }

        [TestMethod]
        public void TestDeploymentSettingsTestNullPackagePathFail()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            string expectedMessage = string.Format(Resources.InvalidOrEmptyArgumentMessage, "packagePath");

            Testing.AssertThrows<ArgumentException>(() => new PublishContext(
                settings,
                null,
                configPath,
                label,
                deploymentName,
                rootPath), expectedMessage);
        }

        #endregion

        #region configPath

        [TestMethod]
        public void TestDeploymentSettingsTestEmptyConfigPathFail()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            string expectedMessage = string.Format(Resources.InvalidOrEmptyArgumentMessage, Resources.ServiceConfiguration);

            Testing.AssertThrows<ArgumentException>(() => new PublishContext(
                settings,
                packagePath,
                string.Empty,
                label,
                deploymentName,
                rootPath), expectedMessage);
        }

        [TestMethod]
        public void TestDeploymentSettingsTestNullConfigPathFail()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            string expectedMessage = string.Format(Resources.InvalidOrEmptyArgumentMessage, Resources.ServiceConfiguration);

            Testing.AssertThrows<ArgumentException>(() => new PublishContext(
                settings,
                packagePath,
                null,
                label,
                deploymentName,
                rootPath), expectedMessage);
        }

        [TestMethod]
        public void TestDeploymentSettingsTestDoesNotConfigPathFail()
        {
            string label = "MyLabel";
            string deploymentName = service.ServiceName;
            string doesNotExistDir = Path.Combine(Directory.GetCurrentDirectory(), "qewindw443298.cscfg");

            try
            {
                PublishContext deploySettings = new PublishContext(
                    settings,
                    packagePath,
                    doesNotExistDir,
                    label,
                    deploymentName,
                    rootPath);
                Assert.Fail("No exception was thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(FileNotFoundException));
                Assert.AreEqual<string>(string.Format(Resources.PathDoesNotExistForElement, Resources.ServiceConfiguration, doesNotExistDir), ex.Message);
            }
        }

        #endregion

        #region label

        [TestMethod]
        public void TestDeploymentSettingsTestNullLabelFail()
        {
            string deploymentName = service.ServiceName;

            try
            {
                PublishContext deploySettings = new PublishContext(
                    settings,
                    packagePath,
                    configPath,
                    null,
                    deploymentName,
                    rootPath);
                Assert.Fail("No exception was thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentException));
                Assert.IsTrue(string.Compare(
                    string.Format(Resources.InvalidOrEmptyArgumentMessage,
                    "serviceName"), ex.Message, true) == 0);
            }
        }

        #endregion

        private ISubscriptionClient CreateMockSubscriptionClient()
        {
            var mock = new Mock<ISubscriptionClient>();
            mock.Setup(c => c.ListResourcesAsync(It.IsAny<IEnumerable<string>>()))
                .Returns(() => Task.Factory.StartNew(() => (IEnumerable<ProviderResource>)new ProviderResource[0]));
            mock.Setup(c => c.RegisterResourceTypeAsync(It.IsAny<string>()))
                .Returns(() => Task.Factory.StartNew(() => true));
            return mock.Object;
        }
    }
}