using StaticConsumerTopicChange.Examples;

var examples = new Example[]
{
    new DynamicConsumerSubscribeToDifferentTopic(),
    new DynamicConsumerSubscribeToAdditionalTopic(),
    new DynamicConsumerSubscribeToAdditionalTopicRegex(),
    new DynamicConsumerSubscribeWithAdditionalPartition(),
    new StaticConsumerSubscribeToDifferentTopic(),
    new StaticConsumerSubscribeToAdditionalTopic(),
    new StaticConsumerSubscribeToAdditionalTopicRegex(),
    new StaticConsumerSubscribeWithAdditionalPartition()
};

for (var i = 0; i < examples.Length; ++i)
{
    Console.WriteLine($"{i:0}: {examples[i].GetType().Name}");
}

Console.Write("Example to run: ");
Console.Out.Flush();
var example = Console.ReadLine();
if (!int.TryParse(example, out var index) || index < 0 || index >= examples.Length)
{
    Console.WriteLine($"Value must be an integer in the range 0 to {examples.Length - 1} (inclusive)");
    return;
}

await examples[index].Run();

