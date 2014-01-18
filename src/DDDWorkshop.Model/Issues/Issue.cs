using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDDWorkshop.Model.Issues
{
    public class Product : AggregateRootEntity    
    {
        public static readonly Func<Product> Factory = () => new Product();

        private ProductId _id;
        private TenantId _tenantId;

        private string name;
        private string description;

        private ProductManager productManager;
        private IssueAssigner issueAssigner;

        Product()
        {
            Register<ProductCreated>(e =>
            {
                _id = new ProductId (e.ProductId);
                _tenantId = new TenantId(e.TenantId);
                name = e.Name;
                description = e.Description;
                productManager = new ProductManager ( e.ProductManager);
                issueAssigner = new IssueAssigner(e.IssueAssigner);
            });
        }

        public Product(
            TenantId tenantId, 
            ProductId id, 
            string name, 
            string description, 
            ProductManager manager, 
            IssueAssigner assigner) : this()
        {
            ApplyChange(new ProductCreated(tenantId, id, name, description, manager, assigner));
        }

        public Issue ReportDefect(IssueId issueId,  string descrption, string summary)
        {
            return new Issue(_tenantId, issueId, _id, description, summary, IssueType.Defect);
        }

        public Issue RequestFeature(IssueId issueId, string descrption, string summary)
        {
            return new Issue(_tenantId, issueId, _id, description, summary, IssueType.Feature);
        }
    }

    public class ProductManager
    {
        private string p;

        public ProductManager(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }

        public override string ToString()
        {
            return p;
        }
    }

    public class IssueAssigner
    {
        private string p;

        public IssueAssigner(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }

        public override string ToString()
        {
            return p;
        }
    }

    public enum IssueType
    {
        Defect, Feature
    }

    public class Issue : AggregateRootEntity
    {
        public static readonly Func<Issue> Factory = () => new Issue();

        private IssueId _id;
        private ProductId _productId;
        private State _state;
        private string _description;
        private string _summary;

        private enum State
        {
            Pending, Confirmed
        }

        Issue()
        {
            Register<IssueCreated>(When);
            Register<IssueConfirmed>(When);
        }

        public Issue(TenantId tenantId, IssueId issueId, ProductId productId, string description, string summary, IssueType type)
            : this()
        {
            ApplyChange(new IssueCreated(tenantId, issueId, productId, description, summary, type));
        }

        private void When(IssueCreated e)
        {
            _id = new IssueId(e.issueId);
            _productId = new ProductId(e.productId);
            _state = State.Pending;
            _type = (IssueType)Enum.Parse(typeof(IssueType), e.issueType);
            _description = e.description;
            _summary = e.summary;
        }

        public void Confirm()
        {
            if (_state != State.Pending)
                throw new Exception("Issue is not pending, cannot confirm");
            ApplyChange(new IssueConfirmed(_id));
        }

        private void When(IssueConfirmed e)
        {
            _state = State.Confirmed;
        }       

        public IssueType _type { get; set; }
    }

    public class IssueId
    {
        private readonly string id;

        public IssueId(string id)
        {
            this.id = id;
        }

        public IssueId()
        {
            this.id = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    public class TenantId
    {
        private readonly string id;

        public TenantId(string id)
        {
            this.id = id;
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    public class ProductId
    {
        private readonly string id;

        public ProductId(string id)
        {
            this.id = id;
        }

        public ProductId()
        {
            this.id = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    public class IssueCreated
    {
        public string tenantId;
        public string issueId;
        public string productId;
        public string description;
        public string summary;
        public string issueType;

        public IssueCreated(
            TenantId tenantId1, 
            IssueId issueId1, 
            ProductId productId1, 
            string description1, 
            string summary1, 
            IssueType issueType)
        {
            // TODO: Complete member initialization
            this.tenantId = tenantId1.ToString();
            this.issueId = issueId1.ToString();
            this.productId = productId1.ToString();
            this.description = description1.ToString();
            this.summary = summary1.ToString();
            this.issueType = issueType.ToString();
        }
    }

    public class IssueConfirmed
    {
        public string IssueId;

        public IssueConfirmed(IssueId issueId)
        {         
            this.IssueId = issueId.ToString();
        }
    }

   

}
