// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Strings;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents a <see cref="ObserverStorageProvider"/> for handling replay observer state storage.
/// </summary>
public class ReplayStorageProvider : ObserverStorageProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public ReplayStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider) : base(executionContextManager, eventStoreDatabaseProvider)
    {
    }

    /// <inheritdoc/>
    public override async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var observerId = grainReference.GetPrimaryKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString);
        var key = GetKeyFrom(observerKey, observerId);

        ExecutionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var state = (ObserverState)grainState.State;

        // Note: The reason we're not using the ObserverState directly is for memory and performance reasons
        // it is faster to just store the next event sequence number directly in the document and less
        // memory footprint than creating an update statement based on the state object.
        var update = Builders<ObserverState>.Update.Set(
            nameof(ObserverState.NextEventSequenceNumber).ToCamelCase(),
            state.NextEventSequenceNumber);

        await Collection.UpdateOneAsync(
            _ => _.Id == key,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
