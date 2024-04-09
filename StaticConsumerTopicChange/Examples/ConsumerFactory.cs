using Confluent.Kafka;

namespace StaticConsumerTopicChange.Examples;

public abstract class ConsumerFactory
{
    protected abstract string CreateGroupInstanceId(int index);

    public IConsumer<string, string> CreateConsumer(ConsumerConfig baseConsumerConfig, int index)
        => new ConsumerBuilder<string, string>(new ConsumerConfig(baseConsumerConfig)
            {
                GroupInstanceId = CreateGroupInstanceId(index)
            })
            .SetLogHandler((consumer, logMessage) =>
            {
                Console.WriteLine($"[{consumer.Name}] LOG: level={logMessage.Level} message=\"{logMessage.Message}\"");
            })
            .SetErrorHandler((consumer, error) =>
            {
                Console.WriteLine($"[{consumer.Name}] ERROR: is_fatal={error.IsFatal}, is_error={error.IsError}, is_local_error={error.IsLocalError}, is_broker_error={error.IsBrokerError}, code={error.Code}, reason={error.Reason}");
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
}