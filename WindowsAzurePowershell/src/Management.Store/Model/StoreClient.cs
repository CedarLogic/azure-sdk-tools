﻿// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Management.Store.Model
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel.Channels;
    using Microsoft.Samples.WindowsAzure.ServiceManagement;
    using Microsoft.Samples.WindowsAzure.ServiceManagement.Marketplace.Contract;
    using Microsoft.Samples.WindowsAzure.ServiceManagement.Marketplace.ResourceModel;
    using Microsoft.Samples.WindowsAzure.ServiceManagement.Store.Contract;
    using Microsoft.Samples.WindowsAzure.ServiceManagement.Store.ResourceModel;
    using Microsoft.WindowsAzure.Management.Model;
    using Microsoft.WindowsAzure.Management.Store.Properties;
    using Microsoft.WindowsAzure.Management.Utilities;

    public class StoreClient
    {
        private IMarketplaceManagement marketplaceChannel;

        private IStoreManagement storeChannel;

        private string subscriptionId;

        const string StoreServicePrefix = "Azure-Stores";

        /// <summary>
        /// Parameterless constructor added for mocking framework.
        /// </summary>
        public StoreClient()
        {

        }

        /// <summary>
        /// Creates new instance from the store client.
        /// </summary>
        /// <param name="subscriptionId">The Windows Azure subscription id</param>
        /// <param name="storeEndpointUri">The service management endpoint uri</param>
        /// <param name="cert">The authentication certificate</param>
        /// <param name="logger">The logger for http request/response</param>
        public StoreClient(string subscriptionId, string storeEndpointUri, X509Certificate2 cert, Action<string> logger)
        {
            this.subscriptionId = subscriptionId;
            Binding storeBinding = ConfigurationConstants.WebHttpBinding(0);
            Binding marketplaceBinding = ConfigurationConstants.AnonymousWebHttpBinding();
            
            marketplaceChannel = ServiceManagementHelper.CreateServiceManagementChannel<IMarketplaceManagement>(
                marketplaceBinding,
                new Uri(Resources.MarketplaceEndpoint),
                new HttpRestMessageInspector(logger));

            if (!string.IsNullOrEmpty(storeEndpointUri) && !string.IsNullOrEmpty(subscriptionId) && cert != null)
            {
                storeChannel = ServiceManagementHelper.CreateServiceManagementChannel<IStoreManagement>(
                storeBinding,
                new Uri(storeEndpointUri),
                cert,
                new HttpRestMessageInspector(logger));
            }
        }

        /// <summary>
        /// Lists all available Windows Azure offers in the Marketplace.
        /// </summary>
        /// <param name="country">The country code</param>
        /// <returns>The available Windows Azure offers in Marketplace</returns>
        public virtual List<WindowsAzureOffer> GetAvailableWindowsAzureAddOns(string country)
        {
            List<WindowsAzureOffer> result = new List<WindowsAzureOffer>();
            List<Offer> offers = marketplaceChannel.ListWindowsAzureOffers();

            foreach (Offer offer in offers)
            {
                string plansQuery = string.Format("CountryCode eq '{0}'", country);
                List<Plan> plans = marketplaceChannel.ListOfferPlans(offer.Id.ToString(), plansQuery);

                if (plans.Count > 0)
                {
                    result.Add(new WindowsAzureOffer(offer, plans));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets add ons based on the passed filter.
        /// </summary>
        /// <param name="searchOptions">The add on search options</param>
        /// <returns>The list of filtered add ons</returns>
        public virtual List<WindowsAzureAddOn> GetAddOn(AddOnSearchOptions searchOptions = null)
        {
            List<WindowsAzureAddOn> addOns = new List<WindowsAzureAddOn>();
            CloudServiceList cloudServices = storeChannel.ListCloudServices(subscriptionId);
            List<CloudService> storeServices = cloudServices.FindAll(
                c => CultureInfo.CurrentCulture.CompareInfo.IsPrefix(c.Name, StoreServicePrefix));

            foreach (CloudService storeService in storeServices)
            {
                if (General.TryEquals(searchOptions.GeoRegion, storeService.GeoRegion))
                {
                    foreach (Resource resource in storeService.Resources)
                    {
                        if (General.TryEquals(searchOptions.Name, resource.Name) && 
                            General.TryEquals(searchOptions.Provider, resource.ResourceProviderNamespace))
                        {
                            addOns.Add(new WindowsAzureAddOn(resource, storeService.GeoRegion, storeService.Name));
                        }
                    }
                }
            }

            return addOns;
        }

        /// <summary>
        /// Removes given Add-On
        /// </summary>
        /// <param name="Name">The add-on name</param>
        public virtual void RemoveAddOn(string Name)
        {
            List<WindowsAzureAddOn> addOns = GetAddOn(new AddOnSearchOptions(Name, null, null));

            if (addOns.Count != 1)
	        {
		        throw new Exception("The Add on is not found");
	        }

            WindowsAzureAddOn addOn = addOns[0];

            storeChannel.DeleteResource(
                subscriptionId,
                addOn.CloudService,
                addOn.Type,
                addOn.AddOn,
                addOn.Name
            );
        }
    }
}
