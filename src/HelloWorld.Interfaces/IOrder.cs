using System.Threading.Tasks;
using Orleans;

namespace HelloWorld.Interfaces
{
    public interface IOrder : IGrainWithStringKey
    {
        Task Handle(string @event);
        Task<string> GetState();
    }
}