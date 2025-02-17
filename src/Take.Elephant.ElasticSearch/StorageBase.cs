﻿using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Take.Elephant.Elasticsearch.Mapping;

namespace Take.Elephant.Elasticsearch
{
    public abstract class StorageBase<T> : IQueryableStorage<T> where T : class
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ConcurrentDictionary<string, Func<T, object>> _propertiesDictionary;
        protected readonly IElasticClient ElasticClient;
        protected IMapping Mapping { get; }

        public StorageBase(string hostname, string username, string password, string defaultIndex, IMapping mapping)
        {
            _propertiesDictionary = new ConcurrentDictionary<string, Func<T, object>>();
            _connectionSettings = new ConnectionSettings(new Uri(hostname))
                .BasicAuthentication(username, password)
                .DefaultIndex(defaultIndex);
            Mapping = mapping;
            ElasticClient = new ElasticClient(_connectionSettings);
        }

        public StorageBase(ConnectionSettings connectionSettings, IMapping mapping)
        {
            _propertiesDictionary = new ConcurrentDictionary<string, Func<T, object>>();
            _connectionSettings = connectionSettings;
            Mapping = mapping;
            ElasticClient = new ElasticClient(_connectionSettings);
        }

        public StorageBase(IElasticClient elasticClient, IMapping mapping)
        {
            _propertiesDictionary = new ConcurrentDictionary<string, Func<T, object>>();
            Mapping = mapping;
            ElasticClient = elasticClient;
        }

        public async Task<QueryResult<T>> QueryAsync<TResult>(
            Expression<Func<T, bool>> where,
            Expression<Func<T, TResult>> select, int skip, int take,
            CancellationToken cancellationToken = default)
        {
            var queryDescriptor = where.ParseToQueryContainer<T>();

            var result = await ElasticClient.SearchAsync<T>(s => s
                .Index(Mapping.Index)
                .Type(Mapping.Type)
                .Query(_ => queryDescriptor)
                .From(skip).Size(take), cancellationToken);

            return new QueryResult<T>(new AsyncEnumerableWrapper<T>(result.Documents), (int)result.Total);
        }

        public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var response = await ElasticClient.DocumentExistsAsync<T>(key, d => d
                .Index(Mapping.Index)
                .Type(Mapping.Type),
                cancellationToken);

            return response.Exists;
        }

        public async Task<T> GetValueOrDefaultAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var result = await ElasticClient.GetAsync<T>(key,
                d => d
                .Index(Mapping.Index)
                .Type(Mapping.Type),
                cancellationToken);

            return result?.Source;
        }

        public async Task<bool> TryAddAsync(string key, T value, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (overwrite || !await ContainsKeyAsync(key, cancellationToken))
            {
                var result = await ElasticClient.IndexAsync(new IndexRequest<T>(value,
                    Mapping.Index,
                    Mapping.Type,
                    key.ToString()), cancellationToken);

                return result.IsValid;
            }

            return false;
        }

        public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var result = await ElasticClient.DeleteAsync<T>(key,
                d => d.Index(Mapping.Index)
                      .Type(Mapping.Type),
                cancellationToken);

            return result.IsValid;
        }

        protected string GetPropertyValue(T entity, string property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(property);
            }

            var propertyAcessor = _propertiesDictionary
                .GetOrAdd(property, e => 
                    TypeUtil.BuildGetAccessor(
                        typeof(T).GetProperties()
                        .Single(p => p.Name == property)));

            return propertyAcessor(entity)?.ToString();
        }
    }
}
