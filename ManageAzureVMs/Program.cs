using Microsoft.Azure.Insights;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManageAzureVMs
{
    class Program
    {
        static string _subscriptionId = "f4263b41-406a-461b-8fcc-62523fc174aa";
        static string _tenantId = "458ca4db-d6bd-46dd-89b2-d64236082e22";
        static string _applicationId = "f9799f08-6cd8-4782-ab3e-f129e1cc7a00";
        static string _applicationSecret = "0L0cHSjH2vIKJCRsraIJwJX6aqPs/JR+mBvvcU4622M=";

        static void Main(string[] args)
        {
            Random r = new Random();
            var groupName = "myResourceGroup" + r.Next(10, 100); ;
            var subscriptionId = _subscriptionId;
            var storageName = "mystorageaccount" + r.Next(1, 1000);
            var ipName = "myPublicIP" + r.Next(10, 100);

            var token = GetAccessTokenAsync();
            var credential = new TokenCredentials(token.Result.AccessToken);
            var computeManagementClient = new ComputeManagementClient(credential) { SubscriptionId = subscriptionId };
        }

        private static async Task<AuthenticationResult> GetAccessTokenAsync()
        {
            var cc = new ClientCredential(_applicationId, _applicationSecret);
            AuthenticationContext authContext = new AuthenticationContext("https://login.windows.net/" + _tenantId);
            var token = await authContext.AcquireTokenAsync("https://management.azure.com/", cc);


            if (token == null)
            {
                throw new InvalidOperationException("Could not get the token");
            }
            return token;


        }

        public static dynamic CreateResourceGroupAsync(TokenCredentials credential, string groupName, string subscriptionId, string location)
        {
            var resourceManagementClient = new ResourceManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
            };


            var resourceGroup = new ResourceGroup { Location = location };
            var abc = resourceManagementClient.ResourceGroups.List(null);
            return abc;
        }

        public static async Task<StorageAccount> CreateStorageAccountAsync(TokenCredentials credential, string groupName, string subscriptionId, string location, string storageName)
        {
            var storageManagementClient = new StorageManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn"),

            };

            Console.WriteLine("Creating the storage account...");

            return await storageManagementClient.StorageAccounts.CreateAsync(groupName, storageName, new StorageAccountCreateParameters()
            {
                Sku = new Microsoft.Azure.Management.Storage.Models.Sku()
                { Name = SkuName.StandardLRS },
                Kind = Kind.Storage,
                Location = location
            });
        }

        public static async Task<List<VirtualMachineImageResource>> ListVirtualMachineImageAsync(TokenCredentials credential, string subscriptionId, string location)
        {
            var computeManagementClient = new ComputeManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                //BaseUri = new Uri("https://management.chinacloudapi.cn")
            };

            var resourcedisks = await computeManagementClient.Disks.ListAsync();

            List<VirtualMachineImageResource> virtualMachineImageResources = new List<VirtualMachineImageResource>();

            var VirtualMachineImages = await computeManagementClient.VirtualMachineImages.ListPublishersAsync(location);

            foreach (var VirtualMachineImage in VirtualMachineImages)
            {
                List<OfferResource> offerResources = new List<OfferResource>();
                var vmOffers = await computeManagementClient.VirtualMachineImages.ListOffersAsync(location, VirtualMachineImage.Name);

                foreach (var vmOffer in vmOffers)
                {

                    List<SKUResource> SKUResources = new List<SKUResource>();
                    var vmSkus = await computeManagementClient.VirtualMachineImages.ListSkusAsync(location, VirtualMachineImage.Name, vmOffer.Name);
                    foreach (var vmSku in vmSkus)
                    {
                        Console.WriteLine(vmSku.Name);
                        SKUResources.Add(new SKUResource()
                        {
                            SKUResourceId = vmSku.Id,
                            Name = vmSku.Name,
                            Location = vmSku.Location
                        });
                    }
                    offerResources.Add(new OfferResource()
                    {
                        OfferResourceId = vmOffer.Id,
                        Name = vmOffer.Name,
                        Location = vmOffer.Location,
                        SKUResources = SKUResources
                    });
                }

                virtualMachineImageResources.Add(new VirtualMachineImageResource()
                {
                    VirtualMachineImageResourceId = VirtualMachineImage.Id,
                    Location = VirtualMachineImage.Location,
                    Name = VirtualMachineImage.Name,
                    OfferResources = offerResources
                });
            }

            return virtualMachineImageResources;
        }

        public static async Task<PublicIPAddress> CreatePublicIPAddressAsync(TokenCredentials credential, string groupName, string subscriptionId, string location, string ipName)
        {
            var networkManagementClient = new NetworkManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn")
            };

            Console.WriteLine("Creating the public ip...");

            return await networkManagementClient.PublicIPAddresses.CreateOrUpdateAsync(groupName, ipName, new PublicIPAddress
            {
                Location = location,
                PublicIPAllocationMethod = "Dynamic"
            }
            );
        }

        public static async Task<VirtualNetwork> CreateVirtualNetworkAsync(TokenCredentials credential, string groupName, string subscriptionId, string location, string vnetName, string subnetName)
        {
            var networkManagementClient = new NetworkManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn")
            };

            var subnet = new Subnet
            {
                Name = subnetName,
                AddressPrefix = "10.0.0.0/24"
            };

            var address = new AddressSpace
            {
                AddressPrefixes = new List<string> { "10.0.0.0/16" }
            };

            Console.WriteLine("Creating the virtual network...");
            return await networkManagementClient.VirtualNetworks.CreateOrUpdateAsync(
              groupName,
              vnetName,
              new VirtualNetwork
              {
                  Location = location,
                  AddressSpace = address,
                  Subnets = new List<Subnet> { subnet }
              }
            );
        }

        public static async Task<NetworkInterface> CreateNetworkInterfaceAsync(TokenCredentials credential, string groupName, string subscriptionId, string location, string subnetName, string vnetName, string ipName, string nicName)
        {
            var networkManagementClient = new NetworkManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn")
            };
            var subnet = await networkManagementClient.Subnets.GetAsync(groupName, vnetName, subnetName
            );
            var publicIP = await networkManagementClient.PublicIPAddresses.GetAsync(
              groupName,
              ipName
            );

            Console.WriteLine("Creating the network interface...");

            return await networkManagementClient.NetworkInterfaces.CreateOrUpdateAsync(groupName, nicName, new NetworkInterface
            {
                Location = location,
                IpConfigurations = new List<NetworkInterfaceIPConfiguration> {
                    new NetworkInterfaceIPConfiguration
                    {
                    Name = nicName,
                    PublicIPAddress = publicIP,
                    Subnet = subnet
                    }
                  }
            }
            );
        }

        public static async Task<AvailabilitySet> CreateAvailabilitySetAsync(TokenCredentials credential, string groupName, string subscriptionId, string location, string avsetName)
        {
            var computeManagementClient = new ComputeManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn")
            };

            Console.WriteLine("Creating the availability set...");
            return await computeManagementClient.AvailabilitySets.CreateOrUpdateAsync(groupName, avsetName, new AvailabilitySet() { Location = location }
            );
        }

        public static async Task<VirtualMachine> CreateVirtualMachineAsync(TokenCredentials credential, string groupName, string subscriptionId, string location, string nicName, string avsetName, string storageName, string adminName, string adminPassword, string vmName)
        {
            var networkManagementClient = new NetworkManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn")
            };
            var computeManagementClient = new ComputeManagementClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.chinacloudapi.cn")
            };

            var nic = await networkManagementClient.NetworkInterfaces.GetAsync(
              groupName,
              nicName);
            var avSet = await computeManagementClient.AvailabilitySets.GetAsync(
              groupName,
              avsetName);

            Console.WriteLine("Creating the virtual machine...");
            return await computeManagementClient.VirtualMachines.CreateOrUpdateAsync(
              groupName,
              vmName,
              new VirtualMachine
              {
                  Location = location,
                  AvailabilitySet = new Microsoft.Azure.Management.Compute.Models.SubResource
                  {
                      Id = avSet.Id
                  },
                  HardwareProfile = new HardwareProfile
                  {
                      VmSize = "Standard_A0"
                  },
                  OsProfile = new OSProfile
                  {
                      AdminUsername = adminName,
                      AdminPassword = adminPassword,
                      ComputerName = vmName,
                      WindowsConfiguration = new WindowsConfiguration
                      {
                          ProvisionVMAgent = true
                      }
                  },
                  NetworkProfile = new NetworkProfile
                  {
                      NetworkInterfaces = new List<NetworkInterfaceReference>
                        {
                 new NetworkInterfaceReference { Id = nic.Id }
                        }
                  },
                  StorageProfile = new StorageProfile
                  {
                      ImageReference = new ImageReference
                      {
                          Publisher = "MicrosoftWindowsServer",
                          Offer = "WindowsServer",
                          Sku = "2012-R2-Datacenter",
                          Version = "latest"
                      },
                      OsDisk = new OSDisk
                      {
                          Name = "mytestod1",
                          CreateOption = DiskCreateOptionTypes.FromImage,
                          Vhd = new VirtualHardDisk
                          {
                              Uri = "http://" + storageName + ".blob.core.windows.net/vhds/mytestod1.vhd"
                          }
                      },
                      DataDisks = new List<DataDisk> { new DataDisk()
                      {
                          Name ="",
                          DiskSizeGB= 100,
                          ManagedDisk= new ManagedDiskParameters(),
                          Caching = CachingTypes.ReadWrite,
                          CreateOption = DiskCreateOptionTypes.Attach,
                          Image = new VirtualHardDisk()
                          {
                              Uri = ""
                          },
                          Lun = 0,
                          Vhd= new VirtualHardDisk()
                          {
                              Uri=""
                          }


                      }
                      }

                  }
              }
            );
        }
    }
}
