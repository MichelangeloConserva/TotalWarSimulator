using System.Threading.Tasks;

namespace DbscanImplementation.Eventing
{
    public interface IDbscanEventSubscriber<TR>
    {
        Task<TR> Subscribe();
    }
}