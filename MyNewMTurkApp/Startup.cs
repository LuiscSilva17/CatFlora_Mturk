// Startup.cs
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Amazon.S3.Transfer;
using Amazon.MTurk;
using Amazon.MTurk.Model;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add the AWS S3 service
        services.AddDefaultAWSOptions(_configuration.GetAWSOptions());
        services.AddMvc();

    }

    // Rest of the class remains the same
}