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

namespace Microsoft.WindowsAzure.Management.Utilities.Common
{
    using System.Collections.Generic;
    using System.Net.Http.Headers;

    public class ArgumentConstants
    {
        public static Dictionary<LocationName, string> Locations { get; private set; }
        public static Dictionary<string, LocationName> ReverseLocations { get; private set; }
        public static Dictionary<SlotType, string> Slots { get; private set; }

        static ArgumentConstants()
        {
            Locations = new Dictionary<LocationName, string>()
            {
                { LocationName.AnywhereAsia, "anywhere asia" },
                { LocationName.AnywhereEurope, "anywhere europe" },
                { LocationName.AnywhereUS, "anywhere us" },
                { LocationName.EastAsia, "east asia" },
                { LocationName.NorthCentralUS, "north central us" },
                { LocationName.NorthEurope, "north europe" },
                { LocationName.SouthCentralUS, "south central us" },
                { LocationName.SouthEastAsia, "southeast asia" },
                { LocationName.WestEurope, "west europe" },
                { LocationName.EastUS, "east us" },
                { LocationName.WestUS, "west us" }
            };

            ReverseLocations = new Dictionary<string, LocationName>()
            {
                { "anywhere asia", LocationName.EastAsia },
                { "anywhere europe", LocationName.NorthEurope },
                { "anywhere us", LocationName.SouthCentralUS },
                { "east asia", LocationName.EastAsia },
                { "north central us", LocationName.NorthCentralUS },
                { "north europe", LocationName.NorthEurope },
                { "south central us", LocationName.SouthCentralUS },
                { "southeast asia", LocationName.SouthEastAsia },
                { "west europe", LocationName.WestEurope },
                { "west us", LocationName.WestUS },
                { "east us", LocationName.EastUS },
            };
            Slots = new Dictionary<SlotType, string>()
            {
                { SlotType.Production, "production" },
                { SlotType.Staging, "staging" }
            };
        }
    }

    public class SDKVersion
    {
        public const string Version180 = "1.8.0";
    }

    public enum LocationName
    {
        NorthCentralUS,
        AnywhereUS,
        AnywhereEurope,
        WestEurope,
        SouthCentralUS,
        NorthEurope,
        AnywhereAsia,
        SouthEastAsia,
        EastAsia,
        EastUS,
        WestUS
    }

    public enum SlotType
    {
        Production,
        Staging
    }

    public enum DevEnv
    {
        Local,
        Cloud
    }

    public enum RoleType
    {
        WebRole,
        WorkerRole
    }

    public enum RuntimeType
    {
        IISNode,
        Node,
        PHP,
        Cache,
        Null
    }

    public class ManagementConstants
    {
        public const string CurrentSubscriptionEnvironmentVariable = "_wappsCmdletsCurrentSubscription";

        public const string ServiceManagementNS = "http://schemas.microsoft.com/windowsazure";
    }

    public static class StorageServiceStatus
    {
        public const string ResolvingDns = "Suspending";
        public const string Created = "Created";
        public const string Creating = "Creating";
    }

    public static class ApiConstants
    {
        public static string DefaultApiVersion = "2011-03-01";
        public static string ResourceRegistrationApiVersion = "2012-08-01";

        public const string VersionHeaderName = "x-ms-version";
        public const string RequestHeaderName = "x-ms-request-id";
        public const string MarkerHeaderName = "x-ms-continuation-Marker";
        public const string TracingHeaderName = "x-ms-tracing";

        public const string AuthorizationHeaderName = "Authorization";

        public const string BasicAuthorization = "Basic";

        public const string TracingEventResponseHeaderPrefix = "TracingEvent_";

        public const string RunningState = "Running";
        public const string StoppedState = "Stopped";

        public const string CustomDomainsEnabledSettingsName = "CustomDomainsEnabled";
        public const string SslSupportSettingsName = "SslSupport";

        public const string UserAgentHeaderValue = "WindowsAzurePowershell/v0.6.12";
        public static ProductInfoHeaderValue UserAgentValue = new ProductInfoHeaderValue(
            "WindowsAzurePowershell",
            "v0.6.12");
    }

    public static class HttpConstants
    {
        public static readonly MediaTypeWithQualityHeaderValue JsonMediaType =
            MediaTypeWithQualityHeaderValue.Parse("application/json");

        public static readonly MediaTypeWithQualityHeaderValue XmlMediaType =
            MediaTypeWithQualityHeaderValue.Parse("application/xml");
    }
}
