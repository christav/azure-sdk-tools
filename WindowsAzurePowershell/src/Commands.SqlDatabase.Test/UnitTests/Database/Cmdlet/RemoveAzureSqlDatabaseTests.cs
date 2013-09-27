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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Database.Cmdlet
{
    using System;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Commands.Test.Utilities.Common;
    using MockServer;
    using Services;
    using Services.Server;
    using SqlDatabase.Database.Cmdlet;

    [TestClass]
    public class RemoveAzureSqlDatabaseTests : TestBase
    {
        [TestCleanup]
        public void CleanupTest()
        {
            DatabaseTestHelper.SaveDefaultSessionCollection();
        }

        [TestMethod]
        public void RemoveAzureSqlDatabaseWithSqlAuth()
        {
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                // Create a context
                NewAzureSqlDatabaseServerContextTests.CreateServerContextSqlAuth(
                    powershell,
                    "$context");

                // Create 2 test databases
                NewAzureSqlDatabaseTests.CreateTestDatabasesWithSqlAuth(
                    powershell,
                    "$context");

                HttpSession testSession = DatabaseTestHelper.DefaultSessionCollection.GetSession(
                    "UnitTests.RemoveAzureSqlDatabaseWithSqlAuth");
                DatabaseTestHelper.SetDefaultTestSessionSettings(testSession);
                testSession.RequestValidator =
                    new Action<HttpMessage, HttpMessage.Request>(
                        (expected, actual) =>
                        {
                            Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                            Assert.AreEqual(expected.RequestInfo.UserAgent, actual.UserAgent);
                            if (expected.Index < 8)
                            {
                                // Request 0-5: Remove database requests
                                // Request 6-7: Get all database request
                                DatabaseTestHelper.ValidateHeadersForODataRequest(
                                    expected.RequestInfo,
                                    actual);
                            }
                            else
                            {
                                Assert.Fail("No more requests expected.");
                            }
                        });

                using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
                {
                    // Create context with both ManageUrl and ServerName overriden
                    Collection<PSObject> databases;
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        powershell.InvokeBatchScript(
                            @"Remove-AzureSqlDatabase " +
                            @"-Context $context " +
                            @"-DatabaseName testdb1 " +
                            @"-Force");
                        powershell.InvokeBatchScript(
                            @"Remove-AzureSqlDatabase " +
                            @"-Context $context " +
                            @"-DatabaseName testdb2 " +
                            @"-Force");

                        databases = powershell.InvokeBatchScript(
                            @"Get-AzureSqlDatabase " +
                            @"-Context $context");
                    }

                    Assert.AreEqual(0, powershell.Streams.Error.Count, "Errors during run!");
                    Assert.AreEqual(0, powershell.Streams.Warning.Count, "Warnings during run!");
                    powershell.Streams.ClearStreams();

                    Assert.AreEqual(1, databases.Count, "Expecting only master database object");
                }
            }
        }

        /// <summary>
        /// Test removing a database using certificate authentication
        /// </summary>
        [TestMethod]
        public void RemoveAzureSqlDatabaseWithCertAuth()
        {
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            
            // This is needed because RemoveDatabases calls GetDatabases in order to 
            // get the necessary database information for the delete.
            channel.GetDatabaseThunk = ar =>
            {
                Assert.AreEqual(
                    ar.Values["databaseName"], 
                    "testdb1", 
                    "The input databaseName did not match the expected");

                SqlDatabaseResponse db1 = new SqlDatabaseResponse();
                db1.CollationName = "Japanese_CI_AS";
                db1.Edition = "Web";
                db1.Id = "1";
                db1.MaxSizeGB = "1";
                db1.Name = "testdb1";
                db1.CreationDate = DateTime.Now.ToString();
                db1.IsFederationRoot = true.ToString();
                db1.IsSystemObject = true.ToString();
                db1.MaxSizeBytes = "1073741824";

                return db1;
            };

            channel.RemoveDatabaseThunk = ar =>
            {
                Assert.AreEqual(
                    ar.Values["databaseName"], 
                    "testdb1", 
                    "The input databaseName did not match the expected");

                Assert.AreEqual(
                    ((SqlDatabaseInput)ar.Values["input"]).Name, 
                    "testdb1",
                    "The database Name input parameter does not match");
                Assert.AreEqual(
                    ((SqlDatabaseInput)ar.Values["input"]).MaxSizeGB, 
                    "1",
                    "The database MaxSizeGB input parameter does not match");
                Assert.AreEqual(
                    ((SqlDatabaseInput)ar.Values["input"]).CollationName, 
                    "Japanese_CI_AS",
                    "The database CollationName input parameter does not match");
                Assert.AreEqual(
                    ((SqlDatabaseInput)ar.Values["input"]).Edition, 
                    "Web",
                    "The database Edition input parameter does not match");
            };

            WindowsAzureSubscription subscription = UnitTestHelper.CreateUnitTestSubscription();
            subscription.ServiceEndpoint = new Uri(MockHttpServer.DefaultHttpsServerPrefixUri.AbsoluteUri);

            NewAzureSqlDatabaseServerContext contextCmdlet = new NewAzureSqlDatabaseServerContext();

            ServerDataServiceCertAuth service = 
                contextCmdlet.GetServerDataServiceByCertAuth("TestServer", subscription);
            service.Channel = channel;

            service.RemoveDatabase("testdb1");
        }
    }
}
