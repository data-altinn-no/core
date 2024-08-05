# Introduction

Welcome to data.altinn.no (DAN)! This repository has a solution file containing several components

## DAN CORE

DAN Core is the main component of DAN, implementing the public API. It utilizes that Altinn REST API for Service Owners for authorization (including consent) and maps requests for data sets to the relevant plugins implementing the codes.

The repository contains a .NET6 console application utilizing the isolated-process Azure Functions v4 runtime.

## DAN Common

DAN Common contains all shared models and utilites used across all DAN projects. A nuget is built in Azure Devops which must be referred to in all plugin implementations. 

# Getting Started

## Requirements

DAN can be used with any IDE / editor of your choosing, but we recommend Visual Studio 2022 or later, Visual Studio Code or Jetbrains Rider.

### 1. .NET 6 SDK and runtime

### 2. Azure Functions Core Tools
In order to build an run functions locally, you will need to install the [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) which depends on [NodeJS](https://docs.npmjs.com/getting-started/installing-node).

### 3. Microsoft Azure Storage Emulator
In order to run functions locally, you will need a Azure Storage Emulator, preferably [Azurerite](https://github.com/Azure/Azurite) installed and running on 127.0.0.1:10000 (default)

### 4. CosmosDB emulator
Download and install the CosmosDB emulator. For Windows, [click here to download](https://aka.ms/cosmosdb-emulator). See [this page](https://docs.microsoft.com/en-us/azure/cosmos-db/linux-emulator?tabs=ssl-netstd21) for information about running on Linux / MacOS.

After installation you will ned to create the default database. Go to https://localhost:8081/_explorer/index.html, select Explorer, Select "New Container". Name the database "nadobe-dev", and the container id "Accreditations". Supply "/owner" as partition key. 

### 5. Redis

DAN is utilizing Redis for caching. See https://github.com/ServiceStack/redis-windows for options for Windows. Redis is also available for Linux / MacOS, and should be installed and run with default settings.

# Contribute

The `master` branch is locked and cannot be pushed to directly, instead you must create a feature branch to which you commit all your code.

Feature branches should be named with the prefix `Feature/` and be named in a descriptive manner *(details yet to be determined)*. When you have pushed your feature branch, create a pull request against `master` and request a review from your team. When the pull request is approved, you may merge the branch.

Commits to `master` will initiate a full test run (unit, functional, integration) and will upon successful completion tag and deploy the master branch to the `dev` function slot in Azure. 

## A note about settings

Settings for the application is stored in Azure, and can be viewed in [Azure Portal](https://portal.azure.com) if you have access (this is not required for development). When developing and running functions locally, the settings are loaded from `local.settings.json`. Note that while this is a part of the repository for your convenience, these settings are **only** used locally and **not** in Azure,
