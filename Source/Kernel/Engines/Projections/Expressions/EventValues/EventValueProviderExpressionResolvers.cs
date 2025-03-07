// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions.EventValues;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertyExpressionResolvers"/>.
/// </summary>
public class EventValueProviderExpressionResolvers : IEventValueProviderExpressionResolvers
{
    readonly IEventValueProviderExpressionResolver[] _resolvers = new IEventValueProviderExpressionResolver[]
    {
        new EventSourceIdExpressionResolver(),
        new EventContextPropertyExpressionResolver(),
        new EventContentExpressionResolver(),
        new ValueExpressionResolver()
    };
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITypeFormats"/> class.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for finding target types.</param>
    public EventValueProviderExpressionResolvers(ITypeFormats typeFormats)
    {
        _typeFormats = typeFormats;
    }

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _resolvers.Any(_ => _.CanResolve(expression));

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(JsonSchemaProperty targetSchemaProperty, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(expression));
        ThrowIfUnsupportedEventValueExpression(targetSchemaProperty, expression, resolver);

        return (e) => Convert(targetSchemaProperty, resolver!.Resolve(expression)(e));
    }

    void ThrowIfUnsupportedEventValueExpression(JsonSchemaProperty targetSchemaProperty, string expression, IEventValueProviderExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedEventValueExpression(targetSchemaProperty, expression);
        }
    }

    object Convert(JsonSchemaProperty schemaProperty, object input)
    {
        if (input is ExpandoObject)
        {
            var expandoObject = (input as IDictionary<string, object>)!;
            var properties = schemaProperty.IsArray ?
                schemaProperty.Item.GetFlattenedProperties() :
                schemaProperty.GetFlattenedProperties();

            foreach (var property in properties)
            {
                expandoObject[property.Name] = Convert(property, expandoObject[property.Name]);
            }
            return expandoObject;
        }

        var targetType = schemaProperty.GetTargetTypeForJsonSchemaProperty(_typeFormats);
        if (targetType is not null)
        {
            return TypeConversion.Convert(targetType, input);
        }

        if (input.GetType().IsEnumerable())
        {
            var children = new List<object>();
            foreach (var child in (input as IEnumerable)!)
            {
                children.Add(Convert(schemaProperty, child));
            }

            return children;
        }

        return input;
    }
}
