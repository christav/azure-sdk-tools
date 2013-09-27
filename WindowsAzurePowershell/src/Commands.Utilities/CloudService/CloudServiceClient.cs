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


namespace Microsoft.WindowsAzure.Commands.Utilities.CloudService
{
    using System.Net;
    using AzureTools;
    using Common;
    using Management;
    using Management.Compute;
    using Management.Compute.Models;
    using Management.Storage;
    using Management.Storage.Models;
    using Model;
    using Properties;
    using Storage;
    using Storage.Auth;
    using Storage.Blob;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using ConfigCertificate = Common.XmlSchema.ServiceConfigurationSchema.Certificate;
    using ConfigConfigurationSetting = Common.XmlSchema.ServiceConfigurationSchema.ConfigurationSetting;
    using DeploymentStatus = Model.DeploymentStatus;
    using RoleInstance = Model.RoleInstance;
    using RoleInstanceStatus = Management.Compute.Models.RoleInstanceStatus;

    public class CloudServiceClient : ICloudServiceClient
    {
        internal CloudBlobUtility CloudBlobUtility { get; set; }

        internal ManagementClient ManagementClient { get; set; }

        internal StorageManagementClient StorageClient { get; set; }

        internal ComputeManagementClient ComputeClient { get; set; }

        public WindowsAzureSubscription Subscription { get; set; }

        public Action<string> DebugStream { get; set; }

        public Action<string> VerboseStream { get; set; }

        public Action<string> WarningStream { get; set; }

        public string CurrentDirectory { get; set; }

        public const int SleepDuration = 1000;

        enum CloudServiceState
        {
            Start,
            Stop
        }

        private void VerifyDeploymentExists(HostedServiceGetDetailedResponse cloudService, DeploymentSlot slot)
        {
            bool exists = false;

            if (cloudService.Deployments != null)
            {
                exists = cloudService.Deployments.Any(d => d.DeploymentSlot == slot );
            }

            if (!exists)
            {
                throw new Exception(string.Format(Resources.CannotFindDeployment, cloudService.ServiceName, slot));
            }
            
        }

        private void SetCloudServiceState(string name, DeploymentSlot slot, CloudServiceState state)
        {
            HostedServiceGetDetailedResponse cloudService = GetCloudService(name);

            VerifyDeploymentExists(cloudService, slot);
            ComputeClient.Deployments.UpdateStatusByDeploymentSlot(cloudService.ServiceName,
                slot, new DeploymentUpdateStatusParameters
                {
                    Status =
                        state == CloudServiceState.Start
                            ? UpdatedDeploymentStatus.Running
                            : UpdatedDeploymentStatus.Suspended
                });
        }
        
        private void WriteToStream(Action<string> stream, string format, params object[] args)
        {
            if (stream != null)
            {
                stream(string.Format(format, args));
            }
        }

        private void WriteWarning(string format, params object[] args)
        {
            WriteToStream(WarningStream, format, args);
        }

        private void WriteVerbose(string format, params object[] args)
        {
            WriteToStream(VerboseStream, format, args);
        }

        private void WriteVerboseWithTimestamp(string format, params object[] args)
        {
            WriteVerbose(string.Format("{0:T} - {1}", DateTime.Now, string.Format(format, args)));
        }

        private void TranslateException(Action a)
        {
            try
            {
                a();
            }
            catch (CloudException ex)
            {
                throw new Exception(string.Format(
                    Resources.OperationFailedMessage,
                    ex.Message,
                    ex.Response.StatusCode), ex);
            }
        }

        private void PrepareCloudServicePackagesRuntime(PublishContext context)
        {
            CloudServiceProject cloudServiceProject = new CloudServiceProject(context.RootPath, null);
            string warning = cloudServiceProject.ResolveRuntimePackageUrls();

            if (!string.IsNullOrEmpty(warning))
            {
                WriteWarning(Resources.RuntimeMismatchWarning, context.ServiceName);
                WriteWarning(warning);
            }
        }

        private void UpdateCacheWorkerRolesCloudConfiguration(PublishContext context)
        {
            string connectionString = GetStorageServiceConnectionString(context.ServiceSettings.StorageServiceName);
            var cloudServiceProject = new CloudServiceProject(context.RootPath, null);

            var connectionStringConfig = new ConfigConfigurationSetting
            {
                name = Resources.CachingConfigStoreConnectionStringSettingName,
                value = string.Empty
            };

            cloudServiceProject.Components.ForEachRoleSettings(
                r => Array.Exists(r.ConfigurationSettings, c => c.Equals(connectionStringConfig)),
                r =>
                    {
                        int index = Array.IndexOf(r.ConfigurationSettings, connectionStringConfig);
                        r.ConfigurationSettings[index] = new ConfigConfigurationSetting
                        {
                            name = Resources.CachingConfigStoreConnectionStringSettingName,
                            value = connectionString
                        };
                    });

            cloudServiceProject.Components.Save(cloudServiceProject.Paths);
        }

