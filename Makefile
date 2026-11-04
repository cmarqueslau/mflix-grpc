.PHONY: all setup up down clean download restore run-server-csharp run-client-csharp

DATA_DIR       := data
ARCHIVE_FILE   := $(DATA_DIR)/sampledata.archive
ARCHIVE_URL    := https://atlas-education.s3.amazonaws.com/sampledata.archive
CONTAINER_NAME := mflix-db

all: setup

# Sobe o MongoDB e carrega a base de exemplo (sample_mflix)
setup: restore

# Docker (MongoDB)
up:
	sudo docker compose up -d

down:
	sudo docker compose down

clean:
	sudo docker compose down -v

download:
	mkdir -p $(DATA_DIR)
	wget -N -P $(DATA_DIR) $(ARCHIVE_URL)

restore: up download
	sudo docker exec -i $(CONTAINER_NAME) mongorestore --archive --drop < $(ARCHIVE_FILE)

# C# (gRPC) — o código do .proto é gerado pelo Grpc.Tools no dotnet build
run-server-csharp:
	dotnet run --project csharp/server

run-client-csharp:
	dotnet run --project csharp/client
