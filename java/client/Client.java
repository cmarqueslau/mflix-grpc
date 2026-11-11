/*
   Data última atualização: 10/11/2026
   Estudante: Maria Eduarda Bambini
   Cliente do gerenciador de filmes (Mflix) via gRPC.
*/

package client;

import java.util.ArrayList;
import java.util.List;
import java.util.NoSuchElementException;
import java.util.Scanner;
import java.util.concurrent.TimeUnit;

import common.Config;
import common.Style;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;
import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import mflix.CreateMovieRequest;
import mflix.DeleteMovieRequest;
import mflix.GetMovieRequest;
import mflix.ListByActorRequest;
import mflix.ListByGenreRequest;
import mflix.Movie;
import mflix.MovieServiceGrpc;
import mflix.UpdateMovieRequest;

/* Clase cliente exibe o menu e chama o servidor via gRPC. */
public class Client {

    private static final int DEADLINE_SECONDS = 10;
    private static Scanner reader = new Scanner(System.in);

    /* Cria o canal gRPC e executa o laço do menu até o usuário escolher sair. */
    public static void main(String[] args) {
        Config config = new Config(".env");

        ManagedChannel channel = ManagedChannelBuilder
                .forAddress(config.getServerHost(), config.getServerPort())
                .usePlaintext()
                .build();
        MovieServiceGrpc.MovieServiceBlockingStub stub = MovieServiceGrpc.newBlockingStub(channel);

        Style.showInfo("Canal gRPC configurado para " + config.getServerHost() + ":" + config.getServerPort());

        try {
            while (true) {
                showMenu();
                String option = ask("Escolha uma opção: ");

                if (option.equals("0")) {
                    Style.showInfo("Encerrando o cliente. Até logo!");
                    break;
                }

                try {
                    execute(option, stub);
                } catch (StatusRuntimeException e) {
                    showRpcError(e);
                }
            }
        } catch (NoSuchElementException e) {
            Style.showInfo("Entrada encerrada.");
        } finally {
            shutdown(channel);
        }
    }

    /* Encerra o canal gRPC, esperando até 3 segundos pelas chamadas em andamento. */
    private static void shutdown(ManagedChannel channel) {
        try {
            channel.shutdown().awaitTermination(3, TimeUnit.SECONDS);
        } catch (InterruptedException e) {
            channel.shutdownNow();
            Thread.currentThread().interrupt();
        }
    }

    /* Devolve um stub com deadline por chamada, para o cliente não ficar bloqueado se o servidor não responder. */
    private static MovieServiceGrpc.MovieServiceBlockingStub withDeadline(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        return stub.withDeadlineAfter(DEADLINE_SECONDS, TimeUnit.SECONDS);
    }

    /* Exibe o menu de opções. */
    private static void showMenu() {
        String rule = Style.BOLD + Style.BLUE + "============================" + Style.RESET;

        Style.printLine("");
        Style.printLine(Style.BOLD + Style.BLUE + "======= MFlix Client =======" + Style.RESET);
        menuItem("1", "Criar filme");
        menuItem("2", "Consultar filme por ID");
        menuItem("3", "Atualizar filme");
        menuItem("4", "Excluir filme");
        menuItem("5", "Listar filmes por ator");
        menuItem("6", "Listar filmes por gênero");
        menuItem("0", "Sair");
        Style.printLine(rule);
    }

    /* Imprime uma opção do menu, com o número destacado. */
    private static void menuItem(String number, String label) {
        Style.printLine(Style.MAGENTA + Style.BOLD + number + "." + Style.RESET + " " + label);
    }

    /* Mostra o rótulo como prompt, lê uma linha do teclado e devolve o texto sem espaços. */
    private static String ask(String label) {
        Style.currentPrompt = Style.prompt(label);
        System.out.print(Style.currentPrompt);
        System.out.flush();
        String line = reader.nextLine().trim();
        Style.currentPrompt = "";
        return line;
    }

    /* Chama o método da operação correspondente à opção escolhida. Falhas de RPC chegam com StatusRuntimeException. */
    private static void execute(String option, MovieServiceGrpc.MovieServiceBlockingStub stub) {
        switch (option) {
            case "1":
                createMovie(stub);
                break;
            case "2":
                getMovie(stub);
                break;
            case "3":
                updateMovie(stub);
                break;
            case "4":
                deleteMovie(stub);
                break;
            case "5":
                listByActor(stub);
                break;
            case "6":
                listByGenre(stub);
                break;
            default:
                Style.showWarning("Opção inválida. Escolha um número de 0 a 6.");
        }
    }

    /* Operações disponíveis: */

    /* Operação de criar filme: lê os dados do filme, chama CreateMovie e exibe o filme criado. */
    private static void createMovie(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        Style.showInfo("Opção selecionada: criar filme.");
        CreateMovieRequest request = CreateMovieRequest.newBuilder().setMovie(readMovie("")).build();
        Movie created = withDeadline(stub).createMovie(request);
        Style.showSuccess("Filme criado com sucesso.");
        showSingleMovie(created);
    }

    /* Oeração de consultar um filme por ID: lê o id, chama GetMovie e exibe o filme encontrado. */
    private static void getMovie(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        Style.showInfo("Opção selecionada: consultar filme por ID.");
        String id = ask("Digite o ID do filme: ");
        Movie movie = withDeadline(stub).getMovie(GetMovieRequest.newBuilder().setId(id).build());
        showSingleMovie(movie);
    }

