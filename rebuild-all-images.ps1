docker build -t leanda/core-persistence:local -f Sds.Osdr.Persistence/Dockerfile .
docker build -t leanda/core-frontend:local -f Sds.Osdr.Domain.FrontEnd/Dockerfile .
docker build -t leanda/core-backend:local -f Sds.Osdr.Domain.BackEnd/Dockerfile .
docker build -t leanda/core-sagahost:local -f Sds.Osdr.Domain.SagaHost/Dockerfile .
docker build -t leanda/core-web-api:local -f Sds.Osdr.WebApi/Dockerfile .
docker build -t leanda/integration:local -f Sds.Osdr.IntegrationTests/Dockerfile .
docker build -t leanda/webapi-integration:local -f Sds.Osdr.WebApi.IntegrationTests/Dockerfile .
docker build -t leanda/e2e-tests:local -f Sds.Osdr.EndToEndTests/Dockerfile .
docker image ls leanda/*