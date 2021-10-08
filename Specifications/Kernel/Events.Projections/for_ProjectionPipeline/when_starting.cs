// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_starting : given.a_pipeline
    {
        void Because() => pipeline.Start();

        [Fact] void should_start_event_provider() => event_provider.Verify(_ => _.ProvideFor(projection.Object), Once());
    }
}