    /* Opção de atualizar filme: lê o id e os novos dados, chama UpdateMovie e exibe o filme atualizado. */
    private static void updateMovie(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        Style.showInfo("Opção selecionada: atualizar filme.");
        String id = ask("Digite o ID do filme: ");
        UpdateMovieRequest request = UpdateMovieRequest.newBuilder().setMovie(readMovie(id)).build();
        Movie updated = withDeadline(stub).updateMovie(request);
        Style.showSuccess("Filme atualizado com sucesso.");
        showSingleMovie(updated);
    }

    /* Opção de excluir filme: lê o id e chama DeleteMovie. */
    private static void deleteMovie(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        Style.showInfo("Opção selecionada: excluir filme.");
        String id = ask("Digite o ID do filme: ");
        withDeadline(stub).deleteMovie(DeleteMovieRequest.newBuilder().setId(id).build());
        Style.showSuccess("Filme excluído com sucesso.");
    }

    /* Opção de listar filmes por ator: lê o nome do ator, recebe os filmes por streaming e exibe a lista. */
    private static void listByActor(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        Style.showInfo("Opção selecionada: pesquisar por ator.");
        String actor = ask("Digite o nome do ator: ");
        List<Movie> movies = new ArrayList<>();
        withDeadline(stub).listByActor(ListByActorRequest.newBuilder().setActor(actor).build())
                .forEachRemaining(movies::add);
        showMovieList(movies);
    }

    /* Opção de listar filmes por gênero: lê o gênero, recebe os filmes por streaming e exibe a lista. */
    private static void listByGenre(MovieServiceGrpc.MovieServiceBlockingStub stub) {
        Style.showInfo("Opção selecionada: pesquisar por gênero.");
        String genre = ask("Digite o gênero dos filmes: ");
        List<Movie> movies = new ArrayList<>();

        withDeadline(stub).listByGenre(ListByGenreRequest.newBuilder().setGenre(genre).build())
                .forEachRemaining(movies::add);
        showMovieList(movies);
    }

    /* Lê do teclado todos os campos de um filme; os campos de lista são separados por vírgula. */
    private static Movie readMovie(String id) {
        Movie.Builder movie = Movie.newBuilder();
        movie.setId(id);
        movie.setTitle(ask("Título: "));
        movie.setPlot(ask("Sinopse: "));
        movie.setFullplot(ask("Sinopse completa: "));
        movie.setRated(ask("Classificação: "));
        movie.setType(ask("Tipo (ex.: movie): "));
        movie.setReleased(ask("Lançamento: "));
        movie.setRuntime(readInt("Duração (min): "));
        movie.setYear(readInt("Ano: "));
        movie.addAllGenres(splitList(ask("Categorias (separe por vírgula): ")));
        movie.addAllCast(splitList(ask("Elenco (separe por vírgula): ")));
        movie.addAllDirectors(splitList(ask("Diretores (separe por vírgula): ")));
        movie.addAllCountries(splitList(ask("Países (separe por vírgula): ")));
        return movie.build();
    }

    /* Separa o texto por vírgula, remove espaços e ignora itens vazios. */
    private static List<String> splitList(String text) {
        List<String> items = new ArrayList<>();

        for (String part : text.split(",")) {
            String item = part.trim();
            if (!item.isEmpty()) {
                items.add(item);
            }
        }
        return items;
    }

    /* Lê um inteiro do teclado; se vier vazio ou inválido, devolve 0. */
    private static int readInt(String label) {
        try {
            return Integer.parseInt(ask(label));
        } catch (NumberFormatException e) {
            return 0;
        }
    }

    /* Traduz o status de erro do gRPC para uma mensagem amigável. */
    private static void showRpcError(StatusRuntimeException e) {

        Status.Code code = e.getStatus().getCode();
        if (code == Status.Code.UNAVAILABLE) {
            Style.showError("Servidor indisponível.");
        } else if (code == Status.Code.DEADLINE_EXCEEDED) {
            Style.showError("O servidor demorou demais para responder.");
        } else {
            Style.showError(code + ": " + e.getStatus().getDescription());
        }
    }

    /* Exibe a ficha de um único filme, precedida de uma linha em branco. */
    private static void showSingleMovie(Movie movie) {
        Style.printLine("");
        showMovie(movie);
    }

    /* Exibe a quantidade de filmes encontrados, enumerados. */
    private static void showMovieList(List<Movie> movies) {
        int total = movies.size();
        if (total == 0) {
            Style.showWarning("Nenhum filme encontrado.");
            return;
        }

        Style.showSuccess(total + " filme(s) encontrado(s).");
        for (int i = 0; i < total; i++) {
            Style.printLine("");
            Style.printLine(Style.GRAY + "Filme " + (i + 1) + " de " + total + Style.RESET);
            showMovie(movies.get(i));
        }
    }

    /*  Imprime os campos de um filme (ID, título, gêneros, elenco, diretores, duração, classificação e sinopse). */
    private static void showMovie(Movie movie) {
        Style.field("ID", movie.getId());
        Style.field("Título", movie.getTitle() + " (" + movie.getYear() + ")");
        Style.field("Gêneros", String.join(", ", movie.getGenresList()));
        Style.field("Elenco", String.join(", ", movie.getCastList()));
        Style.field("Diretores", String.join(", ", movie.getDirectorsList()));
        Style.field("Duração", movie.getRuntime() + " min");
        Style.field("Classificação", movie.getRated());
        Style.field("Sinopse", movie.getPlot());
    }
}