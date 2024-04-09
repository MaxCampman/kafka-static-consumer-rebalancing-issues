namespace StaticConsumerTopicChange.Examples;

public sealed class DynamicConsumerFactory : ConsumerFactory
{
    protected override string CreateGroupInstanceId(int index)
        => $"dynamic.kafka.consumer.{index}.{Guid.NewGuid():N}";
}