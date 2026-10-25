namespace PropertyHub.Interfaces.IManager
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key,T value,TimeSpan expiration);
        /// <summary>Stores the value and ties it to a group so it can be cleared with <see cref="RemoveGroup"/>.</summary>
        void Set<T>(string key,T value,TimeSpan expiration,string group);
        void Remove(string key);
        /// <summary>Evicts every entry that was stored with the given group.</summary>
        void RemoveGroup(string group);
    }
}
