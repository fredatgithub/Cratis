// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapter"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapter : IQueueAdapter
{
    readonly ConcurrentDictionary<QueueId, EventSequenceQueueAdapterReceiver> _receivers = new();

    readonly IStreamQueueMapper _mapper;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public bool IsRewindable => true;

    /// <inheritdoc/>
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapter"/> class.
    /// </summary>
    /// <param name="name">Name of stream.</param>
    /// <param name="mapper"><see cref="IStreamQueueMapper"/> for getting queue identifiers.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    public EventSequenceQueueAdapter(
        string name,
        IStreamQueueMapper mapper,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider)
    {
        Name = name;
        _mapper = mapper;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
    }

    /// <inheritdoc/>
    public IQueueAdapterReceiver CreateReceiver(QueueId queueId) => CreateReceiverIfNotExists(queueId);

    /// <inheritdoc/>
    public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
    {
        var queueId = _mapper.GetQueueForStream(streamGuid, streamNamespace);
        CreateReceiverIfNotExists(queueId);
        if (!token.IsWarmUp())
        {
            var appendedEvents = new List<AppendedEvent>();
            foreach (var @event in events)
            {
                var appendedEvent = (@event as AppendedEvent)!;
                try
                {
                    await _eventSequenceStorageProvider().Append(
                        streamGuid,
                        appendedEvent.Metadata.SequenceNumber,
                        appendedEvent.Context.EventSourceId,
                        appendedEvent.Metadata.Type,
                        appendedEvent.Context.ValidFrom,
                        appendedEvent.Content);

                    appendedEvents.Add(appendedEvent);
                }
                catch (Exception ex)
                {
                    // Make sure we put all successful events on the stream for any subscribers to get.
                    _receivers[queueId].AddAppendedEvent(streamGuid, streamNamespace, appendedEvents, requestContext);
                    var microserviceAndTenant = MicroserviceAndTenant.Parse(streamNamespace);

                    throw new UnableToAppendToEventSequence(
                        streamGuid,
                        microserviceAndTenant.MicroserviceId,
                        microserviceAndTenant.TenantId,
                        appendedEvent.Metadata.SequenceNumber,
                        appendedEvent.Context.EventSourceId,
                        ex);
                }
            }
        }

        _receivers[queueId].AddAppendedEvent(streamGuid, streamNamespace, events.Cast<AppendedEvent>().ToArray(), requestContext);
    }

    IQueueAdapterReceiver CreateReceiverIfNotExists(QueueId queueId)
    {
        if (_receivers.ContainsKey(queueId)) return _receivers[queueId];

        var receiver = new EventSequenceQueueAdapterReceiver();
        _receivers[queueId] = receiver;
        return receiver;
    }
}
