// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Store;
using Orleans;
using EngineProjection = Aksio.Cratis.Events.Projections.IProjection;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
public class Projection : Grain, IProjection
{
    readonly IProjectionDefinitions _projectionDefinitions;
    readonly IProjectionFactory _projectionFactory;
    readonly IObjectsComparer _objectsComparer;
    readonly IProjectionEventProviders _projectionEventProviders;
    readonly IEventLogStorageProvider _eventLogStorageProvider;
    EngineProjection? _projection;
    IProjectionEventProvider? _projectionEventProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection"/> class.
    /// </summary>
    /// <param name="projectionDefinitions"><see cref="IProjectionDefinitions"/>.</param>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
    /// <param name="projectionEventProviders"><see cref="IProjectionEventProviders"/> in the system.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
    public Projection(
        IProjectionDefinitions projectionDefinitions,
        IProjectionFactory projectionFactory,
        IProjectionEventProviders projectionEventProviders,
        IObjectsComparer objectsComparer,
        IEventLogStorageProvider eventLogStorageProvider)
    {
        _projectionDefinitions = projectionDefinitions;
        _projectionFactory = projectionFactory;
        _objectsComparer = objectsComparer;
        _projectionEventProviders = projectionEventProviders;
        _eventLogStorageProvider = eventLogStorageProvider;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        var projectionId = this.GetPrimaryKey();
        var definition = await _projectionDefinitions.GetFor(projectionId);
        _projection = await _projectionFactory.CreateFrom(definition);
        _projectionEventProvider = _projectionEventProviders.GetForType("c0c0196f-57e3-4860-9e3b-9823cf45df30");
    }

    /// <inheritdoc/>
    public async Task<JsonObject> GetModelInstanceById(EventSourceId eventSourceId)
    {
        if (_projection is null)
        {
            return new JsonObject();
        }
        var cursor = await _eventLogStorageProvider.GetFromSequenceNumber(EventLogSequenceNumber.First, eventSourceId, _projection.EventTypes);
        var state = new ExpandoObject();
        while (await cursor.MoveNext())
        {
            if (!cursor.Current.Any())
            {
                break;
            }

            foreach (var @event in cursor.Current)
            {
                var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectsComparer, @event, state);
                var keyResolver = _projection.GetKeyResolverFor(@event.Metadata.Type);
                var key = await keyResolver(_projectionEventProvider!, @event);
                var context = new ProjectionEventContext(key, @event, changeset);

                await HandleEventFor(_projection!, context);

                foreach (var change in changeset.Changes)
                {
                    switch (change)
                    {
                        case PropertiesChanged<ExpandoObject> propertiesChanged:
                            state = state.OverwriteWith((change.State as ExpandoObject)!);
                            break;

                        case ChildAdded childAdded:
                            var items = state.EnsureCollection<ExpandoObject>(childAdded.ChildrenProperty, key.ArrayIndexers);
                            items.Add(childAdded.Child.AsExpandoObject());
                            break;
                    }
                }
            }
        }

        // TODO: Conversion from ExpandoObject to JsonObject can be improved - they're effectively both just Dictionary<string, object>
        var json = JsonSerializer.Serialize(state);
        var jsonObject = JsonNode.Parse(json)!;
        return (jsonObject as JsonObject)!;
    }

    async Task HandleEventFor(EngineProjection projection, ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Metadata.Type))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            await HandleEventFor(child, context);
        }
    }
}
