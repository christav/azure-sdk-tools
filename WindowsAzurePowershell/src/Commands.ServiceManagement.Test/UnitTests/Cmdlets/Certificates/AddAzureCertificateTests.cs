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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.Test.UnitTests.Cmdlets.Certificates
{
    using System.Management.Automation;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Commands.Utilities.Common;
    using Commands.Test.Utilities.CloudService;
    using Commands.Test.Utilities.Common;
    using Commands.ServiceManagement.Certificates;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AddAzureCertificateTests : TestBase
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
        public void AddAzureCertificateTest()
        {
            // Setup
            bool created = false;
            SimpleServiceManagement channel = new SimpleServiceManagement();
            channel.AddCertificatesThunk = ar =>
            {
                created = true;
            };

            // Test
            AddAzureCertificate addAzureCertificate = new AddAzureCertificate(channel)
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime()
            };

            const string certificateData = "MIIC7TCCAdmgAwIBAgIQbT6PtZgKY6hE3eW/9rU3LTAJBgUrDgMCHQUAMBIxEDAOBgNVBAMTB2RlcGxveTMwHhcNMTExMTAxMTk0MDAyWhcNMzkxMjMxMjM1OTU5WjASMRAwDgYDVQQDEwdkZXBsb3kzMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxCOGSNPr/okaNeSMaL8faNNTsIc0rNDH+dOLYN45A8xPB8hcRe0EdpyXDN8L5cgtW9/nkuU6Ra27oDD+s4xdbkmqlurjpO0QoSAo7+ifcZUJ8ZUpGf9qYpnFkSgViO6uRZOW6c+HAOfRDSavRvqcoEKtDGScPcx74sFC0In12AiznZgThBmO6PUveSvKzbvdeDHUPeDwcnPo/UeOR6cf5Go1yiD3QSm63JMB1SG4uBlVEdiV3dFD6lYsJP4A+8phAxlSvfK2tNgkfdEC0VX2FAP8G6hRKI9s0JTdWDdFAYn2WCYqswsqmmyOYfaXLTtk4aseoRYPeIE6yTYkIFuDIwIDAQABo0cwRTBDBgNVHQEEPDA6gBCtYSe/k41tbXAWrmFYlPaVoRQwEjEQMA4GA1UEAxMHZGVwbG95M4IQbT6PtZgKY6hE3eW/9rU3LTAJBgUrDgMCHQUAA4IBAQAFkmz/ALFf1FpVKQzT1zRKh8aNQlfKksfPFuXcfqOcYEW1HW5ll3yeSvM5PmPtRGIBYa5HCdDrq2GsrdmcpcZpwS/NKO4WX0FmX9raZ+EjR71BwpyW17qjHC2hPA21kry1wpWFr9vgaRpp8OIKIXvXUMCBTuXNa3wp9KdopLSYVegJo9iLsL94UVXkHqmysOelwasA1gUuUUjpAPlmxtco1jFdEdwCVjSBEshvdrxcFXGTQ0FRGGKD94kwkGNq48kgqNqLB7wRxSt7LMiBVRXdhITjNdO3aRryrKHUFr2lfMyDh0jsv6H9MDCvrjD46BJUptEnzNvMdd0PQ3Dl9w56";
            byte[] rawData = new UTF8Encoding().GetBytes(certificateData);
            X509Certificate2 certificate = new X509Certificate2(rawData);
            addAzureCertificate.CertToDeploy = new PSObject(certificate);
            addAzureCertificate.ExecuteCommand();
            Assert.IsTrue(created);
        }
    }
}