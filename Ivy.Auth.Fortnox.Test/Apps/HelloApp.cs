using Ivy.Apps;
using Ivy.Core;

namespace Ivy.Auth.Fortnox.Test.Apps;

[App]
public class HelloApp : ViewBase
{
    public override async Task<object?> Build()
    {
        var auth = this.UseService<IAuthService>();
        var userInfo = await auth.GetUserInfoAsync();

        if (userInfo is null)
            return "You are not authenticated";

        return "You are authenticated as " + userInfo.Email;
    }
}