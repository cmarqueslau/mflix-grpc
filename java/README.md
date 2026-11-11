## Sobre o programa

Serviço de gerenciamento de filmes sobre a coleção `movies` do banco `sample_mflix` (MongoDB), acessado remotamente gRPC.

* Última atualização: 10/11/2026
* Estudante: Maria Eduarda Bambini 

## Dependências

- JDK (Java 17 ou superior)
- Um MongoDB com o dataset `sample_mflix`, seja com Docker (local) ou MongoDB Atlas.
- Make
- Protoc
- Maven

## Configuração

Crie uma cópia do arquivo `.env.example` e o altere conforme suas especificações:

```cp .env.example .env```

## Como compilar

Execute na raiz do repositório (opcional):

```bash
make compile-java
```

Esse comando gera o código Java a partir do `proto/mflix.proto`, baixa as dependências para `java/lib` e compila o cliente e o servidor em `java/build`.

## Como executar

Execute os comandos na raiz do repositório, pois o arquivo `.env` é lido a partir da pasta em que o programa é iniciado.

Inicie o servidor:

```bash
make run-server-java
```

Em outro terminal, inicie o cliente:

```bash
make run-client-java
```

O servidor escuta no endereço definido em `SERVER_ADDR` (por exemplo, `127.0.0.1:5000`) usando HTTP/2 sem TLS, e o cliente se conecta ao mesmo endereço. Inicie o servidor antes do cliente. Os dois comandos compilam o projeto antes de executar, então rodar `make compile-java` separadamente é opcional.