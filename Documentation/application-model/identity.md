# Identity

Cratis' Application Model provides a way to easily work with providing an object that represents properties the application finds important for describing
the logged in user. The purpose of this is to provide details about the logged in user on the ingress level of an application and letting it
provide the details on the request going in. Having it on the ingress level lets you expose the details to all microservices behind the ingress.

> Note: Aksio has an ingress middleware that uses this technique and takes the details and puts it on a cookie. For more details go [here](https://github.com/aksio-insurtech/IngressMiddleware).

The values provided by the provider are values that are typically application specific and goes beyond what is already found in the token representing the user.
This is optimized for working with Microsoft Azure well known HTTP headers passed on by the different app services, such as Azure ContainerApps or WebApps.
Internally, it is based on the following HTTP headers to be present.

| Header | Description |
| ------ | ----------- |
| x-ms-client-principal | The token holding all the details, base64 encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| x-ms-client-principal-id | The unique identifier from the identity provider for the identity |
| x-ms-client-principal-name | The name of the identity, typically resolved from claims within the token |

> Important note: Since local development is not configured with the identity provider, but you still need a way to test that both the backend and the frontend
> deals with the identity in the correct way. This can be achieved by creating the correct token and injecting it as request headers using
> a browser extension. Read more [here](./generating-principal.md).

The token in the `x-ms-client-principal` should be a base64 encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data).
This is unwrapped by the application model and encapsulates it into what is called a `IdentityProviderContext` for you as a developer to consume in a type-safe
manner.

To support the identity details, one of your microservices in your application can implement the `IProvideIdentityDetails` interface
found in the `Aksio.Cratis.ApplicationModel.Identity` namespace.

> Note: If your application has just one microservice, you let it implement the `IProvideIdentityDetails` interface.

Below is an example of an implementation:

```csharp
public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var result = new IdentityDetails(true, new { Hello = "World" });
        return Task.FromResult(result);
    }
}
```

The `IdentityProviderContext` holds the following properties:

| Property | Description |
| -------- | ----------- |
| Id | The identity identifier specific from from the identity provider |
| Name | The name of the identity |
| Token | Parsed principal data definition represented as a `JsonObject`|
| Claims | Collection of `KeyValuePair<string, string>` of the claims found in the token |

The code then returns `IdentityDetails` which holds the following properties:

| Property | Description |
| -------- | ----------- |
| IsUserAuthorized | Whether or not the user is authorized into your application or not |
| Details | The actual details in the form of an object, letting you create your own structure |

If the `IsUserAuthorized` property is set to false the return from this will be an HTTP 403. While if it is authorized, a regular HTTP 200.

> Note: Dependency inversion works for this, so your provider can take any dependencies it wants on its constructor.

Your provider will be exposed on a well known route: `/.aksio/me`.
