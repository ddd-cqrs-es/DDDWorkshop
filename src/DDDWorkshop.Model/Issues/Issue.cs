using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDDWorkshop.Model.Issues
{
    public class ProductDefectiveness
    {
        public ProductId productId;
        public int Rank;

        public ProductDefectiveness(ProductId productId, int defectCount)
        {
            // TODO: Complete member initialization
            this.productId = productId;
            this.Rank = defectCount;
        }
    }

    public class ProductDefectivenessRanker
    {
        private readonly  List<Issue> repo = new List<Issue>();

        public ProductDefectivenessRanker(List<Issue> repo)
        {
            this.repo = repo;
        }

        public ProductDefectiveness MostDefectiveProductFrom(TenantId tenantId)
        {
            return AllDefectiveProductsFrom(tenantId).Last();
        }

        public List<ProductDefectiveness> AllDefectiveProductsFrom(TenantId tenantId)
        {
            var productIssues = repo.GroupBy(x => x.ProductId.id);
            
            int rank = 0;
            return productIssues
                .Select(x => new { ProductId = x.Key, NumberOfDefects = x.Count() })
                .OrderBy(x => x.NumberOfDefects)
                .Select(x => new ProductDefectiveness(new ProductId(x.ProductId), ++rank)).ToList();                         
        }
    }

    //sorted list most to least defective products
    //getMostDefectiveSinglePRoduct
    public class Product : AggregateRootEntity
    {
        public static readonly Func<Product> Factory = () => new Product();

        private ProductId _id;
        private TenantId _tenantId;

        private string name;
        private string description;

        private ProductManager productManager;
        private IssueAssigner issueAssigner;

        private Release release;

        Product()
        {
            Register<ProductCreated>(e =>
            {
                _id = new ProductId(e.ProductId);
                _tenantId = new TenantId(e.TenantId);
                name = e.Name;
                description = e.Description;
                productManager = new ProductManager(e.ProductManager);
                issueAssigner = new IssueAssigner(e.IssueAssigner);
            });
        }

        public Product(
            TenantId tenantId,
            ProductId id,
            string name,
            string description,
            ProductManager manager,
            IssueAssigner assigner)
            : this()
        {
            ApplyChange(new ProductCreated(tenantId, id, name, description, manager, assigner));
        }

        //determine stats
        //kloc / number of defects 

        public Release ScheduleRelease(string name, DefectStatistics stats)
        {
            return new Release(stats);
        }        

        public Issue ReportDefect(IssueId issueId, string descrption, string summary)
        {
            return new Issue(_tenantId, issueId, _id, description, summary, IssueType.Defect);
        }

        public Issue RequestFeature(IssueId issueId, string descrption, string summary)
        {
            return new Issue(_tenantId, issueId, _id, description, summary, IssueType.Feature);
        }
    }


    public class Release
    {
        public Release(DefectStatistics defectStatistics)
        {
            this.DefectStatistics = defectStatistics;
        }

        public ReleaseId Id { get; set; }

        public List<DefectDensity> Densities = new List<DefectDensity>();

        public DefectStatistics DefectStatistics { get; set; }

        public DefectDensity CalculateDefectDensity(KlocMEasurement measurement)
        {
            var density = DefectStatistics.CalculateDefectDensity(measurement);
            Densities.Add(density);
            return density;
        }
    }

    public class ReleaseId
    {
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

    public class DefectDensity : Measurement
    {
        private float p;

        public DefectDensity(float p)
        {
            this.p = p;
        }
    }

    public class Measurement
    {

    }

    public class KlocMEasurement : Measurement
    {
        public KlocMEasurement(float p)
        {
            this.Value = p;
        }

        public float Value { get; set; }
    }

    public class DefectStatistics
    {
        public int NumberReported;
        public int NumberFixed;
        public int NumberKnown;
        

        public DefectStatistics(int numberReported, int numberFixed, int numberKnown)
        {
            NumberReported = numberReported;
            NumberFixed = numberFixed;
            NumberKnown = numberKnown;
        }

        public DefectStatistics(List<Issue> AllIssues)
            : this(AllIssues.Count(), AllIssues.Count(x => x.IsFixed), AllIssues.Count(x => !x.IsFixed))
        {
           
        }

        public DefectStatistics IssueReported()
        {
            return new DefectStatistics(NumberReported, NumberFixed + 1, NumberKnown + 1);
        }

        public DefectStatistics IssueFixed()
        {
            return new DefectStatistics(NumberReported, NumberFixed + 1, NumberKnown - 1);
        }

        public DefectDensity CalculateDefectDensity(KlocMEasurement klock)
        {
            return new DefectDensity(NumberKnown / klock.Value);
        }
    }

    public class Issue : AggregateRootEntity
    {
        public static readonly Func<Issue> Factory = () => new Issue();

        private IssueId _id;
        public ProductId ProductId;
        private State _state;
        private string _description;
        private string _summary;

        private enum State
        {
            Pending, Confirmed, Fixed
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
            ProductId = new ProductId(e.productId);
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

        public void Fixed(string resolution)
        {
            ApplyChange(new IssueFixed(_id, resolution));
        }

        private void When(IssueConfirmed e)
        {
            _state = State.Confirmed;
        }

        public IssueType _type { get; set; }

        public IssueId Id { get { return _id; } }

        public void Resolve(string resolution)
        {
            _state = State.Fixed;
        }

        public bool IsFixed
        {
            get { return _state == State.Fixed; }
        }
    }

        public class IssueId
        {
            private readonly string id;

            public IssueId(string id)
            {
                this.id = id;
            }

            public override bool Equals(object obj)
            {
                var other = obj as IssueId;
                if (other == null) return false;
                return other.id == this.id;
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
            public readonly string id;

            public ProductId(string id)
            {
                this.id = id;
            }

            public ProductId()
            {
                this.id = Guid.NewGuid().ToString();
            }

            public override bool Equals(object obj)
            {
                var other = obj as ProductId;
                if (other == null) return false;
                return other.id == this.id;
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

   