        private void CreateDeployment(PublishContext context)
        {
            var deploymentParams = new DeploymentCreateParameters
            {
                PackageUri = UploadPackage(context),
                Configuration = General.GetConfiguration(context.ConfigPath),
                Label = context.ServiceName,
                Name = context.DeploymentName,
                StartDeployment = true
            };

            WriteVerboseWithTimestamp(Resources.PublishStartingMessage);

            ServiceCertificateListResponse uploadedCertificates = ComputeClient.ServiceCertificates.List(context.ServiceName);
            AddCertificates(uploadedCertificates, context);

            ComputeClient.Deployments.Create(context.ServiceName, GetSlot(context.ServiceSettings.Slot),
                deploymentParams);
        }

        private void AddCertificates(ServiceCertificateListResponse uploadedCertificates, PublishContext context)
        {
            string name = context.ServiceName;
            CloudServiceProject cloudServiceProject = new CloudServiceProject(context.RootPath, null);
            if (cloudServiceProject.Components.CloudConfig.Role != null)
            {
                foreach (ConfigCertificate certElement in cloudServiceProject.Components.CloudConfig.Role.
                    SelectMany(r => r.Certificates ?? new ConfigCertificate[0]).Distinct())
                {
                    if (uploadedCertificates == null || (uploadedCertificates.Certificates.Count(c => c.Thumbprint.Equals(
                        certElement.thumbprint, StringComparison.OrdinalIgnoreCase)) < 1))
                    {
                        X509Certificate2 cert = General.GetCertificateFromStore(certElement.thumbprint);
                        UploadCertificate(cert, certElement, name);
                    }
                }
            }
        }

        private void UploadCertificate(X509Certificate2 cert, ConfigCertificate certElement, string name)
        {
            try
            {
                var createParams = new ServiceCertificateCreateParameters
                {
                    Data = cert.Export(X509ContentType.Pfx, string.Empty),
                    Password = string.Empty,
                    CertificateFormat = CertificateFormat.Pfx
                };
                TranslateException(() => ComputeClient.ServiceCertificates.Create(name, createParams));

            }
            catch (CryptographicException ex)
            {
                throw new ArgumentException(string.Format(
                    Resources.CertificatePrivateKeyAccessError,
                    certElement.name), ex);                
            }
        }

        private void UpgradeDeployment(PublishContext context)
        {
            var upgradeParams = new DeploymentUpgradeParameters
            {
                Configuration = General.GetConfiguration(context.ConfigPath),
                PackageUri = UploadPackage(context),
                Label = context.ServiceName,
                Mode = DeploymentUpgradeMode.Auto
            };

            WriteVerboseWithTimestamp(Resources.PublishUpgradingMessage);

            var uploadedCertificates = ComputeClient.ServiceCertificates.List(context.ServiceName);
            AddCertificates(uploadedCertificates, context);

            ComputeClient.Deployments.UpgradeBySlot(context.ServiceName, GetSlot(context.ServiceSettings.Slot), upgradeParams);
        }

