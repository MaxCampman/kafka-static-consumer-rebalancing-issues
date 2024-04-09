using Confluent.Kafka;

namespace StaticConsumerTopicChange.Examples;

public abstract class Example
{
    protected const int SessionTimeoutMilliseconds = 30_000;
    protected const int AdminClientTimeoutMilliseconds = SessionTimeoutMilliseconds / 4;

    public abstract Task Run();

    protected static ConsumerConfig CreateConsumerConfig(string groupId)
        => new()
        {
            BootstrapServers = "localhost:29092",
            GroupId = groupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SessionTimeoutMs = SessionTimeoutMilliseconds,
            HeartbeatIntervalMs = SessionTimeoutMilliseconds / 10,
        };

    protected static IAdminClient CreateAdminClient()
        => new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = "localhost:29092",
            })
            .Build();

    protected static IConsumer<string, string>[] CreateConsumers(ConsumerFactory consumerFactory,
        ConsumerConfig consumerConfig)
        => Enumerable.Range(0, 3).Select(i => consumerFactory.CreateConsumer(consumerConfig, i)).ToArray();

    protected static async Task SubscribeAndConsumeForAWhile(IConsumer<string, string>[] consumers, string[] topics)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(SessionTimeoutMilliseconds);

        Console.WriteLine($"Subscribing to {string.Join(',', topics)}");
        foreach (var consumer in consumers)
        {
            consumer.Subscribe(topics);
        }

        await Task.WhenAll(
            consumers.Select(
                consumer => Task.Factory.StartNew(
                    () => ConsumeUntilCanceled(consumer, cts.Token),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default)));

        Console.WriteLine("Disposing of consumers");
        foreach (var consumer in consumers)
        {
            consumer.Dispose();
        }
    }

    private static void ConsumeUntilCanceled(IConsumer<string, string> consumer,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting {consumer.Name}");
        while (!cancellationToken.IsCancellationRequested)
        {
            _ = consumer.Consume(TimeSpan.FromMilliseconds(500));
        }
        Console.WriteLine($"Stopping {consumer.Name}");
    }
}