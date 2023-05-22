using System.Threading.Tasks;

namespace OGCGeometryValidatorCore
{
    public interface IGeometryValidatorService
    {
        Task Run(string[] args);
    }
}