# Kegocnizer Azure Implementation

For all the resources, use the same Subscription, Location and Resource Group for better management.
In this example, the location will be West US and the resource group will be  KegocnizerDemo.
While creating any resources, add prefix or suffix to differentiate with your own name.

Below are the steps used to create the Azure databases and functions that support the keg client and
admin applications.

## Creating the Azure CosmosDB database

a. Create CosmosDB in Azure
  1. portal.azure.com
  2. Click on '+ Create new resource' link on the top left corner
  3. Search for Azure Cosmos DB and Click Create
  4. Enter ID: 'kegocnizerdemodb', API: SQL and select subscription, Resource Group: 'KegocnizerDemo'
  Click on Create

Wait a few moments while Azure creates the Azure Cosmos DB resource.

b. Go to above resource once created
  * Click on Data Explorer
  * Click on 'New Database' and enter 'KegocnizerData' and Ok
  * Once database got created, Click on tilde symbol adjacent to database created and select 'New Collection'
  * Collection id : Items, Partition Key: /type
  * Click OK

c. Create Function App
  * Click on '+ Create a resource'
  * Search for 'Function App'
  * Enter:  App name: 'KegocnizerDemoFunctions'
  * Select appropriate subscription
  * Use same 'KegocnizerDemo' Resource Group
  * OS: Windows
  * Hosting Plan: 'Consumption Plan'
  * Location: West US or appropriate
  * Storage: 'kegocnizerdemostorage'
  * Application Insights: On
  * Application Insights Location: 'West US2' or appropriate
	
d. Navigate to Azure Function created above
  * Host Key Creation
      * Click on the Resource Name and naviagate to 'Function app settings'
	  * Create Key using 'Add new host key' button
	  * Provide any name and save for autogeneration of key. This key should be updated in the Keg.DAL\Constants.cs for 'AF_KEGKEY'
	  * Make sure Authorization level is 'Function' for all the methods

  * Method: AddConfig
    * Click '+' adjacent to Functions to create new Method
    * Select 'HTTP trigger' template
    * Select c# as Language, and enter 'AddConfig' as Name
    * Select AuthroizationLevel: Function
    * And Click on 'Create'
    * Once 'AddConfig' method is created, 
    * Expand AddConfig and Select 'Integrate'
      * Under Outputs: 
        * Click on '+New Output' to add Azure Cosmos DB from the available list
          * Being selected that added Azure Cosmos DB, 
            a. Change the database name : KegocnizerData  [ This is name of database created ]
            b. Collection Name: Items
            c. Click on 'new' for Azure Cosmos DB account connection and select KegocnizerDemodb account
        * Under Triggers 'HTTP(req)
          * Make sure Authorization level is 'Function', POST HTTP methods
        * Select AddConfig Function in left pane
        * In the space provided on the right: run.csx, copy code from Functions.txt under Method: AddConfig 
        * And Click Save
	
  * Method: AddUser
    * Same as above steps, except
    * HTTP Methods: POST
    * Add Azure Cosmos DB under "Outputs" section
    * Use the same Azure Cosmos DB account Connectionstring created above
	
  * Method: GetUser
    * Same as above except
      * Triggers:
        * Http Methods: Get
        * Under Route Template: keguser/{id}
      * Inputs:
        * Click on +New Input
        * Update Database name: KegocnizerData
        * SQL Query: SELECT * FROM c WHERE c.type = "KegUser" AND c.hashcode = {id}
        * Select appropriate Cosmos DB account connectionstring
        * Document parameter name: items  ( this is being used in run.csx)
        * Collection name: Items
      * Outputs:
        * No change
		
  * Method: GetConfig
    * Same as above except
      * Triggers:
        * Http Methods: Get
        * Triggers Under Route Template: kegconfig/{id}
      * Inputs: (Click on +)
        * Update Database name: KegocnizerData
        * SQL Query: SELECT * FROM c WHERE c.type = "KegConfig"  AND c.id = {id}
        * Select appropriate Cosmos DB account connectionstring
        * Document parameter name: items  ( this is being used in run.csx)
        * Collection name: Items
      * Outputs:
        * No change

All the GetConfig changes can be validated by seeing View Files

  * Click on GetConfig on Left Pane
  * Expand 'View files' available on the right side of window collapsed.
  * Select function.json
	
Note: Azure functions can be mapped to Items Collection directly from CosmosDB resource as well, see below.

Under same Azure Cosmos DB resource created OR Go to Go to 'kegocnizerdemodb' resource created

  * Click on 'Add Azure Function' ( left Pane)
  * Select Collection created ( ex: Items)
  * Select the newly created Azure Function 'KegocnizerDemoFunctions'
  * Enter new name of Function to be created
  * Keep rest same and Click Save

Note: You can configure Add methods with more restrictive access using Authorization Levels. But that is not covered here.

