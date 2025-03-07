// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICatchUp"/>.
/// </summary>
public class CatchUp : ObserverWorker, ICatchUp
{
    readonly ILogger<CatchUp> _logger;
    readonly List<FailedPartition> _failedPartitions = new();
    ObserverKey? _observerKey;
    IDisposable? _timer;
    bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="ObserverState"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public CatchUp(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        [PersistentState(nameof(ObserverState), ObserverState.CatchUpStorageProvider)] IPersistentState<ObserverState> observerState,
        ILogger<CatchUp> logger) : base(executionContextManager, eventSequenceStorageProvider, observerState, logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override MicroserviceId MicroserviceId => _observerKey!.MicroserviceId;

    /// <inheritdoc/>
    protected override TenantId TenantId => _observerKey!.TenantId;

    /// <inheritdoc/>
    protected override EventSequenceId EventSequenceId => _observerKey!.EventSequenceId;

    /// <inheritdoc/>
    protected override MicroserviceId? SourceMicroserviceId => _observerKey!.SourceMicroserviceId;

    /// <inheritdoc/>
    protected override TenantId? SourceTenantId => _observerKey!.SourceTenantId;

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        _ = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(ObserverSubscription subscription)
    {
        if (_isRunning)
        {
            _logger.AlreadyCatchingUp(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
            return;
        }

        await ReadStateAsync();

        _logger.Starting(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
        CurrentSubscription = subscription;
        _isRunning = true;
        _timer = RegisterTimer(PerformCatchUp, null, TimeSpan.Zero, TimeSpan.MaxValue);
    }

    /// <inheritdoc/>
    public async Task Stop()
    {
        _logger.Stopping(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
        _isRunning = false;
        _timer?.Dispose();
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public override Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        _failedPartitions.Add(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace));
        return Task.CompletedTask;
    }

    async Task PerformCatchUp(object arg)
    {
        _timer?.Dispose();
        var provider = EventSequenceStorageProvider;

        var next = State.NextEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : State.NextEventSequenceNumber;
        var nextSequenceNumber = await provider.GetNextSequenceNumberGreaterOrEqualThan(_observerKey!.EventSequenceId!, next, State.EventTypes);
        if (nextSequenceNumber == EventSequenceNumber.Unavailable)
        {
            nextSequenceNumber = EventSequenceNumber.First;
        }
        using var cursor = await provider.GetFromSequenceNumber(_observerKey!.EventSequenceId!, nextSequenceNumber, eventTypes: State.EventTypes);
        while (await cursor.MoveNext())
        {
            if (!_isRunning) break;

            foreach (var @event in cursor.Current)
            {
                if (!_isRunning) break;
                await Handle(@event);
            }
        }

        _isRunning = false;
        _logger.CaughtUp(ObserverId, MicroserviceId, TenantId, EventSequenceId, SourceMicroserviceId, SourceTenantId);
        await Supervisor.NotifyCatchUpComplete(_failedPartitions.ToArray());
        _failedPartitions.Clear();
    }
}
