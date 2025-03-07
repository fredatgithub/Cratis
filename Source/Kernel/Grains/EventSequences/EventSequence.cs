// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Engines.Compliance;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Grains.Workers;
using Aksio.Cratis.Schemas;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
[StorageProvider(ProviderName = EventSequenceState.StorageProvider)]
public class EventSequence : Grain<EventSequenceState>, IEventSequence
{
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly IEventSequenceMetricsFactory _metricsFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly IJsonComplianceManager _jsonComplianceManagerProvider;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<EventSequence> _logger;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    MicroserviceAndTenant _microserviceAndTenant = MicroserviceAndTenant.NotSet;
    IAsyncStream<AppendedEvent>? _stream;
    IEventSequenceMetrics? _metrics;
    long _appendedEventsCount;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequence"/>.
    /// </summary>
    /// <param name="schemaStoreProvider">Provider for <see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="metricsFactory">Factory for creating metrics.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public EventSequence(
        ProviderFor<ISchemaStore> schemaStoreProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        IEventSequenceMetricsFactory metricsFactory,
        IExecutionContextManager executionContextManager,
        IJsonComplianceManager jsonComplianceManagerProvider,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<EventSequence> logger)
    {
        _schemaStoreProvider = schemaStoreProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _metricsFactory = metricsFactory;
        _executionContextManager = executionContextManager;
        _jsonComplianceManagerProvider = jsonComplianceManagerProvider;
        _expandoObjectConverter = expandoObjectConverter;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _eventSequenceId = this.GetPrimaryKey(out var streamNamespace);
        _microserviceAndTenant = MicroserviceAndTenant.Parse(streamNamespace);

        var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, streamNamespace);

        _appendedEventsCount = await _eventSequenceStorageProvider().GetCount(_eventSequenceId);

        _metrics = _metricsFactory.CreateFor(
            _eventSequenceId,
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            () => _appendedEventsCount);

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => Task.FromResult(State.SequenceNumber);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber() => Task.FromResult(State.SequenceNumber - 1);

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, EventType eventType, JsonObject content, DateTimeOffset? validFrom = default)
    {
        var updateSequenceNumber = false;
        var eventName = "[N/A]";
        try
        {
            var eventSchema = await _schemaStoreProvider().GetFor(eventType.Id, eventType.Generation);
            eventName = eventSchema.Schema.GetDisplayName();
            _logger.Appending(
                _microserviceAndTenant.MicroserviceId,
                _microserviceAndTenant.TenantId,
                _eventSequenceId,
                eventType,
                eventName,
                eventSourceId,
                State.SequenceNumber);

            var compliantEvent = await _jsonComplianceManagerProvider.Apply(eventSchema.Schema, eventSourceId, content);

            var compliantEventAsExpandoObject = _expandoObjectConverter.ToExpandoObject(compliantEvent, eventSchema.Schema);

            var appending = true;
            while (appending)
            {
                try
                {
                    var appendedEvent = new AppendedEvent(
                        new(State.SequenceNumber, eventType),
                        new(
                            eventSourceId,
                            State.SequenceNumber,
                            DateTimeOffset.UtcNow,
                            validFrom ?? DateTimeOffset.MinValue,
                            _microserviceAndTenant.TenantId,
                            _executionContextManager.Current.CorrelationId,
                            _executionContextManager.Current.CausationId,
                            _executionContextManager.Current.CausedBy),
                        compliantEventAsExpandoObject);

                    await _stream!.OnNextAsync(appendedEvent, new EventSequenceNumberToken(State.SequenceNumber));

                    _metrics?.AppendedEvent(eventSourceId, eventName);
                    _appendedEventsCount++;

                    appending = false;
                }
                catch (DuplicateEventSequenceNumber)
                {
                    _metrics?.DuplicateEventSequenceNumber(eventSourceId, eventName);
                    State.SequenceNumber++;
                    await WriteStateAsync();
                }
            }
            updateSequenceNumber = true;
        }
        catch (UnableToAppendToEventSequence ex)
        {
            _metrics?.FailedAppending(eventSourceId, eventName);
            _logger.FailedAppending(
                _microserviceAndTenant.MicroserviceId,
                _microserviceAndTenant.TenantId,
                eventType,
                ex.StreamId,
                ex.EventSourceId,
                ex.SequenceNumber,
                ex);

            throw;
        }
        catch (Exception ex)
        {
            _logger.ErrorAppending(
                _microserviceAndTenant.MicroserviceId,
                _microserviceAndTenant.TenantId,
                _eventSequenceId,
                eventSourceId,
                State.SequenceNumber,
                ex);

            throw;
        }

        if (updateSequenceNumber)
        {
            State.SequenceNumber++;
            await WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public Task Compensate(EventSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default)
    {
        _logger.Compensating(
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            eventType,
            _eventSequenceId,
            sequenceNumber);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IWorker<RewindPartitionForObserversAfterRedactRequest, RewindPartitionForObserversAfterRedactResponse>> Redact(EventSequenceNumber sequenceNumber, RedactionReason reason)
    {
        _logger.Redacting(
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            _eventSequenceId,
            sequenceNumber);

        var affectedEvent = await _eventSequenceStorageProvider().Redact(_eventSequenceId, sequenceNumber, reason);
        return await RewindPartitionForAffectedObservers(affectedEvent.Context.EventSourceId, sequenceNumber, new[] { affectedEvent.Metadata.Type });
    }

    /// <inheritdoc/>
    public async Task<IWorker<RewindPartitionForObserversAfterRedactRequest, RewindPartitionForObserversAfterRedactResponse>> Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType> eventTypes)
    {
        _logger.RedactingMultiple(
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            _eventSequenceId,
            eventSourceId,
            eventTypes);

        var affectedEventTypes = await _eventSequenceStorageProvider().Redact(_eventSequenceId, eventSourceId, reason, eventTypes);
        return await RewindPartitionForAffectedObservers(eventSourceId, EventSequenceNumber.First, affectedEventTypes);
    }

    async Task<IWorker<RewindPartitionForObserversAfterRedactRequest, RewindPartitionForObserversAfterRedactResponse>> RewindPartitionForAffectedObservers(EventSourceId eventSourceId, EventSequenceNumber sequenceNumber, IEnumerable<EventType> affectedEventTypes)
    {
        var worker = GrainFactory.GetGrain<IWorker<RewindPartitionForObserversAfterRedactRequest, RewindPartitionForObserversAfterRedactResponse>>(Guid.NewGuid());

        await worker.Start(new(
            _microserviceAndTenant.MicroserviceId,
            _microserviceAndTenant.TenantId,
            _eventSequenceId,
            eventSourceId,
            sequenceNumber,
            affectedEventTypes));

        return worker;
    }
}
