#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression
#addin nuget:?package=Cake.Kudu.Client
using System;
using System.Net.Http
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

var target = Argument("target", "Build");
var cnAppName = "CustomerNotifier";
var lrsAppName = "LoanRequestSender";
var ccAppName = "CreditChecker";
var lrrAppName = "LoanRequestRetriever";
var azureSubscriptionId = EnvironmentVariable("AZ_SUB_ID");
var cakeDeployerClientId = EnvironmentVariable("CAKE_DEPLOYER_CLIENT_ID");
var cakeDeployerClientSecret = EnvironmentVariable("CAKE_DEPLOYER_CLIENT_SECRET");
var azureTenantId = EnvironmentVariable("AZ_TENANT_ID");

var splunkHost = EnvironmentVariable("SplunkHost");
var lrrToken = EnvironmentVariable("LRRToken");
var ccToken = EnvironmentVariable("CCToken");
var lrsToken = EnvironmentVariable("LRSToken");
var cnToken = EnvironmentVariable("CNToken");

var lrrPublishingPassword = string.Empty;
var ccPublishingPassword = string.Empty;
var lrsPublishingPassword = string.Empty;
var cnPublishingPassword = string.Empty;
var accessToken = string.Empty;

void SetUpNuget()
{
	var feed = new
	{
		Name = "<feednam>",
	    Source = "<your feed url>"
	};

	if (!NuGetHasSource(source:feed.Source))
	{
	    var nugetSourceSettings = new NuGetSourcesSettings
                             {
                                 UserName = "<your usename>",
                                 Password = EnvironmentVariable("NUGET_PAT"),
                                 Verbosity = NuGetVerbosity.Detailed
                             };		

		NuGetAddSource(
		    name:feed.Name,
		    source:feed.Source,
		    settings:nugetSourceSettings);
	}	
}

async Task<string> PasswordFor(string siteName){
	var password = string.Empty;
	Information($"Retrieving publishing profile for {siteName}...");
	using (var client = new HttpClient())
    {
        client.BaseAddress = new Uri("https://management.azure.com/");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var content = new StringContent(
		  "{\"PublishingProfileFormat\":\"WebDeploy\"}", 
		  Encoding.UTF8, 
		  "application/json");

        var response = await client.PostAsync(
            $"subscriptions/{azureSubscriptionId}/resourceGroups/loanbrokerage-testing/providers/Microsoft.Web/sites/{siteName}/publishxml?api-version=2016-03-01",
            new StringContent(string.Empty));

        if (response.IsSuccessStatusCode)
        {
            var xml = await response.Content.ReadAsStringAsync();
            string pattern = @"userPWD=""(\w)+""";
            RegexOptions options = RegexOptions.Multiline;
            Match match = Regex.Match(xml, pattern, options);
            password = match
			.Value
			.Replace("userPWD=\"", string.Empty)
			.Replace("\"", string.Empty);			
        }
    }

	return password;
}

Task("Restore")
    .Does(() => {		
		SetUpNuget();
		DotNetCoreRestore("./LoanBrokerage.sln");	
});

Task("Build")
	.IsDependentOn("Restore")
    .Does(()=>{
		var config = new DotNetCoreBuildSettings
		{
			Configuration = "Release",
			NoRestore = true
		};
        DotNetCoreBuild("./LoanBrokerage.sln", config);
});

Task("Test")
	 .IsDependentOn("Build")
     .Does(() =>
 {
     var settings = new DotNetCoreTestSettings
     {
         Configuration = "Release",
		 NoBuild = true
     };

     var projectFiles = GetFiles("./**/*.UnitTests.csproj");

     foreach(var file in projectFiles)
     {
         DotNetCoreTest(file.FullPath, settings);
     }
 });

