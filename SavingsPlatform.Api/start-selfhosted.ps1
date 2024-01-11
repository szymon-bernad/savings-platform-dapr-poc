dapr run `
    --app-id dapr-savings-acc `
    --app-port 5136 `
    --dapr-http-port 3605 `
    --dapr-grpc-port 60005 `
    --config ../dapr/config.yaml `
    --resources-path ../dapr/components `
    dotnet run