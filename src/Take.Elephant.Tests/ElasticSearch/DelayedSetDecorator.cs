﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Take.Elephant.Tests.Elasticsearch
{
    internal class DelayedSetDecorator<T> : ISet<T>
    {
        private readonly ISet<T> _set;
        private readonly int _delay;

        public DelayedSetDecorator(ISet<T> set, int delay)
        {
            _set = set;
            _delay = delay;
        }
        public async Task AddAsync(T value, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            await _set.AddAsync(value, cancellationToken);
        }

        public async Task<IAsyncEnumerable<T>> AsEnumerableAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return await _set.AsEnumerableAsync(cancellationToken);
        }

        public async Task<bool> ContainsAsync(T value, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return await _set.ContainsAsync(value, cancellationToken);
        }

        public async Task<long> GetLengthAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return await _set.GetLengthAsync(cancellationToken);
        }

        public async Task<bool> TryRemoveAsync(T value, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return await _set.TryRemoveAsync(value, cancellationToken);
        }
    }
}
