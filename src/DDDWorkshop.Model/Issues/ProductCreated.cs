using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDWorkshop.Model.Issues
{
    public class ProductCreated
    {
        public readonly string ProductId;
        public readonly string TenantId;
        public readonly string Name;
        public readonly string Description;
        public readonly string ProductManager;
        public readonly string IssueAssigner;        

        public ProductCreated(TenantId tenantId, Issues.ProductId id, string name, string description, ProductManager manager, IssueAssigner assigner)
        {
            // TODO: Complete member initialization
            this.TenantId = tenantId.ToString();
            this.ProductId = id.ToString();
            this.Name = name.ToString();
            this.Description = description.ToString();
            this.ProductManager = manager.ToString();
            this.IssueAssigner = assigner.ToString();
        }
    }
}
