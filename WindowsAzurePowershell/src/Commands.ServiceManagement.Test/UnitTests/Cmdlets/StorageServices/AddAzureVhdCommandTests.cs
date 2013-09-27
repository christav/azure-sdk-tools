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
    using System;
    using System.Reflection;
    using Commands.Utilities.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Commands.ServiceManagement.StorageServices;
    using Commands.Test.Utilities.Common;

    [TestClass]
    public class AddAzureVhdCommandTests : TestBase
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

        public class TestInputParameters
        {
            public string Description { get; set; }
            public Uri Destination { get; set; }
            public Uri BaseImage { get; set; }
            public bool ExpectedResult { get; set; }
        }

        public void ExecuteTestWithInputParameters(TestInputParameters input)
        {
            var command = new AddAzureVhdCommand
            {
                Destination = input.Destination, 
                BaseImageUriToPatch = input.BaseImage
            };
            try
            {
                command.ValidateParameters();
                if(!input.ExpectedResult)
                {
                    Assert.Fail(input.Description);
                }
            }
            catch (Exception)
            {
                if(input.ExpectedResult)
                {
                    Assert.Fail(input.Description);
                }
            }
        }

        [TestMethod]
        public void EmptyDestinationUriWithEmptyBaseImage()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = null,
                BaseImage = null,
                ExpectedResult = false
            });
        }

        [TestMethod]
        public void InvalidDestinationUriWithEmptyBaseImage()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = new Uri("http://localhost"),
                BaseImage = null,
                ExpectedResult = false
            });
        }

        [TestMethod]
        public void EmptyDestinationUriWithInvalidBaseImage()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = null,
                BaseImage = new Uri("http://localhost"),
                ExpectedResult = false
            });
        }

        [TestMethod]
        public void ValidDestinationUriWithEmptyBaseImage()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob"),
                BaseImage = null,
                ExpectedResult = true
            });
        }

        [TestMethod]
        public void ValidDestinationUriWithInvalidBaseImage()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob"),
                BaseImage = new Uri("http://localhost"),
                ExpectedResult = false
            });
        }

        [TestMethod]
        public void EmptyDestinationUriWithValidBaseImageUri()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = null,
                BaseImage = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob"),
                ExpectedResult = false
            });
        }

        [TestMethod]
        public void ValidDestinationUriWithValidBaseImage()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob"),
                BaseImage = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob"),
                ExpectedResult = true
            });
        }

        [TestMethod]
        public void SasUriInDestinationNotSupportedInPatchMode()
        {
            ExecuteTestWithInputParameters(new TestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                Destination = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob?st=2013-01-09T22%3A15%3A49Z&se=2013-01-09T23%3A10%3A49Z&sr=b&sp=w&sig=13T9Ow%2FRJAMmhfO%2FaP3HhKKJ6AY093SmveOSIV4%2FR7w%3D"),
                BaseImage = new Uri("http://myaccount.blob.core.windows.net/mycontainer/myblob"),
                ExpectedResult = false
            });
        }
    }
}