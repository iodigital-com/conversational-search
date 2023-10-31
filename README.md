# ConversationalSearchPlatform

## DB Migrations

### ApplicatinDbContext

```
dotnet ef migrations add --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.ApplicationDbContext --configuration Debug MIGRATION_NAME --output-dir Data/Migrations
dotnet ef database update --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.ApplicationDbContext --configuration Debug GENERATED_MIGRATION_NAME
```

### TentantDbContext

```
dotnet ef migrations add --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.TenantDbContext --configuration Debug MIGRATION_NAME --output-dir Data/Migrations/Tenant
dotnet ef database update --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.TenantDbContext --configuration Debug GENERATED_MIGRATION_NAME
```


## Github actions
- There are two workflows:
  - `application.yml`: deployment for the application, should be triggered on pushes on `main` + changes in the `backoffice/**` folder
  - `scraper.yml`: deployment for the application, should be triggered on pushes on `main` + changes in the `scraper/**` folder
- Some remarks:
  - Make sure that `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` exist in Github secrets.
  - Make sure there is a new service principal which can be used to pull and push images (AcrPull, AcrPush roles) to the container registry which is bound the AKS cluster. 


## Deployment
- Deployment is done to Kubernetes.
- In the `iac` folder, manifests can be found per application which can be applied with `kubectl apply -f ./foldername'
- Some remarks:
  - The application, which can be found in the `1-app` folder has:
    - a `SecretProvider` class. This Secret provider class needs `Workload identity` creation before being able to pull secrets from an Azure KeyVault.
      - Use the following guide to create this `Workload identity` https://learn.microsoft.com/en-us/azure/aks/csi-secrets-store-driver.
      - Also make sure that the Kubernetes cluster has OIDC Issuer enabled https://learn.microsoft.com/en-us/azure/aks/use-oidc-issuer#update-an-aks-cluster-with-oidc-issuer
    - If the pod cannot mount the secrets volume:
      - make sure to check all the steps concerning the secret provider.
      - make sure all the secrets mentioned in the SecretProvider are in the keyvault.
  - Run the Github Action Workflows first, they will create the correct images in the container registry before trying to deploy to AKS.

