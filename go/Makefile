GO_OUT     := internal/pb
PROTO_DIR  := ../proto
PROTO_FILE := mflix.proto

.PHONY: all run-server run-client build-server build-client build clean

all: proto build

run-server: 
	go run ./cmd/server

run-client: 
	go run ./cmd/client

build-server: 
	go build -o bin/server ./cmd/server

build-client: 
	go build -o bin/client ./cmd/client

build: build-server build-client

clean: 
	rm -rf bin/

proto: 
	mkdir -p $(GO_OUT)
	protoc --proto_path=$(PROTO_DIR) \
	       --go_out=$(GO_OUT) --go_opt=paths=source_relative \
	       --go-grpc_out=$(GO_OUT) --go-grpc_opt=paths=source_relative \
		   $(PROTO_FILE)
