using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManageAzureVMs
{
    public class OfferResource
    {
        public string OfferResourceId { get; set; }

        public string Location { get; set; }

        public string Name { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public List<SKUResource> SKUResources { get; set; }
    }
}
