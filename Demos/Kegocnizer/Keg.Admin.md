# Kegocnizer Admin App, v1.0

This demo project describes the development of an UWP app to authorize users of legal drinking age to use the Kegocnizer to dispense beer.

## The Requirements

* UWP app that can be run by Admins to whitelist users and change configuration settings for the Kegocnizer
* Store the whitelisted users in Cosmos DB documents in the Cloud

## Hardware Components:

* Keyboard Wedge card reader - RFIDeas PcProx Plus

## Software Components:

* Microsoft Visual Studio 2017

## Software Setup

* Use Command Prompt to navigate to the folder where you want your project
* Run the git clone command to download the project
* Open the Admin.UWP.sln file in the Admin.UWP folder using Visual Studio 2017
* Replace the CosmosDB keys in the code
* Run the code

## Software Use

* There is no Add Admin feature in the v1.0 of the Admin.UWP app. Please make sure you have already added a user as an Admin in CosmosDB
* Connect the USB card reader
* When the app launches, scan the Admin Card to get access to the whitelisting functionality
* Click on the Add New User button to add a new user
* Scan the new users badge to add them to CosmosDB and whitelist them to use the Kegocnizer
