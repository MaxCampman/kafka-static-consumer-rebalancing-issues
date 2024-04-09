using Confluent.Kafka.Admin;

namespace StaticConsumerTopicChange.Examples;

public class StaticConsumerSubscribeWithAdditionalPartition : Example
{
    public override async Task Run()
    {
        var consumerConfig = CreateConsumerConfig("static.consumer.subscribe.to.additional.partition");

        var adminClient = CreateAdminClient();

        var topics = new[]
        {
            "static.consumer.subscribe.to.additional.partition.0",
            "static.consumer.subscribe.to.additional.partition.1",
            "static.consumer.subscribe.to.additional.partition.2"
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
                OperationTimeout = TimeSpan.FromMinutes(1),
                RequestTimeout = TimeSpan.FromMinutes(1),
            });

        var consumerFactory = new StaticConsumerFactory();

        // Subscribe to some topics
        await SubscribeAndConsumeForAWhile(CreateConsumers(consumerFactory, consumerConfig), [
            "static.consumer.subscribe.to.additional.partition.0",
            "static.consumer.subscribe.to.additional.partition.1",
            "static.consumer.subscribe.to.additional.partition.2"
        ]);

        await adminClient.CreatePartitionsAsync(
            new PartitionsSpecification[]
            {
                new()
                {
                    Topic = "static.consumer.subscribe.to.additional.partition.0",
                    IncreaseTo = 6
                }
            },
            new CreatePartitionsOptions
            {
                RequestTimeout = TimeSpan.FromMilliseconds(AdminClientTimeoutMilliseconds),
                OperationTimeout = TimeSpan.FromMilliseconds(AdminClientTimeoutMilliseconds)
            });

        await Task.Delay(AdminClientTimeoutMilliseconds);

        // Try to subscribe to an additional topic. Will notice none of the partitions for the new topic are assigned.
        await SubscribeAndConsumeForAWhile(CreateConsumers(consumerFactory, consumerConfig), [
            "static.consumer.subscribe.to.additional.partition.0",
            "static.consumer.subscribe.to.additional.partition.1",
            "static.consumer.subscribe.to.additional.partition.2"
        ]);
    }
}