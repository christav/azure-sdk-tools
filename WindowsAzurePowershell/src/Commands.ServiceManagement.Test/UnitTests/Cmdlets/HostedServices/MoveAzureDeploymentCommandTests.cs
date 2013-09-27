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
    using System.Net;
    using System.Reflection;
    using Commands.Utilities.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Commands.ServiceManagement.HostedServices;
    using Commands.Test.Utilities.CloudService;
    using Commands.Test.Utilities.Common;
    using Microsoft.WindowsAzure.ServiceManagement;

    [TestClass]
    public class MoveAzureDeploymentCommandTests : TestBase
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

        public class MoveAzureDeploymentTestInputParameters
        {
            public string Description { get; set; }
            public bool ProductionExists { get; set; }
            public bool StagingExists { get; set; }
            public bool ThrowsException { get; set; }
            public Type ExceptionType { get; set; }
            public Deployment ProductionDeployment { get; set; }
            public Deployment StagingDeployment { get; set; }
        }

        public void ExecuteTestCase(MoveAzureDeploymentTestInputParameters parameters)
        {
            var channel = new SimpleServiceManagement
            {
                GetDeploymentBySlotThunk = ar =>
                {
                    if (ar.Values["deploymentSlot"].ToString() == DeploymentSlotType.Production)
                    {
                        if (parameters.ProductionDeployment == null)
                        {
                            throw new ServiceManagementClientException(HttpStatusCode.NotFound, new ServiceManagementError(), String.Empty);
                        }
                        return parameters.ProductionDeployment;
                    }
                    if (ar.Values["deploymentSlot"].ToString() == DeploymentSlotType.Staging)
                    {
                        if (parameters.StagingDeployment == null)
                        {
                            throw new ServiceManagementClientException(HttpStatusCode.NotFound, new ServiceManagementError(), String.Empty);
                        }
                        return parameters.StagingDeployment;
                    }

                    return null;
                },
                SwapDeploymentThunk = ar =>
                {
                    var input = (SwapDeploymentInput)ar.Values["input"];

                    if (input.Production == null && parameters.ProductionDeployment == null)
                    {
                        if (input.SourceDeployment != parameters.StagingDeployment.Name)
                        {
                            Assert.Fail("Expected values Staging/Prod'{0},{1}', found '{2},{3}'",
                                parameters.StagingDeployment.Name, null, input.SourceDeployment, null);
                        }
                    }
                    else if (input.Production != parameters.ProductionDeployment.Name || input.SourceDeployment != parameters.StagingDeployment.Name)
                    {
                        Assert.Fail("Expected values Staging/Prod'{0},{1}', found '{2},{3}'",
                            parameters.StagingDeployment.Name, parameters.ProductionDeployment.Name, input.SourceDeployment, input.Production);
                    }
                }
            };

            // Test
            var moveAzureDeployment = new MoveAzureDeploymentCommand(channel)
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime(),
                ServiceName = "testService",
                CurrentSubscription = new WindowsAzureSubscription
                {
                    SubscriptionId = "testId"
                }
            };

            try
            {
                moveAzureDeployment.ExecuteCommand();
                if(parameters.ThrowsException)
                {
                    Assert.Fail(parameters.Description);       
                }
            }
            catch (Exception e)
            {
                if(e.GetType() != parameters.ExceptionType)
                {
                    Assert.Fail("Expected exception type is {0}, however found {1}", parameters.ExceptionType, e.GetType());
                }
                if(!parameters.ThrowsException)
                {
                    Assert.Fail("{0} fails unexpectedly: {1}", parameters.Description, e);
                }
            }
        }

        [TestMethod]
        public void NoProductionAndNoStagingDeployment()
        {
            ExecuteTestCase(new MoveAzureDeploymentTestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                ThrowsException = true,
                ExceptionType = typeof(ArgumentOutOfRangeException),
                ProductionDeployment = null,
                StagingDeployment = null,
            });
        }

        [TestMethod]
        public void ProductionExistsWithNoStagingDeployment()
        {
            ExecuteTestCase(new MoveAzureDeploymentTestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                ThrowsException = true,
                ExceptionType = typeof(ArgumentOutOfRangeException),
                ProductionDeployment = new Deployment
                {
                    Name = "productionDeployment",
                    RoleList = new RoleList
                    {
                        new Role
                        {
                            RoleType = String.Empty
                        }
                    }
                },
                StagingDeployment = null,
            });
        }

        [TestMethod]
        public void ProductionExistsWithPersistenVMRoleNoStagingDeployment()
        {
            ExecuteTestCase(new MoveAzureDeploymentTestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                ThrowsException = true,
                ExceptionType = typeof(ArgumentException),
                ProductionDeployment = new Deployment
                {
                    Name = "productionDeployment",
                    RoleList = new RoleList
                    {
                        new Role
                        {
                            RoleType = "PersistentVMRole"
                        }
                    }
                },
                StagingDeployment = null,
            });
        }

        [TestMethod]
        public void NoProductionWithStagingDeploymentWithPersistenVMRole()
        {
            ExecuteTestCase(new MoveAzureDeploymentTestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                ThrowsException = true,
                ExceptionType = typeof(ArgumentException),
                ProductionDeployment = null,
                StagingDeployment = new Deployment
                {
                    Name = "stagingDeployment",
                    RoleList = new RoleList
                    {
                        new Role
                        {
                            RoleType = "PersistentVMRole"
                        }
                    }
                },
            });
        }

        [TestMethod]
        public void NoProductionWithStagingDeployment()
        {
            ExecuteTestCase(new MoveAzureDeploymentTestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                ThrowsException = false,
                ExceptionType = null,
                ProductionDeployment = null,
                StagingDeployment = new Deployment
                {
                    Name = "stagingDeployment",
                    RoleList = new RoleList
                    {
                        new Role
                        {
                            RoleType = String.Empty
                        }
                    }
                },
            });
        }

        [TestMethod]
        public void ProductionDeploymentExistsWithStagingDeployment()
        {
            ExecuteTestCase(new MoveAzureDeploymentTestInputParameters
            {
                Description = MethodBase.GetCurrentMethod().Name,
                ThrowsException = false,
                ExceptionType = null,
                ProductionDeployment = new Deployment
                {
                    Name = "prodDeployment",
                    RoleList = new RoleList
                    {
                        new Role
                        {
                            RoleType = String.Empty
                        }
                    }
                },
                StagingDeployment = new Deployment
                {
                    Name = "stagingDeployment",
                    RoleList = new RoleList
                    {
                        new Role
                        {
                            RoleType = String.Empty
                        }
                    }
                },
            });
        }
   }
}