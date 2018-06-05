using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace HelloWorld.Interfaces
{
    public interface IPartner : IGrainWithGuidKey
    {
        [AlwaysInterleave]
        Task<(int byOrders, int byPartner)> TotalHandledEvents();
    }
}