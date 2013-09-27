﻿// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Services.Server
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.WindowsAzure.Commands.Utilities.Common;

    /// <summary>
    /// Implementation of the <see cref="IServerDataServiceContext"/> with Certificate authentication.
    /// </summary>
    public partial class ServerDataServiceCertAuth : IServerDataServiceContext
    {
        #region Private Fields

        /// <summary>
        /// The previous request's client request ID
        /// </summary>
        private string clientRequestId;

        /// <summary>
        /// The name of the server we are connected to.
        /// </summary>
        private readonly string serverName;

        /// <summary>
        /// The REST Management API endpoint
        /// </summary>
        private readonly Uri serviceEndpoint;

        /// <summary>
        /// The ID of the subscription containing the server
        /// </summary>
        private readonly string subscriptionId;

        /// <summary>
        /// The certificate used to authenticate.
        /// </summary>
        private readonly X509Certificate2 certificate;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDataServicesCertAuth"/> class
        /// </summary>
        /// <param name="serviceEndpoint">The REST Management API endpoint</param>
        /// <param name="subscriptionId">The ID of the subscription containing the server</param>
        /// <param name="serverName">The name of the server to connect to</param>
        /// <param name="certificate">The certificate used to authenticate</param>
        private ServerDataServiceCertAuth(
            Uri serviceEndpoint,
            string subscriptionId,
            string serverName,
            X509Certificate2 certificate)
        {
            this.serviceEndpoint = serviceEndpoint;
            this.subscriptionId = subscriptionId;
            this.serverName = serverName;
            this.certificate = certificate;
        }

        #region Public Properties

        /// <summary>
        /// Gets the client per-session tracing ID.
        /// </summary>
        public string ClientSessionId
        {
            get
            {
                return SqlDatabaseManagementCmdletBase.clientSessionId;
            }
        }

        /// <summary>
        /// Gets the previous request's client request ID.
        /// </summary>
        public string ClientRequestId
        {
            get
            {
                return this.clientRequestId;
            }
        }

        /// <summary>
        /// Gets the name of the server for this context.
        /// </summary>
        public string ServerName
        {
            get
            {
                return this.serverName;
            }
        }

        /// <summary>
        /// Gets or sets the channel to use for communication
        /// </summary>
        public ISqlDatabaseManagement Channel { get; set; } 

        #endregion

        /// <summary>
        /// Creates and returns a new instance of the <see cref="ServerDataServiceCertAuth"/> class
        /// which connects to the specified server using the specified subscription credentials.
        /// </summary>
        /// <param name="serverName">The name of the server to connect to</param>
        /// <param name="subscription">The information used to connect and authenticate</param>
        /// <returns>An instance of <see cref="ServerDataServiceCertAuth"/> class.</returns>
        public static ServerDataServiceCertAuth Create(
            string serverName,
            WindowsAzureSubscription subscription)
        {
            if (string.IsNullOrEmpty(serverName))
            {
                throw new ArgumentException("serverName");
            }

            if (subscription == null)
            {
                throw new ArgumentNullException("subscription");
            }

            // Create a new ServerDataServiceCertAuth object to be used
            return new ServerDataServiceCertAuth(
                subscription.ServiceEndpoint,
                subscription.SubscriptionId,
                serverName,
                subscription.Certificate);
        }

        #region Helper functions

        /// <summary>
        /// Creates a channel for communication with the server.
        /// </summary>
        /// <returns>a new channel to use for communication</returns>
        private ISqlDatabaseManagement GetManagementChannel()
        {
            if (this.Channel == null)
            {
                //create a channel to the server for communication
                ISqlDatabaseManagement channel = SqlDatabaseManagementHelper.CreateSqlDatabaseManagementChannel(
                    ConfigurationConstants.WebHttpBinding(ConfigurationConstants.MaxStringContentLength),
                    this.serviceEndpoint,
                    this.certificate,
                    this.clientRequestId);
                this.Channel = channel;
            }
            return this.Channel;
        }

        /// <summary>
        /// Given a <see cref="SqlDatabaseResponse"/> this will create and return a <see cref="Database"/> 
        /// object with the fields filled in.
        /// </summary>
        /// <param name="response">The response to turn into a <see cref="Database"/></param>
        /// <returns>a <see cref="Database"/> object.</returns>
        private Database CreateDatabaseFromResponse(SqlDatabaseResponse response)
        {
            Database result = new Database()
            {
                CollationName = response.CollationName,
                CreationDate = DateTime.Parse(response.CreationDate, CultureInfo.InvariantCulture),
                Edition = response.Edition,
                Id = int.Parse(response.Id),
                IsFederationRoot = bool.Parse(response.IsFederationRoot),
                IsSystemObject = bool.Parse(response.IsSystemObject),
                MaxSizeGB = int.Parse(response.MaxSizeGB),
                MaxSizeBytes = long.Parse(response.MaxSizeBytes),
                Name = response.Name,
            };

            // Parse any additional database information
            if (!string.IsNullOrEmpty(response.SizeMB))
            {
                result.SizeMB = decimal.Parse(response.SizeMB, CultureInfo.InvariantCulture);
            }

            // Parse the service objective information
            if (!string.IsNullOrEmpty(response.ServiceObjectiveAssignmentErrorCode))
            {
                result.ServiceObjectiveAssignmentErrorCode = int.Parse(response.ServiceObjectiveAssignmentErrorCode);
            }
            if (!string.IsNullOrEmpty(response.ServiceObjectiveAssignmentErrorDescription))
            {
                result.ServiceObjectiveAssignmentErrorDescription = response.ServiceObjectiveAssignmentErrorDescription;
            }
            if (!string.IsNullOrEmpty(response.ServiceObjectiveAssignmentState))
            {
                result.ServiceObjectiveAssignmentState = byte.Parse(response.ServiceObjectiveAssignmentState);
            }
            if (!string.IsNullOrEmpty(response.ServiceObjectiveAssignmentStateDescription))
            {
                result.ServiceObjectiveAssignmentStateDescription = response.ServiceObjectiveAssignmentStateDescription;
            }
            if (!string.IsNullOrEmpty(response.ServiceObjectiveAssignmentSuccessDate))
            {
                result.ServiceObjectiveAssignmentSuccessDate = DateTime.Parse(response.ServiceObjectiveAssignmentSuccessDate, CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(response.ServiceObjectiveId))
            {
                result.ServiceObjectiveId = Guid.Parse(response.ServiceObjectiveId);
            }
            
            result.LoadExtraProperties(this);

            return result;
        }

        #endregion

        #region IServerDataServiceContext Members

        /// <summary>
        /// Gets a list of all the databases in the current context.
        /// </summary>
        /// <returns>An array of databases in the current context</returns>
        public Database[] GetDatabases()
        {
            this.clientRequestId = SqlDatabaseManagementHelper.GenerateClientTracingId();

            ISqlDatabaseManagement channel = GetManagementChannel();

            SqlDatabaseList databases = 
                channel.EndGetDatabases(
                    channel.BeginGetDatabases(this.subscriptionId, this.ServerName, null, null));

            List<Database> results = new List<Database>();
            foreach (var db in databases)
            {
                //Create the database from the response
                results.Add(CreateDatabaseFromResponse(db));
            }

            return results.ToArray();
        }

        /// <summary>
        /// Retrieve a specific database from the current context
        /// </summary>
        /// <param name="databaseName">The name of the database to retrieve</param>
        /// <returns>A database object</returns>
        public Database GetDatabase(string databaseName)
        {
            this.clientRequestId = SqlDatabaseManagementHelper.GenerateClientTracingId();

            //create a channel to the server for communication
            ISqlDatabaseManagement channel = GetManagementChannel();

            //query the server for the database
            SqlDatabaseResponse database = 
                channel.EndGetDatabase(
                    channel.BeginGetDatabase(this.subscriptionId, this.ServerName, databaseName, null, null));

            //Create the database from the response
            Database result = CreateDatabaseFromResponse(database);

            //return the database
            return result;
        }

        /// <summary>
        /// Creates a new sql database.
        /// </summary>
        /// <param name="databaseName">The name for the new database</param>
        /// <param name="databaseMaxSize">The maximum size of the new database</param>
        /// <param name="databaseCollation">The collation for the new database</param>
        /// <param name="databaseEdition">The edition for the new database</param>
        /// <returns>The newly created Sql Database</returns>
        public Database CreateNewDatabase(
            string databaseName,
            int? databaseMaxSize,
            string databaseCollation,
            DatabaseEdition databaseEdition)
        {
            this.clientRequestId = SqlDatabaseManagementHelper.GenerateClientTracingId();

            ISqlDatabaseManagement channel = GetManagementChannel();

            SqlDatabaseInput input = new SqlDatabaseInput();
            input.Name = databaseName;
            input.CollationName = databaseCollation ?? string.Empty;

            //determine the edition
            if (databaseEdition != DatabaseEdition.None)
            {
                input.Edition = databaseEdition.ToString();
            }
            else
            {
                input.Edition = string.Empty;
            }

            //determine the maximum size
            if (databaseMaxSize.HasValue)
            {
                input.MaxSizeGB = databaseMaxSize.ToString();
            }

            //create a new database on the server
            SqlDatabaseResponse response = 
                channel.EndNewDatabase(
                    channel.BeginNewDatabase(this.subscriptionId, this.serverName, input, null, null));

            Database database = CreateDatabaseFromResponse(response);

            return database;
        }

        /// <summary>
        /// Update a database on the server.
        /// </summary>
        /// <param name="databaseName">The name of the database to modify</param>
        /// <param name="newDatabaseName">The new name of the database</param>
        /// <param name="databaseMaxSize">The new maximum size of the database</param>
        /// <param name="databaseEdition">The new edition of the database</param>
        /// <returns>The updated database</returns>
        public Database UpdateDatabase(
            string databaseName,
            string newDatabaseName,
            int? databaseMaxSize,
            DatabaseEdition? databaseEdition, 
            ServiceObjective serviceObjective)
        {
            this.clientRequestId = SqlDatabaseManagementHelper.GenerateClientTracingId();

            ISqlDatabaseManagement channel = GetManagementChannel();

            Database database = this.GetDatabase(databaseName);

            //make sure the database exists.
            if (database == null)
            {
                throw new Exception(
                    "Error: Result of GetDatabase() in ServerDataServiceCertAuth.UpdateDatabase() is null");
            }

            SqlDatabaseInput input = new SqlDatabaseInput();

            //Set the database ID and collation
            input.Id = database.Id.ToString();
            input.CollationName = database.CollationName;

            if (serviceObjective != null)
            {
                input.ServiceObjectiveId = serviceObjective.Id.ToString();
            }

            //Determine what the new name for the database should be
            if (!string.IsNullOrEmpty(newDatabaseName))
            {
                input.Name = newDatabaseName;
            }
            else
            {
                input.Name = database.Name;
            }
            
            //Determine what the new edition for the database should be
            if (databaseEdition.HasValue && (databaseEdition != DatabaseEdition.None))
            {
                input.Edition = databaseEdition.ToString();
            }
            else
            {
                input.Edition = database.Edition;
            }

            //Determine what the new maximum size for the database should be.
            if (databaseMaxSize.HasValue)
            {
                input.MaxSizeGB = databaseMaxSize.ToString();
            }
            else
            {
                input.MaxSizeGB = database.MaxSizeGB.ToString();
            }

            //Send the update request and wait for the response.
            SqlDatabaseResponse response = channel.EndUpdateDatabase(
                channel.BeginUpdateDatabase(
                    this.subscriptionId, 
                    this.serverName, 
                    databaseName, 
                    input, 
                    null, 
                    null));

            //Transform the response into a database object.
            Database updatedDatabase = CreateDatabaseFromResponse(response);

            return updatedDatabase;
        }

        /// <summary>
        /// Remove a database from a server
        /// </summary>
        /// <param name="databaseName">The name of the database to delete</param>
        public void RemoveDatabase(string databaseName)
        {
            this.clientRequestId = SqlDatabaseManagementHelper.GenerateClientTracingId();

            ISqlDatabaseManagement channel = GetManagementChannel();

            Database database = this.GetDatabase(databaseName);

            SqlDatabaseInput input = new SqlDatabaseInput();
            input.Name = databaseName;
            input.Id = database.Id.ToString();
            input.CollationName = database.CollationName;
            input.Edition = database.Edition.ToString();
            input.MaxSizeGB = database.MaxSizeGB.ToString();

            channel.EndRemoveDatabase(
                channel.BeginRemoveDatabase(
                    this.subscriptionId, 
                    this.serverName, 
                    databaseName, 
                    input, 
                    null, 
                    null));
        }

        #endregion

        public void LoadProperty(object obj, string propertyName)
        {
            throw new NotImplementedException();
        }

        public ServerQuota GetQuota(string quotaName)
        {
            throw new NotImplementedException();
        }

        public ServerQuota[] GetQuotas()
        {
            throw new NotImplementedException();
        }

        public ServiceObjective[] GetServiceObjectives()
        {
            throw new NotImplementedException();
        }

        public ServiceObjective GetServiceObjective(string serviceObjectiveName)
        {
            throw new NotImplementedException();
        }
    }
}
