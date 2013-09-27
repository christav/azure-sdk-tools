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
namespace Microsoft.WindowsAzure.Commands.ServiceManagement.Test.FunctionalTests
{
    using Commands.Utilities.Common;
    using ConfigDataInfo;
    using Extensions;
    using IaasCmdletInfo;
    using Model;
    using PaasCmdletInfo;
    using PlatformImageRepository.Model;
    using Storage.Blob;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Management.Automation;
    using System.Security.Cryptography.X509Certificates;
    using System.Xml;
    using VisualStudio.TestTools.UnitTesting;
    using WindowsAzure.ServiceManagement;

    using PIRCmdletInfo;

    public class ServiceManagementCmdletTestHelper
    {
        /// <summary>
        /// Run a powershell cmdlet that returns the first PSObject as a return value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        private T RunPSCmdletAndReturnFirst<T>(PowershellCore.CmdletsInfo cmdlet)
        {
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(cmdlet);
            Collection<PSObject> result = azurePowershellCmdlet.Run();
            if (result.Count == 1)
            {
                try
                {
                    var operation = (ManagementOperationContext)result[0].BaseObject;
                    Console.WriteLine("Operation ID: {0}", operation.OperationId);
                }
                catch
                {
                }

                return (T) result[0].BaseObject;
            }
            return default(T);
        }

        /// <summary>
        /// Run a powershell cmdlet that returns a collection of PSObjects as a return value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        private Collection<T> RunPSCmdletAndReturnAll<T>(PowershellCore.CmdletsInfo cmdlet)
        {
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(cmdlet);
            Collection<PSObject> result = azurePowershellCmdlet.Run();
            Collection<T> resultCollection = new Collection<T>();
            foreach (PSObject re in result)
            {
                resultCollection.Add((T)re.BaseObject);
            }

            try
            {
                var operation = (ManagementOperationContext)result[0].BaseObject;
                Console.WriteLine("Operation ID: {0}", operation.OperationId);
            }
            catch
            {
            }

            return resultCollection;
        }

        public Collection <PSObject> RunPSScript(string script)
        {
            List<string> st = new List<string>();
            st.Add(script);

            WindowsAzurePowershellScript azurePowershellCmdlet = new WindowsAzurePowershellScript(st);
            return azurePowershellCmdlet.Run();
        }


        public CopyState CheckCopyBlobStatus(string destContainer, string destBlob)
        {
            List<string> st = new List<string>();
            st.Add(string.Format("Get-AzureStorageBlobCopyState -Container {0} -Blob {1}", destContainer, destBlob));

            WindowsAzurePowershellScript azurePowershellCmdlet = new WindowsAzurePowershellScript(st);
            return (CopyState)azurePowershellCmdlet.Run()[0].BaseObject;
        }
        public bool TestAzureServiceName(string serviceName)
        {
            return RunPSCmdletAndReturnFirst<bool>(new TestAzureNameCmdletInfo("Service", serviceName));
        }

        public Collection<LocationsContext> GetAzureLocation()
        {
            return RunPSCmdletAndReturnAll<LocationsContext>(new GetAzureLocationCmdletInfo());
        }

        public string GetAzureLocationName(string[] keywords)
        {
            Collection<LocationsContext> locations = GetAzureLocation();
            if (keywords != null)
            {
                foreach (LocationsContext location in locations)
                {
                    if (MatchExactWords(location.Name, keywords) >= 0)
                    {
                        return location.Name;
                    }
                }
            }
            else
            {
                if (locations.Count == 1)
                {
                    return locations[0].Name;
                }
            }
            return null;
        }

        private static int MatchExactWords(string input, string[] keywords)
        { //returns -1 for no match, 0 for exact match, and a positive number for how many keywords are matched.
            int result = 0;
            if (string.IsNullOrEmpty(input) || keywords.Length == 0)
                return -1;
            foreach (string keyword in keywords)
            {
                //For whole word match, modify pattern to be "\b{0}\b"
                if (!string.IsNullOrEmpty(keyword) && keyword.ToLowerInvariant().Equals(input.ToLowerInvariant()))
                {
                    result++;
                }
            }
            if (result == keywords.Length)
            {
                return 0;
            }
            else if (result == 0)
            {
                return -1;
            }
            else
            {
                return result;
            }
        }

        public Collection<OSVersionsContext> GetAzureOSVersion()
        {
            return RunPSCmdletAndReturnAll<OSVersionsContext>(new GetAzureOSVersionCmdletInfo());
        }

        #region CertificateSetting, VMConifig, ProvisioningConfig

        public CertificateSetting NewAzureCertificateSetting(string store, string thumbprint)
        {
            return RunPSCmdletAndReturnFirst<CertificateSetting>(new NewAzureCertificateSettingCmdletInfo(store, thumbprint));
        }

