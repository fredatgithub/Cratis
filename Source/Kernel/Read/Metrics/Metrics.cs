// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Queries;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;

namespace Aksio.Cratis.Kernel.Read.Metrics;

/// <summary>
/// Represents the API for working with metrics.
/// </summary>
[Route("/api/metrics")]
public class Metrics : Controller
{
    internal static MetricCollection _metrics = new();

    /// <summary>
    /// Observe all metrics.
    /// </summary>
    /// <returns>A <see cref="ClientObservable{T}"/> of a collection of <see cref="Metric"/>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<MetricMeasurement>>> AllMetrics()
    {
        var metricsObservable = new ClientObservable<IEnumerable<MetricMeasurement>>();

        void contentChanged()
        {
            metricsObservable.OnNext(_metrics.Measurements.OrderBy(_ => _.Name));
        }

        _metrics.ContentChanged += contentChanged;
        metricsObservable.ClientDisconnected += () => _metrics.ContentChanged -= contentChanged;
        return Task.FromResult(metricsObservable);
    }
}
