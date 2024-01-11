dapr run `
    --app-id dapr-savings-events `
    --app-port 5163 `
    --dapr-http-port 3603 `
    --dapr-grpc-port 60003 `
    --config ../dapr/config.yaml `
    --resources-path ../dapr/components `
    dotnet run