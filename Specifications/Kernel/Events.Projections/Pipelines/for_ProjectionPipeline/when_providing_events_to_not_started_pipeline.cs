// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines.for_ProjectionPipeline
{
    public class when_providing_events_to_paused_pipeline : given.a_pipeline_with_one_store
    {
        async Task Establish()
        {
            await pipeline.Start();
            await pipeline.Pause();
        }

        void Because() => subject.OnNext(@event);

        [Fact] void should_forward_to_pipeline_handler() => pipeline_handler.Verify(_ => _.Handle(@event, pipeline, result_store.Object, configuration), Never());
    }
}
