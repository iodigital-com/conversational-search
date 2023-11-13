# ConversationalSearchPlatform

## Projects

### ConversationalSearchPlatform.BackOffice

- Built with:
    - .NET 8 RC 2 (can, and should be updated to .NET 8)
    - Blazor server
- Contains:
    - Contains all application logic, services and jobs concerning:
        - Indexing
        - Managing tenants
        - Scraping
        - Holding conversations
        - Consumption cost

### ConversationalSearchPlatform.Scraper

- Built with
    - Node 18.18.0
    - Express
    - Puppeteer
- Contains:
    - Simple web application that spawns a headless chrome instance that scrapes a page

### ConversationalSearchPlatform.Widget

- Built with
    - Node 18.18.0
    - Preact
    - Vite
- Contains:
    - A (for now) unstyled chat component developed in Preact that gets built into a WebComponent.
    - The goal is for this component is to be used for consumers and client demo purposes.

## Local development - BackOffice

### Start required services

The following command will boot up:

- A SQL Server database docker container
- Unstructured docker container (tool which chunks html content)
- Weaviate vector database with text2vec transformers and multi2vec-clip modules
- Scraper docker container using puppeteer

#### Windows / Intel Mac

- `docker-compose up -d csp-db-amd64 unstructured weaviate t2v-transformers multi2vec-clip scraper`

#### OSX

- `docker-compose up -d csp-db-arm unstructured weaviate t2v-transformers multi2vec-clip scraper`

### Execute database migrations

- Use the `dotnet ef update` statements [below](#db-migrations) for both DbContexts to update your database to the
  latest migrations.
- This will install several users, roles and tenants which can be found in `UserAndRolesDatabaseSeeder`
  an `TenantSeeder`

### Start the application

- Use your favorite IDE Visual Studio / Rider / VS Code or even the CLI (dotnet run) to start
  the `ConversationalSearchPlatform.Backoffice` application with the development profile.
- At first startup some Hangfire schema migrations might occur.

### DB Migrations

#### ApplicationDbContext

```
dotnet ef migrations add --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.ApplicationDbContext --configuration Debug MIGRATION_NAME --output-dir Data/Migrations
dotnet ef database update --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.ApplicationDbContext --configuration Debug GENERATED_MIGRATION_NAME
```

#### TentantDbContext

```
dotnet ef migrations add --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.TenantDbContext --configuration Debug MIGRATION_NAME --output-dir Data/Migrations/Tenant
dotnet ef database update --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.TenantDbContext --configuration Debug GENERATED_MIGRATION_NAME
```

## Local development - Scraper

### Install dependencies

- In the `ConversationalSearchPlatform.Scraper folder execute:`
- `npm install`

### Start the application

- `npm run start`

## Local development - Widget

### Install dependencies

- In the `ConversationalSearchPlatform.Widget folder execute:`
- `yarn install`

### Start the application

- `yarn dev`

## Deployment

### Github actions

- There are two workflows:
    - `application.yml`: deployment for the application, should be triggered on pushes on `main` + changes in
      the `backoffice/**` folder
    - `scraper.yml`: deployment for the application, should be triggered on pushes on `main` + changes in
      the `scraper/**` folder
- Some remarks for if this project ever gets moved to another resource group:
    - Make sure that `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` exist in Github secrets (and are valid).
    - Make sure there is a new service principal which can be used to pull and push images (AcrPull, AcrPush roles) to
      the container registry which is bound the AKS cluster.

### Kubernetes

- Deployment is done to Kubernetes.

#### Access Kubernetes

You will only have to execute these steps once to save the kubeconfig credentials locally.

##### Requirements

- Azure CLI

##### Steps

- Find your tenantId in Azure Portal (Top right hand corner -> Switch directory -> Directory ID)
- `az login --tenant {AzureTenantId}`
- From this command you will get a list of subscriptions, choose the right subscription id and execute:
- `az account set --subscription {SubscriptionId}`
- Go to Azure Portal and copy the resource group name and AKS deployment name. Execute the following command:
- `az aks get-credentials --overwrite-existing --resource-group {ResourceGroupName} --name {AKSDeploymentName}`
- You have now added the credentials to your local kubeconfig. Subsequently you can now
  use [(Open)Lens](https://github.com/MuhammedKalkan/OpenLens/releases/tag/v6.5.2-366) to view progress
  in the cluster.

#### Applying changes

- In the `iac` folder, manifests can be found per application which can be applied with `kubectl apply -f ./foldername'
- The weaviate folder does not use plain old Kubernetes objects, but uses helm instead. Read the readme in
  the `2-weaviate` folder to understand how to (re)deploy weaviate.

#### Remarks Azure move

- Some remarks for when we should ever move this POC to another resource group in context of it being not a POC anymore:
    - The application, which can be found in the `1-app` folder has:
        - a `SecretProvider` class. This Secret provider class needs `Workload identity` creation before being able to
          pull secrets from an Azure KeyVault.
            - Use the following guide to create
              this `Workload identity` https://learn.microsoft.com/en-us/azure/aks/csi-secrets-store-driver.
            - Also make sure that the Kubernetes cluster has OIDC Issuer
              enabled https://learn.microsoft.com/en-us/azure/aks/use-oidc-issuer#update-an-aks-cluster-with-oidc-issuer
        - If the pod cannot mount the secrets volume:
            - make sure to check all the steps concerning the secret provider.
            - make sure all the secrets mentioned in the SecretProvider are in the keyvault.
    - Run the Github Action Workflows first, they will create the correct images in the container registry before trying
      to deploy to AKS.