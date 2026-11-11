.PHONY: all setup up down clean download restore \
        proto-java grpc-libs mongo-libs compile-java clean-java \
        run-server-java run-client-java \
        run-server-csharp run-client-csharp

DATA_DIR       := data
ARCHIVE_FILE   := $(DATA_DIR)/sampledata.archive
ARCHIVE_URL    := https://atlas-education.s3.amazonaws.com/sampledata.archive
CONTAINER_NAME := mflix-db

# Java
JAVA_DIR     := java
JAVA_LIB     := $(JAVA_DIR)/lib
JAVA_OUT     := $(JAVA_DIR)/generated
JAVA_BUILD   := $(JAVA_DIR)/build
TOOLS_DIR    := tools
MAVEN_MONGO  := https://repo1.maven.org/maven2/org/mongodb
MONGO_VER    := 5.2.0

PROTO_DIR    := proto/
PROTO_FILE   := mflix.proto

PROTOC       ?= protoc
PROTOC_VER   := $(shell $(PROTOC) --version 2>/dev/null | sed 's/libprotoc //')
PROTOBUF_VER ?= $(shell echo "$(PROTOC_VER)" | awk -F. '{ if ($$1 >= 22) print "4." $$1 "." $$2; else print $$0 }')

GRPC_VER         ?= 1.81.0
GRPC_JAVA_PLUGIN := $(TOOLS_DIR)/protoc-gen-grpc-java
GRPC_PLUGIN_URL  := https://repo1.maven.org/maven2/io/grpc/protoc-gen-grpc-java/$(GRPC_VER)/protoc-gen-grpc-java-$(GRPC_VER)-linux-x86_64.exe

JAVA_CP      := $(JAVA_BUILD):$(JAVA_LIB)/*

all: compile-java

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

# Java (gRPC)
$(GRPC_JAVA_PLUGIN):
	mkdir -p $(TOOLS_DIR)
	wget -O $@ $(GRPC_PLUGIN_URL)
	chmod +x $@

proto-java: $(GRPC_JAVA_PLUGIN)
	mkdir -p $(JAVA_OUT)
	$(PROTOC) --plugin=protoc-gen-grpc-java=$(GRPC_JAVA_PLUGIN) --proto_path=$(PROTO_DIR) --java_out=$(JAVA_OUT) --grpc-java_out=$(JAVA_OUT) $(PROTO_FILE)

grpc-libs:
	mkdir -p $(JAVA_LIB)
	MAVEN_OPTS="--sun-misc-unsafe-memory-access=allow" mvn -q -f pom.xml dependency:copy-dependencies -DoutputDirectory=$(JAVA_LIB) -Dprotobuf.version=$(PROTOBUF_VER) -Dgrpc.version=$(GRPC_VER)

mongo-libs:
	mkdir -p $(JAVA_LIB)
	wget -N -P $(JAVA_LIB) $(MAVEN_MONGO)/mongodb-driver-sync/$(MONGO_VER)/mongodb-driver-sync-$(MONGO_VER).jar
	wget -N -P $(JAVA_LIB) $(MAVEN_MONGO)/mongodb-driver-core/$(MONGO_VER)/mongodb-driver-core-$(MONGO_VER).jar
	wget -N -P $(JAVA_LIB) $(MAVEN_MONGO)/bson/$(MONGO_VER)/bson-$(MONGO_VER).jar
	wget -N -P $(JAVA_LIB) $(MAVEN_MONGO)/bson-record-codec/$(MONGO_VER)/bson-record-codec-$(MONGO_VER).jar

compile-java: proto-java grpc-libs mongo-libs
	rm -rf $(JAVA_BUILD)
	mkdir -p $(JAVA_BUILD)
	javac -cp "$(JAVA_LIB)/*" -d $(JAVA_BUILD) $$(find $(JAVA_DIR)/client $(JAVA_DIR)/common $(JAVA_DIR)/server $(JAVA_OUT) -name '*.java')

run-server-java: compile-java
	java --enable-native-access=ALL-UNNAMED --sun-misc-unsafe-memory-access=allow -cp "$(JAVA_CP)" server.Server

run-client-java: compile-java
	java --enable-native-access=ALL-UNNAMED --sun-misc-unsafe-memory-access=allow -cp "$(JAVA_CP)" client.Client

clean-java:
	rm -rf $(JAVA_BUILD) $(JAVA_OUT)

# C# (gRPC) — o código do .proto é gerado pelo Grpc.Tools no dotnet build
run-server-csharp:
	dotnet run --project csharp/server

run-client-csharp:
	dotnet run --project csharp/client