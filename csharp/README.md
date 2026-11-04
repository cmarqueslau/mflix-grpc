# MFlix — C#

Aluna: Caroline Marques Lau

Implementação em C# de um serviço de gerenciamento de filmes que utiliza MongoDB para armazenamento e gRPC para comunicação entre cliente e servidor.

## Tecnologias

- C#
- .NET 10
- ASP.NET Core (Kestrel, HTTP/2)
- gRPC (`Grpc.AspNetCore` no servidor, `Grpc.Net.Client` no cliente)
- Protocol Buffers (Protobuf)
- MongoDB
- Serilog

## Pré-requisitos

- .NET SDK 10.
- MongoDB em execução e acessível pelo servidor (veja a seção *Banco de Dados* no README da raiz do repositório).
- Arquivo `.env` na raiz do repositório (veja a seção *Configuração* no README da raiz). Cliente e servidor procuram o `.env` na pasta atual e nas pastas acima dela.

## Compilação

Execute na raiz do repositório:

```bash
dotnet build csharp/server
dotnet build csharp/client
```

## Execução

Inicie o servidor:

```bash
make run-server-csharp
```

Em outro terminal, execute o cliente:

```bash
make run-client-csharp
```

O servidor escuta no endereço definido em `SERVER_ADDR` usando HTTP/2 sem TLS, e o cliente se conecta a `http://<SERVER_ADDR>`.

## Funcionalidades

- Criar, consultar, atualizar e excluir filmes.
- Listar filmes por ator e gênero (recebidos por *server streaming*).
