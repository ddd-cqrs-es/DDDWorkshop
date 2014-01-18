using AggregateSource;
using AggregateSource.EventStore;
using AggregateSource.EventStore.Resolvers;
using DDDWorkshop.Model.Issues;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DDDWorkshop
{
    class Program
    {
        static void Main()
        {
            //Make sure you start an instance of EventStore before running this!!
            var credentials = new UserCredentials("admin", "changeit");
            var connection = EventStoreConnection.Create(
                ConnectionSettings.Create().
                    UseConsoleLogger().
                    SetDefaultUserCredentials(
                        credentials),
                new IPEndPoint(IPAddress.Loopback, 1113),
                "EventStoreShopping");
            connection.Connect();



            var productUow = new UnitOfWork();
            var productRepository = new Repository<Product>(
                Product.Factory,
                productUow,
                connection,
                new EventReaderConfiguration(
                    new SliceSize(512),
                    new JsonDeserializer(),
                    new PassThroughStreamNameResolver(),
                    new FixedStreamUserCredentialsResolver(credentials)));

            var tenantId = new TenantId(Guid.NewGuid().ToString());
            var productId = new ProductId();
            productRepository.Add(productId.ToString(),
                new Product(tenantId,
                    productId,
                    "dolphin-tuna",
                    "non-dolphin free",
                    new ProductManager(Guid.NewGuid().ToString()),
                    new IssueAssigner(Guid.NewGuid().ToString())));

            var product = productRepository.Get(productId.ToString());

            List<Issue> issues = new List<Issue>();
            var issueId = new IssueId();
            issues.Add(product.ReportDefect(issueId, "shit be bad yo", "fo real"));
            issues.Add(product.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));

            DefectStatistics stats1 = new DefectStatistics(issues);

            var release1 = product.ScheduleRelease("new relased", stats1);



            var density = release1.CalculateDefectDensity(new KlocMEasurement(10));

            issues.Add(product.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));

            issues.First().Resolve("cool");

            DefectStatistics stats2 = new DefectStatistics(issues);

            var release2 = product.ScheduleRelease("new relased", stats2);

            var density2 = release2.CalculateDefectDensity(new KlocMEasurement(10));

            var product2 = productRepository.Get(productId.ToString());

            var issueIdForProduct2 = new IssueId();
            issues.Add(product2.ReportDefect(issueIdForProduct2, "shit be bad yo", "fo real"));
            issues.Add(product2.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product2.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product2.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));
            issues.Add(product2.ReportDefect(new IssueId(), "shit be bad yo", "fo real"));

            ProductDefectivenessRanker ranker = new ProductDefectivenessRanker(issues);

            ProductDefectiveness mostDefective = ranker.MostDefectiveProductFrom(tenantId);





        }

        private static void AppendToStream(UserCredentials credentials, IEventStoreConnection connection, UnitOfWork unitOfWork)
        {
            var affected = unitOfWork.GetChanges().Single();
            connection.AppendToStream(
                affected.Identifier,
                affected.ExpectedVersion,
                affected.Root.GetChanges().
                    Select(_ =>
                        new EventData(
                            Guid.NewGuid(),
                            _.GetType().Name,
                            true,
                            ToJsonByteArray(_),
                            new byte[0])),
                credentials);
        }

        class JsonDeserializer : IEventDeserializer
        {
            public IEnumerable<object> Deserialize(ResolvedEvent resolvedEvent)
            {
                var type = Type.GetType(resolvedEvent.Event.EventType, true);
                using (var stream = new MemoryStream(resolvedEvent.Event.Data))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        yield return JsonSerializer.CreateDefault().Deserialize(reader, type);
                    }
                }
            }
        }

        static byte[] ToJsonByteArray(object @event)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    JsonSerializer.CreateDefault().Serialize(writer, @event);
                    writer.Flush();
                }
                return stream.ToArray();
            }
        }
    }
}
