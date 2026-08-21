using Ling.RemoteServices.Attributes;

namespace Ling.RemoteServices.Tests;

public class EndpointPolicyAttributeTests
{
    [Fact]
    public void Policy_attributes_do_not_add_AspNetCore_assembly_references()
    {
        var referencedAssemblies = typeof(RemoteAuthorizeAttribute)
            .Assembly
            .GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, reference =>
            reference.Name?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Optional_policy_attributes_support_the_host_default_policy()
    {
        Assert.Null(new RemoteAuthorizeAttribute().PolicyName);
        Assert.Null(new RemoteOutputCacheAttribute().PolicyName);
    }
}
