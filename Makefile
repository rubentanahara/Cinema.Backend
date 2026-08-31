SOLUTION := Cinema.slnx
API_PROJ := src/Api/Cinema.Api.csproj
CONFIG   ?= Debug
MODULES  := Catalog Seating Pricing Ordering Payments Ticketing Loyalty Concessions Identity Notifications
MODULE   ?= Catalog
ARCH     := $(shell uname -m | sed 's/x86_64/x64/;s/aarch64/arm64/')
API      := http://localhost:5100

.DEFAULT_GOAL := build
.PHONY: build restore clean dev image up logs down tools format test schema migrate migration seed status health

build:
	dotnet build $(SOLUTION) -c $(CONFIG)

restore:
	dotnet restore $(SOLUTION)

clean: down
	rm -rf artifacts

tools:
	dotnet tool restore

image:
	dotnet publish $(API_PROJ) -c Release --os linux --arch $(ARCH) /t:PublishContainer \
		-p:ContainerRepository=cinema-api -p:ContainerImageTag=latest

up: image
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

migrate:
	@for m in $(MODULES); do \
		printf '%-14s ' "$$m"; \
		dotnet ef database update --context $${m}DbContext \
			--project src/Modules/$$m/Cinema.$$m.csproj --startup-project $(API_PROJ) \
			2>&1 | tail -1; \
	done

seed:
	@docker compose exec -T postgres psql -U cinema -d cinema -v ON_ERROR_STOP=1 -c "\
		insert into catalog.\"Movies\" (\"Id\",\"Title\",\"RuntimeMinutes\",\"ReleasedOn\") values \
		('11111111-1111-1111-1111-111111111111','Dune',155,'2021-10-22'), \
		('22222222-2222-2222-2222-222222222222','Arrival',116,'2016-11-11'), \
		('33333333-3333-3333-3333-333333333333','Sicario',121,'2015-09-18') \
		on conflict (\"Id\") do nothing;"


migration:
	dotnet ef migrations add $(NAME) --output-dir Infrastructure/Migrations \
		--context $(MODULE)DbContext \
		--project src/Modules/$(MODULE)/Cinema.$(MODULE).csproj --startup-project $(API_PROJ)

status:
	@curl -s --max-time 5 -X POST $(API)/graphql \
		-H 'Content-Type: application/json' \
		-d '{"query":"{ movies { title } }"}' \
		|| echo "api unreachable"
	@echo ""

health:
	@printf 'api %s\n' "$$(curl -s --max-time 3 $(API)/health || echo unreachable)"
