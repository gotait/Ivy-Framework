using System.Globalization;
using Ivy;
using Ivy.Auth.Fortnox;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
server.UseHotReload();
server.AddAppsFromAssembly();
server.UseChrome();
server.UseAuth<FortnoxAuthProvider>(c => c.UseFortnox());
await server.RunAsync();