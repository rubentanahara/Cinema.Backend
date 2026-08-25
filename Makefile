SOLUTION := Cinema.slnx
APPHOST  := Src/AppHost/Cinema.AppHost.csproj
CONFIG   ?= Debug
SERVICES := Catalog Seating Pricing Ordering Payments Ticketing Loyalty Concessions Identity Notifications
PORTS    := 5101 5102 5103 5104 5105 5106 5107 5108 5109 5110

.DEFAULT_GOAL := build
.PHONY: build restore clean run dev tools format test status health down

build:
	dotnet build $(SOLUTION) -c $(CONFIG)

restore:
	dotnet restore $(SOLUTION)

clean: down
	rm -rf artifacts

tools:
	dotnet tool restore

run:
	aspire run --project $(APPHOST)

dev:
	dotnet run --project $(APPHOST)

format:
	dotnet format $(SOLUTION)

test:
	dotnet test $(SOLUTION) -c $(CONFIG)

status:
	@for p in $(PORTS); do \
		printf '%s ' "$$p"; \
		curl -s --max-time 3 -X POST http://localhost:$$p/graphql \
			-H 'Content-Type: application/json' \
			-d '{"query":"{ serviceStatus { name checkedAt } }"}' || echo "no response"; \
		echo ""; \
	done

health:
	@for p in $(PORTS); do \
		printf '%s %s\n' "$$p" "$$(curl -s --max-time 3 http://localhost:$$p/health || echo unreachable)"; \
	done

down:
	@pkill -f Cinema.AppHost 2>/dev/null || true
	@docker ps -q --filter name=postgres- | xargs -r docker rm -f >/dev/null 2>&1 || true
	@echo "stopped"
