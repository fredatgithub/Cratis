// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Applications.Rules.for_Rules.for_RulesModelValidator;

public class ModelClassWithKey
{
    [ModelKey]
    public string Id { get; set; }
}
