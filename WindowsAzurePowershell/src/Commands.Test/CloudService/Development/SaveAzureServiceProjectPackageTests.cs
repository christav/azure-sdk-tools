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

namespace Microsoft.WindowsAzure.Commands.Test.CloudService.Development
{
    using Commands.CloudService.Development;
    using Commands.CloudService.Development.Scaffolding;
    using Commands.Utilities.CloudService;
    using Commands.Utilities.Common;
    using Commands.Utilities.Properties;
    using System;
    using System.IO;
    using System.Management.Automation;
    using Test.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Basic unit tests for the Enable-AzureServiceProjectRemoteDesktop enableRDCmdlet.
    /// </summary>
    [TestClass]
    public class SaveAzureServiceProjectPackageCommandTest : TestBase
    {
        static private MockCommandRuntime mockCommandRuntime;

        static private SaveAzureServiceProjectPackageCommand cmdlet;

        private AddAzureNodeWebRoleCommand addNodeWebCmdlet;

        private AddAzureNodeWorkerRoleCommand addNodeWorkerCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            mockCommandRuntime = new MockCommandRuntime();

            addNodeWebCmdlet = new AddAzureNodeWebRoleCommand();
            addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand();
            cmdlet = new SaveAzureServiceProjectPackageCommand();

            addNodeWorkerCmdlet.CommandRuntime = mockCommandRuntime;
            addNodeWebCmdlet.CommandRuntime = mockCommandRuntime;
            cmdlet.CommandRuntime = mockCommandRuntime;
        }

        [TestMethod]
        public void TestCreatePackageSuccessfull()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                files.CreateNewService("NEW_SERVICE");
                string rootPath = Path.Combine(files.RootPath, "NEW_SERVICE");
                string packagePath = Path.Combine(rootPath, Resources.CloudPackageFileName);

                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath);

                cmdlet.ExecuteCmdlet();

                PSObject obj = mockCommandRuntime.OutputPipeline[0] as PSObject;
                Assert.AreEqual<string>(string.Format(Resources.PackageCreated, packagePath), mockCommandRuntime.VerboseStream[0]);
                Assert.AreEqual<string>(packagePath, obj.GetVariableValue<string>(Parameters.PackagePath));
                Assert.IsTrue(File.Exists(packagePath));
            }
        }

        [TestMethod]
        public void TestCreatePackageWithEmptyServiceSuccessfull()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                files.CreateNewService("NEW_SERVICE");
                string rootPath = Path.Combine(files.RootPath, "NEW_SERVICE");
                string packagePath = Path.Combine(rootPath, Resources.CloudPackageFileName);

                cmdlet.ExecuteCmdlet();

                PSObject obj = mockCommandRuntime.OutputPipeline[0] as PSObject;
                Assert.AreEqual<string>(string.Format(Resources.PackageCreated, packagePath), mockCommandRuntime.VerboseStream[0]);
                Assert.AreEqual<string>(packagePath, obj.GetVariableValue<string>(Parameters.PackagePath));
                Assert.IsTrue(File.Exists(packagePath));
            }
        }

        [TestMethod]
        public void TestCreatePackageWithMultipleRolesSuccessfull()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                files.CreateNewService("NEW_SERVICE");
                string rootPath = Path.Combine(files.RootPath, "NEW_SERVICE");
                string packagePath = Path.Combine(rootPath, Resources.CloudPackageFileName);

                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath);
                service.AddWorkerRole(Data.NodeWorkerRoleScaffoldingPath);
                service.AddWorkerRole(Data.NodeWorkerRoleScaffoldingPath);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath);

                cmdlet.ExecuteCmdlet();

                PSObject obj = mockCommandRuntime.OutputPipeline[0] as PSObject;
                Assert.AreEqual<string>(string.Format(Resources.PackageCreated, packagePath), mockCommandRuntime.VerboseStream[0]);
                Assert.AreEqual<string>(packagePath, obj.GetVariableValue<string>(Parameters.PackagePath));
                Assert.IsTrue(File.Exists(packagePath));
            }
        }

        [TestMethod]
        public void ThrowsErrorForInvalidCacheVersion()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                files.CreateNewService("NEW_SERVICE");
                string rootPath = Path.Combine(files.RootPath, "NEW_SERVICE");
                string packagePath = Path.Combine(rootPath, Resources.CloudPackageFileName);
                string cacheRoleName = "WorkerRole1";
                AddAzureCacheWorkerRoleCommand addCacheWorkerCmdlet = new AddAzureCacheWorkerRoleCommand()
                {
                    CommandRuntime = mockCommandRuntime
                };
                EnableAzureMemcacheRoleCommand enableCacheCmdlet = new EnableAzureMemcacheRoleCommand()
                {
                    CacheRuntimeVersion = "1.8.0",
                    CommandRuntime = mockCommandRuntime
                };

                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath);
                addCacheWorkerCmdlet.AddAzureCacheWorkerRoleProcess(cacheRoleName, 1, rootPath);
                enableCacheCmdlet.EnableAzureMemcacheRoleProcess("WebRole1", cacheRoleName, rootPath);

                Testing.AssertThrows<Exception>(
                    () => cmdlet.ExecuteCmdlet(),
                    string.Format(Resources.CacheMismatchMessage, "WebRole1", "2.0.0"));
            }
        }
    }
}
