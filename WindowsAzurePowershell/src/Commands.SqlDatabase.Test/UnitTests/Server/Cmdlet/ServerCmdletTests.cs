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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Server.Cmdlet
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using System.ServiceModel;
    using System.Xml;
    using Commands.Utilities.Common;
    using Database.Cmdlet;
    using Commands.Test.Utilities.Common;
    using MockServer;
    using Services;
    using Services.Server;
    using SqlDatabase.Server.Cmdlet;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ServerCmdletTests : TestBase
    {
        [TestMethod]
        public void NewAzureSqlDatabaseServerProcessTest()
        {
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            channel.NewServerThunk = ar =>
            {
                string newServerName = "NewServerName";

                Assert.AreEqual("MyLogin", ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLogin);
                Assert.AreEqual("MyPassword", ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLoginPassword);
                Assert.AreEqual("MyLocation", ((NewSqlDatabaseServerInput)ar.Values["input"]).Location);

                XmlElement operationResult = new XmlDocument().CreateElement("ServerName", "http://schemas.microsoft.com/sqlazure/2010/12/");
                operationResult.InnerText = newServerName;
                return operationResult;
            };

            NewAzureSqlDatabaseServer newAzureSqlDatabaseServer = new NewAzureSqlDatabaseServer(channel) { ShareChannel = true };
            newAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var newServerResult = newAzureSqlDatabaseServer.NewAzureSqlDatabaseServerProcess("MyLogin", "MyPassword", "MyLocation");
            Assert.AreEqual("NewServerName", newServerResult.ServerName);
            Assert.AreEqual("Success", newServerResult.OperationStatus);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }

        [TestMethod]
        public void GetAzureSqlDatabaseServerProcessTest()
        {
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            SqlDatabaseServerList serverList = new SqlDatabaseServerList();

            channel.NewServerThunk = ar =>
            {
                string newServerName = "TestServer" + serverList.Count.ToString();
                serverList.Add(new SqlDatabaseServer()
                {
                    Name = newServerName,
                    AdministratorLogin = ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLogin,
                    Location = ((NewSqlDatabaseServerInput)ar.Values["input"]).Location
                });

                XmlElement operationResult = new XmlDocument().CreateElement("ServerName", "http://schemas.microsoft.com/sqlazure/2010/12/");
                operationResult.InnerText = newServerName;
                return operationResult;
            };

            channel.GetServersThunk = ar =>
            {
                return serverList;
            };

            // Add two servers
            NewAzureSqlDatabaseServer newAzureSqlDatabaseServer = new NewAzureSqlDatabaseServer(channel) { ShareChannel = true };
            newAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var newServerResult = newAzureSqlDatabaseServer.NewAzureSqlDatabaseServerProcess("MyLogin0", "MyPassword0", "MyLocation0");
            Assert.AreEqual("TestServer0", newServerResult.ServerName);
            Assert.AreEqual("Success", newServerResult.OperationStatus);

            newServerResult = newAzureSqlDatabaseServer.NewAzureSqlDatabaseServerProcess("MyLogin1", "MyPassword1", "MyLocation1");
            Assert.AreEqual("TestServer1", newServerResult.ServerName);
            Assert.AreEqual("Success", newServerResult.OperationStatus);

            // Get all servers
            GetAzureSqlDatabaseServer getAzureSqlDatabaseServer = new GetAzureSqlDatabaseServer(channel) { ShareChannel = true };
            getAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            getAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var getServerResult = getAzureSqlDatabaseServer.GetAzureSqlDatabaseServersProcess(null);
            Assert.AreEqual(2, getServerResult.Count());
            var firstServer = getServerResult.First();
            Assert.AreEqual("TestServer0", firstServer.ServerName);
            Assert.AreEqual("MyLogin0", firstServer.AdministratorLogin);
            Assert.AreEqual("MyLocation0", firstServer.Location);
            Assert.AreEqual("Success", firstServer.OperationStatus);
            var lastServer = getServerResult.Last();
            Assert.AreEqual("TestServer1", lastServer.ServerName);
            Assert.AreEqual("MyLogin1", lastServer.AdministratorLogin);
            Assert.AreEqual("MyLocation1", lastServer.Location);
            Assert.AreEqual("Success", lastServer.OperationStatus);

            // Get one server
            getServerResult = getAzureSqlDatabaseServer.GetAzureSqlDatabaseServersProcess("TestServer1");
            Assert.AreEqual(1, getServerResult.Count());
            firstServer = getServerResult.First();
            Assert.AreEqual("TestServer1", firstServer.ServerName);
            Assert.AreEqual("MyLogin1", firstServer.AdministratorLogin);
            Assert.AreEqual("MyLocation1", firstServer.Location);
            Assert.AreEqual("Success", firstServer.OperationStatus);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }

        [TestMethod]
        public void RemoveAzureSqlDatabaseServerProcessTest()
        {
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            SqlDatabaseServerList serverList = new SqlDatabaseServerList();

            channel.NewServerThunk = ar =>
            {
                string newServerName = "TestServer" + serverList.Count.ToString();
                serverList.Add(new SqlDatabaseServer()
                {
                    Name = newServerName,
                    AdministratorLogin = ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLogin,
                    Location = ((NewSqlDatabaseServerInput)ar.Values["input"]).Location
                });

                XmlElement operationResult = new XmlDocument().CreateElement("ServerName", "http://schemas.microsoft.com/sqlazure/2010/12/");
                operationResult.InnerText = newServerName;
                return operationResult;
            };

            channel.GetServersThunk = ar =>
            {
                return serverList;
            };

            channel.RemoveServerThunk = ar =>
            {
                string serverName = (string)ar.Values["serverName"];
                var serverToDelete = serverList.SingleOrDefault((server) => server.Name == serverName);
                if (serverToDelete == null)
                {
                    throw new CommunicationException("Server does not exist!");
                }

                serverList.Remove(serverToDelete);
            };

            // Add two servers
            NewAzureSqlDatabaseServer newAzureSqlDatabaseServer = new NewAzureSqlDatabaseServer(channel) { ShareChannel = true };
            newAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var newServerResult = newAzureSqlDatabaseServer.NewAzureSqlDatabaseServerProcess("MyLogin0", "MyPassword0", "MyLocation0");
            Assert.AreEqual("TestServer0", newServerResult.ServerName);
            Assert.AreEqual("Success", newServerResult.OperationStatus);

            newServerResult = newAzureSqlDatabaseServer.NewAzureSqlDatabaseServerProcess("MyLogin1", "MyPassword1", "MyLocation1");
            Assert.AreEqual("TestServer1", newServerResult.ServerName);
            Assert.AreEqual("Success", newServerResult.OperationStatus);

            // Get all servers
            GetAzureSqlDatabaseServer getAzureSqlDatabaseServer = new GetAzureSqlDatabaseServer(channel) { ShareChannel = true };
            getAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            getAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var getServerContext = getAzureSqlDatabaseServer.GetAzureSqlDatabaseServersProcess(null);
            Assert.AreEqual(2, getServerContext.Count());

            // Remove TestServer0
            RemoveAzureSqlDatabaseServer removeAzureSqlDatabaseServer = new RemoveAzureSqlDatabaseServer(channel) { ShareChannel = true };
            removeAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            removeAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var removeServerContext = removeAzureSqlDatabaseServer.RemoveAzureSqlDatabaseServerProcess("TestServer0");

            // Verify only one server is left
            getAzureSqlDatabaseServer = new GetAzureSqlDatabaseServer(channel) { ShareChannel = true };
            getAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            getAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var getServerResult = getAzureSqlDatabaseServer.GetAzureSqlDatabaseServersProcess(null);
            Assert.AreEqual(1, getServerContext.Count());
            var firstServer = getServerResult.First();
            Assert.AreEqual("TestServer1", firstServer.ServerName);
            Assert.AreEqual("MyLogin1", firstServer.AdministratorLogin);
            Assert.AreEqual("MyLocation1", firstServer.Location);
            Assert.AreEqual("Success", firstServer.OperationStatus);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);

            // Remove TestServer0 again
            removeAzureSqlDatabaseServer = new RemoveAzureSqlDatabaseServer(channel) { ShareChannel = true };
            removeAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            removeAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            removeServerContext = removeAzureSqlDatabaseServer.RemoveAzureSqlDatabaseServerProcess("TestServer0");
            Assert.AreEqual(1, commandRuntime.ErrorStream.Count);
            Assert.IsTrue(commandRuntime.WarningStream.Count > 0);
        }

        [TestMethod]
        public void SetAzureSqlDatabaseServerProcessTest()
        {
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();

            string password = null;

            channel.NewServerThunk = ar =>
            {
                string newServerName = "NewServerName";

                Assert.AreEqual("MyLogin", ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLogin);
                Assert.AreEqual("MyPassword", ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLoginPassword);
                Assert.AreEqual("MyLocation", ((NewSqlDatabaseServerInput)ar.Values["input"]).Location);
                password = ((NewSqlDatabaseServerInput)ar.Values["input"]).AdministratorLoginPassword;

                XmlElement operationResult = new XmlDocument().CreateElement("ServerName", "http://schemas.microsoft.com/sqlazure/2010/12/");
                operationResult.InnerText = newServerName;
                return operationResult;
            };

            channel.SetPasswordThunk = ar =>
            {
                Assert.AreEqual("NewServerName", (string)ar.Values["serverName"]);
                var passwordElement = (XmlElement)ar.Values["password"];
                Assert.AreEqual("AdministratorLoginPassword", passwordElement.Name);
                password = passwordElement.InnerText;
            };

            NewAzureSqlDatabaseServer newAzureSqlDatabaseServer = new NewAzureSqlDatabaseServer(channel) { ShareChannel = true };
            newAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var newServerResult = newAzureSqlDatabaseServer.NewAzureSqlDatabaseServerProcess("MyLogin", "MyPassword", "MyLocation");
            Assert.AreEqual("NewServerName", newServerResult.ServerName);
            Assert.AreEqual("Success", newServerResult.OperationStatus);

            SetAzureSqlDatabaseServer setAzureSqlDatabaseServer = new SetAzureSqlDatabaseServer(channel) { ShareChannel = true };
            setAzureSqlDatabaseServer.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            setAzureSqlDatabaseServer.CommandRuntime = commandRuntime;
            var setPasswordResult = setAzureSqlDatabaseServer.ResetAzureSqlDatabaseServerAdminPasswordProcess("NewServerName", "NewPassword");
            Assert.AreEqual("NewServerName", setPasswordResult.ServerName);
            Assert.AreEqual("Success", setPasswordResult.OperationStatus);
            Assert.AreEqual("NewPassword", password);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }

        
        [TestMethod]
        public void GetAzureSqlDatabaseServerQuotaSqlAuthTest()
        {
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                // Create a context
                NewAzureSqlDatabaseServerContextTests.CreateServerContextSqlAuth(
                    powershell,
                    "$context");

                // Issue another create testdb1, causing a failure
                HttpSession testSession = DatabaseTestHelper.DefaultSessionCollection.GetSession(
                    "UnitTest.GetAzureSqlDatabaseServerQuotaSqlAuthTest");

                DatabaseTestHelper.SetDefaultTestSessionSettings(testSession);

                testSession.RequestValidator =
                    new Action<HttpMessage, HttpMessage.Request>(
                    (expected, actual) =>
                    {
                        Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                        Assert.AreEqual(expected.RequestInfo.UserAgent, actual.UserAgent);
                        switch (expected.Index)
                        {
                            // Request 0-1: Create testdb1
                            case 0:
                            case 1:
                                DatabaseTestHelper.ValidateHeadersForODataRequest(
                                    expected.RequestInfo,
                                    actual);
                                break;
                            default:
                                Assert.Fail("No more requests expected.");
                                break;
                        }
                    });

                using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
                {
                    Services.Server.ServerDataServiceSqlAuth context;
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        Collection<PSObject> ctxPsObject = powershell.InvokeBatchScript("$context");

                        context =
                            (Services.Server.ServerDataServiceSqlAuth)ctxPsObject.First().BaseObject;

                        Collection<PSObject> q1, q2;
                        q1 = powershell.InvokeBatchScript(
                            @"$context | Get-AzureSqlDatabaseServerQuota");

                        q2 = powershell.InvokeBatchScript(
                            @"$context | Get-AzureSqlDatabaseServerQuota -QuotaName ""Premium_Databases""");

                        ServerQuota quota1 = q1.FirstOrDefault().BaseObject as ServerQuota;
                        ServerQuota quota2 = q2.FirstOrDefault().BaseObject as ServerQuota;

                        Assert.AreEqual(
                            "premium_databases",
                            quota1.Name,
                            "Unexpected quota name");
                        Assert.AreEqual(
                            "premium_databases",
                            quota2.Name,
                            "Unexpected quota name");
                    }
                }

                Assert.AreEqual(0, powershell.Streams.Error.Count, "There were errors while running the tests!");
                Assert.AreEqual(0, powershell.Streams.Warning.Count, "There were warnings while running the tests!");
            }
        }
    }
}
