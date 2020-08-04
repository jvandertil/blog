using System.Threading.Tasks;
using Pulumi;

class Program
{
    public static Task<int> Main()
    {
        return Deployment.RunAsync<BlogStack>();
    }
}