        public PersistentVM NewAzureVMConfig(AzureVMConfigInfo vmConfig)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new NewAzureVMConfigCmdletInfo(vmConfig));
        }

        public PersistentVM AddAzureProvisioningConfig(AzureProvisioningConfigInfo provConfig)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new AddAzureProvisioningConfigCmdletInfo(provConfig));
        }

        #endregion

        #region AzureAffinityGroup

        public ManagementOperationContext NewAzureAffinityGroup(string name, string location, string label, string description)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext> (new NewAzureAffinityGroupCmdletInfo(name, location, label, description));
        }

        public Collection<AffinityGroupContext> GetAzureAffinityGroup(string name)
        {
            return RunPSCmdletAndReturnAll<AffinityGroupContext>(new GetAzureAffinityGroupCmdletInfo(name));
        }

        public ManagementOperationContext SetAzureAffinityGroup(string name, string label, string description)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext> (new SetAzureAffinityGroupCmdletInfo(name, label, description));
        }

        public ManagementOperationContext RemoveAzureAffinityGroup(string name)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureAffinityGroupCmdletInfo(name));
        }

        #endregion

        #region AzureAvailabilitySet

        public PersistentVM SetAzureAvailabilitySet(string availabilitySetName, PersistentVM vm)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureAvailabilitySetCmdletInfo(availabilitySetName, vm));
        }

        public PersistentVM SetAzureAvailabilitySet(string vmName, string serviceName, string availabilitySetName)
        {
            if (!string.IsNullOrEmpty(availabilitySetName))
            {
                PersistentVM vm = GetAzureVM(vmName, serviceName).VM;

                return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureAvailabilitySetCmdletInfo(availabilitySetName, vm));
            }
            else
            {
                return null;
            }
        }

        #endregion AzureAvailabilitySet

        #region AzureCertificate

        public ManagementOperationContext AddAzureCertificate(string serviceName, PSObject cert, string password = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new AddAzureCertificateCmdletInfo(serviceName, cert, password));
        }

        public Collection <CertificateContext> GetAzureCertificate(string serviceName, string thumbprint = null, string algorithm = null)
        {
            return RunPSCmdletAndReturnAll<CertificateContext> (new GetAzureCertificateCmdletInfo(serviceName, thumbprint, algorithm));
        }

        public ManagementOperationContext RemoveAzureCertificate(string serviceName, string thumbprint, string algorithm)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureCertificateCmdletInfo(serviceName, thumbprint, algorithm));
        }

        #endregion

        #region AzureDataDisk

        public PersistentVM AddAzureDataDisk(AddAzureDataDiskConfig diskConfig)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new AddAzureDataDiskCmdletInfo(diskConfig));
        }

        public void AddDataDisk(string vmName, string serviceName, AddAzureDataDiskConfig [] diskConfigs)
        {
            PersistentVM vm = GetAzureVM(vmName, serviceName).VM;

            foreach (AddAzureDataDiskConfig config in diskConfigs)
            {
                config.Vm = vm;
                vm = AddAzureDataDisk(config);
            }
            UpdateAzureVM(vmName, serviceName, vm);
        }

        public PersistentVM SetAzureDataDisk(SetAzureDataDiskConfig discCfg)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureDataDiskCmdletInfo(discCfg));
        }

        public void SetDataDisk(string vmName, string serviceName, HostCaching hc, int lun)
        {
            SetAzureDataDiskConfig config = new SetAzureDataDiskConfig(hc, lun);
            config.Vm = GetAzureVM(vmName, serviceName).VM;
            UpdateAzureVM(vmName, serviceName, SetAzureDataDisk(config));
        }

        public Collection<DataVirtualHardDisk> GetAzureDataDisk(string vmName, string serviceName)
        {
            PersistentVMRoleContext vmRolectx = GetAzureVM(vmName, serviceName);

            return RunPSCmdletAndReturnAll<DataVirtualHardDisk>(new GetAzureDataDiskCmdletInfo(vmRolectx.VM));
        }

        public Collection<DataVirtualHardDisk> GetAzureDataDisk(PersistentVM vm)
        {
            return RunPSCmdletAndReturnAll<DataVirtualHardDisk>(new GetAzureDataDiskCmdletInfo(vm));
        }
        public PersistentVM RemoveAzureDataDisk(RemoveAzureDataDiskConfig discCfg)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new RemoveAzureDataDiskCmdletInfo(discCfg));
        }

        public void RemoveDataDisk(string vmName, string serviceName, int [] lunSlots)
        {
            PersistentVM vm = GetAzureVM(vmName, serviceName).VM;

            foreach (int lun in lunSlots)
            {
                RemoveAzureDataDiskConfig config = new RemoveAzureDataDiskConfig(lun, vm);
                RemoveAzureDataDisk(config);
            }
            UpdateAzureVM(vmName, serviceName, vm);
        }

        #endregion

        #region AzureDeployment

        public ManagementOperationContext NewAzureDeployment(string serviceName, string packagePath, string configPath, string slot, string label, string name, bool doNotStart, bool warning, ExtensionConfigurationInput config = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new NewAzureDeploymentCmdletInfo(serviceName, packagePath, configPath, slot, label, name, doNotStart, warning, config));
        }

        public DeploymentInfoContext GetAzureDeployment(string serviceName, string slot)
        {
            return RunPSCmdletAndReturnFirst<DeploymentInfoContext>(new GetAzureDeploymentCmdletInfo(serviceName, slot));
        }

        public DeploymentInfoContext GetAzureDeployment(string serviceName)
        {
            return GetAzureDeployment(serviceName, DeploymentSlotType.Production);
        }

        private ManagementOperationContext SetAzureDeployment(SetAzureDeploymentCmdletInfo cmdletInfo)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(cmdletInfo);
        }

        public ManagementOperationContext SetAzureDeploymentStatus(string serviceName, string slot, string newStatus)
        {
            return SetAzureDeployment(SetAzureDeploymentCmdletInfo.SetAzureDeploymentStatusCmdletInfo(serviceName, slot, newStatus));
        }

        public ManagementOperationContext SetAzureDeploymentConfig(string serviceName, string slot, string configPath)
        {
            return SetAzureDeployment(SetAzureDeploymentCmdletInfo.SetAzureDeploymentConfigCmdletInfo(serviceName, slot, configPath));
        }

        public ManagementOperationContext SetAzureDeploymentUpgrade(string serviceName, string slot, string mode, string packagePath, string configPath)
        {
            return SetAzureDeployment(SetAzureDeploymentCmdletInfo.SetAzureDeploymentUpgradeCmdletInfo(serviceName, slot, mode, packagePath, configPath));
        }

        public ManagementOperationContext SetAzureDeployment(string option, string serviceName, string packagePath, string newStatus, string configName, string slot, string mode, string label, string roleName, bool force)
        {
            return SetAzureDeployment(new SetAzureDeploymentCmdletInfo(option, serviceName, packagePath, newStatus, configName, slot, mode, label, roleName, force));
        }

        public ManagementOperationContext RemoveAzureDeployment(string serviceName, string slot, bool force)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureDeploymentCmdletInfo(serviceName, slot, force));
        }

        public ManagementOperationContext MoveAzureDeployment(string serviceName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new MoveAzureDeploymentCmdletInfo(serviceName));
        }

        public ManagementOperationContext SetAzureWalkUpgradeDomain(string serviceName, string slot, int domainNumber)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureWalkUpgradeDomainCmdletInfo(serviceName, slot, domainNumber));
        }

        #endregion

        #region AzureDisk

        // Add-AzureDisk
        public DiskContext AddAzureDisk(string diskName, string mediaPath, string label, string os)
        {
            DiskContext result = new DiskContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<DiskContext>(new AddAzureDiskCmdletInfo(diskName, mediaPath, label, os)),
                "409", 3, 60);
            return result;
        }

        // Get-AzureDisk
        public Collection<DiskContext> GetAzureDisk(string diskName)
        {
            return GetAzureDisk(new GetAzureDiskCmdletInfo(diskName));
        }

        public Collection<DiskContext> GetAzureDisk()
        {
            return GetAzureDisk(new GetAzureDiskCmdletInfo((string)null));
        }

        private Collection<DiskContext> GetAzureDisk(GetAzureDiskCmdletInfo getAzureDiskCmdletInfo)
        {
            return RunPSCmdletAndReturnAll<DiskContext>(getAzureDiskCmdletInfo);
        }

        public Collection<DiskContext> GetAzureDiskAttachedToRoleName(string[] roleName, bool exactMatch = true)
        {
            Collection<DiskContext> retDisks = new Collection<DiskContext>();
            Collection<DiskContext> disks = GetAzureDisk();
            foreach (DiskContext disk in disks)
            {
                if (disk.AttachedTo != null && disk.AttachedTo.RoleName != null)
                {
                    if (Utilities.MatchKeywords(disk.AttachedTo.RoleName, roleName, exactMatch) >= 0)
                        retDisks.Add(disk);
                }
            }
            return retDisks;
        }

        // Remove-AzureDisk
        public ManagementOperationContext RemoveAzureDisk(string diskName, bool deleteVhd)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureDiskCmdletInfo(diskName, deleteVhd));
        }

        // Update-AzureDisk
        public DiskContext UpdateAzureDisk(string diskName, string label)
        {
            return RunPSCmdletAndReturnFirst<DiskContext>(new UpdateAzureDiskCmdletInfo(diskName, label));
        }

        #endregion

        #region AzureDns

        public DnsServer NewAzureDns(string name, string ipAddress)
        {
            return RunPSCmdletAndReturnFirst<DnsServer>(new NewAzureDnsCmdletInfo(name, ipAddress));
        }

        public DnsServerList GetAzureDns(DnsSettings settings)
        {
            GetAzureDnsCmdletInfo getAzureDnsCmdletInfo = new GetAzureDnsCmdletInfo(settings);
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(getAzureDnsCmdletInfo);

            Collection<PSObject> result = azurePowershellCmdlet.Run();
            DnsServerList dnsList = new DnsServerList();

            foreach (PSObject re in result)
            {
                dnsList.Add((DnsServer)re.BaseObject);
            }
            return dnsList;
        }

        #endregion

        #region AzureEndpoint

        public PersistentVM AddAzureEndPoint(AzureEndPointConfigInfo endPointConfig)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new AddAzureEndpointCmdletInfo(endPointConfig));
        }

        public void AddEndPoint(string vmName, string serviceName, AzureEndPointConfigInfo [] endPointConfigs)
        {
            PersistentVM vm = GetAzureVM(vmName, serviceName).VM;

            foreach (AzureEndPointConfigInfo config in endPointConfigs)
            {
                config.Vm = vm;
                vm = AddAzureEndPoint(config);
            }
            UpdateAzureVM(vmName, serviceName, vm);
        }

        public Collection <InputEndpointContext> GetAzureEndPoint(PersistentVMRoleContext vmRoleCtxt)
        {
            return RunPSCmdletAndReturnAll<InputEndpointContext>(new GetAzureEndpointCmdletInfo(vmRoleCtxt));
        }

        public Collection<InputEndpointContext> GetAzureEndPoint(PersistentVM vm)
        {
            return RunPSCmdletAndReturnAll<InputEndpointContext>(new GetAzureEndpointCmdletInfo(vm));
        }

        public void SetEndPoint(string vmName, string serviceName, AzureEndPointConfigInfo endPointConfig)
        {
            endPointConfig.Vm = GetAzureVM(vmName, serviceName).VM;
            UpdateAzureVM(vmName, serviceName, SetAzureEndPoint(endPointConfig));
        }

        public PersistentVM SetAzureEndPoint(AzureEndPointConfigInfo endPointConfig)
        {
            if (null != endPointConfig)
            {
                return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureEndpointCmdletInfo(endPointConfig));
            }
            return null;
        }

        public void SetLBEndPoint(string vmName, string serviceName, AzureEndPointConfigInfo endPointConfig, AzureEndPointConfigInfo.ParameterSet paramset)
        {
            endPointConfig.Vm = GetAzureVM(vmName, serviceName).VM;
            SetAzureLoadBalancedEndPoint(endPointConfig, paramset);

            //UpdateAzureVM(vmName, serviceName, SetAzureLoadBalancedEndPoint(endPointConfig, paramset));
        }

        private ManagementOperationContext SetAzureLoadBalancedEndPoint(AzureEndPointConfigInfo endPointConfig, AzureEndPointConfigInfo.ParameterSet paramset)
        {
            if (null != endPointConfig)
            {
                return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureLoadBalancedEndpointCmdletInfo(endPointConfig, paramset));
            }
            return null;
        }

        public PersistentVMRoleContext RemoveAzureEndPoint(string epName, PersistentVMRoleContext vmRoleCtxt)
        {
            return RunPSCmdletAndReturnFirst<PersistentVMRoleContext>(new RemoveAzureEndpointCmdletInfo(epName, vmRoleCtxt));
        }

        public PersistentVM RemoveAzureEndPoint(string epName, PersistentVM vm)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new RemoveAzureEndpointCmdletInfo(epName, vm));
        }

        public void RemoveEndPoint(string vmName, string serviceName, string [] epNames)
        {
            PersistentVMRoleContext vmRoleCtxt = GetAzureVM(vmName, serviceName);

            foreach (string ep in epNames)
            {
                vmRoleCtxt.VM = RemoveAzureEndPoint(ep, vmRoleCtxt).VM;
            }
            UpdateAzureVM(vmName, serviceName, vmRoleCtxt.VM);
        }

        #endregion

        #region AzureOSDisk

        public PersistentVM SetAzureOSDisk(HostCaching hc, PersistentVM vm)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureOSDiskCmdletInfo(hc, vm));
        }

        public OSVirtualHardDisk GetAzureOSDisk(PersistentVM vm)
        {
            return RunPSCmdletAndReturnFirst<OSVirtualHardDisk>(new GetAzureOSDiskCmdletInfo(vm));
        }

        #endregion

        #region AzureRole

        public ManagementOperationContext SetAzureRole(string serviceName, string slot, string roleName, int count)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureRoleCmdletInfo(serviceName, slot, roleName, count));
        }

        public Collection<RoleContext> GetAzureRole(string serviceName, string slot, string roleName, bool details)
        {
            return RunPSCmdletAndReturnAll<RoleContext>(new GetAzureRoleCmdletInfo(serviceName, slot, roleName, details));
        }

        #endregion

        #region AzureQuickVM

        public ManagementOperationContext NewAzureQuickVM(OS os, string name, string serviceName, string imageName, string userName, string password, string locationName, InstanceSize? instanceSize)
        {
            ManagementOperationContext result = new ManagementOperationContext();
            try
            {
                result = RunPSCmdletAndReturnFirst<ManagementOperationContext>(new NewAzureQuickVMCmdletInfo(os, name, serviceName, imageName, userName, password, locationName, instanceSize));
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("409"))
                {
                    Utilities.RetryActionUntilSuccess(
                        () => result = RunPSCmdletAndReturnFirst<ManagementOperationContext>(new NewAzureQuickVMCmdletInfo(os, name, serviceName, imageName, userName, password, null, instanceSize)),
                        "409", 4, 60);
                }
                else
                {
                    Console.WriteLine(e.InnerException.ToString());
                    throw;
                }
            }
            return result;
        }

        public ManagementOperationContext NewAzureQuickVM(OS os, string name, string serviceName, string imageName, string userName, string password, string locationName = null)
        {
            return NewAzureQuickVM(os, name, serviceName, imageName, userName, password, locationName, null);
        }

        #endregion

        #region AzurePlatformVMImage


        internal ManagementOperationContext SetAzurePlatformVMImageReplicate(string imageName, string[] locations)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new  SetAzurePlatformVMImageCmdletInfo(imageName, null, locations));
        }

        internal ManagementOperationContext SetAzurePlatformVMImagePublic(string imageName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzurePlatformVMImageCmdletInfo(imageName, "Public", null));
        }

        internal ManagementOperationContext SetAzurePlatformVMImagePrivate(string imageName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzurePlatformVMImageCmdletInfo(imageName, "Private", null));
        }

        internal OSImageDetailsContext GetAzurePlatformVMImage(string imageName)
        {
            return RunPSCmdletAndReturnFirst<OSImageDetailsContext>(new GetAzurePlatformVMImageCmdletInfo(imageName));
        }

        internal ManagementOperationContext RemoveAzurePlatformVMImage(string imageName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzurePlatformVMImageCmdletInfo(imageName));
        }

        #endregion

        #region AzurePublishSettingsFile

        internal void ImportAzurePublishSettingsFile()
        {
            this.ImportAzurePublishSettingsFile(CredentialHelper.PublishSettingsFile);
        }

        internal void ImportAzurePublishSettingsFile(string publishSettingsFile)
        {
            ImportAzurePublishSettingsFileCmdletInfo importAzurePublishSettingsFileCmdlet = new ImportAzurePublishSettingsFileCmdletInfo(publishSettingsFile);

            WindowsAzurePowershellCmdlet importAzurePublishSettingsFile = new WindowsAzurePowershellCmdlet(importAzurePublishSettingsFileCmdlet);
            var i = importAzurePublishSettingsFile.Run();
            Console.WriteLine(i.ToString());
        }

        #endregion

        #region AzureSubscription

        public Collection<SubscriptionData> GetAzureSubscription()
        {
            return RunPSCmdletAndReturnAll<SubscriptionData>(new GetAzureSubscriptionCmdletInfo());
        }

        public SubscriptionData GetCurrentAzureSubscription()
        {
            Collection<SubscriptionData> subscriptions = GetAzureSubscription();
            foreach (SubscriptionData subscription in subscriptions)
            {
                if (subscription.IsDefault)
                {
                    return subscription;
                }
            }
            return null;
        }

        public SubscriptionData SetAzureSubscription(string subscriptionName, string currentStorageAccount)
        {
            SetAzureSubscriptionCmdletInfo setAzureSubscriptionCmdlet = new SetAzureSubscriptionCmdletInfo(subscriptionName, currentStorageAccount);
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(setAzureSubscriptionCmdlet);
            azurePowershellCmdlet.Run();

            Collection<SubscriptionData> subscriptions = GetAzureSubscription();
            foreach (SubscriptionData subscription in subscriptions)
            {
                if (subscription.SubscriptionName == subscriptionName)
                {
                    return subscription;
                }
            }
            return null;
        }

        public SubscriptionData SetDefaultAzureSubscription(string subscriptionName)
        {
            SelectAzureSubscriptionCmdletInfo selectAzureSubscriptionCmdlet = new SelectAzureSubscriptionCmdletInfo(subscriptionName);
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(selectAzureSubscriptionCmdlet);
            azurePowershellCmdlet.Run();

            Collection<SubscriptionData> subscriptions = GetAzureSubscription();
            foreach (SubscriptionData subscription in subscriptions)
            {
                if (subscription.SubscriptionName == subscriptionName)
                {
                    return subscription;
                }
            }
            return null;
        }

        public bool SelectAzureSubscription(string subscriptionName, bool clear = false, string subscriptionDataFile = null)
        {
            return RunPSCmdletAndReturnFirst<bool>(new SelectAzureSubscriptionCmdletInfo(subscriptionName, clear, subscriptionDataFile));
        }

        #endregion

        #region AzureSubnet

        public SubnetNamesCollection GetAzureSubnet(PersistentVM vm)
        {
            GetAzureSubnetCmdletInfo getAzureSubnetCmdlet = new GetAzureSubnetCmdletInfo(vm);
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(getAzureSubnetCmdlet);
            Collection <PSObject> result = azurePowershellCmdlet.Run();

            SubnetNamesCollection subnets = new SubnetNamesCollection();
            foreach (PSObject re in result)
            {
                subnets.Add((string)re.BaseObject);
            }
            return subnets;
        }

        public PersistentVM SetAzureSubnet(PersistentVM vm, string [] subnetNames)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureSubnetCmdletInfo(vm, subnetNames));
        }

        #endregion

        #region AzureStorageAccount

        public ManagementOperationContext NewAzureStorageAccount(string storageName, string locationName, string affinity, string label, string description)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new NewAzureStorageAccountCmdletInfo(storageName, locationName, affinity, label, description));
        }

        public StorageServicePropertiesOperationContext NewAzureStorageAccount(string storageName, string locationName)
        {
            NewAzureStorageAccount(storageName, locationName, null, null, null);

            Collection<StorageServicePropertiesOperationContext> storageAccounts = GetAzureStorageAccount(null);
            foreach (StorageServicePropertiesOperationContext storageAccount in storageAccounts)
            {
                if (storageAccount.StorageAccountName == storageName)
                    return storageAccount;
            }
            return null;
        }

        public Collection<StorageServicePropertiesOperationContext> GetAzureStorageAccount(string accountName)
        {
            return RunPSCmdletAndReturnAll<StorageServicePropertiesOperationContext>(new GetAzureStorageAccountCmdletInfo(accountName));
        }

        public ManagementOperationContext SetAzureStorageAccount(string accountName, string label, string description, bool? geoReplication)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureStorageAccountCmdletInfo(accountName, label, description, geoReplication));
        }

        public void RemoveAzureStorageAccount(string storageAccountName)
        {
            Utilities.RetryActionUntilSuccess(
                () => RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureStorageAccountCmdletInfo(storageAccountName)),
                "409", 3, 60);
        }

        #endregion

        #region AzureStorageKey

        public StorageServiceKeyOperationContext GetAzureStorageAccountKey(string stroageAccountName)
        {
            return RunPSCmdletAndReturnFirst<StorageServiceKeyOperationContext>(new GetAzureStorageKeyCmdletInfo(stroageAccountName));
        }

        public StorageServiceKeyOperationContext NewAzureStorageAccountKey(string stroageAccountName, string keyType)
        {
            return RunPSCmdletAndReturnFirst<StorageServiceKeyOperationContext>(new NewAzureStorageKeyCmdletInfo(stroageAccountName, keyType));
        }

        #endregion

        #region AzureService

        public ManagementOperationContext NewAzureService(string serviceName, string location)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new NewAzureServiceCmdletInfo(serviceName, location));
        }

        internal void NewAzureService(string serviceName, string serviceLabel, string locationName)
        {
            NewAzureServiceCmdletInfo newAzureServiceCmdletInfo = new NewAzureServiceCmdletInfo(serviceName, serviceLabel, locationName);
            WindowsAzurePowershellCmdlet newAzureServiceCmdlet = new WindowsAzurePowershellCmdlet(newAzureServiceCmdletInfo);

            Collection<PSObject> result = newAzureServiceCmdlet.Run();
        }

        public bool RemoveAzureService(string serviceName)
        {
            bool result = false;
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<bool>(new RemoveAzureServiceCmdletInfo(serviceName)),
                "ConflictError", 3, 60);
            return result;
        }

        public HostedServiceDetailedContext GetAzureService(string serviceName)
        {
            return RunPSCmdletAndReturnFirst<HostedServiceDetailedContext>(new GetAzureServiceCmdletInfo(serviceName));
        }

        #endregion

        #region AzureServiceDiagnosticsExtension

        // New-AzureServiceDiagnosticsExtensionConfig
        public ExtensionConfigurationInput NewAzureServiceDiagnosticsExtensionConfig(string storage, XmlDocument config = null, string[] roles = null)
        {
            return RunPSCmdletAndReturnFirst<ExtensionConfigurationInput>(new NewAzureServiceDiagnosticsExtensionConfigCmdletInfo(storage, config, roles));
        }

        public ExtensionConfigurationInput NewAzureServiceDiagnosticsExtensionConfig
            (string storage, X509Certificate2 cert, XmlDocument config = null, string[] roles = null)
        {
            return RunPSCmdletAndReturnFirst<ExtensionConfigurationInput>
                (new NewAzureServiceDiagnosticsExtensionConfigCmdletInfo(storage, cert, config, roles));
        }

        public ExtensionConfigurationInput NewAzureServiceDiagnosticsExtensionConfig
            (string storage, string thumbprint, string algorithm = null, XmlDocument config = null, string[] roles = null)
        {
            return RunPSCmdletAndReturnFirst<ExtensionConfigurationInput>
                (new NewAzureServiceDiagnosticsExtensionConfigCmdletInfo(storage, thumbprint, algorithm, config, roles));
        }

        // Set-AzureServiceDiagnosticsExtension
        public ManagementOperationContext SetAzureServiceDiagnosticsExtension
            (string service, string storage, XmlDocument config = null, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureServiceDiagnosticsExtensionCmdletInfo(service, storage, config, roles, slot));
        }

        public ManagementOperationContext SetAzureServiceDiagnosticsExtension(string service, string storage, X509Certificate2 cert, XmlDocument config = null, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureServiceDiagnosticsExtensionCmdletInfo(service, storage, cert, config, roles, slot));
        }

        public ManagementOperationContext SetAzureServiceDiagnosticsExtension(string service, string storage, string thumbprint, string algorithm = null, XmlDocument config = null, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureServiceDiagnosticsExtensionCmdletInfo(service, storage, thumbprint, algorithm, config, roles, slot));
        }

        // Get-AzureServiceDiagnosticsExtension
        public Collection <DiagnosticExtensionContext> GetAzureServiceDiagnosticsExtension(string serviceName, string slot = null)
        {
            return RunPSCmdletAndReturnAll<DiagnosticExtensionContext>(new GetAzureServiceDiagnosticsExtensionCmdletInfo(serviceName, slot));
        }

        // Remove-AzureServiceDiagnosticsExtension
        public ManagementOperationContext RemoveAzureServiceDiagnosticsExtension(string serviceName, bool uninstall = false, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureServiceDiagnosticsExtensionCmdletInfo(serviceName, uninstall, roles, slot));
        }

        #endregion

        #region AzureServiceRemoteDesktopExtension

        // New-AzureServiceRemoteDesktopExtensionConfig
        public ExtensionConfigurationInput NewAzureServiceRemoteDesktopExtensionConfig(PSCredential cred, DateTime? exp = null, string[] roles = null)
        {
            return RunPSCmdletAndReturnFirst<ExtensionConfigurationInput>(new NewAzureServiceRemoteDesktopExtensionConfigCmdletInfo(cred, exp, roles));
        }

        public ExtensionConfigurationInput NewAzureServiceRemoteDesktopExtensionConfig(PSCredential cred, X509Certificate2 cert, string alg = null, DateTime? exp = null, string[] roles = null)
        {
            return RunPSCmdletAndReturnFirst<ExtensionConfigurationInput>(new NewAzureServiceRemoteDesktopExtensionConfigCmdletInfo(cred, cert, alg, exp, roles));
        }

        public ExtensionConfigurationInput NewAzureServiceRemoteDesktopExtensionConfig(PSCredential cred, string thumbprint, string algorithm = null, DateTime? exp = null, string[] roles = null)
        {
            return RunPSCmdletAndReturnFirst<ExtensionConfigurationInput>(new NewAzureServiceRemoteDesktopExtensionConfigCmdletInfo(cred, thumbprint, algorithm, exp, roles));
        }

        // Set-AzureServiceRemoteDesktopExtension
        public ManagementOperationContext SetAzureServiceRemoteDesktopExtension(string serviceName, PSCredential cred, DateTime? exp = null, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureServiceRemoteDesktopExtensionCmdletInfo(serviceName, cred, exp, roles, slot));
        }

        public ManagementOperationContext SetAzureServiceRemoteDesktopExtension(string serviceName, PSCredential credential, X509Certificate2 cert, DateTime? expiration = null, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureServiceRemoteDesktopExtensionCmdletInfo(serviceName, credential, cert, expiration, roles, slot));
        }

        public ManagementOperationContext SetAzureServiceRemoteDesktopExtension(string serviceName, PSCredential credential, string thumbprint, string algorithm = null, DateTime? expiration = null, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureServiceRemoteDesktopExtensionCmdletInfo(serviceName, credential, thumbprint, algorithm, expiration, roles, slot));
        }

        // Get-AzureServiceRemoteDesktopExtension
        public Collection <RemoteDesktopExtensionContext> GetAzureServiceRemoteDesktopExtension(string serviceName, string slot = null)
        //public RemoteDesktopExtensionContext GetAzureServiceRemoteDesktopExtension(string serviceName, string slot = null)
        {
            return RunPSCmdletAndReturnAll<RemoteDesktopExtensionContext>(new GetAzureServiceRemoteDesktopExtensionCmdletInfo(serviceName, slot));
            //return RunPSCmdletAndReturnFirst<RemoteDesktopExtensionContext>(new GetAzureServiceRemoteDesktopExtensionCmdletInfo(serviceName, slot));
        }

        // Remove-AzureServiceRemoteDesktopExtension
        public ManagementOperationContext RemoveAzureServiceRemoteDesktopExtension(string serviceName, bool uninstall = false, string[] roles = null, string slot = null)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureServiceRemoteDesktopExtensionCmdletInfo(serviceName, uninstall, roles, slot));
        }

        #endregion

        #region AzureVM

        internal Collection<ManagementOperationContext> NewAzureVM(string serviceName, PersistentVM[] VMs, string location  = null)
        {
            return NewAzureVM(serviceName, VMs, null, null, null, null, null, null, location);
        }

        internal Collection<ManagementOperationContext> NewAzureVM(string serviceName, PersistentVM[] vms, string vnetName, DnsServer[] dnsSettings,
            string serviceLabel, string serviceDescription, string deploymentLabel, string deploymentDescription, string location =null, string affinityGroup = null)
        {
            Collection<ManagementOperationContext> result = new Collection<ManagementOperationContext>();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnAll<ManagementOperationContext>(new NewAzureVMCmdletInfo(serviceName, vms, vnetName, dnsSettings, serviceLabel, serviceDescription, deploymentLabel, deploymentDescription, location, affinityGroup)),
                "409", 5, 60);
            return result;
        }

        public PersistentVMRoleContext GetAzureVM(string vmName, string serviceName)
        {
            return RunPSCmdletAndReturnFirst<PersistentVMRoleContext>(new GetAzureVMCmdletInfo(vmName, serviceName));
        }

        public ManagementOperationContext RemoveAzureVM(string vmName, string serviceName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureVMCmdletInfo(vmName, serviceName));
        }

        public ManagementOperationContext StartAzureVM(string vmName, string serviceName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new StartAzureVMCmdletInfo(vmName, serviceName));
        }

        public ManagementOperationContext StopAzureVM(PersistentVM vm, string serviceName, bool stay = false, bool force = false)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new StopAzureVMCmdletInfo(vm, serviceName, stay, force));
        }

        public ManagementOperationContext StopAzureVM(string vmName, string serviceName, bool stay = false, bool force = false)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new StopAzureVMCmdletInfo(vmName, serviceName, stay, force));
        }

        public void RestartAzureVM(string vmName, string serviceName)
        {
            RestartAzureVMCmdletInfo restartAzureVMCmdlet = new RestartAzureVMCmdletInfo(vmName, serviceName);
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(restartAzureVMCmdlet);
            azurePowershellCmdlet.Run();
        }

        public PersistentVMRoleContext ExportAzureVM(string vmName, string serviceName, string path)
        {
            return RunPSCmdletAndReturnFirst<PersistentVMRoleContext>(new ExportAzureVMCmdletInfo(vmName, serviceName, path));
        }

        public Collection<PersistentVM> ImportAzureVM(string path)
        {
            return RunPSCmdletAndReturnAll<PersistentVM>(new ImportAzureVMCmdletInfo(path));
        }

        public ManagementOperationContext UpdateAzureVM(string vmName, string serviceName, PersistentVM persistentVM)
        {
            ManagementOperationContext result = new ManagementOperationContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<ManagementOperationContext>(new UpdateAzureVMCmdletInfo(vmName, serviceName, persistentVM)),
                "409", 3, 60);
            return result;
        }

        #endregion

        #region AzureVMImage

        public OSImageContext AddAzureVMImage(string imageName, string mediaLocation, OS os, string label = null)
        {
            OSImageContext result = new OSImageContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<OSImageContext>(new AddAzureVMImageCmdletInfo(imageName, mediaLocation, os, label)),
                "409", 3, 60);
            return result;
        }

        public OSImageContext AddAzureVMImage(string imageName, string mediaLocation, OS os, InstanceSize recommendedSize)
        {
            OSImageContext result = new OSImageContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<OSImageContext>(new AddAzureVMImageCmdletInfo(imageName, mediaLocation, os, null, recommendedSize)),
                "409", 3, 60);
            return result;
        }

        public OSImageContext UpdateAzureVMImage(string imageName, string label)
        {
            return RunPSCmdletAndReturnFirst<OSImageContext>(new UpdateAzureVMImageCmdletInfo(imageName, label));
        }

        public OSImageContext UpdateAzureVMImage(string imageName, InstanceSize recommendedSize)
        {
            return RunPSCmdletAndReturnFirst<OSImageContext>(new UpdateAzureVMImageCmdletInfo(imageName, null, recommendedSize));
        }

        public ManagementOperationContext RemoveAzureVMImage(string imageName, bool deleteVhd = false)
        {
            ManagementOperationContext result = new ManagementOperationContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureVMImageCmdletInfo(imageName, deleteVhd)),
                "409", 3, 60);
            return result;
        }

        public void SaveAzureVMImage(string serviceName, string vmName, string newVmName, string newImageName = null)
        {
            RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SaveAzureVMImageCmdletInfo(serviceName, vmName, newVmName, newImageName));
        }

        public Collection<OSImageContext> GetAzureVMImage(string imageName = null)
        {
            return RunPSCmdletAndReturnAll<OSImageContext>(new GetAzureVMImageCmdletInfo(imageName));
        }

        public string GetAzureVMImageName(string[] keywords, bool exactMatch = true)
        {
            Collection<OSImageContext> vmImages = GetAzureVMImage();
            foreach (OSImageContext image in vmImages)
            {
                if (Utilities.MatchKeywords(image.ImageName, keywords, exactMatch) >= 0)
                    return image.ImageName;
            }
            foreach (OSImageContext image in vmImages)
            {
                if (Utilities.MatchKeywords(image.OS, keywords, exactMatch) >= 0)
                    return image.ImageName;
            }
            return null;
        }

        #endregion

        #region AzureVhd

        public string AddAzureVhdStop(FileInfo localFile, string destination, int ms)
        {
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(new AddAzureVhdCmdletInfo(destination, localFile.FullName, null, false, null));
            return azurePowershellCmdlet.RunAndStop(ms).ToString();
        }

        public VhdUploadContext AddAzureVhd(FileInfo localFile, string destination)
        {
            return AddAzureVhd(localFile, destination, null);
        }

        public VhdUploadContext AddAzureVhd(FileInfo localFile, string destination, string baseImage)
        {
            return AddAzureVhd(localFile, destination, null, false, baseImage);
        }

        public VhdUploadContext AddAzureVhd(FileInfo localFile, string destination, bool overwrite)
        {
            return AddAzureVhd(localFile, destination, null, overwrite);
        }

        public VhdUploadContext AddAzureVhd(FileInfo localFile, string destination, int? numberOfUploaderThreads, bool overWrite = false, string baseImage = null)
        {
            VhdUploadContext result = new VhdUploadContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<VhdUploadContext>(new AddAzureVhdCmdletInfo(destination, localFile.FullName, numberOfUploaderThreads, overWrite, baseImage)),
                "pipeline is already running", 3, 30);
            return result;
        }

        public VhdDownloadContext SaveAzureVhd(Uri source, FileInfo localFilePath, int? numThreads, string storageKey, bool overwrite)
        {
            VhdDownloadContext result = new VhdDownloadContext();
            Utilities.RetryActionUntilSuccess(
                () => result = RunPSCmdletAndReturnFirst<VhdDownloadContext>(new SaveAzureVhdCmdletInfo(source, localFilePath, numThreads, storageKey, overwrite)),
                "pipeline is already running", 3, 30);
            return result;
        }

        public string SaveAzureVhdStop(Uri source, FileInfo localFilePath, int? numThreads, string storageKey, bool overwrite, int ms)
        {
            SaveAzureVhdCmdletInfo saveAzureVhdCmdletInfo = new SaveAzureVhdCmdletInfo(source, localFilePath, numThreads, storageKey, overwrite);
            WindowsAzurePowershellCmdlet azurePowershellCmdlet = new WindowsAzurePowershellCmdlet(saveAzureVhdCmdletInfo);
            return azurePowershellCmdlet.RunAndStop(ms).ToString();
        }

        #endregion

        #region AzureVnetConfig

        public Collection<VirtualNetworkConfigContext> GetAzureVNetConfig(string filePath)
        {
            return RunPSCmdletAndReturnAll<VirtualNetworkConfigContext>(new GetAzureVNetConfigCmdletInfo(filePath));
        }

        public ManagementOperationContext SetAzureVNetConfig(string filePath)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureVNetConfigCmdletInfo(filePath));
        }

        public ManagementOperationContext RemoveAzureVNetConfig()
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureVNetConfigCmdletInfo());
        }

        #endregion

        #region AzureVNetGateway

        public ManagementOperationContext NewAzureVNetGateway(string vnetName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new NewAzureVNetGatewayCmdletInfo(vnetName));
        }

        public Collection <VirtualNetworkGatewayContext> GetAzureVNetGateway(string vnetName)
        {
            return RunPSCmdletAndReturnAll<VirtualNetworkGatewayContext>(new GetAzureVNetGatewayCmdletInfo(vnetName));
        }

        public ManagementOperationContext SetAzureVNetGateway(string option, string vnetName, string localNetwork)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new SetAzureVNetGatewayCmdletInfo(option, vnetName, localNetwork));
        }

        public ManagementOperationContext RemoveAzureVNetGateway(string vnetName)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new RemoveAzureVNetGatewayCmdletInfo(vnetName));
        }

        public SharedKeyContext GetAzureVNetGatewayKey(string vnetName, string localnet)
        {
            return RunPSCmdletAndReturnFirst<SharedKeyContext>(new GetAzureVNetGatewayKeyCmdletInfo(vnetName, localnet));
        }

        #endregion

        #region AzureVNet

        public Collection<GatewayConnectionContext> GetAzureVNetConnection(string vnetName)
        {
            return RunPSCmdletAndReturnAll<GatewayConnectionContext>(new GetAzureVNetConnectionCmdletInfo(vnetName));
        }

        public Collection<VirtualNetworkSiteContext> GetAzureVNetSite(string vnetName)
        {
            return RunPSCmdletAndReturnAll<VirtualNetworkSiteContext>(new GetAzureVNetSiteCmdletInfo(vnetName));
        }

        #endregion

        public ManagementOperationContext GetAzureRemoteDesktopFile(string vmName, string serviceName, string localPath, bool launch)
        {
            return RunPSCmdletAndReturnFirst<ManagementOperationContext>(new GetAzureRemoteDesktopFileCmdletInfo(vmName, serviceName, localPath, launch));
        }

        internal PersistentVM GetPersistentVM(PersistentVMConfigInfo configInfo)
        {
            PersistentVM vm = null;

            if (null != configInfo)
            {
                if (configInfo.VmConfig != null)
                {
                    vm = NewAzureVMConfig(configInfo.VmConfig);
                }

                if (configInfo.ProvConfig != null)
                {
                    configInfo.ProvConfig.Vm = vm;
                    vm = AddAzureProvisioningConfig(configInfo.ProvConfig);
                }

                if (configInfo.DiskConfig != null)
                {
                    configInfo.DiskConfig.Vm = vm;
                    vm = AddAzureDataDisk(configInfo.DiskConfig);
                }

                if (configInfo.EndPointConfig != null)
                {
                    configInfo.EndPointConfig.Vm = vm;
                    vm = AddAzureEndPoint(configInfo.EndPointConfig);
                }
            }

            return vm;
        }

        internal void AddVMDataDisks(string vmName, string serviceName, AddAzureDataDiskConfig[] diskConfig)
        {
            PersistentVMRoleContext vmRolectx = GetAzureVM(vmName, serviceName);

            foreach (AddAzureDataDiskConfig discCfg in diskConfig)
            {
                discCfg.Vm = vmRolectx.VM;
                vmRolectx.VM = AddAzureDataDisk(discCfg);
            }

            UpdateAzureVM(vmName, serviceName, vmRolectx.VM);
        }

        internal void SetVMDataDisks(string vmName, string serviceName, SetAzureDataDiskConfig[] diskConfig)
        {
            PersistentVMRoleContext vmRolectx = GetAzureVM(vmName, serviceName);

            foreach (SetAzureDataDiskConfig discCfg in diskConfig)
            {
                discCfg.Vm = vmRolectx.VM;
                vmRolectx.VM = SetAzureDataDisk(discCfg);
            }

            UpdateAzureVM(vmName, serviceName, vmRolectx.VM);
        }

        internal void SetVMSize(string vmName, string serviceName, SetAzureVMSizeConfig vmSizeConfig)
        {
            PersistentVMRoleContext vmRolectx = GetAzureVM(vmName, serviceName);

            vmSizeConfig.Vm = vmRolectx.VM;
            vmRolectx.VM = SetAzureVMSize(vmSizeConfig);

            UpdateAzureVM(vmName, serviceName, vmRolectx.VM);
        }

        public PersistentVM SetAzureVMSize(SetAzureVMSizeConfig sizeCfg)
        {
            return RunPSCmdletAndReturnFirst<PersistentVM>(new SetAzureVMSizeCmdletInfo(sizeCfg));
        }

        internal void AddVMDataDisksAndEndPoint(string vmName, string serviceName, AddAzureDataDiskConfig[] dataDiskConfig, AzureEndPointConfigInfo endPointConfig)
        {
            AddVMDataDisks(vmName, serviceName, dataDiskConfig);

            AddEndPoint(vmName, serviceName, new [] {endPointConfig});
        }

        public void RemoveAzureSubscriptions()
        {
            // Remove all subscriptions.  SAS Uri should work without a subscription.
            try
            {
                RunPSScript("Get-AzureSubscription | Remove-AzureSubscription -Force");
            }
            catch
            {
                Console.WriteLine("Subscriptions cannot be removed");
            }

            // Check if all subscriptions are removed.
            try
            {
                Assert.AreEqual(0, GetAzureSubscription().Count, "Subscription was not removed");
            }
            catch (Exception e)
            {
                if (e is AssertFailedException)
                {
                    throw;
                }
            }
        }

        public void RemoveAzureSubscription(string subscriptionName, bool force)
        {
            RemoveAzureSubscriptionCmdletInfo removeAzureSubscriptionCmdletInfo = new RemoveAzureSubscriptionCmdletInfo(subscriptionName, null, force);
            WindowsAzurePowershellCmdlet removeAzureSubscriptionCmdlet = new WindowsAzurePowershellCmdlet(removeAzureSubscriptionCmdletInfo);

            var result = removeAzureSubscriptionCmdlet.Run();
        }

        internal NetworkAclObject NewAzureAclConfig()
        {
            return RunPSCmdletAndReturnFirst<NetworkAclObject>(new NewAzureAclConfigCmdletInfo());
        }

        // Set-AzureAclConfig -AddRule -ACL $acl2 -Order 100 -Action Deny -RemoteSubnet "172.0.0.0/8" -Description "notes3"
         //   vmPowershellCmdlets.SetAzureAclConfig(SetACLConfig.AddRule, aclObj, 100, ACLAction.Permit,  "172.0.0.0//8", "Desc");
        internal void SetAzureAclConfig(SetACLConfig aclConfig, NetworkAclObject aclObj, int order, ACLAction aclAction, string remoteSubnet, string desc)
        {
            SetAzureAclConfigCmdletInfo setAzureAclConfigCmdletInfo = new SetAzureAclConfigCmdletInfo(aclConfig.ToString(), aclObj, order, aclAction.ToString(), remoteSubnet, desc, null);

            WindowsAzurePowershellCmdlet setAzureAclConfigCmdlet = new WindowsAzurePowershellCmdlet(setAzureAclConfigCmdletInfo);

            var result = setAzureAclConfigCmdlet.Run();
        }
    }
}