        private void VerifyDeployment(PublishContext context)
        {
            try
            {
                WriteVerboseWithTimestamp(Resources.PublishInitializingMessage);

                var roleInstanceSnapshot = new Dictionary<string, RoleInstance>();

                // Continue polling for deployment until all of the roles
                // indicate they're ready
                Deployment deployment;
                do
                {
                    deployment = new Deployment(
                        ComputeClient.Deployments.GetBySlot(context.ServiceName, GetSlot(context.ServiceSettings.Slot)));

                    // The goal of this loop is to output a message whenever the status of a role 
                    // instance CHANGES. To do that, we have to remember the last status of all role instances
                    // and that's what the roleInstanceSnapshot array is for
                    foreach (RoleInstance currentInstance in deployment.RoleInstanceList)
                    {
                        // We only care about these three statuses, ignore other intermediate statuses
                        if (string.Equals(currentInstance.InstanceStatus, RoleInstanceStatus.BusyRole) ||
                            string.Equals(currentInstance.InstanceStatus, RoleInstanceStatus.ReadyRole) ||
                            string.Equals(currentInstance.InstanceStatus, RoleInstanceStatus.CreatingRole))
                        {
                            bool createdOrChanged = false;

                            // InstanceName is unique and concatenates the role name and instance name
                            if (roleInstanceSnapshot.ContainsKey(currentInstance.InstanceName))
                            {
                                // If we already have a snapshot of that role instance, update it
                                RoleInstance previousInstance = roleInstanceSnapshot[currentInstance.InstanceName];
                                if (!string.Equals(previousInstance.InstanceStatus, currentInstance.InstanceStatus))
                                {
                                    // If the instance status changed, we need to output a message
                                    previousInstance.InstanceStatus = currentInstance.InstanceStatus;
                                    createdOrChanged = true;
                                }
                            }
                            else
                            {
                                // If this is the first time we run through, we also need to output a message
                                roleInstanceSnapshot[currentInstance.InstanceName] = currentInstance;
                                createdOrChanged = true;
                            }

                            if (createdOrChanged)
                            {
                                string statusResource;
                                switch (currentInstance.InstanceStatus)
                                {
                                    case RoleInstanceStatus.BusyRole:
                                        statusResource = Resources.PublishInstanceStatusBusy;
                                        break;

                                    case RoleInstanceStatus.ReadyRole:
                                        statusResource = Resources.PublishInstanceStatusReady;
                                        break;

                                    default:
                                        statusResource = Resources.PublishInstanceStatusCreating;
                                        break;
                                }

                                WriteVerboseWithTimestamp(
                                    Resources.PublishInstanceStatusMessage,
                                    currentInstance.InstanceName,
                                    currentInstance.RoleName,
                                    statusResource);
                            }
                        }
                    }

                    // If a deployment has many roles to initialize, this
                    // thread must throttle requests so the Azure portal
                    // doesn't reply with a "too many requests" error
                    Thread.Sleep(SleepDuration);
                }
                while (deployment.RoleInstanceList.Any(r => r.InstanceStatus != RoleInstanceStatus.ReadyRole));

                WriteVerboseWithTimestamp(Resources.PublishCreatedWebsiteMessage, deployment.Url);

            }
            catch (CloudException)
            {
                throw new InvalidOperationException(
                    string.Format(Resources.CannotFindDeployment, context.ServiceName, context.ServiceSettings.Slot));
            }
        }

        private void DeleteDeploymentIfExists(string name, DeploymentSlot slot)
        {
            if (DeploymentExists(name, slot))
            {
                WriteVerboseWithTimestamp(Resources.RemoveDeploymentWaitMessage, slot, name);
                TranslateException(() => ComputeClient.Deployments.DeleteBySlot(name, slot));
            }   
        }

        private string GetDeploymentId(PublishContext context)
        {
            Deployment deployment = new Deployment();

            do
            {
                // If a deployment has many roles to initialize, this
                // thread must throttle requests so the Azure portal
                // doesn't reply with a "too many requests" error
                Thread.Sleep(SleepDuration);

                try
                {
                    deployment = new Deployment(
                        ComputeClient.Deployments.GetBySlot(context.ServiceName,
                        GetSlot(context.ServiceSettings.Slot)));
                }
                catch (CloudException ex)
                {
                    if (ex.Response.StatusCode != HttpStatusCode.InternalServerError)
                    {
                        throw;
                    }
                }
            }
            while (deployment.Status != DeploymentStatus.Starting && deployment.Status != DeploymentStatus.Running);

            return deployment.PrivateID;
        }

        private DeploymentSlot GetSlot(string slot)
        {
            if (string.IsNullOrEmpty(slot))
            {
                return DeploymentSlot.Production;
            }
            if (string.Compare(slot, "Staging", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return DeploymentSlot.Staging;
            }  
            if (string.Compare(slot, "Production", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return DeploymentSlot.Production;
            }
            throw new ArgumentException(string.Format(Resources.InvalidDeploymentSlot, slot), slot);
        }

        private string GetCloudServiceName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = GetCurrentServiceProject().ServiceName;
            }

            return name;
        }

        private string GetCurrentDirectory()
        {
            return CurrentDirectory != null ? CurrentDirectory : Environment.CurrentDirectory;
        }

        private Uri UploadPackage(PublishContext context)
        {
            WriteVerboseWithTimestamp(
                    Resources.PublishUploadingPackageMessage,
                    context.ServiceSettings.StorageServiceName);

            return CloudBlobUtility.UploadPackageToBlob(
                StorageClient,
                context.ServiceSettings.StorageServiceName,
                context.PackagePath,
                new BlobRequestOptions());
        }

