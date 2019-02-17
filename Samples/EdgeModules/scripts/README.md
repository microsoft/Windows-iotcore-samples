# Scripts Overview

The scripts and json files in this directory provide a simple macro and template system for managing Azure credentials and hardware configurations for deployment files in a safer and more modular way.

## Setup

copy credsmacrosexample.json creds.macros.json
copy urls.credsmacroexample.json urls.creds.macros.json

edit the new files and fill in urls, usernames, and passwords as appropriate for you environment.
if you're only using public bits and not a private preview then you can delete or ignore the preview
sections.

these new files that have been edited to contain real credentials match a .gitignore pattern in this repo and can't inadvertently be checked in.

## Usage

publish-deployment.ps1 <deployment template file>.json -hardwaremacrofile <board type>.macros.json

