using System.Threading.Tasks;
using Orleans;

namespace HelloWorld.Interfaces
{
    public interface IDatabase : IGrainWithStringKey
    {
        Task Initialize();
    }
}