        private HostedServiceGetDetailedResponse GetCloudService(string name)
        {
            name = GetCloudServiceName(name);
            try
            {
                return ComputeClient.HostedServices.GetDetailed(name);
            }
            catch
            {
                throw new Exception(string.Format(Resources.ServiceDoesNotExist, name));
            }
        }

        private CloudServiceProject GetCurrentServiceProject()
        {
            return new CloudServiceProject(General.GetServiceRootPath(GetCurrentDirectory()), null);
        }

        private PublishContext CreatePublishContext(
            string name,
            string slot,
            string location,
            string affinityGroup,
            string storageServiceName,
            string deploymentName)
        {
            string serviceName;
            CloudServiceProject cloudServiceProject = GetCurrentServiceProject();

            // If the name provided is different than existing name change it
            if (!string.IsNullOrEmpty(name) && name != cloudServiceProject.ServiceName)
            {
                cloudServiceProject.ChangeServiceName(name, cloudServiceProject.Paths);
            }

            // If there's no storage service provided, try using the default one
            if (string.IsNullOrEmpty(storageServiceName))
            {
                storageServiceName = Subscription.CurrentStorageAccountName;
            }

            // Use default location if not location and affinity group provided
            location = string.IsNullOrEmpty(location) && string.IsNullOrEmpty(affinityGroup) ? 
                GetDefaultLocation() : 
                location;

            ServiceSettings serviceSettings = ServiceSettings.LoadDefault(
                cloudServiceProject.Paths.Settings,
                slot,
                location,
                affinityGroup,
                Subscription.Name,
                storageServiceName,
                name,
                cloudServiceProject.ServiceName,
                out serviceName
                );

            PublishContext context = new PublishContext(
                serviceSettings,
                Path.Combine(GetCurrentDirectory(), cloudServiceProject.Paths.CloudPackage),
                Path.Combine(GetCurrentDirectory(), cloudServiceProject.Paths.CloudConfiguration),
                serviceName,
                deploymentName,
                cloudServiceProject.Paths.RootPath);

            return context;
        }

        /// <summary>
        /// Creates new instance from CloudServiceClient.
        /// </summary>
        /// <param name="subscription">The subscription data</param>
        /// <param name="currentLocation">Directory to do operations in</param>
        /// <param name="debugStream">Action used to log http requests/responses</param>
        /// <param name="verboseStream">Action used to log detailed client progress</param>
        /// <param name="warningStream">Action used to log warning messages</param>
        public CloudServiceClient(
            WindowsAzureSubscription subscription,
            string currentLocation = null,
            Action<string> debugStream = null,
            Action<string> verboseStream = null,
            Action<string> warningStream = null)
            : this(currentLocation, debugStream, warningStream, verboseStream)
        {
            Subscription = subscription;
            CloudBlobUtility = new CloudBlobUtility();

            ManagementClient = subscription.CreateClient<ManagementClient>();
            StorageClient = subscription.CreateClient<StorageManagementClient>();
            ComputeClient = subscription.CreateClient<ComputeManagementClient>();
        }

        private CloudServiceClient(string currentLocation, Action<string> debugStream, Action<string> verboseStream,
                                   Action<string> warningStream)
        {
            CurrentDirectory = currentLocation;
            DebugStream = debugStream;
            VerboseStream = verboseStream;
            WarningStream = warningStream;
        }

        internal CloudServiceClient(
            WindowsAzureSubscription subscription,
            ManagementClient managementClient,
            StorageManagementClient storageManagementClient,
            ComputeManagementClient computeManagementClient)
            : this((string)null, null, null, null)
        {
            Subscription = subscription;
            CurrentDirectory = null;
            DebugStream = null;
            VerboseStream = null;
            WarningStream = null;

            CloudBlobUtility = new CloudBlobUtility();
            ManagementClient = managementClient;
            StorageClient = storageManagementClient;
            ComputeClient = computeManagementClient;
        }

        /// <summary>
        /// Starts a cloud service.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        /// <param name="slot">The deployment slot</param>
        public void StartCloudService(string name = null, string slot = null)
        {
            DeploymentSlot deploymentSlot = GetSlot(slot);

            SetCloudServiceState(name, deploymentSlot, CloudServiceState.Start);
        }

