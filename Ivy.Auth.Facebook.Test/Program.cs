using System.Globalization;
using Ivy;
using Ivy.Auth.Facebook;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
server.UseHotReload();
server.AddAppsFromAssembly();
server.UseChrome();
server.UseAuth<FacebookAuthProvider>(c => c.UseFacebook());
await server.RunAsync();