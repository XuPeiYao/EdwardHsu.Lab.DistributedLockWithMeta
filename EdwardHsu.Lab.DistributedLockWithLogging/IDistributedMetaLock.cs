using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;

namespace EdwardHsu.Lab.DistributedLockWithLogging
{
    public interface IDistributedMetaLock
    {
        (IDistributedSynchronizationHandle? handle, IDictionary<string,string> meta) TryAcquireWithMeta(
            TimeSpan          timeout           = new TimeSpan(),
            CancellationToken cancellationToken = new CancellationToken()
        );

        (IDistributedSynchronizationHandle handle, IDictionary<string, string> meta) AcquireWithMeta(
            TimeSpan?         timeout           = null,
            CancellationToken cancellationToken = new CancellationToken()
        );

        ValueTask<(IDistributedSynchronizationHandle? handle, IDictionary<string, string> meta)> TryAcquireWithMetaAsync(
            TimeSpan          timeout           = new TimeSpan(),
            CancellationToken cancellationToken = new CancellationToken()
        );

        ValueTask<(IDistributedSynchronizationHandle handle, IDictionary<string, string> meta)> AcquireWithMetaAsync(
            TimeSpan?         timeout           = null,
            CancellationToken cancellationToken = new CancellationToken()
        );
    }
}
