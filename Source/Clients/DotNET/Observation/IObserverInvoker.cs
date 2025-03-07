// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Defines the invoker for an <see cref="ObserverHandler"/>.
/// </summary>
public interface IObserverInvoker
{
    /// <summary>
    /// Invoke the observer.
    /// </summary>
    /// <param name="content">Event content to invoke with.</param>
    /// <param name="eventContext"><see cref="EventContext"/> for the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Invoke(object content, EventContext eventContext);

    /// <summary>
    /// Gets the supported <see cref="EventType">event types</see>.
    /// </summary>
    IEnumerable<EventType> EventTypes { get; }
}
