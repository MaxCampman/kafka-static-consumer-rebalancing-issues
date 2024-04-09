using Confluent.Kafka;
using Confluent.Kafka.Admin;

const int sessionTimeoutMilliseconds = 10_000;

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = "localhost:29092",
    GroupId = "kafka.group.id",
    EnableAutoCommit = false,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    SessionTimeoutMs = sessionTimeoutMilliseconds,
    HeartbeatIntervalMs = sessionTimeoutMilliseconds / 10
};


var adminClient = new AdminClientBuilder(new AdminClientConfig
{
    BootstrapServers = "localhost:29092"
}).Build();

Console.WriteLine("Deleting topics \"topic.old\" and \"topic.new\" so we can start afresh");
var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
await adminClient.DeleteTopicsAsync(
    metadata.Topics.Select(x => x.Topic).Where(topic => topic is "topic.old" or "topic.new"),
    new DeleteTopicsOptions
    {
        RequestTimeout = TimeSpan.FromMilliseconds(sessionTimeoutMilliseconds),
        OperationTimeout = TimeSpan.FromMilliseconds(sessionTimeoutMilliseconds)
    });

Console.WriteLine("Creating topics \"topic.old\" and \"topic.new\"");
await adminClient.CreateTopicsAsync(
    new TopicSpecification[]
    {
        new()
        {
            Name = "topic.old",
            NumPartitions = 3,
            ReplicationFactor = 1
        },
        new()
        {
            Name = "topic.new",
            NumPartitions = 3,
            ReplicationFactor = 1
        }
    },
    new CreateTopicsOptions
    {
        RequestTimeout = TimeSpan.FromMinutes(1),
        OperationTimeout = TimeSpan.FromMinutes(1),
    });

IConsumer<string, string>[] consumers;

consumers = Enumerable.Range(0, 3).Select(index => CreateConsumer(consumerConfig, index)).ToArray();

Console.WriteLine("Subscribing to \"topic.old\"");
foreach (var consumer in consumers)
{
    consumer.Subscribe("topic.old");
}

using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(2 * sessionTimeoutMilliseconds);

    await Task.WhenAll(consumers.Select(
        consumer => Task.Factory.StartNew(
            () => ConsumeUntilCanceled(consumer, cts.Token),
            TaskCreationOptions.LongRunning)));
}

Console.WriteLine("Disposing consumers listening to \"topic.old\"");
foreach (var consumer in consumers)
{
    consumer.Dispose();
}

// Wait for a short while in order to simulate a deployment.
await Task.Delay(sessionTimeoutMilliseconds / 2);

consumers = Enumerable.Range(0, 3).Select(index => CreateConsumer(consumerConfig, index)).ToArray();

Console.WriteLine("Subscribing to \"topic.new\"");
foreach (var consumer in consumers)
{
    consumer.Subscribe("topic.new");
}

using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(2 * sessionTimeoutMilliseconds);

    await Task.WhenAll(consumers.Select(
        consumer => Task.Factory.StartNew(
            () => ConsumeUntilCanceled(consumer, cts.Token),
            TaskCreationOptions.LongRunning)));
}

Console.WriteLine("Disposing consumers listening to \"topic.new\"");
foreach (var consumer in consumers)
{
    consumer.Dispose();
}


IConsumer<string, string> CreateConsumer(ConsumerConfig baseConsumerConfig, int index)
    => new ConsumerBuilder<string, string>(new ConsumerConfig(baseConsumerConfig)
        {
            GroupInstanceId = $"kafka.consumer.{index}"
        })
        .SetLogHandler((consumer, logMessage) =>
        {
            Console.WriteLine($"[{consumer.Name}] LOG: level={logMessage.Level} message=\"{logMessage.Message}\"");
        })
        .SetErrorHandler((consumer, error) =>
        {
            Console.WriteLine(
                $"[{consumer.Name}] ERROR: is_fatal={error.IsFatal}, is_error={error.IsError}, is_local_error={error.IsLocalError}, is_broker_error={error.IsBrokerError}, code={error.Code}, reason={error.Reason}");
        })
        .SetPartitionsAssignedHandler((consumer, partitions) =>
        {
            Console.WriteLine($"[{consumer.Name}] PARTITIONS ASSIGNED {string.Join(',', partitions)}");
        })
        .SetPartitionsRevokedHandler((consumer, offsets) =>
        {
            Console.WriteLine($"[{consumer.Name}] PARTITIONS ASSIGNED {string.Join(',', offsets)}");
        })
        .SetPartitionsLostHandler((consumer, offsets) =>
        {
            Console.WriteLine($"[{consumer.Name}] PARTITIONS ASSIGNED {string.Join(',', offsets)}");
        })
        .Build();

void ConsumeUntilCanceled(IConsumer<string, string> consumer, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        _ = consumer.Consume(TimeSpan.FromMilliseconds(100));
    }
}