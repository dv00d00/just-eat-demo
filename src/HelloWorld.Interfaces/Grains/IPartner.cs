using System.Threading.Tasks;
using Orleans;

namespace HelloWorld.Interfaces
{
    public interface IPartner : IGrainWithGuidKey
    {
        Task<(int byOrders, int byPartner)> TotalHandledEvents();
    }
}