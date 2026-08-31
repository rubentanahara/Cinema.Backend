SOLUTION := Cinema.slnx
CONFIG   ?= Debug
SERVICES := Catalog Seating Pricing Ordering Payments Ticketing Loyalty Concessions Identity Notifications
GATEWAY  := http://localhost:5100

.DEFAULT_GOAL := build
.PHONY: build restore clean up logs tools format test status health down schema

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

schema:
	./Scripts/export-schemas.sh

format:
	dotnet format $(SOLUTION)

test:
	dotnet test $(SOLUTION) -c $(CONFIG)

status:
	@curl -s --max-time 5 -X POST $(GATEWAY)/graphql \
		-H 'Content-Type: application/json' \
		-d '{"query":"{ catalogStatus { name } seatingStatus { name } pricingStatus { name } orderingStatus { name } paymentsStatus { name } ticketingStatus { name } loyaltyStatus { name } concessionsStatus { name } identityStatus { name } notificationsStatus { name } }"}' \
		|| echo "gateway unreachable"
	@echo ""

health:
	@printf 'gateway %s\n' "$$(curl -s --max-time 3 $(GATEWAY)/health || echo unreachable)"

down:
	docker compose down
