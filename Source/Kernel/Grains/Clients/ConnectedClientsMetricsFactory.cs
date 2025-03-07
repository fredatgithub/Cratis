// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsMetricsFactory"/>.
/// </summary>
public class ConnectedClientsMetricsFactory : IConnectedClientsMetricsFactory
{
    readonly Meter _meter;

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectedClientsMetricsFactory"/>.
    /// </summary>
    /// <param name="meter">Meter for the Kernel.</param>
    public ConnectedClientsMetricsFactory(Meter meter) => _meter = meter;

    /// <inheritdoc/>
    public IConnectedClientsMetrics Create(MicroserviceId microserviceId) => new ConnectedClientsMetrics(_meter, microserviceId);
}
