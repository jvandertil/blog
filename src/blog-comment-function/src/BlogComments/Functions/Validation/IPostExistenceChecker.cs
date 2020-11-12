using System.Threading.Tasks;

namespace BlogComments.Functions.Validation
{
    public interface IPostExistenceChecker
    {
        Task<string?> TryGetPostFileNameFromRepositoryAsync(string postName);
    }
}
