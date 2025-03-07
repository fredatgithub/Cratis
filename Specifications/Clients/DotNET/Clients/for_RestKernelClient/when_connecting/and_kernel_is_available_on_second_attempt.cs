// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients.for_RestKernelClient.when_connecting;

public class and_kernel_is_available_on_second_attempt : given.a_rest_kernel_client
{
    void Establish()
    {
        client.http_client
            .SetupSequence(_ => _.SendAsync(IsAny<HttpRequestMessage>(), CancellationToken.None))
            .Returns(() => Task.FromResult(not_found_message))
            .Returns(() => Task.FromResult(success_message))
            .Returns(() => Task.FromResult(success_message))
            .Returns(() => Task.FromResult(success_message));
    }

    async Task Because() => await client.Connect();

    [Fact] void client_should_notify_client_lifecycle_about_being_connected() => client_lifecycle.Verify(_ => _.Connected(), Once);
}
