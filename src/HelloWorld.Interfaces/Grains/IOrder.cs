using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace HelloWorld.Interfaces
{
    public interface IOrder : IGrainWithStringKey
    {
        Task Handle(string @event);

        [AlwaysInterleave]
        Task<string> GetState();

        Task<int> GetHandledEventsCount();
    }
}