// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_short_concept_to_json : given.converter_for_converting_to_json<ShortConcept, short>
{
    protected override short Expected => 42;

    protected override string FormattedExpected => "42";

    [Fact] void should_convert_to_correct_short() => ShouldConvertToJson();
}
