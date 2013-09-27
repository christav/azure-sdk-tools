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
    using Commands.Utilities.CloudService;
    using Commands.Utilities.Common;
    using System;
    using Test.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;
    using TestBase = Test.Utilities.Common.TestBase;
    using Testing = Test.Utilities.Common.Testing;

    [TestClass]
    public class ServiceSettingsTests : TestBase
    {
        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
        }

        [TestMethod]
        public void ServiceSettingsTest()
        {


            ServiceSettings settings = new ServiceSettings();
            AzureAssert.AreEqualServiceSettings(string.Empty, string.Empty, string.Empty, string.Empty, settings);
        }

        /// <summary>
        /// Verify that using an invalid storage account name throws an
        /// exception.
        /// </summary>
        [TestMethod]
        public void InvalidStorageAccountName()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                string serviceName = null;
                Testing.AssertThrows<ArgumentException>(() =>
                    ServiceSettings.LoadDefault(null, null, null, null, null, "I HAVE INVALID CHARACTERS !@#$%", null, null, out serviceName));
                Testing.AssertThrows<ArgumentException>(() =>
                    ServiceSettings.LoadDefault(null, null, null, null, null, "ihavevalidcharsbutimjustwaytooooooooooooooooooooooooooooooooooooooooolong", null, null, out serviceName));
            }
        }

        /// <summary>
        /// Verify that a service name with invalid characters is correctly
        /// sanitized to a storage account name.
        /// </summary>
        [TestMethod]
        public void SanitizeServiceNameForStorageAccountName()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                string serviceName = null;
                ServiceSettings settings = ServiceSettings.LoadDefault(null, null, null, null, null, null, "My-Custom-Service!", null, out serviceName);
                Assert.AreEqual("myx2dcustomx2dservicex21", settings.StorageServiceName);

                settings = ServiceSettings.LoadDefault(null, null, null, null, null, null, "MyCustomServiceIsWayTooooooooooooooooooooooooLong", null, out serviceName);
                Assert.AreEqual("mycustomserviceiswaytooo", settings.StorageServiceName);
            }
        }

        /// <summary>
        /// Verify that ServicSettings will accept unknown Windows Azure RDFE location.
        /// </summary>
        [TestMethod]
        public void GetDefaultLocationWithUnknwonLocation()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                string serviceName = null;
                string unknownLocation = "Unknown Location";

                ServiceSettings settings = ServiceSettings.LoadDefault(null, null, unknownLocation, null, null, null, "My-Custom-Service!", null, out serviceName);
                Assert.AreEqual<string>(unknownLocation.ToLower(), settings.Location.ToLower());

            }
        }
    }
}