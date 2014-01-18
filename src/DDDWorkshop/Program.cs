﻿using AggregateSource;
using AggregateSource.EventStore;
using AggregateSource.EventStore.Resolvers;
using DDDWorkshop.Model.Issues;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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


            while (true)
            {
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

                AppendToStream(credentials, connection, productUow);

                var product = productRepository.Get(productId.ToString());

                while (true)
                {
                    var issueUow = new UnitOfWork();
                    var issueRepository = new Repository<Issue>(
                        Issue.Factory,
                        issueUow,
                        connection,
                        new EventReaderConfiguration(
                            new SliceSize(512),
                            new JsonDeserializer(),
                            new PassThroughStreamNameResolver(),
                            new FixedStreamUserCredentialsResolver(credentials)));

                    var issueId = new IssueId();
                    issueRepository.Add(issueId.ToString(), product.ReportDefect(issueId, "This is shit", "really"));

                    var issue = issueRepository.Get(issueId.ToString());

                    issue.Confirm();

                    try
                    {
                        //cant do twice;
                        issue.Confirm();
                    }
                    catch (Exception)
                    {

                    }

                    //Append to stream
                    AppendToStream(credentials, connection, issueUow);
                }
            }
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