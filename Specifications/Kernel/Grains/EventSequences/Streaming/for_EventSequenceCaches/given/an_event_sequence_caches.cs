// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCaches.given;

public class an_event_sequence_caches : Specification
{
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IEventSequenceStorage> event_sequence_storage_provider;
    protected Mock<IEventSequenceCacheFactory> event_sequence_cache_factory;
    protected EventSequenceCaches caches;

    void Establish()
    {
        execution_context_manager = new();
        event_sequence_storage_provider = new();
        event_sequence_cache_factory = new();
        caches = new(event_sequence_cache_factory.Object);
    }
}
