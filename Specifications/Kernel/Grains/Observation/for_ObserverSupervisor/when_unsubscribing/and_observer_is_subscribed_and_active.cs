// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_unsubscribing;

public class and_observer_is_subscribed_and_active : given.an_observer_and_two_event_types
{
    async Task Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult(EventSequenceNumber.Unavailable));
        await observer.Subscribe<ObserverSubscriber>(event_types);
    }

    async Task Because() => await observer.Unsubscribe();

    [Fact] void should_set_state_to_disconnected() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_unsubscribe_subscription() => subscription_handles[0].Verify(_ => _.UnsubscribeAsync(), Once);
}
