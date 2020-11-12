using System.Threading.Tasks;

namespace BlogComments.Functions.Validation
{
    public interface IPostExistenceValidator
    {
        Task<string?> TryGetPostFileNameFromRepositoryAsync(string postName);
    }
}
