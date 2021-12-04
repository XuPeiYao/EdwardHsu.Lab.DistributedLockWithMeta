using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace EdwardHsu.Lab.DistributedLockWithLogging
{
    public class RedisDistributedMetaLock : IDistributedLock , IDistributedMetaLock  
    {
        private readonly RedisDistributedLock _lock;
        private readonly IEnumerable<IDatabase> _databases;
        private readonly IDictionary<string, string> _meta;

        /// <summary>
        /// Constructs a lock named <paramref name="key"/> using the provided <paramref name="database"/> and <paramref name="options"/>.
        /// </summary>
        public RedisDistributedMetaLock(
            RedisKey                                               key,
            IDatabase                                              database,
            IDictionary<string, string>                            meta    = null,
            Action<RedisDistributedSynchronizationOptionsBuilder>? options = null
        )
            : this(key, new[] {database ?? throw new ArgumentNullException(nameof(database))}, meta, options)
        {
        }

        /// <summary>
        /// Constructs a lock named <paramref name="key"/> using the provided <paramref name="databases"/> and <paramref name="options"/>.
        /// </summary>
        public RedisDistributedMetaLock(
            RedisKey                                               key,
            IEnumerable<IDatabase>                                 databases,
            IDictionary<string, string>                            meta    = null,
            Action<RedisDistributedSynchronizationOptionsBuilder>? options = null
        )
        {
            _databases = databases;
            _lock      = new RedisDistributedLock(key, databases, options);
            _meta      = meta ?? new Dictionary<string, string>();
        }

        public string Name => _lock.Name;

        private async Task UpdateRedisValueAsync(string stackTrace)
        {
            List<Task> tasks = new List<Task>();
            var        name  = new RedisKey(Name);
            foreach (var database in this._databases)
            {
                tasks.Add(
                    Task.Run(
                        async () =>
                        {
                            IDictionary<string, string> meta = new Dictionary<string, string>();
                            foreach (var kv in _meta)
                            {
                                meta.Add(kv.Key, kv.Value);
                            }

                            var ttl = await database.KeyTimeToLiveAsync(name, CommandFlags.DemandMaster);
                            var originalValue = await database.StringGetAsync(
                                name, CommandFlags.DemandMaster);
                            meta["originalValue"] = originalValue;
                            meta["stackTrace"]    = stackTrace;
                            var newValue =
                                System.Text.Json.JsonSerializer.Serialize(meta);
                            await database.StringSetAsync(new RedisKey(Name), newValue, ttl);
                        }));
            }

            await Task.WhenAll(tasks);
        }

        private void UpdateRedisValue(string stackTrace)
        {
            UpdateRedisValueAsync(stackTrace).Wait();
        }

        private async Task<IDictionary<string, string>> GetRedisValueAsync()
        {
            List<Task<IDictionary<string,string>>> tasks = new List<Task<IDictionary<string, string>>>();
            var                                    name  = new RedisKey(Name);
            foreach (var database in this._databases)
            {
                tasks.Add(
                    Task.Run(
                        async () =>
                        {
                            var value = await database.StringGetAsync(
                                name, CommandFlags.DemandMaster);

                            var result =
                                System.Text.Json.JsonSerializer.Deserialize<IDictionary<string, string>>(value);

                            return result;
                        }));
            }

            return (await Task.WhenAny(tasks)).Result;
        }
        
        private IDictionary<string, string> GetRedisValue()
        {
            return GetRedisValueAsync().GetAwaiter().GetResult();
        }
        
 
        (IDistributedSynchronizationHandle? handle, IDictionary<string, string> meta) InternalTryAcquire(
            TimeSpan          timeout,
            CancellationToken cancellationToken
        )
        {
            var                         result = _lock.TryAcquire(timeout, cancellationToken);
            IDictionary<string, string> meta   = null;
            if (result != null)
            {
                try
                {
                    UpdateRedisValue(Environment.StackTrace);
                }
                catch (Exception e)
                {
                    // result.Dispose();
                    // throw e;
                }
            }
            else
            {
                meta = GetRedisValue();
            }

            return (result, meta);
        }
        
        public IDistributedSynchronizationHandle? TryAcquire(
            TimeSpan          timeout           = new TimeSpan(),
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            return InternalTryAcquire(timeout, cancellationToken).handle;
        }

        (IDistributedSynchronizationHandle? handle, IDictionary<string, string> meta) InternalAcquire(
            TimeSpan?         timeout           = null,
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            var result = _lock.Acquire(timeout, cancellationToken);
            IDictionary<string, string> meta   = null;
            if (result != null)
            {
                try
                {
                    UpdateRedisValue(Environment.StackTrace);
                }
                catch (Exception e)
                {
                    // result.Dispose();
                    // throw e;
                }
            }
            else
            {
                meta = GetRedisValue();
            }

            return (result,meta);
        }

        public IDistributedSynchronizationHandle Acquire(
            TimeSpan?         timeout           = null,
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            return InternalAcquire(timeout, cancellationToken).handle;
        }


        async ValueTask<(IDistributedSynchronizationHandle? handle, IDictionary<string, string> meta)>
            InternalTryAcquireAsync(
                TimeSpan          timeout           = new TimeSpan(),
                CancellationToken cancellationToken = new CancellationToken())
        {
            var                         result = await _lock.TryAcquireAsync(timeout, cancellationToken);
            IDictionary<string, string> meta   = null;
            if (result != null)
            {
                try
                {
                    await UpdateRedisValueAsync(Environment.StackTrace);
                }
                catch (Exception e)
                {
                    // result.Dispose();
                    // throw e;
                }
            }
            else
            {
                meta = await GetRedisValueAsync();
            }

            return (result,meta);
        }
        public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(
            TimeSpan          timeout           = new TimeSpan(),
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            return (await InternalTryAcquireAsync(timeout, cancellationToken)).handle;
        }

        async ValueTask<(IDistributedSynchronizationHandle? handle, IDictionary<string, string> meta)>
            InternalAcquireAsync(
                TimeSpan?          timeout          = null,
                CancellationToken cancellationToken = new CancellationToken())
        {
            var                         result = await _lock.AcquireAsync(timeout, cancellationToken);
            IDictionary<string, string> meta   = null;
            if (result != null)
            {
                try
                {
                    await UpdateRedisValueAsync(Environment.StackTrace);
                }
                catch (Exception e)
                {
                    // result.Dispose();
                    // throw e;
                }
            }
            else
            {
                meta = await GetRedisValueAsync();
            }

            return (result, meta);
        }

        public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(
            TimeSpan?         timeout           = null,
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            return (await InternalAcquireAsync(timeout, cancellationToken)).handle;
        }

        public (IDistributedSynchronizationHandle handle, IDictionary<string, string> meta) TryAcquireWithMeta(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            return InternalTryAcquire(timeout, cancellationToken);
        }

        public (IDistributedSynchronizationHandle handle, IDictionary<string, string> meta) AcquireWithMeta(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return InternalAcquire(timeout, cancellationToken);
        }

        public ValueTask<(IDistributedSynchronizationHandle handle, IDictionary<string, string> meta)> TryAcquireWithMetaAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            return InternalTryAcquireAsync(timeout, cancellationToken);
        }

        public ValueTask<(IDistributedSynchronizationHandle handle, IDictionary<string, string> meta)> AcquireWithMetaAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return InternalAcquireAsync(timeout, cancellationToken);
        }
    }
}