e. Testing Azure Methods Created above

  * Check if you are able to run the Host ( as per configuration above, this is url, it might be different for you)
    https://kegocnizerdemofunctions.azurewebsites.net/

  * Test AddConfig

    * Navigate to AddConfig Method and Click Run Button. There is Logs Pane in bottom of the screen to see host output. 
    
    Sample Output:
    
```csharp
2018-05-22T21:19:44 No new trace in the past 1 min(s). 
2018-05-22T21:20:31.240 [Info] Script for function 'AddConfig' changed. Reloading. 
2018-05-22T21:20:31.459 [Info] Compilation succeeded. 
2018-05-22T21:20:32.178 [Info] Function started (Id=f0f2be78-ba13-4145-9304-3c592bcb8b41)
2018-05-22T21:20:32.397 [Info] AddConfig: Adding new KegConfig ... 
2018-05-22T21:20:32.397 [Info] AddConfig: Adding KegConfig complete. 
2018-05-22T21:20:32.506 [Info] Function completed (Success, Id=f0f2be78-ba13-4145-9304-3c592bcb8b41, Duration=324ms) 
```

  * Test GetConfig

    * Copy the above generated Id guid and go to GetConfig Method
    * Right Pane, Expand Test Pane at the Right of window and Select HTTP Method = Get
    * Add Parameter and enter
    * Id and fdaeadba-4027-407d-bd9a-dd679c223f65 ( Go to the KegocnizerData > Items and see the newly created entry related to KegConfig )
		
This Guid is to be updated in Constants.cs under KEGSETTINGSGUID
		
  * Test AddUser

    * Same as above

  * Test GetUser

    * Same as above, but while updating the parameter, go to database, update the hashcode to some value and test with that value here. Because we are getting user by hashcode and not exactly with id. See the SQL Query of GetUser

f. Getting the Application Insights Instrumentation Key
  
  * You can get this in multiple ways as we are using same Application Insights Key for Function Apps, Keg UWP, KegAdmin. You could choose to use different though.
  * Navigate to KegocnizerDemo Resource Group and look for application insights
  * Open it and look for Keys(Properties) - this value is to be updated in Constants.cs under INSTRUMENTKEY
	
g. Getting the KegConfig

  * Navigate to the database and get appropriate KegConfig after updating the value either using Admin App or manually. 

## Source of the Azure Functions referenced by the steps above

Method: AddConfig

```csharp
#r "Newtonsoft.Json"

using System;
using Newtonsoft.Json;

public static void Run(HttpRequestMessage req, out dynamic outputDocument)
{
    log.Info("C# HTTP trigger function processed a request.");
    dynamic body = req.Content.ReadAsStringAsync().Result;
    var e = JsonConvert.DeserializeObject<KegConfig>(body as string);
    e.Type = "KegConfig"; // mandatory
    outputDocument = e;
}

public class KegConfig
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("id")]
    public string Id {get;}

    [JsonProperty("maxeventdurationminutes")]
    public int MaxEventDurationMinutes { get; set; }
    [JsonProperty("maxvisitorsperevent")]
    public int MaxPersonsPerEvent { get; set; }
    [JsonProperty("maxuserouncesperhour")]
    public int MaxUserOuncesPerHour { get; set; }
    [JsonProperty("corehours")]
    public string CoreHours { get; set; }
    [JsonProperty("coredays")]
    public string CoreDays { get; set; }
    [JsonProperty("maintenance")]
    public bool MaintenanceMode { get; set; }
    [JsonProperty("weightcallibration")]
    public Int64 WeightCallibration { get; set; }
    [JsonProperty("maxkegweight")]
    public float MaxKegWeight { get; set; }
    [JsonProperty("emptykegweight")]
    public float EmptyKegWeight { get; set; }
    [JsonProperty("maxkegvolumeinpints")]
    public float MaxKegVolumeInPints { get; set; }
}

``` 

Method: AddUser

```csharp
#r "Newtonsoft.Json"

using System;
using Newtonsoft.Json;

public static void Run(HttpRequestMessage req, out dynamic outputDocument)
{
    log.Info("AddUser: Adding new KegUser!");
	dynamic body = req.Content.ReadAsStringAsync().Result;
    var e = JsonConvert.DeserializeObject<KegUser>(body as string);
    e.Type = "KegUser"; // mandatory
    outputDocument = e;
}

public class KegUser
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("hashcode")]
    public string HashCode { get; set; }
    [JsonProperty("isapprover")]
    public bool IsApprover { get; set; }
}

```

Method: GetUser

```csharp
using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, IEnumerable<dynamic> items)
{
    return req.CreateResponse(HttpStatusCode.OK, items);
}


Method: GetConfig

using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, IEnumerable<dynamic> items)
{
    return req.CreateResponse(HttpStatusCode.OK, items);
}
```
