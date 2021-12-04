using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace EdwardHsu.Lab.DistributedLockWithLogging
{
    internal class Program
    {
        private const string _lockName = @"TestLock";
        private const string _connectionString = @"redis:6379";
        static async Task Main(string[] args)
        {
            var redis = ConnectionMultiplexer.Connect(_connectionString);
            var db    = redis.GetDatabase();
            var dlock = new RedisDistributedMetaLock(_lockName, db, new Dictionary<string, string>()
            {
                ["app"] = "AAA"
            });
            // 故意不release lock
            var _lock = dlock.TryAcquireWithMeta(TimeSpan.FromSeconds(10));
            
            // 使用同樣的lockName，嘗試碰撞
            var dlock2 = new RedisDistributedMetaLock(
                _lockName, db, new Dictionary<string, string>()
                {
                    ["app"] = "BBB"
                });
            var _lock2 = dlock2.TryAcquireWithMeta(TimeSpan.FromSeconds(10));

            // 當無法取得lock
            if (_lock2.handle == null)
            { 
                // 輸出在redis上的app資訊，用來描述lock被誰占用
                Console.WriteLine($"Resource is used by: {_lock2.meta["app"]}");
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
