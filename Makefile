SOLUTION := Cinema.slnx
API_PROJ := src/Api/Cinema.Api.csproj
CONFIG   ?= Debug
API      := http://localhost:5100

.DEFAULT_GOAL := build
.PHONY: build restore clean dev up logs down tools format test schema status health

build:
	dotnet build $(SOLUTION) -c $(CONFIG)

restore:
	dotnet restore $(SOLUTION)

clean: down
	rm -rf artifacts

tools:
	dotnet tool restore

up:
	docker compose up -d

logs:
	docker compose logs -f

down:
	docker compose down

dev:
	dotnet run --project $(API_PROJ)

format:
	dotnet format $(SOLUTION)

test:
	dotnet test $(SOLUTION) -c $(CONFIG)

schema:
	dotnet run --project $(API_PROJ) -- schema export --output $(CURDIR)/src/Api/schema.graphql

status:
	@curl -s --max-time 5 -X POST $(API)/graphql \
		-H 'Content-Type: application/json' \
		-d '{"query":"{ catalogStatus { name } seatingStatus { name } pricingStatus { name } orderingStatus { name } paymentsStatus { name } ticketingStatus { name } loyaltyStatus { name } concessionsStatus { name } identityStatus { name } notificationsStatus { name } }"}' \
		|| echo "api unreachable"
	@echo ""

health:
	@printf 'api %s\n' "$$(curl -s --max-time 3 $(API)/health || echo unreachable)"
