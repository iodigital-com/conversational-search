helm repo add weaviate https://weaviate.github.io/weaviate-helm
helm install my-weaviate weaviate/weaviate

secrets to be created:
- authentication.apikey.allowed_keys
- 