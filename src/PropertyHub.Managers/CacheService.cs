using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using PropertyHub.Interfaces.IManager;

namespace PropertyHub.Managers
{
    // Registered as a singleton: the group tokens must outlive a single request.
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string,CancellationTokenSource> _groups=new();
        public CacheService(IMemoryCache cache)
        {
            _cache=cache;
        }
        public T? Get<T>(string key)
        {
            _cache.TryGetValue(key,out T? value);
            return value;
        }
        public void Set<T>(string key,T value,TimeSpan expiration)
        {
            _cache.Set(key,value,expiration);
        }
        public void Set<T>(string key,T value,TimeSpan expiration,string group)
        {
            var source=_groups.GetOrAdd(group,_=>new CancellationTokenSource());
            var options=new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration)
                .AddExpirationToken(new CancellationChangeToken(source.Token));
            _cache.Set(key,value,options);
        }
        public void Remove(string key)
        {
            _cache.Remove(key);
        }
        public void RemoveGroup(string group)
        {
            if (_groups.TryRemove(group,out var source))
            {
                source.Cancel();
                source.Dispose();
            }
        }
    }
}
