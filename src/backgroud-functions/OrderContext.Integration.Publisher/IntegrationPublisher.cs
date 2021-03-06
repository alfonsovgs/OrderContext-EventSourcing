using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrderContext.Domain.Messages.Orders;
using OrderContext.Integration.Events;
using OrderContext.Integration.Publisher.Application;

namespace OrderContext.Integration.Publisher
{
    public static class IntegrationPublisher
    {
        [FunctionName("IntegrationPublisher")]
        public static async Task Run([CosmosDBTrigger(
            databaseName: ".",
            collectionName: ".",
            ConnectionStringSetting = ".",
            LeaseCollectionName = "leases-Integrations",
            StartFromBeginning = true,
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> input, ILogger log)
        {
            foreach (var @event in input)
            {
                switch (CastEventToDynamic(@event))
                {
                    case OrderStartedEvent e:
                        await _eventBus.PublishAsync(new IntegrationEvents.V1.OrderStartedEvent(e.OrderId));
                        break; 
                }
            }
        }

        private static readonly IEventBus _eventBus = new NullInstanceEventBus();
         

        private static dynamic CastEventToDynamic(Document @event)
        {
            var eData = ((dynamic)@event).Data;
            var eType = ((dynamic)@event).Type;

            var type = Convert.ToString(eType);

            var castedEvent = JsonConvert.DeserializeObject(eData, Type.GetType(type));

            return castedEvent;
        }
    }
}
