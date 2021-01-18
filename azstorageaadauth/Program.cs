using azstorageaadauth;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

IConfiguration Configuration = new ConfigurationBuilder()
    .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
    .Build();
//
// 1. Need to have an app registration in place
// 2. Add the AD security group into the desired storage container and provide Blob storage contributor perms
// 3. Storage contributor perms comes with R/W/X access at the container level
// 4. Azure Storage SDK v11 is legacy - https://github.com/Azure/azure-storage-net#support-statement
// 5. Android app is seemingly using v11 Storage SDK - Needs confirmation

var app = PublicClientApplicationBuilder.Create(Configuration["Authentication:ClientId"])
                                        .WithRedirectUri("http://localhost/")
                                        .WithLogging(
                                            Log,
                                            Microsoft.Identity.Client.LogLevel.Verbose,
                                            enablePiiLogging: true,
                                            enableDefaultPlatformLogging: true)
                                        .WithTenantId(Configuration["Authentication:TenantId"])
                                        .Build();

#region Sign in the user interactively and acquire token
AuthenticationResult result;
var accounts = await app.GetAccountsAsync();
string[] scopes = new string[] { "user.read", "https://storage.azure.com/user_impersonation" };

try
{
    result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
}
catch (MsalUiRequiredException)
{
    result = await app.AcquireTokenInteractive(scopes)
                .ExecuteAsync();
}

accounts = await app.GetAccountsAsync();
#endregion

#region Azure Storage SDK v11 - Microsoft is terming v11 as legacy(https://github.com/Azure/azure-storage-net#support-statement)

// Error prone initialization. Does not take into token expiry and renewal in to account
// In case of exception(what is the exception type?) due to token expiration, need to renew the token
CloudBlobClient cloudBlobClient = new CloudBlobClient(
                                   new Uri("https://haritest3.blob.core.windows.net/"),
                                          credentials: new Microsoft.Azure.Storage.Auth.StorageCredentials(
                                          tokenCredential: new Microsoft.Azure.Storage.Auth.TokenCredential(
                                          result.AccessToken)));

Task<NewTokenAndFrequency> RenewToken(object state, CancellationToken cancellationToken)
{
    throw new NotImplementedException();
}

var containerClient = cloudBlobClient.GetContainerReference("test");
var blob = containerClient.GetBlockBlobReference("Graphql500Error.txt");
blob.UploadFromFile(@"C:\Users\hari.subramaniam\Downloads\Graphql500Error.txt");

foreach (var blob1 in containerClient.ListBlobs())
{
    Console.WriteLine($"Storage SDK v11 - {blob1.Uri}");
}

#endregion

#region Azure Storage SDK v12

// This credential type unfornately does not seem to reuse the  token present in user token cache
// Need to investigate if it is not meant to or is there something I am missing
// DANGER - This will open up a interactive session for the user to login based on the platforms perferred browser.
var interactiveBrowserCredential = new InteractiveBrowserCredential(
                                new InteractiveBrowserCredentialOptions()
                                {
                                    ClientId = Configuration["Authentication:ClientId"],
                                    TenantId = Configuration["Authentication:TenantId"]
                                });


var blobServiceClient = new BlobServiceClient(
                            new Uri("https://haritest3.blob.core.windows.net/"),
                            // Azure.Core.Tokencredentail does not seem to have a way to instatiate it with existing Access token. Very poor
                                     credential: interactiveBrowserCredential);
var container = blobServiceClient.GetBlobContainerClient("test");
foreach (var v12sdkBlob in container.GetBlobs())
{
    Console.WriteLine(v12sdkBlob.Name);
}

// Another option is to implement a custom token crential implemenation of Azure.Core.TokenCredential. 
// Again hacky, because we have to manage expiry and renewal
var customTokenCredential = new CustomTokenCredential(result.AccessToken, result.ExpiresOn);

var blobServiceClientWithCustomTokenCredential = new BlobServiceClient(
                            new Uri("https://haritest3.blob.core.windows.net/"),
                                     // Azure.Core.Tokencredentail does not seem to have a way to instatiate it with existing Access token. Very poor
                                     credential: customTokenCredential);
var container1 = blobServiceClientWithCustomTokenCredential.GetBlobContainerClient("test");
foreach (var v12sdkBlob in container1.GetBlobs())
{
    Console.WriteLine(v12sdkBlob.Name);
}
#endregion

static void Log(Microsoft.Identity.Client.LogLevel level, string message, bool containsPii)
{
    if (containsPii)
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }
    Console.WriteLine($"{level} {message}");
    Console.ResetColor();
}

