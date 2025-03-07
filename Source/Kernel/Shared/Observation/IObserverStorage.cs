// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Observation;

/// <summary>
/// Defines a storage provider for working with observers.
/// </summary>
public interface IObserverStorage
{
    /// <summary>
    /// Get the information for a specific observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="ObserverInformation"/>.</returns>
    Task<ObserverInformation> GetObserver(ObserverId observerId);

    /// <summary>
    /// Get all observers for specific event types.
    /// </summary>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverInformation>> GetObserversForEventTypes(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverInformation"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverInformation>> GetAllObservers();
}
