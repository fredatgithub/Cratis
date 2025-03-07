// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing;

public class and_sequence_is_ahead : given.an_observer_and_two_event_types
{
    void Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, null, null)).Returns(Task.FromResult((EventSequenceNumber)1));
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)1));
    }

    async Task Because() => await observer.Subscribe<ObserverSubscriber>(event_types, subscriber_args);

    [Fact] void should_set_state_to_catching_up() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.CatchingUp);
    [Fact] void should_initiate_catchup() => catch_up.Verify(_ => _.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args)), Once);
}
