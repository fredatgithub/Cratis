// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Timers;

namespace Aksio.Cratis.Common.Grains;

public abstract class GrainSpecification<TState> : GrainSpecification
    where TState : new()
{
    protected Mock<IStorage<TState>> storage;
    protected TState state;
    protected List<TState> written_states = new();
    protected TState most_recent_written_state;
    protected Type grain_type = typeof(Grain<TState>);

        
    protected override void OnStateManagement()
    {
        state ??= new TState();
        
        var storageProperty = grain_type.GetField("storage", BindingFlags.FlattenHierarchy | 
                                                             BindingFlags.Instance | 
                                                             BindingFlags.NonPublic);

        if (storageProperty is null)
            throw new MissingMemberException(grain.GetType().Name, "storage");

        storage = new Mock<IStorage<TState>>();
        storageProperty.SetValue(grain, storage.Object);

        storage.SetupGet(_ => _.State).Returns(state);
        storage.Setup(_ => _.WriteStateAsync()).Returns(() =>
        {
            var serialized = JsonSerializer.Serialize(state);
            most_recent_written_state = JsonSerializer.Deserialize<TState>(serialized);
            written_states.Add(most_recent_written_state);
            return Task.CompletedTask;
        });
    }
}

public abstract class GrainSpecification : Specification
{
    protected Mock<IGrainIdentity> grain_identity;
    protected Mock<IGrainRuntime> runtime;
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IKeyedServiceCollection<string, IStreamProvider>> stream_provider_collection;
    protected Mock<IReminderRegistry> reminder_registry;
    protected Mock<ITimerRegistry> timer_registry;
    protected Mock<IGrainFactory> grain_factory;
    protected Grain grain;

    protected abstract Guid GrainId { get; }
    protected abstract string GrainKeyExtension { get; }

    protected abstract Grain GetGrainInstance();

    protected virtual void OnBeforeGrainActivate()
    {
    }

    protected virtual void OnStateManagement()
    {
    }

    void Establish()
    {
        grain = GetGrainInstance();

        var identityField = typeof(Grain).GetField("Identity", BindingFlags.Instance | BindingFlags.NonPublic);
        grain_identity = new Mock<IGrainIdentity>();
        identityField.SetValue(grain, grain_identity.Object);

        var runtimeProperty = typeof(Grain).GetProperty("Runtime", BindingFlags.Instance | BindingFlags.NonPublic);
        runtime = new Mock<IGrainRuntime>();
        runtimeProperty.SetValue(grain, runtime.Object);

        grain_factory = new();
        runtime.SetupGet(_ => _.GrainFactory).Returns(grain_factory.Object);

        reminder_registry = new();
        runtime.SetupGet(_ => _.ReminderRegistry).Returns(reminder_registry.Object);

        timer_registry = new();
        runtime.SetupGet(_ => _.TimerRegistry).Returns(timer_registry.Object);

        service_provider = new Mock<IServiceProvider>();
        runtime.SetupGet(_ => _.ServiceProvider).Returns(service_provider.Object);

        stream_provider_collection = new Mock<IKeyedServiceCollection<string, IStreamProvider>>();
        service_provider.Setup(_ => _.GetService(typeof(IKeyedServiceCollection<string, IStreamProvider>))).Returns(stream_provider_collection.Object);

        var key = GrainKeyExtension;
        grain_identity.Setup(_ => _.GetPrimaryKey(out key)).Returns(GrainId);

        OnStateManagement();
        OnBeforeGrainActivate();

        Orleans.GrainReferenceExtensions.GetReferenceOverride = (grain) => grain;

        grain.OnActivateAsync();
    }
}
