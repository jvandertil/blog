using System.Threading;
using System.Threading.Tasks;

namespace BlogComments.Functions.Validation
{
    public interface ITurnstileValidator
    {
        Task<bool> IsValidAsync(string? token, CancellationToken cancellationToken = default);
    }
}
