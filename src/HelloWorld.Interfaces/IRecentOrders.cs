using System.Threading.Tasks;
using Orleans;

namespace HelloWorld.Interfaces
{
    public interface IRecentOrders : IGrainWithGuidKey
    {
        Task<string[]> GetOrders();
        Task<string[]> GetStates();
    }
}