        /// <summary>
        /// Stops a cloud service.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        /// <param name="slot">The deployment slot</param>
        public void StopCloudService(string name = null, string slot = null)
        {
            DeploymentSlot deploymentSlot = GetSlot(slot);
            SetCloudServiceState(name, deploymentSlot, CloudServiceState.Stop);
        }

        /// <summary>
        /// Check if the deployment exists for given cloud service.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        /// <param name="slot">The deployment slot name</param>
        /// <returns>Flag indicating the deployment exists or not</returns>
        public bool DeploymentExists(string name = null, string slot = null)
        {
            DeploymentSlot deploymentSlot = GetSlot(slot);
            return DeploymentExists(name, deploymentSlot);
        }

        private bool DeploymentExists(string name, DeploymentSlot slot)
        {
            HostedServiceGetDetailedResponse cloudService = GetCloudService(name);
            try
            {
                VerifyDeploymentExists(cloudService, slot);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Publishes a service project on Windows Azure.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        /// <param name="slot">The deployment slot</param>
        /// <param name="location">The deployment location</param>
        /// <param name="affinityGroup">The deployment affinity group</param>
        /// <param name="storageAccount">The storage account to store the package</param>
        /// <param name="deploymentName">The deployment name</param>
        /// <param name="launch">Launch the service after publishing</param>
        /// <returns>The created deployment</returns>
        public Deployment PublishCloudService(
            string name = null,
            string slot = null,
            string location = null,
            string affinityGroup = null,
            string storageAccount = null,
            string deploymentName = null,
            bool launch = false)
        {
            // Initialize publish context
            PublishContext context = CreatePublishContext(
                name,
                slot,
                location,
                affinityGroup,
                storageAccount,
                deploymentName);
            WriteVerbose(string.Format(Resources.PublishServiceStartMessage, context.ServiceName));

            // Set package runtime information
            WriteVerboseWithTimestamp(Resources.RuntimeDeploymentStart, context.ServiceName);
            PrepareCloudServicePackagesRuntime(context);

            // Verify storage account exists
            WriteVerboseWithTimestamp(
                Resources.PublishVerifyingStorageMessage,
                context.ServiceSettings.StorageServiceName);

            CreateStorageServiceIfNotExist(
                context.ServiceSettings.StorageServiceName,
                context.ServiceName,
                context.ServiceSettings.Location,
                context.ServiceSettings.AffinityGroup);

            // Update cache worker roles configuration
            WriteVerboseWithTimestamp(
                    Resources.PublishPreparingDeploymentMessage,
                    context.ServiceName,
                    Subscription.SubscriptionId);
            UpdateCacheWorkerRolesCloudConfiguration(context);

            // Create cloud package
            AzureTool.Validate();
            if (File.Exists(context.PackagePath))
            {
                File.Delete(context.PackagePath);
            }
            CloudServiceProject cloudServiceProject = new CloudServiceProject(context.RootPath, null);
            string unused;
            cloudServiceProject.CreatePackage(DevEnv.Cloud, out unused, out unused);

            // Publish cloud service
            WriteVerboseWithTimestamp(Resources.PublishConnectingMessage);
            CreateCloudServiceIfNotExist(
                context.ServiceName,
                affinityGroup: context.ServiceSettings.AffinityGroup,
                location: context.ServiceSettings.Location);

            if (DeploymentExists(context.ServiceName, context.ServiceSettings.Slot))
            {
                // Upgrade the deployment
                UpgradeDeployment(context);
            }
            else
            {
                // Create new deployment
                CreateDeployment(context);
            }

            // Get the deployment id and show it.
            WriteVerboseWithTimestamp(Resources.PublishCreatedDeploymentMessage, GetDeploymentId(context));

            // Verify the deployment succeeded by checking that each of the roles are running
            VerifyDeployment(context);

            // Get object of the published deployment
            DeploymentGetResponse deployment = ComputeClient.Deployments.GetBySlot(context.ServiceName, GetSlot(context.ServiceSettings.Slot));

            if (launch)
            {
                General.LaunchWebPage(deployment.Uri.ToString());
            }

            return new Deployment(deployment);
        }

        /// <summary>
        /// Checks if a cloud service exists or not.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        /// <returns>True if exists, false otherwise</returns>
        public bool CloudServiceExists(string name)
        {
            try
            {
                return ComputeClient.HostedServices.GetDetailed(name) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates cloud service if it does not exist.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        /// <param name="label">The cloud service label</param>
        /// <param name="location">The location to create the cloud service in.</param>
        /// <param name="affinityGroup">Affinity group name for cloud service</param>
        public void CreateCloudServiceIfNotExist(
            string name,
            string label = null,
            string location = null,
            string affinityGroup = null)
        {
            if (!CloudServiceExists(name))
            {
                WriteVerboseWithTimestamp(Resources.PublishCreatingServiceMessage);

                var createParameters = new HostedServiceCreateParameters {ServiceName = name, Label = label};

                if (!string.IsNullOrEmpty(affinityGroup))
                {
                    createParameters.AffinityGroup = affinityGroup;
                }
                else
                {
                    location = string.IsNullOrEmpty(location) ? GetDefaultLocation() : location;
                    createParameters.Location = location;
                }

                ComputeClient.HostedServices.Create(createParameters);

                WriteVerboseWithTimestamp(Resources.PublishCreatedServiceMessage, name);
            }
        }

        /// <summary>
        /// Creates storage service if it does not exist.
        /// </summary>
        /// <param name="name">The storage service name</param>
        /// <param name="label">The storage service label</param>
        /// <param name="location">The location name. If not provided default one will be used</param>
        /// <param name="affinityGroup">The affinity group name</param>
        public void CreateStorageServiceIfNotExist(
            string name,
            string label = null,
            string location = null,
            string affinityGroup = null)
        {
            if (!StorageServiceExists(name))
            {
                var createParameters = new StorageAccountCreateParameters {ServiceName = name, Label = label};
             
                if (!string.IsNullOrEmpty(affinityGroup))
                {
                    createParameters.AffinityGroup = affinityGroup;
                }
                else
                {
                    location = string.IsNullOrEmpty(location) ? GetDefaultLocation() : location;
                    createParameters.Location = location;
                }

                TranslateException(() => StorageClient.StorageAccounts.Create(createParameters));
            }
        }

        /// <summary>
        /// Gets the default subscription location.
        /// </summary>
        /// <returns>The location name</returns>
        public string GetDefaultLocation()
        {
            return ManagementClient.Locations.List().Locations.First().Name;   
        }

        /// <summary>
        /// Checks if the provided storage service exists under the subscription or not.
        /// </summary>
        /// <param name="name">The storage service name</param>
        /// <returns>True if exists, false otherwise</returns>
        public bool StorageServiceExists(string name)
        {
            try
            {
                return StorageClient.StorageAccounts.Get(name) != null;
            }
            catch (CloudException ex)
            {
                // Don't write error message.  This catch block is used to
                // detect that there's no such endpoint which indicates that
                // the storage account doesn't exist.
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                // Something else went wrong
                throw;
            }
        }

        /// <summary>
        /// Gets connection string of the given storage service name.
        /// </summary>
        /// <param name="name">The storage service name</param>
        /// <returns>The connection string</returns>
        public string GetStorageServiceConnectionString(string name)
        {
            StorageServiceGetResponse storageService;
            StorageAccountGetKeysResponse storageKeys;

            try
            {
                storageService = StorageClient.StorageAccounts.Get(name);
                storageKeys = StorageClient.StorageAccounts.GetKeys(name);
            }
            catch
            {
                throw new Exception(string.Format(Resources.StorageAccountNotFound, name));
            }

            Debug.Assert(storageService.ServiceName != null);
            Debug.Assert(storageKeys != null);

            StorageCredentials credentials = new StorageCredentials(
                storageService.ServiceName,
                storageKeys.PrimaryKey);

            CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(
                credentials,
                General.CreateHttpsEndpoint(storageService.Properties.Endpoints[0].ToString()),
                General.CreateHttpsEndpoint(storageService.Properties.Endpoints[1].ToString()),
                General.CreateHttpsEndpoint(storageService.Properties.Endpoints[2].ToString())
                );

            return cloudStorageAccount.ToString(true);
        }

        /// <summary>
        /// Removes all deployments in the given cloud service and the service itself.
        /// </summary>
        /// <param name="name">The cloud service name</param>
        public void RemoveCloudService(string name)
        {
            var cloudService = GetCloudService(name);

            DeleteDeploymentIfExists(cloudService.ServiceName, DeploymentSlot.Production);
            DeleteDeploymentIfExists(cloudService.ServiceName, DeploymentSlot.Staging);

            WriteVerboseWithTimestamp(string.Format(Resources.RemoveAzureServiceWaitMessage, cloudService.ServiceName));
            TranslateException(() => ComputeClient.HostedServices.Delete(cloudService.ServiceName));
        }
    }
}
