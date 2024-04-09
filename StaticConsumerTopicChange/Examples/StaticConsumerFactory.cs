namespace StaticConsumerTopicChange.Examples;

public sealed class StaticConsumerFactory : ConsumerFactory
{
    protected override string CreateGroupInstanceId(int index)
        => $"static.kafka.consumer.{index}";
}