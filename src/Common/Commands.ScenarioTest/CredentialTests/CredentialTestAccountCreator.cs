using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Graph.RBAC;
using Microsoft.Azure.Graph.RBAC.Models;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Utilities.HttpRecorder;
using Microsoft.WindowsAzure.Testing;

namespace Microsoft.WindowsAzure.Commands.ScenarioTest.CredentialTests
{
    public class CredentialTestAccountCreator : IDisposable
    {
        // Keys used for storing information in the mock recording.
        private const string orgIdOneSubscriptionKey = "OrgIdOneSubscription";

        private readonly TestEnvironment testEnvironment;
        private string tenantId;
        private bool disposed;

        private CreatedUserInfo userToDelete;
        private RoleAssignment roleAssignmentToDelete;

        public CredentialTestAccountCreator()
        {
            var testFactory = new CSMTestEnvironmentFactory();
            testEnvironment = testFactory.GetTestEnvironment();

            using (UndoContext ctx = UndoContext.Current)
            {
                ctx.Start("CredentialTestAccountCreator", "setup");

                if (HttpMockServer.Mode != HttpRecorderMode.Playback)
                {
                    userToDelete = CreateDomainUser();
                    roleAssignmentToDelete = AddUserAsSubscriptionOwner(userToDelete);

                    if (HttpMockServer.Mode == HttpRecorderMode.Record)
                    {
                        HttpMockServer.Variables[orgIdOneSubscriptionKey] = userToDelete.ConnectionString;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (disposed) { return; }
            using (UndoContext.Current)
            {
                if (HttpMockServer.Mode != HttpRecorderMode.Playback)
                {
                    if (roleAssignmentToDelete != null)
                    {
                        var authClient = CreateAuthClient();
                        authClient.RoleAssignments.DeleteById(roleAssignmentToDelete.Id);
                    }

                    if (userToDelete != null)
                    {
                        var graphClient = CreateGraphClient();
                        graphClient.User.Delete(userToDelete.ObjectId);
                    }
                }
            }
            disposed = true;
        }

        private string GetCurrentDomain()
        {
            return testEnvironment.AuthorizationContext.UserId
                .Split(new[] { "@" }, StringSplitOptions.RemoveEmptyEntries)
                .Last();
            
        }
        // Create a non-mocked graph client - this is only used during setup during
        // recording. We do not want to record these calls, as we don't need to do
        // any of this during playback.
        private GraphRbacManagementClient CreateGraphClient()
        {
            tenantId = testEnvironment.AuthorizationContext.TenatId;

            return new GraphRbacManagementClient(tenantId, testEnvironment.Credentials as SubscriptionCloudCredentials, testEnvironment.GraphUri);
        }

        // Create a non-mocked Authorization client - this is only used during setup during
        // recording. We do not want to record these calls, as we don't need to do
        // any of this during playback.
        private AuthorizationManagementClient CreateAuthClient()
        {
            return new AuthorizationManagementClient(testEnvironment.Credentials as SubscriptionCloudCredentials,
                testEnvironment.BaseUri);
        }

        private CreatedUserInfo CreateDomainUser()
        {
            string username = string.Format("CredTestUser-{0}", Guid.NewGuid());
            string upn = string.Format("{0}@{1}", username, GetCurrentDomain());

            var parameters = new UserCreateParameters
            {
                UserPrincipalName = upn,
                DisplayName = username,
                AccountEnabled = true,
                MailNickname = username + "test",
                PasswordProfileSettings = new UserCreateParameters.PasswordProfile
                {
                    ForceChangePasswordNextLogin = false,
                    Password = "NotSoSecret!"
                }
            };

            var graphClient = CreateGraphClient();
            var response = graphClient.User.Create(parameters);
            return new CreatedUserInfo(response.User, parameters.PasswordProfileSettings.Password, testEnvironment,
                testEnvironment.SubscriptionId);
        }

        private RoleAssignment AddUserAsSubscriptionOwner(CreatedUserInfo user)
        {
            var authClient = CreateAuthClient();

            var ownerRole = authClient.RoleDefinitions.List().RoleDefinitions.First(d => d.Properties.RoleName == "Owner");

            var scope = string.Format("subscriptions/{0}", testEnvironment.SubscriptionId);
            var assignmentId = string.Format(
                "{0}/providers/Microsoft.Authorization/roleAssignments/{1}",
                scope,
                Guid.NewGuid());

            var createAssignmentParameters = new RoleAssignmentCreateParameters
            {
                RoleDefinitionId = ownerRole.Id,
                PrincipalId = Guid.Parse(user.ObjectId)
            };

            return authClient.RoleAssignments.CreateById(assignmentId, createAssignmentParameters).RoleAssignment;
        }

        private class CreatedUserInfo
        {
            public string UserName { get; set; }
            public string ObjectId { get; set; }
            public string Password { get; set; }
            public TestEnvironment Environment { get; set; }
            public string ExpectedSubscriptionId { get; set; }
            public string TenantId { get; set; }

            public CreatedUserInfo()
            {
                
            }

            public CreatedUserInfo(User user, string password, 
                TestEnvironment environment, 
                string expectedSubscriptionId, string tenantId = null)
            {
                UserName = user.SignInName;
                ObjectId = user.ObjectId;
                Password = password;
                Environment = environment;
                ExpectedSubscriptionId = expectedSubscriptionId;
                TenantId = tenantId;
            }

            public string ConnectionString
            {
                get
                {
                    var parts = new List<string>
                    {
                        AddPart(ConnectionStringFields.UserId, UserName),
                        AddPart(ConnectionStringFields.Password, Password),
                        AddPart(ConnectionStringFields.SubscriptionId, ExpectedSubscriptionId),
                        AddPart(ConnectionStringFields.AADTenant, TenantId),
                        AddPart(ConnectionStringFields.BaseUri, Environment.BaseUri),
                        AddPart(ConnectionStringFields.GraphUri, Environment.GraphUri),
                        AddPart(ConnectionStringFields.AADAuthenticationEndpoint, Environment.ActiveDirectoryEndpoint)
                    }.Where(p => p != null).ToArray();
                    return string.Join(";", parts);
                }
            }

            private static string AddPart(string key, object value)
            {
                if (value != null)
                {
                    return string.Format("{0}={1}", key, value);
                }
                return null;
            }
        }
    }
}
