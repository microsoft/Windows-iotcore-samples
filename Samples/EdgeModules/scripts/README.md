# Scripts Overview

The scripts and json files in this directory provide a simple macro and template system for managing Azure credentials and hardware configurations for deployment files in a safer and more modular way.

## Setup

1. copy credsmacrosexample.json creds.macros.json
2. copy urls.credsmacroexample.json urls.creds.macros.json

Edit the new files and fill in urls, usernames, and passwords as appropriate for you environment.
If you're only using public bits and not a private preview then you can delete or ignore the preview sections.

These new files that have been edited to contain real credentials match a .gitignore pattern in this repo and can't inadvertently be checked in.

It's also recommended to generate the final processed output into an obj or bin directory since the generated files will contain real creds and bin/* and obj/* are also in .gitignore for this repo.

## Usage

publish-deployment.ps1 <deployment template file>.json -hardwaremacrofile <board type>.macros.json
