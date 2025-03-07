// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients.for_RestKernelClient.when_connected;

public class and_kernel_returns_error : given.a_connected_client
{
    protected override HttpResponseMessage GetMessageForRoute(string route)
    {
        if (route == ping_route)
        {
            client.should_connect = false;
            return failed_message;
        }

        return success_message;
    }

    [Fact] void client_should_notify_client_lifecycle_about_being_disconnected() => client_lifecycle.Verify(_ => _.Disconnected(), Once);
}
