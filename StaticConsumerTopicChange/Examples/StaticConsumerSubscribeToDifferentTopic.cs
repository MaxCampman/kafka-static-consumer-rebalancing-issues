using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace StaticConsumerTopicChange.Examples;

/// <summary>
/// In this example, we create a number of static consumers, which then subscribe to 
/// </summary>
public sealed class StaticConsumerSubscribeToDifferentTopic : Example 
{
    public override async Task Run()
    {
        var adminClientTimeoutMilliseconds = SessionTimeoutMilliseconds / 2;
        var consumerConfig = CreateConsumerConfig("static.consumer.subscribe.to.different.topic");

        var adminClient = CreateAdminClient();

        var topics = new[]
        {
            "static.consumer.subscribe.to.different.topic.0",
            "static.consumer.subscribe.to.different.topic.1"
        };

        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        await adminClient.DeleteTopicsAsync(
            metadata.Topics.Select(x => x.Topic),
            new DeleteTopicsOptions
            {
                RequestTimeout = TimeSpan.FromMilliseconds(adminClientTimeoutMilliseconds),
                OperationTimeout = TimeSpan.FromMilliseconds(adminClientTimeoutMilliseconds)
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
                RequestTimeout = TimeSpan.FromMilliseconds(adminClientTimeoutMilliseconds),
                OperationTimeout = TimeSpan.FromMilliseconds(adminClientTimeoutMilliseconds)
            });

        var consumerFactory = new StaticConsumerFactory();

        // Subscribe to some topics
        await SubscribeAndConsumeForAWhile(CreateConsumers(consumerFactory, consumerConfig), [
            "static.consumer.subscribe.to.different.topic.0"
        ]);

        // Simulate the service being restarted/redeployed. Not long enough for the consumer group to be considered EMPTY
        await Task.Delay(SessionTimeoutMilliseconds / 2);

        // Try to subscribe to a different topic. Will notice none of the partitions for the new topic are assigned.
        await SubscribeAndConsumeForAWhile(CreateConsumers(consumerFactory, consumerConfig), [
            "static.consumer.subscribe.to.different.topic.1"
        ]);
    }
}