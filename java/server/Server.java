/*
   Data última atualização: 10/11/2026
   Estudante: Maria Eduarda Bambini
   Servidor do gerenciador de filmes (Mflix) via gRPC.
*/

package server;

import java.io.IOException;

import common.Config;
import common.Style;
import io.grpc.ServerBuilder;

/* Inicializa o servidor gRPC e registra o serviço de filmes. */
public class Server {

    /* Carrega as configurações, conecta ao MongoDB e inicia o servidor. */
    public static void main(String[] args) throws InterruptedException {
        Config config = new Config(".env");
        MovieRepository repository = new MovieRepository(config.getMongoUri(), config.getMongoDb(), config.getMongoCollection());

        io.grpc.Server grpcServer = ServerBuilder.forPort(config.getServerPort())
                .addService(new MovieServiceImpl(repository))
                .build();

        try {
            grpcServer.start();
        } catch (IOException e) {
            Style.showError(e.getMessage());
            return;
        }

        Style.showInfo("Servidor gRPC aguardando conexões na porta " + config.getServerPort());
        Runtime.getRuntime().addShutdownHook(new Thread(grpcServer::shutdown));
        grpcServer.awaitTermination();
    }
}