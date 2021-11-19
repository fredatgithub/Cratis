// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.Json;
using Cratis.Extensions.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
    /// </summary>
    public class MongoDBProjectionPipelineDefinitionsStorage : IProjectionPipelineDefinitionsStorage
    {
        readonly IJsonProjectionPipelineSerializer _projectionPipelineSerializer;
        readonly IMongoCollection<BsonDocument> _collection;

        /// <summary>
        /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
        /// </summary>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for connecting to mongo.</param>
        /// <param name="projectionPipelineSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
        public MongoDBProjectionPipelineDefinitionsStorage(
            IMongoDBClientFactory clientFactory,
            IJsonProjectionPipelineSerializer projectionPipelineSerializer)
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");

            _projectionPipelineSerializer = projectionPipelineSerializer;

            var client = clientFactory.Create(settings.Freeze());
            var database = client.GetDatabase("projections");
            _collection = database.GetCollection<BsonDocument>("pipelines");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProjectionPipelineDefinition>> GetAll()
        {
            var result = await _collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
            var definitionsAsBson = result.ToList();
            return definitionsAsBson.Select(_ =>
            {
                _.Remove("_id");
                var definitionAsJson = _.ToJson();
                return _projectionPipelineSerializer.Deserialize(definitionAsJson);
            }).ToArray();
        }

        /// <inheritdoc/>
        public async Task Save(ProjectionPipelineDefinition definition)
        {
            var json = _projectionPipelineSerializer.Serialize(definition);
            var document = BsonDocument.Parse(json);
            var id = new BsonBinaryData(definition.ProjectionId.Value, GuidRepresentation.Standard);
            document["_id"] = id;

            await _collection.ReplaceOneAsync(
                filter: new BsonDocument("_id", id),
                options: new ReplaceOptions { IsUpsert = true },
                replacement: document
            );
        }
    }
}
