
# Kegocnizer, v1.0 - Architecture

## Different Layers of the solution:
### a. Keg.DAL
	DataAccessLayer is the single point of interface for the UI layer in setting and getting any data from sensors, storage and cloud. In this current architecture, each sensor and its opertions are under separate binary and combined under one DAL.
	For Instance, Temperature and Weight Sensors are disconnected from DAL and can be managed separately. If we are building number of applications, keeping each sensor separate and reference them as needed would be more clean and serviced separetly. It does not mean that we cannot combine all sensors into one library.
	Again, recommended to separate all the sensors into different components so that they can be individually managed and serviced at later time with out impacting other layers.
	
### b. Keg.UWP
	UX Layer (App Layer) with 2 different Pages. 
	1. MainPage.xaml: Front view Interface
	2. Page2.xaml	: Flow Consumption

	Pages are designed considering the responsive layout, but its driven by IoT Core Orientation configured in Windows Device Portal (typically http://<IPAddress:8080> ). 
	Removing this dependency is pretty easy and can be configured as per screen size as well. See VisualState.StateTriggers in .xaml. More info at [Responsive Design](https://docs.microsoft.com/en-us/windows/uwp/design/layout/layouts-with-xaml target="_blank")

### c. Sensors.Temperature
	Exposes interfaces to configure, read temperature from unlined temperature sensors using configured GPIO Pins.

### d. Sensors.Weight
	Exposes interfaces to configure, get Weight from weight sensors.

## Nuget Packages:
1. Microsoft.Data.Sqlite: Local Storage. For storing user visits, visitor consumption in during an event. 
2. Microsoft.Toolkit.UWP: Responsive layout. Used DockPanel and other controls.
3. Microsoft.ApplicationInsights: Logging insights events, exceptions, traces, metrics to the cloud for future analysis.
4. CosmosDB: Storing System configuration, User Authorization ( via. Admin App)
5. Azure Functions - Encapsulating Secret key from UWP and connecting to CosmosDB


## CosmosDB Configurations:
Currently App is configured to use Kegocnizer Database ( CosmosDB) with name 'Items' as Document List. This Items Collection stores Users (type: KegUser) and Configs ( type:KegConfig). 
In future expansion, This collection can be single interface for hosting many kegocnizers backed with one database. But, as per current design, each app deployed uses one KegConfig hardcoded into application under \\Keg.UWP\\Utils\\Common.cs ( KEGSETTINGSGUID )

### Following are the different Entities in KegConfig
Below are the properties that are being managed from Cloud ( CosmosDB) depending on one own's policies.
<ol type="a">
<li>maxeventdurationminutes: Reset Timeline in limiting user in consuming.</li>
<li>userconsumptionreset: User consumption reset in minutes.</li>
<li>maxuserouncesperhour : Max consumption allowed. That said, user can consume <i>'maxuserouncesperhour'</i> quantity in <i>'userconsumptionreset'</i> minutes defined. </li>
<li>corehours: Time in which user <b>NOT</b> allowed to consume or machine is locked to despense.
	</br>ex: 6:30T15:30  - User is not allowed to consume between 6:30 AM to 3:30 PM. 
	</br>ex: 6:30T15:30,20:30T23:30 - Multiple timeslots
</li>
<li>coredays: Days in which user is <b>allowed</b> to consume </li>
<li>maintenance: 1 - Maintenance mode, 0 - Regular mode.</li>
<li>maxkegvolumeinpints: Maximum keg volume inside the keg based on keg size.</li>
<li>Weightcallibration, maxkegweight, emptykegweight:
	</br>Above 3 helps in callibration of the actual keg volume left inside the keg.
</li>
<li>maxvisitorsperevent: Maximum number of persons allowed in each event (maxeventdurationminutes)</li>
</ol>

### Constants (Inside App)
Apart from above, there are couple of more details configured and hardcoded inside the app
1. Constants ( \\Keg.DAL\\Constants.cs )   - <b>REQUIRED</b>
<ol type="i">
	<li><b>COSMOSAzureFunctionsURL:</b> Update this with Azure Functions Url </li>
	<li><b>KEGSETTINGSGUID:</b> Guid required to reach out to cloud and pull the configurations.</li>
</ol>
2. Common ( Keg.UWP) ( Keg.UWP\\Utils\Common.cs )
<ol type="i">
	<li>CounterWait : Default Timer</li>
	<li>CounterShortWait: Backup timer helps in showing Popup to direct user to main if keg is idle</li>
	<li>AppWindowWidth:  Since we are depending on Orientation from Windows Device Portal, using this as staging property, but user who want to configure their devices or app on custom width and height can use this to show in portrait or landscape.</li>
</ol>

## Localization:
	Currently it just tuned for en-us as default and Localization strings are from en-us.
	(\\Keg.UWP\\Strings\\en-us )

## Styles.xaml:
	Control, Font styles are mostly managed through Styles.xaml


## Getting Started
Needed:
a. Azure Subscription
	1. CosmosDB
	2. Azure Functions
b. Raspberry Pi w/SD card
c. Install Windows 10 IoT Core ( use IoT Dashboard from 
