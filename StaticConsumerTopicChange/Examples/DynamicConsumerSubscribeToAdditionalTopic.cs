using Confluent.Kafka.Admin;

namespace StaticConsumerTopicChange.Examples;

/// <summary>
/// In this example, we create a number of static consumers, which then subscribe to 
/// </summary>
public sealed class DynamicConsumerSubscribeToAdditionalTopic : Example 
{
    public override async Task Run()
    {
        var consumerConfig = CreateConsumerConfig("dynamic.consumer.subscribe.to.additional.topic");

        var adminClient = CreateAdminClient();

        var topics = new[]
        {
            "dynamic.consumer.subscribe.to.additional.topic.0",
            "dynamic.consumer.subscribe.to.additional.topic.1",
            "dynamic.consumer.subscribe.to.additional.topic.2",
        };

        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        await adminClient.DeleteTopicsAsync(
            metadata.Topics.Select(x => x.Topic),
            new DeleteTopicsOptions
            {
                RequestTimeout = TimeSpan.FromMilliseconds(AdminClientTimeoutMilliseconds),
                OperationTimeout = TimeSpan.FromMilliseconds(AdminClientTimeoutMilliseconds)
            });

        await adminClient.CreateTopicsAsync(
            topics.Select(topic => new TopicSpecification
            {
                Name = topic,
                ReplicationFactor = 1,
                NumPartitions = 3,
            }),
            new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromMilliseconds(AdminClientTimeoutMilliseconds),
                OperationTimeout = TimeSpan.FromMilliseconds(AdminClientTimeoutMilliseconds)
            });

        var consumerFactory = new DynamicConsumerFactory();

        // Subscribe to some topics
        await SubscribeAndConsumeForAWhile(CreateConsumers(consumerFactory, consumerConfig), [
            "dynamic.consumer.subscribe.to.additional.topic.0",
            "dynamic.consumer.subscribe.to.additional.topic.1"
        ]);

        // Simulate the service being restarted/redeployed. Not long enough for the consumer group to be considered EMPTY
        await Task.Delay(AdminClientTimeoutMilliseconds);

        // Try to subscribe to an additional topic. This has the expected behaviour because the new consumers trigger a re-balance.
        await SubscribeAndConsumeForAWhile(CreateConsumers(consumerFactory, consumerConfig), [
            "dynamic.consumer.subscribe.to.additional.topic.0",
            "dynamic.consumer.subscribe.to.additional.topic.1",
            "dynamic.consumer.subscribe.to.additional.topic.2"
        ]);
    }
}