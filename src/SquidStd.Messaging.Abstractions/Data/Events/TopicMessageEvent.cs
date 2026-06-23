using SquidStd.Core.Interfaces.Events;

namespace SquidStd.Messaging.Abstractions.Data.Events;

/// <summary>
/// Event published on the in-process event bus when a topic message is bridged. <see cref="Data" /> is the
/// deserialized message.
/// </summary>
public sealed record TopicMessageEvent(string Topic, object Data) : IEvent;