Task("Publish")
	.IsDependentOn("Test")
	.Does(()=>
{	
	 ///
	 Information("Publishing CustomerNotifier...");
	 var settingsNotifier = new DotNetCorePublishSettings
     {
         Configuration = "Release",
         OutputDirectory = $"./artifacts/{cnAppName}",
		 NoRestore = true
     };

     DotNetCorePublish("./CustomerNotifier/CustomerNotifier.csproj", settingsNotifier);	
	 var cnConfig = File($"./artifacts/{cnAppName}/{cnAppName}.exe.config");
	 XmlPoke(cnConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsDashboard']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));
	 
	 XmlPoke(cnConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsStorage']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));

	 XmlPoke(cnConfig, 
	 "configuration/appSettings/add[@key='SplunkHost']/@value", splunkHost);

	 XmlPoke(cnConfig, 
	 "configuration/appSettings/add[@key='CNToken']/@value", cnToken);

	 ///
	 Information("Publishing CreditChecker...");
	 var settingsCreditChecker = new DotNetCorePublishSettings
     {
         Configuration = "Release",
         OutputDirectory = $"./artifacts/{ccAppName}",
		 NoRestore = true
     };

     DotNetCorePublish("./CreditChecker/CreditChecker.csproj", settingsCreditChecker);	
	 var ccConfig = File($"./artifacts/{ccAppName}/{ccAppName}.exe.config");
	 XmlPoke(ccConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsDashboard']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));
	 
	 XmlPoke(ccConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsStorage']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));

	 Information($"CCToken was {ccToken}");

	 XmlPoke(ccConfig, 
	 "configuration/appSettings/add[@key='SplunkHost']/@value", splunkHost);

	 XmlPoke(ccConfig, 
	 "configuration/appSettings/add[@key='CCToken']/@value", ccToken);

	 ///
	 Information("Publishing LoanRequestRetriever...");
	 var settingsRetriever = new DotNetCorePublishSettings
     {
         Configuration = "Release",
         OutputDirectory = $"./artifacts/{lrrAppName}",
		 NoRestore = true
     };

     DotNetCorePublish("./LoanRequestRetriever/LoanRequestRetriever.csproj", settingsRetriever);	
	 var lrrConfig = File($"./artifacts/{lrrAppName}/{lrrAppName}.exe.config");
	 XmlPoke(lrrConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsDashboard']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));
	 
	 XmlPoke(lrrConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsStorage']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));

	 XmlPoke(lrrConfig, 
	 "configuration/appSettings/add[@key='CronExp']/@value", 
	 EnvironmentVariable("CronExp"));	 

	 Information($"LRRToken was {lrrToken}");

	 XmlPoke(lrrConfig, 
	 "configuration/appSettings/add[@key='SplunkHost']/@value", splunkHost);

	 XmlPoke(lrrConfig, 
	 "configuration/appSettings/add[@key='LRRToken']/@value", lrrToken);

	 ///
	 Information("Publishing LoanRequestSender...");
	 var settingsSender = new DotNetCorePublishSettings
     {
         Configuration = "Release",
         OutputDirectory = $"./artifacts/{lrsAppName}",
		 NoRestore = true
     };

     DotNetCorePublish("./LoanRequestSender/LoanRequestSender.csproj", settingsSender);	
	 var lrsConfig = File($"./artifacts/{lrsAppName}/{lrsAppName}.exe.config");
	 XmlPoke(lrsConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsDashboard']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));
	 
	 XmlPoke(lrsConfig, 
	 "configuration/appSettings/add[@key='AzureWebJobsStorage']/@value", 
	 EnvironmentVariable("AzureWebJobsDashboard"));

	 Information($"LRSToken was {lrsToken}");

	 XmlPoke(lrsConfig, 
	 "configuration/appSettings/add[@key='SplunkHost']/@value", splunkHost);

	 XmlPoke(lrsConfig, 
	 "configuration/appSettings/add[@key='LRSToken']/@value", lrsToken);
});

Task("Get-PublishProfiles")
.IsDependentOn("Publish")
.Does(async ()=>{	
	Information("Retrieving access token...");
	using (var client = new HttpClient())
    {
        client.BaseAddress = new Uri("https://login.microsoftonline.com/");

        Dictionary<string, string> kvps = new Dictionary<string, string>();
        kvps.Add("grant_type", "client_credentials");
        kvps.Add("client_id", cakeDeployerClientId);
        kvps.Add("client_secret", cakeDeployerClientSecret);
        kvps.Add("resource", "https://management.azure.com/");

        FormUrlEncodedContent content = new FormUrlEncodedContent(kvps);

        var response = await client.PostAsync($"{azureTenantId}/oauth2/token", content);

        if (response.IsSuccessStatusCode)
        {
            dynamic tokenResult = JsonConvert.DeserializeObject(
                await response.Content.ReadAsStringAsync());
            accessToken = tokenResult.access_token;
            
			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				Information("Access token retrieved successfully!");			
			}
        }
        else
        {
            Information(response.ReasonPhrase);
        }
    }	
});

Task("Azure-WebDeploy")
	.IsDependentOn("Get-PublishProfiles")
    .Does(async () =>
    {			
		Information($"Deploying {lrrAppName}...");
		IKuduClient kuduClientForLrr = KuduClient(
		$"https://{lrrAppName}.scm.azurewebsites.net/",
		"$"+lrrAppName,
		await PasswordFor(lrrAppName.ToLower()));

		DirectoryPath sourceDirectoryPath = $"./artifacts/{lrrAppName}";
		DirectoryPath remoteDirectoryPath = $"/site/wwwroot/app_data/jobs/continuous/{lrrAppName}";

		kuduClientForLrr.ZipUploadDirectory(
		    sourceDirectoryPath,
		    remoteDirectoryPath);		

		Information($"Deploying {ccAppName}...");
		IKuduClient kuduClientForCc = KuduClient(
		$"https://{ccAppName}.scm.azurewebsites.net/",
		"$"+ccAppName,
		await PasswordFor(ccAppName.ToLower()));

		DirectoryPath sourceDirectoryPath2 = $"./artifacts/{ccAppName}";
		DirectoryPath remoteDirectoryPath2 = $"/site/wwwroot/app_data/jobs/continuous/{ccAppName}";

		kuduClientForCc.ZipUploadDirectory(
		    sourceDirectoryPath2,
		    remoteDirectoryPath2);
		
		Information($"Deploying {lrsAppName}...");
		IKuduClient kuduClientForLrs = KuduClient(
		$"https://{lrsAppName}.scm.azurewebsites.net/",
		"$"+lrsAppName,
		await PasswordFor(lrsAppName.ToLower()));

		DirectoryPath sourceDirectoryPath3 = $"./artifacts/{lrsAppName}";
		DirectoryPath remoteDirectoryPath3 = $"/site/wwwroot/app_data/jobs/continuous/{lrsAppName}";

		kuduClientForLrs.ZipUploadDirectory(
		    sourceDirectoryPath3,
		    remoteDirectoryPath3);

		Information($"Deploying {cnAppName}...");
		IKuduClient kuduClientForCn = KuduClient(
		$"https://{cnAppName}.scm.azurewebsites.net/",
		"$"+cnAppName,
		await PasswordFor(cnAppName.ToLower()));

		DirectoryPath sourceDirectoryPath4 = $"./artifacts/{cnAppName}";
		DirectoryPath remoteDirectoryPath4 = $"/site/wwwroot/app_data/jobs/continuous/{cnAppName}";

		kuduClientForCn.ZipUploadDirectory(
		    sourceDirectoryPath4,
		    remoteDirectoryPath4);		
});


RunTarget(target);