# Gerenciamento de filmes

Alunos: Caroline Marques Lau, Gustavo Martins França e Maria Eduarda Bambini

Disciplina: Sistemas Distribuídos

Implementar um serviço de gerenciamento de filmes usando os dados providos pela coleção movies da base de dados
[Mflix](https://www.mongodb.com/docs/atlas/sample-data/sample-mflix/), com comunicação entre cliente e servidor via gRPC.

## Funcionalidades

- Criar, consultar, atualizar e excluir filmes.
- Listar filmes por ator e gênero.

## Banco de Dados

O SGBD utilizado é o MongoDB. Para iniciar um container com os dados de exemplo do MongoDB Atlas (`sample_mflix` e outras coleções), execute na raiz do repositório:

```bash
make setup
```

O banco ficará disponível em `mongodb://localhost:27017`.

Outros comandos:

| Comando      | Descrição                                          |
|--------------|----------------------------------------------------|
| `make up`    | Inicia o container (dados já carregados)           |
| `make down`  | Para o container, mantendo os dados                |
| `make clean` | Para o container e remove o volume com os dados    |

## Configuração

Copie o arquivo `.env.example` para `.env` na raiz do repositório e ajuste os valores, se necessário:

| Variável             | Descrição                                   | Exemplo                     |
|----------------------|---------------------------------------------|-----------------------------|
| `SERVER_ADDR`        | Endereço (IP:porta) do servidor gRPC        | `127.0.0.1:9090`            |
| `MONGODB_URI`        | URI de conexão com o MongoDB                | `mongodb://localhost:27017` |
| `MONGODB_DB`         | Nome da base de dados                       | `sample_mflix`              |
| `MONGODB_COLLECTION` | Nome da coleção                             | `movies`                    |

## Protocolo de Comunicação

A comunicação entre cliente e servidor utiliza **gRPC**, que transporta as chamadas sobre **HTTP/2** e serializa as mensagens com **Protocol Buffers (Protobuf)**. O próprio gRPC delimita as mensagens, identifica a operação chamada e transporta os códigos de erro, portanto não há um protocolo de enquadramento próprio sobre TCP.

O arquivo [`proto/mflix.proto`](proto/mflix.proto) define o contrato entre cliente e servidor.
